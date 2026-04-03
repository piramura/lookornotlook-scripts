using Cysharp.Threading.Tasks;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Reaction;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.UI;
using Piramura.LookOrNotLook.Game.State;
using UnityEngine;
using System;            // IDisposable, OperationCanceledException
using System.Threading;  // CancellationTokenSource, CancellationToken

using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// フレームティック処理と非同期収集フローを担当する。
    /// フェーズ遷移シーケンスは GamePhaseController に委譲する。
    /// </summary>
    public sealed class GameLoop : ITickable
    {
        // Injected services（依存）
        private readonly GamePhaseController controller;
        private readonly FocusTracker focusTracker;
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly ITimerService timer;
        private readonly IGameSession session;
        private readonly ISfxService sfx;
        private readonly IOverheatService overheat;
        private readonly BoardSlotManager boardSlotManager;
        private readonly ComboPopupSpawner comboPopup;
        private readonly IGameStateService state;

        // Tick ゲート（同フレーム内での相互排他）
        // GamePhaseController.Start() が TitleScreen を設定するまで Tick を止める
        private bool finished = true;
        private readonly SemaphoreSlim collectLock = new(1, 1);
        private CancellationToken GameToken => session.Token;


        public GameLoop(
            GamePhaseController controller,
            FocusTracker focusTracker,
            ITimerService timer,
            IScoreService score,
            IAchievementService achievement,
            ISfxService sfx,
            IGameSession session,
            IOverheatService overheat,
            BoardSlotManager boardSlotManager,
            ComboPopupSpawner comboPopup,
            IGameStateService state)
        {
            this.controller = controller;
            this.focusTracker = focusTracker;
            this.score = score;
            this.achievement = achievement;
            this.timer = timer;
            this.sfx = sfx;
            this.session = session;
            this.overheat = overheat;
            this.boardSlotManager = boardSlotManager;
            this.comboPopup = comboPopup;
            this.state = state;
        }


        public void Tick()
        {
            if (state.Phase != GamePhase.Playing) return; // ★フェーズゲート
            // 時間切れなら一度だけ終了処理して止める
            if (!finished && timer != null && timer.IsTimeUp)
            {
                finished = true;
                controller.EnterResult();
                return;
            }

            if (finished) return;

            var completed = focusTracker.Tick(UnityEngine.Time.deltaTime);
            if (completed != null) OnItemCompleted(completed).Forget();
        }

        // ---- フェーズ遷移ファサード（外部 API は変わらない）----

        public void StartGameFromTitle()
        {
            finished = true;
            controller.EnterPlaying();
            finished = false;
        }

        public void RetryFromResult()     => StartGameFromTitle();
        public void DebugResetToPlaying() => StartGameFromTitle();
        public void GoTitleFromResult()   => controller.GoTitleFromResult();

        // ---- 非同期収集フロー ----

        //OnItemCompleted 本体（ローカル関数削除＋分割呼び出しに置換）
        private async UniTask OnItemCompleted(GameObject item)
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
                if (!IsCollectStillValid(token, ver)) return;

                // "確定へ進む意思"があるのでインタラクション無効化
                DisableInteractivity(gazeTarget, cols, ref interactivityDisabled);

                // 演出
                await PlayCompletionReactionAsync(item, token);

                // ガード（2回目）
                if (!IsCollectStillValid(token, ver)) return;

                // 確定
                if (!TryCommitCollect(item, out int centerIndex, out ItemDefinition def, out int delta, out bool isPenalty))
                    return;

                // 後処理（スコア/SE/overheat/ポップ）
                PostCommit(def, delta, isPenalty, item.transform.position);

                // ガード（3回目）
                if (!IsCollectStillValid(token, ver)) return;

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

        // リファクタ：Interactivity操作（ローカル関数→クラス private static）
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

        private bool IsCollectStillValid(CancellationToken token, int ver)
        {
            if (finished) return false;
            if (token.IsCancellationRequested) return false;
            if (timer != null && timer.IsTimeUp) return false;
            if (ver != session.Version) return false;
            return true;
        }

        private async UniTask PlayCompletionReactionAsync(GameObject item, CancellationToken token)
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
    }
}
