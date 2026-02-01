using System;
using Cysharp.Threading.Tasks;
using Piramura.LookOrNotLook.Gaze;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Reaction;
using UnityEngine;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// GazeManager(追跡開始/終了) と ItemProgress/ItemReaction を繋ぐ調停役
    /// - OnSeeingStart: ゲージ開始（追跡開始）
    /// - 見続けて ItemProgress 完了: Collect or Penalty → 演出 → Destroy
    /// </summary>
    public sealed class GazeCollectCoordinator : IStartable, ITickable, IDisposable
    {
        private readonly GazeManager gaze;
        private readonly IGameActions actions;

        private GazeTarget focusedTarget;
        private CollectableItem focusedItem;
        private ItemProgress progress;
        private ItemReaction reaction;

        private bool completing;

        public GazeCollectCoordinator(GazeManager gaze, IGameActions actions)
        {
            this.gaze = gaze;
            this.actions = actions;
        }

        public void Start()
        {
            gaze.OnSeeingStart += HandleSeeingStart;
            gaze.OnSeeingEnd += HandleSeeingEnd;
        }

        public void Dispose()
        {
            gaze.OnSeeingStart -= HandleSeeingStart;
            gaze.OnSeeingEnd -= HandleSeeingEnd;
        }

        private void HandleSeeingStart(GazeTarget target)
        {
            if (target == null) return;

            // 新しい追跡が始まったら、古い追跡を終了（OnSeeingEnd待ちにしない）
            EndFocus();

            // 追跡対象からCollectableItemを引く（あなたの現状構成に一致）
            var item = target.GetComponentInParent<CollectableItem>();
            if (item == null || item.Definition == null)
            {
                return;
            }

            focusedTarget = target;
            focusedItem = item;
            progress = item.GetComponent<ItemProgress>();
            reaction = item.GetComponent<ItemReaction>();

            // 安全策：無い場合は追跡しない
            if (progress == null || reaction == null)
            {
                focusedTarget = null;
                focusedItem = null;
                progress = null;
                reaction = null;
                return;
            }

            completing = false;

            // 追跡開始：ゲージ初期化（表示はTick側で Raw一致時だけONにする）
            progress.ResetProgress();
            reaction.SetFocused(false);
            reaction.SetProgress01(0f);
        }

        private void HandleSeeingEnd(GazeTarget target)
        {
            // 演出中は触らない（Destroy待ちなど）
            if (completing) return;

            if (target != null && target == focusedTarget)
            {
                EndFocus();
            }
        }

        public void Tick()
        {
            if (completing) return;
            if (focusedTarget == null || focusedItem == null || progress == null || reaction == null) return;

            // 重要：OnSeeingEndは「hitがnull」のときしか来ないので、
            // Rawが別ターゲットになった瞬間から isSeeing=false として進捗を止める/リセットする
            bool isSeeingNow = (gaze.CurrentRawTarget == focusedTarget);

            progress.Tick(isSeeingNow, Time.deltaTime);

            // UI（見ている間だけ表示）: ItemReactionがshowOnlyWhenFocusedを持つので、ここでスイッチ
            reaction.SetFocused(isSeeingNow);
            reaction.SetProgress01(progress.Progress01);

            if (!progress.IsCompleted) return;

            // 完了した瞬間に処理開始（多重実行防止）
            completing = true;
            CompleteFlowAsync().Forget();
        }

        private async UniTaskVoid CompleteFlowAsync()
        {
            try
            {
                var def = focusedItem != null ? focusedItem.Definition : null;
                if (def == null)
                {
                    EndFocus();
                    completing = false;
                    return;
                }

                // 取得 or ペナルティ（あなたの仕様(d)に対応）
                if (def.IsForbidden) actions.Penalty(def);
                else actions.Collect(def);

                // 演出 → Destroy（あなたのItemReaction実装をそのまま使う）
                if (reaction != null)
                {
                    await reaction.CompleteAsync();
                }
            }
            finally
            {
                // CompleteAsync内でDestroyされるので、参照は全部クリア
                focusedTarget = null;
                focusedItem = null;
                progress = null;
                reaction = null;
                completing = false;
            }
        }

        private void EndFocus()
        {
            if (reaction != null)
            {
                reaction.SetFocused(false);
                reaction.SetProgress01(0f);
            }

            if (progress != null)
            {
                progress.ResetProgress();
            }

            focusedTarget = null;
            focusedItem = null;
            progress = null;
            reaction = null;
            completing = false;
        }
    }
}
