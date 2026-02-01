using UnityEngine;

namespace Piramura.LookOrNotLook.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        private const string KeyHighScore = "LNL_HighScore";
        private const string KeyLastTitle = "LNL_LastTitle";

        public int HighScore { get; private set; }
        public string LastTitle { get; private set; } = "Beginner";

        public void Load()
        {
            HighScore = PlayerPrefs.GetInt(KeyHighScore, 0);
            LastTitle = PlayerPrefs.GetString(KeyLastTitle, "Beginner");
        }

        public void SaveHighScore(int value)
        {
            HighScore = Mathf.Max(HighScore, value);
            PlayerPrefs.SetInt(KeyHighScore, HighScore);
            PlayerPrefs.Save();
        }

        public void SaveLastTitle(string title)
        {
            LastTitle = string.IsNullOrEmpty(title) ? "Beginner" : title;
            PlayerPrefs.SetString(KeyLastTitle, LastTitle);
            PlayerPrefs.Save();
        }

        public void ClearAll()
        {
            PlayerPrefs.DeleteKey(KeyHighScore);
            PlayerPrefs.DeleteKey(KeyLastTitle);
            PlayerPrefs.Save();
            Load();
        }
    }
}
