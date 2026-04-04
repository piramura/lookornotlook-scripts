using System;
using System.Threading;
using NUnit.Framework;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Session;
using Piramura.LookOrNotLook.UI;
using UnityEngine;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class ItemCollectFlowTests
    {
        private sealed class StubScore : IScoreService
        {
            public int Score { get; private set; }
            public event Action<int> Changed { add { } remove { } }
            public void Reset() => Score = 0;
            public void Add(int delta) => Score += delta;
        }

        private sealed class StubAchievement : IAchievementService
        {
            public string CurrentAchievement => "";
            public event Action<string> Changed { add { } remove { } }
            public int CallCount { get; private set; }
            public ItemDefinition LastDef { get; private set; }
            public int LastDelta { get; private set; }
            public void Reset() { }
            public void OnCollect(ItemDefinition def, int scoreDelta)
            {
                CallCount++;
                LastDef = def;
                LastDelta = scoreDelta;
            }
        }

        private sealed class StubSfx : ISfxService
        {
            public int CollectCount { get; private set; }
            public int PenaltyCount { get; private set; }
            public void PlayCollect() => CollectCount++;
            public void PlayPenalty() => PenaltyCount++;
            public void PlayReset() { }
            public void PlayTimeUp() { }
            public void PlayResult() { }
            public void StopAll() { }
        }

        private sealed class StubOverheat : IOverheatService
        {
            public int Combo { get; set; } = 3;
            public float ForbiddenChance01 => 0f;
            public bool OnCollectCalled { get; private set; }
            public event Action<int, float> Changed { add { } remove { } }
            public void Reset() { }
            public void OnCollect(bool isForbidden) => OnCollectCalled = true;
        }

        private sealed class StubComboPopupSpawner : IComboPopupSpawner
        {
            public int ShowCallCount { get; private set; }
            public int LastCombo { get; private set; }
            public CancellationToken LastToken { get; private set; }

            public void Show(Vector3 worldPos, int combo, CancellationToken token)
            {
                ShowCallCount++;
                LastCombo = combo;
                LastToken = token;
            }
        }

        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            session = new GameSession();
            session.BeginNewSession();
        }

        [TearDown]
        public void TearDown()
        {
            session.Dispose();
        }

        private ItemCollectFlow MakeFlow(
            IScoreService score = null,
            IAchievementService achievement = null,
            ISfxService sfx = null,
            IOverheatService overheat = null,
            IComboPopupSpawner comboPopup = null)
        {
            return new ItemCollectFlow(
                focusTracker: null,
                timer: null,
                score: score ?? new StubScore(),
                achievement: achievement ?? new StubAchievement(),
                sfx: sfx ?? new StubSfx(),
                session: session,
                overheat: overheat,
                boardSlotManager: null,
                comboPopup: comboPopup);
        }

        // --- score ---

        [Test]
        public void PostCommit_ScoreAdded_WhenDefNotNull()
        {
            var stubScore = new StubScore();
            var flow = MakeFlow(score: stubScore);
            var def = ScriptableObject.CreateInstance<ItemDefinition>();

            flow.PostCommit(def, 10, false, Vector3.zero);

            Assert.AreEqual(10, stubScore.Score);
        }

        [Test]
        public void PostCommit_ScoreNotChanged_WhenDefNull()
        {
            var stubScore = new StubScore();
            var flow = MakeFlow(score: stubScore);

            flow.PostCommit(null, 10, false, Vector3.zero);

            Assert.AreEqual(0, stubScore.Score);
        }

        [Test]
        public void PostCommit_AchievementOnCollect_CalledWithDefAndDelta()
        {
            var stubAchievement = new StubAchievement();
            var flow = MakeFlow(achievement: stubAchievement);
            var def = ScriptableObject.CreateInstance<ItemDefinition>();

            flow.PostCommit(def, 10, false, Vector3.zero);

            Assert.AreEqual(1, stubAchievement.CallCount);
            Assert.AreEqual(def, stubAchievement.LastDef);
            Assert.AreEqual(10, stubAchievement.LastDelta);
        }

        [Test]
        public void PostCommit_AchievementOnCollect_NotCalled_WhenDefNull()
        {
            var stubAchievement = new StubAchievement();
            var flow = MakeFlow(achievement: stubAchievement);

            flow.PostCommit(null, 10, false, Vector3.zero);

            Assert.AreEqual(0, stubAchievement.CallCount);
        }

        // --- sfx ---

        [Test]
        public void PostCommit_PlayPenalty_WhenIsPenaltyTrue()
        {
            var stubSfx = new StubSfx();
            var flow = MakeFlow(sfx: stubSfx);

            flow.PostCommit(null, 0, true, Vector3.zero);

            Assert.AreEqual(1, stubSfx.PenaltyCount);
            Assert.AreEqual(0, stubSfx.CollectCount);
        }

        [Test]
        public void PostCommit_PlayCollect_WhenIsPenaltyFalse()
        {
            var stubSfx = new StubSfx();
            var flow = MakeFlow(sfx: stubSfx);

            flow.PostCommit(null, 0, false, Vector3.zero);

            Assert.AreEqual(1, stubSfx.CollectCount);
            Assert.AreEqual(0, stubSfx.PenaltyCount);
        }

        // --- overheat ---

        [Test]
        public void PostCommit_OverheatOnCollect_Called()
        {
            var stubOverheat = new StubOverheat();
            var flow = MakeFlow(overheat: stubOverheat);

            flow.PostCommit(null, 0, false, Vector3.zero);

            Assert.IsTrue(stubOverheat.OnCollectCalled);
        }

        [Test]
        public void PostCommit_OverheatNull_SkipsOverheatAndComboPopup()
        {
            // overheat == null のとき例外なし、かつ comboPopup.Show は呼ばれない
            var stubComboPopup = new StubComboPopupSpawner();
            var flow = MakeFlow(overheat: null, comboPopup: stubComboPopup);

            Assert.DoesNotThrow(() => flow.PostCommit(null, 0, false, Vector3.zero));
            Assert.AreEqual(0, stubComboPopup.ShowCallCount);
        }

        // --- comboPopup ---

        [Test]
        public void PostCommit_ComboPopupShown_WhenNotPenalty()
        {
            var stubOverheat = new StubOverheat { Combo = 5 };
            var stubComboPopup = new StubComboPopupSpawner();
            var flow = MakeFlow(overheat: stubOverheat, comboPopup: stubComboPopup);
            var expectedToken = session.Token;

            flow.PostCommit(null, 0, false, Vector3.zero);

            Assert.AreEqual(1, stubComboPopup.ShowCallCount);
            Assert.AreEqual(5, stubComboPopup.LastCombo);
            Assert.AreEqual(expectedToken, stubComboPopup.LastToken);
        }

        [Test]
        public void PostCommit_ComboPopupNotShown_WhenPenalty()
        {
            var stubComboPopup = new StubComboPopupSpawner();
            var flow = MakeFlow(overheat: new StubOverheat(), comboPopup: stubComboPopup);

            flow.PostCommit(null, 0, true, Vector3.zero);

            Assert.AreEqual(0, stubComboPopup.ShowCallCount);
        }
    }
}
