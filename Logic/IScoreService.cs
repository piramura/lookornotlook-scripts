using System;

namespace Piramura.LookOrNotLook.Logic
{
    public interface IScoreService
    {
        int Score { get; }
        event Action<int> Changed;
        void Reset();
        void Add(int delta);
    }
}
