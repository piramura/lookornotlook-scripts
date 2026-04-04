using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Gaze;
using Piramura.LookOrNotLook.Reaction;
using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// GameLoop.Tick() から切り出したフォーカス管理責務。
    /// ポーリングベースで SeeingLogic.ActiveTarget を監視し、
    /// アイテムの進行・完了を制御する。
    /// </summary>
    public sealed class FocusTracker
    {
        private readonly SeeingLogic seeingLogic;
        private readonly IOverheatService overheat;

        private ItemProgress currentProgress;
        private ItemReaction currentReaction;

        public FocusTracker(SeeingLogic seeingLogic, IOverheatService overheat)
        {
            this.seeingLogic = seeingLogic;
            this.overheat = overheat;
        }

        /// <summary>現在フォーカス中スロットの index。なければ -1。</summary>
        public int FocusedSlotIndex =>
            currentProgress != null
                ? currentProgress.GetComponent<ItemSlot>()?.Index ?? -1
                : -1;

        /// <summary>
        /// 1フレーム分のフォーカス処理を実行する。
        /// このフレームで完了したアイテムの GameObject を返す。完了がなければ null。
        /// </summary>
        public GameObject Tick(float dt)
        {
            var target = seeingLogic.ActiveTarget;

            ItemProgress nextProgress = null;
            ItemReaction nextReaction = null;

            if (target != null)
            {
                nextProgress = target.GetComponent<ItemProgress>();
                if (nextProgress != null)
                    nextReaction = target.GetComponent<ItemReaction>();
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

                // Debug.Log はローカル変数を使い、フィールドには保存しない
                if (nextProgress != null)
                {
                    var nextCollectable = target.GetComponent<CollectableItem>();
                    if (nextCollectable != null && nextCollectable.Definition != null)
                        Debug.Log($"[FocusTracker] Seeing Item -> {nextCollectable.Definition.DisplayName}");
                    else
                        Debug.Log("[FocusTracker] Seeing Item -> (none)");
                }
                else
                {
                    Debug.Log("[FocusTracker] Seeing Item -> (none)");
                }

                currentProgress = nextProgress;
                currentReaction = nextReaction;
            }

            if (currentProgress == null) return null;

            float scaledDt = dt * GetDwellSpeedMultiplier(overheat?.Combo ?? 0);
            currentProgress.Tick(seeingLogic.CanProgress, scaledDt);

            if (currentReaction != null)
                currentReaction.SetProgress01(currentProgress.Progress01);

            if (currentProgress.IsCompleted)
            {
                if (currentReaction != null)
                    currentReaction.SetFocused(false);

                var completedGo = currentProgress.gameObject;
                currentProgress = null;
                currentReaction = null;
                return completedGo;
            }

            return null;
        }

        /// <summary>フォーカスをリセットし、visual 状態もクリアする（旧 ClearFocus）。</summary>
        public void Clear()
        {
            if (currentProgress != null) currentProgress.ResetProgress();
            if (currentReaction != null)
            {
                currentReaction.SetProgress01(0f);
                currentReaction.SetFocused(false);
            }
            currentProgress = null;
            currentReaction = null;
        }

        // コンボに応じた dwell 速度倍率を計算する（純C#）
        private static float GetDwellSpeedMultiplier(int combo)
        {
            const float baseDwell = 1.20f;
            const float minDwell  = 0.35f;
            const float step      = 0.08f;

            float dwell = baseDwell - step * combo;
            if (dwell < minDwell) dwell = minDwell;
            return baseDwell / dwell;
        }
    }
}
