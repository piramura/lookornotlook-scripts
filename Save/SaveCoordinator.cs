using Piramura.LookOrNotLook.Logic;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Save
{
    public sealed class SaveCoordinator : IStartable
    {
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly ISaveService save;

        public SaveCoordinator(IScoreService score, IAchievementService achievement, ISaveService save)
        {
            this.score = score;
            this.achievement = achievement;
            this.save = save;
        }

        public void Start()
        {
            save.Load();

            // 初期保存（タイトルは開始時点も記録）
            save.SaveLastTitle(achievement.CurrentAchievement);
            save.SaveHighScore(score.Score);

            score.Changed += OnScoreChanged;
            achievement.Changed += OnTitleChanged;
        }

        private void OnScoreChanged(int currentScore)
        {
            if (currentScore > save.HighScore)
                save.SaveHighScore(currentScore);
        }

        private void OnTitleChanged(string title)
        {
            save.SaveLastTitle(title);
        }
    }
}
