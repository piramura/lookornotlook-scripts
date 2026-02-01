namespace Piramura.LookOrNotLook.Save
{
    public interface ISaveService
    {
        int HighScore { get; }
        string LastTitle { get; }

        void Load();
        void SaveHighScore(int value);
        void SaveLastTitle(string title);
        void ClearAll();
    }
}
