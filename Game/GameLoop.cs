using System.Collections.Generic;
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
    /// ゲームの進行処理（Update相当）をEntryPointへ移したもの
    /// </summary>
    public sealed class GameLoop : IStartable, ITickable
    {
        // Injected services（依存）
        private readonly SeeingLogic seeingLogic;
        private readonly ItemSpawner itemSpawner;
        private readonly ItemLayout layout;
        private readonly GameManager config; // itemPool / refreshRadius / spawnAllOnStart を持つ薄い設定役
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly ITimerService timer;
        private readonly IGameSession session;
        private readonly ISfxService sfx;
        private readonly IOverheatService overheat;
        private readonly ItemSelectionPolicy itemSelectionPolicy;
        private readonly ComboPopupSpawner comboPopup;
        private readonly IGameStateService state;
        private readonly IBoardCleaner boardCleaner;
        private readonly BoardPlacerToPlayer boardPlacerToPlayer;

        //Board state（slot/freeIndices/aroundBuffer）
        private GameObject[] slotObjects;
        private readonly List<int> freeIndices = new();
        private readonly List<int> aroundBuffer = new();

        //Focus cache（currentProgress等）
        private ItemProgress currentProgress;
        private CollectableItem currentCollectable;
        private ItemReaction currentReaction;

        //Concurrency / session safety（collectLock / token / finished）
        private bool finished;
        private readonly SemaphoreSlim collectLock = new(1, 1);        
        private CancellationToken GameToken => session.Token;


        public GameLoop(
            SeeingLogic seeingLogic, 
            ItemSpawner itemSpawner, 
            ItemLayout layout, 
            GameManager config, 
            ITimerService timer,
            IScoreService score,
            IAchievementService achievement,
            ISfxService sfx,
            IGameSession session,
            IOverheatService overheat,
            ItemSelectionPolicy itemSelectionPolicy,
            ComboPopupSpawner comboPopup,
            IGameStateService state,
            IBoardCleaner boardCleaner,
            BoardPlacerToPlayer boardPlacerToPlayer
            )
        {
            this.seeingLogic = seeingLogic;
            this.itemSpawner = itemSpawner;
            this.layout = layout;
            this.config = config;
            this.score = score;
            this.achievement = achievement;
            this.timer = timer;
            this.sfx = sfx;
            this.session = session;
            this.overheat = overheat;
            this.itemSelectionPolicy = itemSelectionPolicy;
            this.comboPopup = comboPopup;
            this.state = state;
            this.boardCleaner = boardCleaner;
            this.boardPlacerToPlayer = boardPlacerToPlayer;
        }
        

        public void Start()
        {
            finished = true; // ← 起動直後はプレイしてない扱い
            Debug.Log("[GameLoop] Start");
            overheat?.Reset();
            // 起動直後はタイトルで止める
            state.SetPhase(GamePhase.TitleScreen);

            // タイマー止める（勝手に進むの防止）
            timer.StopAll();
            timer.Reset();

            // 盤面は出さない方針なら消す（保険）
            boardCleaner.ClearAll();

            // 内部配列だけ準備
            RebuildFreeIndices();
            slotObjects = new GameObject[layout.Count];
        }
        private void EnterPlaying()
        {
            finished = true;                 // 途中Tickを止める（保険）
            timer.StopAll();                 // 先に止める
            session.EndSession();            // 旧タスク殺す（ResetGame内でやるならどちらか片方に統一）

            ClearFocus();
            boardCleaner.ClearAll();         // 盤面を消す（方針としてここに統一）

            // 盤とUIをプレイヤー正面に配置（必要なら Place 側で距離/高さを調整）
            boardPlacerToPlayer.PlaceBoardAndUiInFrontOfPlayer();

            ResetGame();                     // 盤面・スコア・新セッション開始・再生成
            state.SetPhase(GamePhase.Playing);

            timer.Reset();
            timer.StartTimer();

            finished = false;
        }

        public void StartGameFromTitle() => EnterPlaying();
        public void RetryFromResult()    => EnterPlaying();
        public void DebugResetToPlaying() => EnterPlaying();


        public void EnterResult()
        {
            finished = true;
            session.EndSession();   // ★これが EndGameSession 相当
            timer.StopAll();
            ClearFocus();
            boardCleaner.ClearAll();
            state.SetPhase(GamePhase.Result);
        }


        public void Tick()
        {
            if (state.Phase != GamePhase.Playing) return; // ★ここがゲート
            // 時間切れなら一度だけ終了処理して止める
            if (!finished && timer != null && timer.IsTimeUp)
            {
                EnterResult();
                return;
            }

            if (finished) return;
            if (seeingLogic == null) return;

            var target = seeingLogic.ActiveTarget;

            ItemProgress nextProgress = null;
            CollectableItem nextCollectable = null;
            ItemReaction nextReaction = null;

            if (target != null)
            {
                nextProgress = target.GetComponent<ItemProgress>();
                if (nextProgress != null)
                {
                    nextCollectable = target.GetComponent<CollectableItem>();
                    nextReaction = target.GetComponent<ItemReaction>();
                }
            }

            // ターゲット変化 → リセット
            if (currentProgress != nextProgress)
            {
                if (currentProgress != null)
                    currentProgress.ResetProgress();

                if (currentReaction != null && currentReaction != nextReaction)
                {
                    currentReaction.SetProgress01(0f);
                    currentReaction.SetFocused(false);
                }

                if (nextReaction != null && currentReaction != nextReaction)
                    nextReaction.SetFocused(true);

                if (nextCollectable != null && nextCollectable.Definition != null)
                    Debug.Log($"[GameLoop] Seeing Item -> {nextCollectable.Definition.DisplayName}");
                else
                    Debug.Log("[GameLoop] Seeing Item -> (none)");

                currentProgress = nextProgress;
                currentCollectable = nextCollectable;
                currentReaction = nextReaction;
            }

            if (currentProgress == null) return;

            // currentProgress.Tick(seeingLogic.CanProgress, UnityEngine.Time.deltaTime);
            float dt = UnityEngine.Time.deltaTime;

            if (overheat != null)
            {
                dt *= GetDwellSpeedMultiplier(overheat.Combo);
            }

            currentProgress.Tick(seeingLogic.CanProgress, dt);


            if (currentReaction != null)
                currentReaction.SetProgress01(currentProgress.Progress01);

            if (currentProgress.IsCompleted)
            {
                if (currentReaction != null)
                    currentReaction.SetFocused(false);

                OnItemCompleted(currentProgress.gameObject).Forget();

                currentProgress = null;
                currentCollectable = null;
                currentReaction = null;
            }
        }
        // 倍率計算メソッド
        private static float GetDwellSpeedMultiplier(int combo)
        {
            // 「体感が出る」ように dwell秒を減らして、その分だけ進みを速くする
            const float baseDwell = 1.20f; // コンボ0のときの必要時間（秒）
            const float minDwell  = 0.35f; // 下限（これ以上速いと制御不能になりやすい）
            const float step      = 0.08f; // コンボ1ごとに減る秒数

            float dwell = baseDwell - step * combo;
            if (dwell < minDwell) dwell = minDwell;

            // 進み速度倍率：dwellが短いほど速くなる
            float speed = baseDwell / dwell; // combo0で1.0、comboが増えると >1
            return speed;
        }

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

                // “確定へ進む意思”があるのでインタラクション無効化
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

                TrySpawnAt(centerIndex);

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
        // リファクタ：ガード/演出/確定/後処理メソッドをprivate関数化
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

            slotObjects[centerIndex] = null;
            if (!freeIndices.Contains(centerIndex)) freeIndices.Add(centerIndex);

            RefreshAround(centerIndex);
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



        private static void DisableColliders(GameObject go)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;
        }
        private void SpawnAll()
        {
            while (freeIndices.Count > 0)
                TrySpawnRandomOne();
        }

        private void RebuildFreeIndices()
        {
            freeIndices.Clear();
            for (int i = 0; i < layout.Count; i++)
                freeIndices.Add(i);
        }

        private bool TrySpawnRandomOne()
        {
            if (freeIndices.Count == 0) return false;
            int pick = UnityEngine.Random.Range(0, freeIndices.Count);
            int index = freeIndices[pick];
            return TrySpawnAt(index);
        }

        private bool TrySpawnAt(int index)
        {
            if (!freeIndices.Contains(index)) return false;
            var def = itemSelectionPolicy.Select(config.ItemPool);
            if (def == null) return false;
            var go = itemSpawner.SpawnAt(index, def.Prefab);

            freeIndices.Remove(index);
            slotObjects[index] = go;

            go.GetComponent<ItemSlot>().SetIndex(index);
            go.GetComponent<CollectableItem>().SetDefinition(def);

            return true;
        }

        private void RefreshAround(int centerIndex)
        {
            //centerIndex を含めない
            layout.GetIndicesAround(centerIndex, config.RefreshRadius, aroundBuffer, includeCenter: false);

            if (currentProgress != null)
            {
                int curIndex = currentProgress.GetComponent<ItemSlot>().Index;
                if (aroundBuffer.Contains(curIndex))
                {
                    if (currentReaction != null) currentReaction.SetFocused(false);
                    currentProgress = null;
                    currentCollectable = null;
                    currentReaction = null;
                }
            }

            // 1) 範囲内を消す
            for (int i = 0; i < aroundBuffer.Count; i++)
            {
                int idx = aroundBuffer[i];

                if (slotObjects[idx] != null)
                {
                    UnityEngine.Object.Destroy(slotObjects[idx]);
                    slotObjects[idx] = null;
                }

                if (!freeIndices.Contains(idx))
                    freeIndices.Add(idx);
            }

            // 2) スポーンし直す
            for (int i = 0; i < aroundBuffer.Count; i++)
            {
                TrySpawnAt(aroundBuffer[i]);
            }
        }
        public void ResetGame()
        {
            overheat?.Reset();
            // 旧プレイを止める（旧タスク殺す）
            finished = true;
            session.EndSession();                 // ★旧セッション中の非同期を殺す
            ClearFocus();

            // ここで新プレイ用セッションを先に作る（以降の非同期は新Token基準）
            session.BeginNewSession();

            // 既存スポーンを全消し
            if (slotObjects != null)
            {
                for (int i = 0; i < slotObjects.Length; i++)
                {
                    if (slotObjects[i] != null)
                    {
                        UnityEngine.Object.Destroy(slotObjects[i]);
                        slotObjects[i] = null;
                    }
                }
            }

            // スロット管理を初期化
            RebuildFreeIndices();

            // スコア/称号/タイマーを初期化
            score.Reset();
            achievement.Reset();
            timer.Reset();

            // 再スポーン
            slotObjects = new GameObject[layout.Count];
            SpawnAll();
            finished = false;
        }

        private void ClearFocus()
        {
            if (currentProgress != null) currentProgress.ResetProgress();
            if (currentReaction != null)
            {
                currentReaction.SetProgress01(0f);
                currentReaction.SetFocused(false);
            }
            currentProgress = null;
            currentCollectable = null;
            currentReaction = null;
        }

        public void GoTitleFromResult()
        {
            boardCleaner.ClearAll();     // タイトルでは盤面を消す方針なら
            state.SetPhase(GamePhase.TitleScreen);

            timer.Reset();
            timer.StopAll();
        }
    }
}
