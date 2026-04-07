using System;
using System.Collections.Generic;
using NUnit.Framework;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Save;

namespace Piramura.LookOrNotLook.Tests.Save
{
    public class SaveCoordinatorTests
    {
        private FakeScoreService score;
        private FakeAchievementService achievement;
        private FakeSaveService save;
        private SaveCoordinator coordinator;

        [SetUp]
        public void SetUp()
        {
            score = new FakeScoreService();
            achievement = new FakeAchievementService();
            save = new FakeSaveService();
            coordinator = new SaveCoordinator(score, achievement, save);
        }

        // ── Start() 初期化順序と初期保存 ──────────────────────────────

        [Test]
        public void Start_CallsLoad_BeforeAnySave()
        {
            coordinator.Start();

            Assert.AreEqual(
                new List<string> { "Load", "SaveLastTitle", "SaveHighScore" },
                save.CallLog);
        }

        [Test]
        public void Start_SavesInitialAchievementTitle()
        {
            achievement.CurrentAchievement = "Pro";

            coordinator.Start();

            Assert.AreEqual("Pro", save.SaveLastTitleLastValue);
        }

        [Test]
        public void Start_SavesInitialScore()
        {
            score.Score = 42;

            coordinator.Start();

            Assert.AreEqual(42, save.SaveHighScoreLastValue);
        }

        // ── OnScoreChanged ────────────────────────────────────────────

        [Test]
        public void OnScoreChanged_WhenHigherThanHighScore_Saves()
        {
            save.HighScore = 80;
            coordinator.Start();
            int callCountAfterStart = save.SaveHighScoreCallCount;

            score.RaiseChanged(100);

            Assert.AreEqual(callCountAfterStart + 1, save.SaveHighScoreCallCount);
            Assert.AreEqual(100, save.SaveHighScoreLastValue);
        }

        [Test]
        public void OnScoreChanged_WhenEqualToHighScore_DoesNotSave()
        {
            save.HighScore = 80;
            coordinator.Start();
            int callCountAfterStart = save.SaveHighScoreCallCount;

            score.RaiseChanged(80);

            Assert.AreEqual(callCountAfterStart, save.SaveHighScoreCallCount);
        }

        [Test]
        public void OnScoreChanged_WhenLowerThanHighScore_DoesNotSave()
        {
            save.HighScore = 80;
            coordinator.Start();
            int callCountAfterStart = save.SaveHighScoreCallCount;

            score.RaiseChanged(60);

            Assert.AreEqual(callCountAfterStart, save.SaveHighScoreCallCount);
        }

        // ── OnTitleChanged ────────────────────────────────────────────

        [Test]
        public void OnTitleChanged_AlwaysSavesTitle()
        {
            coordinator.Start();
            int callCountAfterStart = save.SaveLastTitleCallCount;

            achievement.RaiseChanged("Master");

            Assert.AreEqual(callCountAfterStart + 1, save.SaveLastTitleCallCount);
            Assert.AreEqual("Master", save.SaveLastTitleLastValue);
        }

        // ── Fakes ─────────────────────────────────────────────────────

        private class FakeScoreService : IScoreService
        {
            public int Score { get; set; }
            public event Action<int> Changed;
            public void RaiseChanged(int value) => Changed?.Invoke(value);
            public void Reset() { }
            public void Add(int delta) { }
        }

        private class FakeAchievementService : IAchievementService
        {
            public string CurrentAchievement { get; set; } = "Beginner";
            public event Action<string> Changed;
            public void RaiseChanged(string title) => Changed?.Invoke(title);
            public void Reset() { }
            public void OnCollect(ItemDefinition def, int scoreDelta) { }
        }

        private class FakeSaveService : ISaveService
        {
            public int HighScore { get; set; }
            public string LastTitle { get; set; } = "Beginner";

            public List<string> CallLog { get; } = new List<string>();

            public int SaveHighScoreCallCount { get; private set; }
            public int SaveHighScoreLastValue { get; private set; }
            public int SaveLastTitleCallCount { get; private set; }
            public string SaveLastTitleLastValue { get; private set; }

            public void Load()
            {
                CallLog.Add("Load");
            }

            public void SaveHighScore(int value)
            {
                SaveHighScoreLastValue = value;
                SaveHighScoreCallCount++;
                CallLog.Add("SaveHighScore");
            }

            public void SaveLastTitle(string title)
            {
                SaveLastTitleLastValue = title;
                SaveLastTitleCallCount++;
                CallLog.Add("SaveLastTitle");
            }

            public void ClearAll() { }
        }
    }
}
