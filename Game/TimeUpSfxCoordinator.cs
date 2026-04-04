using System;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Session;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    public sealed class TimeUpSfxCoordinator : IStartable, IDisposable
    {
        private readonly ITimerService timer;
        private readonly ISfxService sfx;
        private readonly IGameSession session;
        private readonly IGameStateService state;
        private bool fired;

        public TimeUpSfxCoordinator(ITimerService timer, ISfxService sfx, IGameSession session, IGameStateService state)
        {
            this.timer = timer;
            this.sfx = sfx;
            this.session = session;
            this.state = state;
        }

        public void Start()
        {
            timer.OnTimeUp += OnTimeUp;
            state.Changed += OnPhaseChanged;
        }

        public void Dispose()
        {
            timer.OnTimeUp -= OnTimeUp;
            state.Changed -= OnPhaseChanged;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing) fired = false;
        }

        private void OnTimeUp()
        {
            if (fired) return;
            fired = true;
            sfx.StopAll();        // 残っているOneShotを止める
            sfx.PlayTimeUp();     // session.IsAlive のうちに鳴らす
            session.EndSession(); // セッションを閉じる
        }
    }
}
