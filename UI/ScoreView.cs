using TMPro;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class ScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private string format = "Score: {0}";

        private void Awake()
        {
            if (scoreText == null)
                scoreText = GetComponentInChildren<TMP_Text>(true);
        }

        public void SetScore(int score)
        {
            if (scoreText == null) return;
            scoreText.text = string.Format(format, score);
        }
    }
}
