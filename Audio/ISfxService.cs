namespace Piramura.LookOrNotLook.Audio
{
    public interface ISfxService
    {
        void PlayCollect();
        void PlayPenalty();
        void PlayReset();
        void PlayTimeUp();
        void PlayResult();

        void StopAll(); // ★追加
    }
}
