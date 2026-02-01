using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class ComboPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private float rise = 0.25f;

        public async UniTask Play(int combo, CancellationToken token)
        {
            try
            {
                if (text != null) text.text = $"x{combo}";

                var start = transform.position;
                var end = start + Vector3.up * rise;

                float t = 0f;
                while (t < lifetime)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Clamp01(t / lifetime);

                    transform.position = Vector3.Lerp(start, end, a);

                    if (text != null)
                    {
                        var c = text.color;
                        c.a = 1f - a;
                        text.color = c;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルは正常系扱い（ログ不要）
            }
            finally
            {
                // ★ここが本体：どんな抜け方でも残骸を残さない
                if (this != null) Destroy(gameObject);
            }
        }
    }
}
