using Piramura.LookOrNotLook.Game.State;

namespace Piramura.LookOrNotLook.Game
{
    public interface IResultFlow
    {
        void Retry();
        void GoTitle();
        void ToggleAchievementOverlay();
    }

    public sealed class ResultFlow : IResultFlow
    {
        private readonly GameLoop loop;

        public ResultFlow(GameLoop loop)
        {
            this.loop = loop;
        }

        public void Retry() => loop.RetryFromResult();
        public void GoTitle() => loop.GoTitleFromResult();
        public void ToggleAchievementOverlay()
        {
            // TODO
        }
    }
}
