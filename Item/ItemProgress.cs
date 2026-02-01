using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    /// <summary>
    /// Itemごとの取得進捗を管理するコンポーネント
    /// </summary>
    public class ItemProgress : MonoBehaviour
    {
        [Header("Progress Settings")]
        [SerializeField] private float requiredTime = 2.0f;

        private float currentTime = 0f;

        /// <summary>
        /// 進捗率（0.0〜1.0）
        /// </summary>
        public float Progress01
        {
            get
            {
                if (requiredTime <= 0f) return 1f;
                return Mathf.Clamp01(currentTime / requiredTime);
            }
        }

        /// <summary>
        /// 取得完了しているか
        /// </summary>
        public bool IsCompleted => currentTime >= requiredTime;

        /// <summary>
        /// 見られているかどうかを受け取って進捗を更新
        /// </summary>
        public void Tick(bool isSeeing, float deltaTime)
        {
            if (isSeeing)
            {
                currentTime += deltaTime;
            }
            else
            {
                // 仕様：視線が外れたらリセット
                currentTime = 0f;
            }
        }
        public void SetRequiredTime(float seconds)
        {
            requiredTime = Mathf.Max(0.05f, seconds);
            currentTime = 0f;
        }


        /// <summary>
        /// 外部からリセットしたい場合用（保険）
        /// </summary>
        public void ResetProgress()
        {
            currentTime = 0f;
        }
    }
}
