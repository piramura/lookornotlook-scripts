using Piramura.LookOrNotLook.Game.Timer;
using UnityEngine;
using VContainer;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class TimePresenter : MonoBehaviour
    {
        [SerializeField] private TimeView view;

        private ITimerService timer;

        [Inject]
        public void Construct(ITimerService timer)
        {
            this.timer = timer;
        }

        private void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<TimeView>(true);
        }

        private void OnEnable()
        {
            if (timer == null) return;
            timer.OnRemainingChanged += OnChanged;
            OnChanged(timer.RemainingSeconds); // 初期表示
        }

        private void OnDisable()
        {
            if (timer == null) return;
            timer.OnRemainingChanged -= OnChanged;
        }

        private void OnChanged(float remaining)
        {
            int sec = Mathf.CeilToInt(remaining);
            int m = sec / 60;
            int s = sec % 60;
            view?.SetText($"{m:0}:{s:00}");
        }
    }
}
