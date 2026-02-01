using System;
using UnityEngine;
using Piramura.LookOrNotLook.UI;
using Piramura.Common;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Piramura.LookOrNotLook.Reaction
{
    public class ItemReaction : AsyncMB
    {
        [Header("UI")]
        [SerializeField] private ItemProgressBar progressBar;

        [Tooltip("見ている間だけバーを表示する")]
        [SerializeField] private bool showOnlyWhenFocused = true;

        private bool focused;

        protected override void Awake()
        {
            base.Awake();
            if (progressBar == null)
                progressBar = GetComponentInChildren<ItemProgressBar>(true);

            progressBar.SetVisible(false);
        }

        public void SetFocused(bool value)
        {
            focused = value;
            progressBar.SetVisible(focused);

            if (showOnlyWhenFocused)
            {
                progressBar.SetVisible(focused);

                if (!focused)
                    progressBar.ResetBar();
            }
        }

        public void SetProgress01(float value)
        {
            progressBar.SetProgress01(value);
        }
        public async UniTask CompleteAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();
            await PlayPopAnimation(token);
            if (this == null) return;
            Destroy(gameObject);
        }
        private async UniTask PlayPopAnimation(CancellationToken token)
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;

            try
            {
                while (t < 1f)
                {
                    token.ThrowIfCancellationRequested();
                    t += Time.deltaTime * 8f;

                    float s = Mathf.Lerp(1f, 1.3f, t);
                    transform.localScale = startScale * s;

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Destroy / 再生停止時は何もしない
            }
        }




    }
}
