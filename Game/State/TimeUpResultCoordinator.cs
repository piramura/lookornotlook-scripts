using System;
using Piramura.LookOrNotLook.Game.Timer;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game.State
{
    /// <summary>
    /// TimeUpを受けて Result へ遷移させる（UI切替のトリガ）
    /// </summary>
    public sealed class TimeUpResultCoordinator : IStartable, IDisposable
    {
        private readonly ITimerService timer;
        private readonly IGameStateService state;

        public TimeUpResultCoordinator(ITimerService timer, IGameStateService state)
        {
            this.timer = timer;
            this.state = state;
        }

        public void Start()
        {
            timer.OnTimeUp += HandleTimeUp;
        }

        private void HandleTimeUp()
        {
            state.SetPhase(GamePhase.Result);
        }

        public void Dispose()
        {
            timer.OnTimeUp -= HandleTimeUp;
        }
    }
}
