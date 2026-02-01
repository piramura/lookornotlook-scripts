using System;

namespace Piramura.LookOrNotLook.Game.State
{
    public sealed class GameStateService : IGameStateService
    {
        public GamePhase Phase { get; private set; } = GamePhase.TitleScreen;
        public event Action<GamePhase> Changed;

        public void SetPhase(GamePhase next)
        {
            if (Phase == next) return;
            Phase = next;
            Changed?.Invoke(Phase);
        }
    }
}
