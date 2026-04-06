using NUnit.Framework;
using Piramura.LookOrNotLook.Game.State;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class GameStateServiceTests
    {
        private GameStateService service;

        [SetUp]
        public void SetUp()
        {
            service = new GameStateService();
        }

        [Test]
        public void InitialPhase_IsTitleScreen()
        {
            Assert.AreEqual(GamePhase.TitleScreen, service.Phase);
        }

        [Test]
        public void SetPhase_ChangesPhase()
        {
            service.SetPhase(GamePhase.Playing);

            Assert.AreEqual(GamePhase.Playing, service.Phase);
        }

        [Test]
        public void SetPhase_SamePhase_DoesNotChangePhase()
        {
            service.SetPhase(GamePhase.Playing);
            service.SetPhase(GamePhase.Playing);

            Assert.AreEqual(GamePhase.Playing, service.Phase);
        }

        [Test]
        public void SetPhase_FiresChanged_WithNewPhase()
        {
            GamePhase received = GamePhase.TitleScreen;
            service.Changed += v => received = v;

            service.SetPhase(GamePhase.Playing);

            Assert.AreEqual(GamePhase.Playing, received);
        }

        [Test]
        public void SetPhase_SamePhase_DoesNotFireChanged()
        {
            bool fired = false;
            service.Changed += _ => fired = true;

            service.SetPhase(GamePhase.TitleScreen);

            Assert.IsFalse(fired);
        }

        [Test]
        public void SetPhase_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => service.SetPhase(GamePhase.Playing));
        }

        [Test]
        public void SetPhase_MultipleChanges_PhaseReflectsLatest()
        {
            service.SetPhase(GamePhase.Playing);
            service.SetPhase(GamePhase.Result);
            service.SetPhase(GamePhase.TitleScreen);

            Assert.AreEqual(GamePhase.TitleScreen, service.Phase);
        }
    }
}
