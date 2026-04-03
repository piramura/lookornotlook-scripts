using NUnit.Framework;
using Piramura.LookOrNotLook.Logic;

namespace Piramura.LookOrNotLook.Tests.Logic
{
    public class ScoreServiceTests
    {
        private ScoreService service;

        [SetUp]
        public void SetUp()
        {
            service = new ScoreService();
        }

        [Test]
        public void Add_PositiveDelta_IncreasesScore()
        {
            service.Add(10);
            Assert.AreEqual(10, service.Score);
        }

        [Test]
        public void Add_NegativeDelta_DecreasesScore()
        {
            service.Add(10);
            service.Add(-5);
            Assert.AreEqual(5, service.Score);
        }

        [Test]
        public void Reset_SetsScoreToZero()
        {
            service.Add(20);
            service.Reset();
            Assert.AreEqual(0, service.Score);
        }

        [Test]
        public void Add_FiresChangedEvent()
        {
            int received = -1;
            service.Changed += v => received = v;
            service.Add(10);
            Assert.AreEqual(10, received);
        }

        [Test]
        public void Reset_FiresChangedEvent()
        {
            service.Add(10);
            int received = -1;
            service.Changed += v => received = v;
            service.Reset();
            Assert.AreEqual(0, received);
        }
    }
}
