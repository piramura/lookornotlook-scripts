using System;
using System.Threading;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Session;

namespace Piramura.LookOrNotLook.Game
{
    internal static class CollectGuard
    {
        internal static bool IsValid(
            CancellationToken token,
            int ver,
            Func<bool> isFinished,
            ITimerService timer,
            IGameSession session)
        {
            if (isFinished()) return false;
            if (token.IsCancellationRequested) return false;
            if (timer != null && timer.IsTimeUp) return false;
            if (ver != session.Version) return false;
            return true;
        }
    }
}
