using TMPro;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI.Achievement
{
    public sealed class AchievementView : MonoBehaviour
    {
        [SerializeField] private TMP_Text achievementText;
        [SerializeField] private string format = "Achievement: {0}";

        private void Awake()
        {
            if (achievementText == null)
                achievementText = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>(true);
        }

        public void SetAchievement(string achievement)
        {
            if (achievementText == null) return;
            achievementText.text = string.Format(format, achievement);
        }
    }
}
