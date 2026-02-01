using System;

namespace Piramura.LookOrNotLook.Game.Timer
{
    public interface ITimerService
    {
        float DurationSeconds { get; }
        float RemainingSeconds { get; }
        bool IsTimeUp { get; }

        event Action<float> OnRemainingChanged;
        event Action OnTimeUp;

        void Reset();
        void StartTimer();
        void StopAll();
    }
}
