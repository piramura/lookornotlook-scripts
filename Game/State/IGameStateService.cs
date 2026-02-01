using System;

namespace Piramura.LookOrNotLook.Game.State
{
    public interface IGameStateService
    {
        GamePhase Phase { get; }
        event Action<GamePhase> Changed;

        void SetPhase(GamePhase next);
    }
}
