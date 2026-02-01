using Piramura.LookOrNotLook.Logic;
using UnityEngine;
using VContainer;

namespace Piramura.LookOrNotLook.UI.Achievement
{
    public sealed class AchievementPresenter : MonoBehaviour
    {
        [SerializeField] private AchievementView view;

        private IAchievementService achievement;

        [Inject]
        public void Construct(IAchievementService achievement)
        {
            this.achievement = achievement;
        }

        private void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<AchievementView>(true);
        }

        private void OnEnable()
        {
            if (achievement == null) return;
            achievement.Changed += OnChanged;
            OnChanged(achievement.CurrentAchievement);
        }

        private void OnDisable()
        {
            if (achievement == null) return;
            achievement.Changed -= OnChanged;
        }

        private void OnChanged(string Achievement)
        {
            if (view == null) return;
            view.SetAchievement(Achievement);
        }
    }
}
