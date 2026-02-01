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
        private readonly SeeingLogic seeingLogic;
        private readonly ItemSpawner itemSpawner;
        private readonly ItemLayout layout;
        private readonly GameManager config; // itemPool / refreshRadius / spawnAllOnStart を持つ薄い設定役

        private readonly List<int> freeIndices = new();
        private readonly List<int> aroundBuffer = new();

        private GameObject[] slotObjects;

        private ItemProgress currentProgress;
        private CollectableItem currentCollectable;
        private ItemReaction currentReaction;
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly ITimerService timer;
        private bool finished;
        private readonly ISfxService sfx;
        private readonly SemaphoreSlim collectLock = new(1, 1);
        private readonly IGameSession session;
        private CancellationToken GameToken => session.Token;
        private readonly IOverheatService overheat;
        private readonly ComboPopupSpawner comboPopup;
        private readonly IGameStateService state;
        private readonly IBoardCleaner boardCleaner;
        private readonly BoardPlacerToPlayer boardPlacerToPlayer;
        private readonly Transform boardRoot;
        private readonly Transform uiRoot;


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

            // 盤：遠め
            boardPlacerToPlayer.PlaceBoardAndUiInFrontOfPlayer();

            // UI：近め（プレイヤーの胸〜顔の前）
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


        private async UniTask OnItemCompleted(GameObject item)
        {
            var token = GameToken;
            var ver = session.Version;

            if (item == null) return;

            // 参照は先に取る（Destroyされてもnullに近い挙動になる）
            var gazeTarget = item.GetComponent<Piramura.LookOrNotLook.Gaze.GazeTarget>();
            var cols = item.GetComponentsInChildren<Collider>(true);

            bool lockAcquired = false;
            bool committed = false;
            bool interactivityDisabled = false;

            void DisableInteractivity()
            {
                if (interactivityDisabled) return;
                if (gazeTarget != null) gazeTarget.enabled = false;
                for (int i = 0; i < cols.Length; i++) if (cols[i] != null) cols[i].enabled = false;
                interactivityDisabled = true;
            }

            void RestoreInteractivity()
            {
                if (!interactivityDisabled) return;
                if (gazeTarget != null) gazeTarget.enabled = true;
                for (int i = 0; i < cols.Length; i++) if (cols[i] != null) cols[i].enabled = true;
                interactivityDisabled = false;
            }

            try
            {
                // 直列化
                await collectLock.WaitAsync(token);
                lockAcquired = true;

                // ここでまとめてガード（仕様：時間切れは無効）
                if (finished || token.IsCancellationRequested || timer.IsTimeUp) return;
                if (ver != session.Version) return;

                // ここから “確定へ進む意思” があるので、インタラクション無効化
                DisableInteractivity();

                // 演出
                var reaction = item.GetComponent<ItemReaction>();
                if (reaction != null)
                {
                    await reaction.CompleteAsync().AttachExternalCancellation(token);
                }

                // 演出後、確定前にもう一度ガード
                if (finished || token.IsCancellationRequested || timer.IsTimeUp) return;
                if (ver != session.Version) return;

                // --- ここから確定 ---
                var slot = item.GetComponent<ItemSlot>();
                if (slot == null) return;

                int centerIndex = slot.Index;

                bool isPenalty = false;
                int delta = 0;
                ItemDefinition def = null;

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

                if (def != null)
                {
                    score.Add(delta);
                    achievement.OnCollect(def, delta);
                }

                // SE
                if (isPenalty) sfx.PlayPenalty();
                else sfx.PlayCollect();

                // Overheat更新（スコア確定後、再スポーン前）
                overheat?.OnCollect(isPenalty);
                Debug.Log($"[Overheat] combo={overheat.Combo} p={overheat.ForbiddenChance01:P0}");
                if (!isPenalty && comboPopup != null && overheat != null)
                {
                    comboPopup.Show(item.transform.position, overheat.Combo, GameToken);
                }


                if (finished || token.IsCancellationRequested || timer.IsTimeUp) return;

                TrySpawnAt(centerIndex);

                committed = true;
            }
            catch (OperationCanceledException)
            {
                // Reset/TimeUpでキャンセル：何もしない
            }
            catch (Exception ex)
            {
                // ここ重要：OperationCanceled以外で落ちると “拾えない残骸” を作りやすいのでログ
                UnityEngine.Debug.LogException(ex);
            }
            finally
            {
                // 確定できなかった場合は “触れる状態” に戻す（残骸対策）
                if (!committed)
                {
                    RestoreInteractivity();
                }

                if (lockAcquired)
                {
                    collectLock.Release(); // ★CurrentCount判定やめる
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
            if (config.ItemPool == null || config.ItemPool.Length == 0) return false;

            var def = config.ItemPool[UnityEngine.Random.Range(0, config.ItemPool.Length)];
            // ★Overheat: comboに応じてForbiddenを強制する
            float p = overheat != null ? overheat.ForbiddenChance01 : 0f;
            bool forceForbidden = UnityEngine.Random.value < p;
            if (forceForbidden)
            {
                // Forbidden定義だけ抽出してそこから選ぶ
                ItemDefinition picked = null;
                int count = 0;

                for (int i = 0; i < config.ItemPool.Length; i++)
                {
                    var d = config.ItemPool[i];
                    if (d != null && d.IsForbidden)
                    {
                        count++;
                        // reservoir samplingで1パス選択（軽い）
                        // QiitaにあったからAIに書かせたやつ。過剰実装だからもどそう。
                        // https://qiita.com/otoiku/items/ab994263f082675a806b
                        if (UnityEngine.Random.Range(0, count) == 0)
                            picked = d;
                    }
                }

                // Forbiddenが一個も無かったら通常へフォールバック
                def = picked;
            }

            if (def == null)
            {
                def = config.ItemPool[UnityEngine.Random.Range(0, config.ItemPool.Length)];
            }
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
