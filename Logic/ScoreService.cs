using System;

namespace Piramura.LookOrNotLook.Logic
{
    public sealed class ScoreService : IScoreService
    {
        public int Score { get; private set; }
        public event Action<int> Changed;

        public void Reset()
        {
            Score = 0;
            Changed?.Invoke(Score);
        }

        public void Add(int delta)
        {
            Score += delta;
            Changed?.Invoke(Score);
        }
    }
}
