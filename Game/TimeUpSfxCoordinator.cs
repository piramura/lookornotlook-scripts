using System;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.Game.Timer;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    public sealed class TimeUpSfxCoordinator : IStartable, IDisposable
    {
        private readonly ITimerService timer;
        private readonly ISfxService sfx;
        private readonly IGameSession session;
        private bool fired;

        public TimeUpSfxCoordinator(ITimerService timer, ISfxService sfx, IGameSession session)
        {
            this.timer = timer;
            this.sfx = sfx;
            this.session = session;
            timer.OnTimeUp += OnTimeUp;
        }

        public void Start()
        {
            timer.OnTimeUp += OnTimeUp;
        }

        public void Dispose()
        {
            timer.OnTimeUp -= OnTimeUp;
        }

        private void OnTimeUp()
        {
            if(fired) return;
            fired = true;
            session.EndSession(); // 先に止める
            sfx.StopAll();        // 残ってるOneShotを止める
            sfx.PlayTimeUp();     // TimeUpだけ鳴らす
        }
    }
}
