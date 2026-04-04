using Cysharp.Threading.Tasks;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Reaction;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Session;
using Piramura.LookOrNotLook.UI;
using UnityEngine;
using System;
using System.Threading;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// アイテム収集の非同期フロー（ロック・ガード・演出・確定・後処理）を担当する。
    /// finished フラグは呼び出し元（GameLoop）が Func&lt;bool&gt; として渡す。
    /// </summary>
    public sealed class ItemCollectFlow
    {
        private readonly FocusTracker focusTracker;
        private readonly ITimerService timer;
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly ISfxService sfx;
        private readonly IGameSession session;
        private readonly IOverheatService overheat;
        private readonly BoardSlotManager boardSlotManager;
        private readonly ComboPopupSpawner comboPopup;

        private readonly SemaphoreSlim collectLock = new(1, 1);
        private CancellationToken GameToken => session.Token;

        public ItemCollectFlow(
            FocusTracker focusTracker,
            ITimerService timer,
            IScoreService score,
            IAchievementService achievement,
            ISfxService sfx,
            IGameSession session,
            IOverheatService overheat,
            BoardSlotManager boardSlotManager,
            ComboPopupSpawner comboPopup)
        {
            this.focusTracker = focusTracker;
            this.timer = timer;
            this.score = score;
            this.achievement = achievement;
            this.sfx = sfx;
            this.session = session;
            this.overheat = overheat;
            this.boardSlotManager = boardSlotManager;
            this.comboPopup = comboPopup;
        }

        /// <summary>
        /// アイテム収集フローを実行する。
        /// isFinished: GameLoop の finished フラグを呼び出し側から渡す（循環依存回避）。
        /// </summary>
        public async UniTask ExecuteAsync(GameObject item, Func<bool> isFinished)
        {
            var token = GameToken;
            var ver = session.Version;

            if (item == null) return;

            var gazeTarget = item.GetComponent<Piramura.LookOrNotLook.Gaze.GazeTarget>();
            var cols = item.GetComponentsInChildren<Collider>(true);

            bool lockAcquired = false;
            bool committed = false;
            bool interactivityDisabled = false;

            try
            {
                await collectLock.WaitAsync(token);
                lockAcquired = true;

                // ガード（1回目）
                if (!IsCollectStillValid(token, ver, isFinished)) return;

                // "確定へ進む意思"があるのでインタラクション無効化
                DisableInteractivity(gazeTarget, cols, ref interactivityDisabled);

                // 演出
                await PlayCompletionReactionAsync(item, token);

                // ガード（2回目）
                if (!IsCollectStillValid(token, ver, isFinished)) return;

                // 確定
                if (!TryCommitCollect(item, out int centerIndex, out ItemDefinition def, out int delta, out bool isPenalty))
                    return;

                // 後処理（スコア/SE/overheat/ポップ）
                PostCommit(def, delta, isPenalty, item.transform.position);

                // ガード（3回目）
                if (!IsCollectStillValid(token, ver, isFinished)) return;

                boardSlotManager.SpawnAt(centerIndex);

                committed = true;
            }
            catch (OperationCanceledException)
            {
                // Reset/TimeUpでキャンセル：何もしない
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            finally
            {
                if (!committed)
                {
                    RestoreInteractivity(gazeTarget, cols, ref interactivityDisabled);
                }

                if (lockAcquired)
                {
                    collectLock.Release();
                }
            }
        }

        private bool IsCollectStillValid(CancellationToken token, int ver, Func<bool> isFinished)
        {
            if (isFinished()) return false;
            if (token.IsCancellationRequested) return false;
            if (timer != null && timer.IsTimeUp) return false;
            if (ver != session.Version) return false;
            return true;
        }

        private static async UniTask PlayCompletionReactionAsync(GameObject item, CancellationToken token)
        {
            var reaction = item.GetComponent<ItemReaction>();
            if (reaction == null) return;

            await reaction.CompleteAsync().AttachExternalCancellation(token);
        }

        private bool TryCommitCollect(
            GameObject item,
            out int centerIndex,
            out ItemDefinition def,
            out int delta,
            out bool isPenalty)
        {
            centerIndex = -1;
            def = null;
            delta = 0;
            isPenalty = false;

            var slot = item.GetComponent<ItemSlot>();
            if (slot == null) return false;

            centerIndex = slot.Index;

            var collectable = item.GetComponent<CollectableItem>();
            if (collectable != null && collectable.Definition != null)
            {
                def = collectable.Definition;
                int gain = def.Value;
                int penalty = def.PenaltyValue > 0 ? def.PenaltyValue : def.Value;
                delta = def.IsForbidden ? -penalty : gain;
                isPenalty = def.IsForbidden;
            }

            int focusedIndex = focusTracker.FocusedSlotIndex;

            boardSlotManager.FreeSlot(centerIndex);
            boardSlotManager.RefreshAround(
                centerIndex,
                focusedSlotIndex: focusedIndex,
                onFocusHit: focusedIndex >= 0 ? focusTracker.Clear : null
            );
            return true;
        }

        private void PostCommit(ItemDefinition def, int delta, bool isPenalty, Vector3 pos)
        {
            if (def != null)
            {
                score.Add(delta);
                achievement.OnCollect(def, delta);
            }

            if (isPenalty) sfx.PlayPenalty();
            else sfx.PlayCollect();

            if (overheat != null)
            {
                overheat.OnCollect(isPenalty);
                Debug.Log($"[Overheat] combo={overheat.Combo} p={overheat.ForbiddenChance01:P0}");

                if (!isPenalty && comboPopup != null)
                {
                    comboPopup.Show(pos, overheat.Combo, GameToken);
                }
            }
        }

        private static void DisableInteractivity(
            Piramura.LookOrNotLook.Gaze.GazeTarget gazeTarget,
            Collider[] cols,
            ref bool interactivityDisabled)
        {
            if (interactivityDisabled) return;
            if (gazeTarget != null) gazeTarget.enabled = false;

            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null) cols[i].enabled = false;
            }

            interactivityDisabled = true;
        }

        private static void RestoreInteractivity(
            Piramura.LookOrNotLook.Gaze.GazeTarget gazeTarget,
            Collider[] cols,
            ref bool interactivityDisabled)
        {
            if (!interactivityDisabled) return;
            if (gazeTarget != null) gazeTarget.enabled = true;

            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null) cols[i].enabled = true;
            }

            interactivityDisabled = false;
        }
    }
}
