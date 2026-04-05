namespace Piramura.LookOrNotLook.Game
{
    public interface IGamePhaseController
    {
        void EnterPlaying();
        void EnterResult();
        void GoTitleFromResult();
    }
}
