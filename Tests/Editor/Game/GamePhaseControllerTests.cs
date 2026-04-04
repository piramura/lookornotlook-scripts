using System;
using System.Collections.Generic;
using NUnit.Framework;
using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Session;
using UnityEngine;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class GamePhaseControllerTests
    {
        // ---- Stubs ----

        private sealed class StubState : IGameStateService
        {
            public GamePhase Phase { get; private set; }
            public event Action<GamePhase> Changed { add { } remove { } }
            private readonly List<string> _log;

            public StubState(List<string> log = null) => _log = log;

            public void SetPhase(GamePhase next)
            {
                Phase = next;
                _log?.Add($"state.SetPhase({next})");
            }
        }

        private sealed class StubTimer : ITimerService
        {
            public float DurationSeconds => 60f;
            public float RemainingSeconds => 60f;
            public bool IsTimeUp => false;
            public event Action<float> OnRemainingChanged { add { } remove { } }
            public event Action OnTimeUp { add { } remove { } }

            public int StopAllCount { get; private set; }
            public int ResetCount { get; private set; }
            public int StartTimerCount { get; private set; }
            private readonly List<string> _log;

            public StubTimer(List<string> log = null) => _log = log;

            public void StopAll() { StopAllCount++; _log?.Add("timer.StopAll"); }
            public void Reset() { ResetCount++; _log?.Add("timer.Reset"); }
            public void StartTimer() { StartTimerCount++; _log?.Add("timer.StartTimer"); }
        }

        private sealed class StubScore : IScoreService
        {
            public int Score => 0;
            public event Action<int> Changed { add { } remove { } }
            public int ResetCount { get; private set; }
            private readonly List<string> _log;

            public StubScore(List<string> log = null) => _log = log;

            public void Reset() { ResetCount++; _log?.Add("score.Reset"); }
            public void Add(int delta) { }
        }

        private sealed class StubAchievement : IAchievementService
        {
            public string CurrentAchievement => "";
            public event Action<string> Changed { add { } remove { } }
            public int ResetCount { get; private set; }
            private readonly List<string> _log;

            public StubAchievement(List<string> log = null) => _log = log;

            public void Reset() { ResetCount++; _log?.Add("achievement.Reset"); }
            public void OnCollect(ItemDefinition def, int scoreDelta) { }
        }

        private sealed class StubOverheat : IOverheatService
        {
            public int Combo => 0;
            public float ForbiddenChance01 => 0f;
            public event Action<int, float> Changed { add { } remove { } }
            public int ResetCount { get; private set; }
            private readonly List<string> _log;

            public StubOverheat(List<string> log = null) => _log = log;

            public void Reset() { ResetCount++; _log?.Add("overheat.Reset"); }
            public void OnCollect(bool isForbidden) { }
        }

        private sealed class StubBoardSlotManager : IBoardSlotManager
        {
            public int ResetCount { get; private set; }
            public int SpawnAllCount { get; private set; }
            private readonly List<string> _log;

            public StubBoardSlotManager(List<string> log = null) => _log = log;

            public void Reset() { ResetCount++; _log?.Add("boardSlotManager.Reset"); }
            public void SpawnAll() { SpawnAllCount++; _log?.Add("boardSlotManager.SpawnAll"); }
            public bool SpawnAt(int index) => false;
            public void FreeSlot(int index) { }
            public void RefreshAround(int centerIndex, int focusedSlotIndex = -1, Action onFocusHit = null) { }
        }

        private sealed class StubBoardCleaner : IBoardCleaner
        {
            public int ClearAllCount { get; private set; }
            private readonly List<string> _log;

            public StubBoardCleaner(List<string> log = null) => _log = log;

            public void ClearAll() { ClearAllCount++; _log?.Add("boardCleaner.ClearAll"); }
        }

        private sealed class StubBoardPlacer : IBoardPlacerToPlayer
        {
            public int PlaceCount { get; private set; }
            private readonly List<string> _log;

            public StubBoardPlacer(List<string> log = null) => _log = log;

            public void PlaceBoardAndUiInFrontOfPlayer() { PlaceCount++; _log?.Add("boardPlacer.Place"); }
        }

        private sealed class StubFocusTracker : IFocusTracker
        {
            public int FocusedSlotIndex => -1;
            public int ClearCount { get; private set; }
            private readonly List<string> _log;

            public StubFocusTracker(List<string> log = null) => _log = log;

            public void Clear() { ClearCount++; _log?.Add("focusTracker.Clear"); }
            public GameObject Tick(float dt) => null;
        }

        /// <summary>
        /// SpySession: 実 GameSession をラップして shared call log に書き込む。
        /// </summary>
        private sealed class SpySession : IGameSession
        {
            private readonly GameSession _inner;
            private readonly List<string> _log;

            public SpySession(GameSession inner, List<string> log)
            {
                _inner = inner;
                _log = log;
            }

            public System.Threading.CancellationToken Token => _inner.Token;
            public bool IsAlive => _inner.IsAlive;
            public int Version => _inner.Version;

            public void BeginNewSession() { _log.Add("session.BeginNewSession"); _inner.BeginNewSession(); }
            public void EndSession() { _log.Add("session.EndSession"); _inner.EndSession(); }
        }

        // ---- SetUp / TearDown ----

        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = new GameSession();
            _session.BeginNewSession();
        }

        [TearDown]
        public void TearDown()
        {
            _session.Dispose();
        }

        // ---- Factory ----

        private GamePhaseController MakeController(
            List<string> log = null,
            IGameSession sessionOverride = null,
            StubState state = null,
            StubTimer timer = null,
            StubScore score = null,
            StubAchievement achievement = null,
            StubOverheat overheat = null,
            StubBoardSlotManager boardSlotManager = null,
            StubBoardCleaner boardCleaner = null,
            StubBoardPlacer boardPlacer = null,
            StubFocusTracker focusTracker = null)
        {
            return new GamePhaseController(
                session: sessionOverride ?? _session,
                state: state ?? new StubState(log),
                timer: timer ?? new StubTimer(log),
                score: score ?? new StubScore(log),
                achievement: achievement ?? new StubAchievement(log),
                overheat: overheat ?? new StubOverheat(log),
                boardSlotManager: boardSlotManager ?? new StubBoardSlotManager(log),
                boardCleaner: boardCleaner ?? new StubBoardCleaner(log),
                boardPlacerToPlayer: boardPlacer ?? new StubBoardPlacer(log),
                focusTracker: focusTracker ?? new StubFocusTracker(log));
        }

        // ---- Start() ----

        [Test]
        public void Start_SetsPhase_TitleScreen()
        {
            var state = new StubState();
            var c = MakeController(state: state);

            c.Start();

            Assert.AreEqual(GamePhase.TitleScreen, state.Phase);
        }

        [Test]
        public void Start_CallsOverheatReset()
        {
            var overheat = new StubOverheat();
            var c = MakeController(overheat: overheat);

            c.Start();

            Assert.AreEqual(1, overheat.ResetCount);
        }

        [Test]
        public void Start_CallsTimerStopAll()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.Start();

            Assert.AreEqual(1, timer.StopAllCount);
        }

        [Test]
        public void Start_CallsTimerReset()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.Start();

            Assert.AreEqual(1, timer.ResetCount);
        }

        [Test]
        public void Start_CallsBoardCleanerClearAll()
        {
            var boardCleaner = new StubBoardCleaner();
            var c = MakeController(boardCleaner: boardCleaner);

            c.Start();

            Assert.AreEqual(1, boardCleaner.ClearAllCount);
        }

        [Test]
        public void Start_CallsBoardSlotManagerReset()
        {
            var boardSlotManager = new StubBoardSlotManager();
            var c = MakeController(boardSlotManager: boardSlotManager);

            c.Start();

            Assert.AreEqual(1, boardSlotManager.ResetCount);
        }

        // ---- EnterPlaying() ----

        [Test]
        public void EnterPlaying_SetsPhase_Playing()
        {
            var state = new StubState();
            var c = MakeController(state: state);

            c.EnterPlaying();

            Assert.AreEqual(GamePhase.Playing, state.Phase);
        }

        [Test]
        public void EnterPlaying_CallsTimerStopAll()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.EnterPlaying();

            Assert.AreEqual(1, timer.StopAllCount);
        }

        [Test]
        public void EnterPlaying_SessionVersionIncremented()
        {
            var versionBefore = _session.Version;
            var c = MakeController();

            c.EnterPlaying();

            Assert.AreEqual(versionBefore + 1, _session.Version);
        }

        [Test]
        public void EnterPlaying_CallsFocusTrackerClear()
        {
            var focusTracker = new StubFocusTracker();
            var c = MakeController(focusTracker: focusTracker);

            c.EnterPlaying();

            Assert.AreEqual(1, focusTracker.ClearCount);
        }

        [Test]
        public void EnterPlaying_CallsBoardCleanerClearAll()
        {
            var boardCleaner = new StubBoardCleaner();
            var c = MakeController(boardCleaner: boardCleaner);

            c.EnterPlaying();

            Assert.AreEqual(1, boardCleaner.ClearAllCount);
        }

        [Test]
        public void EnterPlaying_CallsBoardPlacerToPlayer()
        {
            var boardPlacer = new StubBoardPlacer();
            var c = MakeController(boardPlacer: boardPlacer);

            c.EnterPlaying();

            Assert.AreEqual(1, boardPlacer.PlaceCount);
        }

        [Test]
        public void EnterPlaying_CallsOverheatReset()
        {
            var overheat = new StubOverheat();
            var c = MakeController(overheat: overheat);

            c.EnterPlaying();

            Assert.AreEqual(1, overheat.ResetCount);
        }

        [Test]
        public void EnterPlaying_CallsScoreReset()
        {
            var score = new StubScore();
            var c = MakeController(score: score);

            c.EnterPlaying();

            Assert.AreEqual(1, score.ResetCount);
        }

        [Test]
        public void EnterPlaying_CallsAchievementReset()
        {
            var achievement = new StubAchievement();
            var c = MakeController(achievement: achievement);

            c.EnterPlaying();

            Assert.AreEqual(1, achievement.ResetCount);
        }

        [Test]
        public void EnterPlaying_CallsTimerReset()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.EnterPlaying();

            Assert.AreEqual(1, timer.ResetCount);
        }

        [Test]
        public void EnterPlaying_CallsBoardSlotManagerSpawnAll()
        {
            var boardSlotManager = new StubBoardSlotManager();
            var c = MakeController(boardSlotManager: boardSlotManager);

            c.EnterPlaying();

            Assert.AreEqual(1, boardSlotManager.SpawnAllCount);
        }

        [Test]
        public void EnterPlaying_CallsTimerStartTimer()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.EnterPlaying();

            Assert.AreEqual(1, timer.StartTimerCount);
        }

        [Test]
        public void EnterPlaying_CallOrder()
        {
            var log = new List<string>();
            var spy = new SpySession(_session, log);
            var c = MakeController(log: log, sessionOverride: spy);

            c.EnterPlaying();

            var expected = new[]
            {
                "timer.StopAll",
                "session.EndSession",
                "focusTracker.Clear",
                "boardCleaner.ClearAll",
                "boardPlacer.Place",
                "overheat.Reset",
                "session.BeginNewSession",
                "boardSlotManager.Reset",
                "score.Reset",
                "achievement.Reset",
                "timer.Reset",
                "boardSlotManager.SpawnAll",
                "state.SetPhase(Playing)",
                "timer.StartTimer",
            };
            CollectionAssert.AreEqual(expected, log);
        }

        // ---- EnterResult() ----

        [Test]
        public void EnterResult_SetsPhase_Result()
        {
            var state = new StubState();
            var c = MakeController(state: state);

            c.EnterResult();

            Assert.AreEqual(GamePhase.Result, state.Phase);
        }

        [Test]
        public void EnterResult_CallsTimerStopAll()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.EnterResult();

            Assert.AreEqual(1, timer.StopAllCount);
        }

        [Test]
        public void EnterResult_CallsFocusTrackerClear()
        {
            var focusTracker = new StubFocusTracker();
            var c = MakeController(focusTracker: focusTracker);

            c.EnterResult();

            Assert.AreEqual(1, focusTracker.ClearCount);
        }

        [Test]
        public void EnterResult_CallsBoardCleanerClearAll()
        {
            var boardCleaner = new StubBoardCleaner();
            var c = MakeController(boardCleaner: boardCleaner);

            c.EnterResult();

            Assert.AreEqual(1, boardCleaner.ClearAllCount);
        }

        [Test]
        public void EnterResult_SessionEnded()
        {
            var c = MakeController();
            var tokenBefore = _session.Token;

            c.EnterResult();

            Assert.IsTrue(tokenBefore.IsCancellationRequested);
        }

        // ---- GoTitleFromResult() ----

        [Test]
        public void GoTitleFromResult_SetsPhase_TitleScreen()
        {
            var state = new StubState();
            var c = MakeController(state: state);

            c.GoTitleFromResult();

            Assert.AreEqual(GamePhase.TitleScreen, state.Phase);
        }

        [Test]
        public void GoTitleFromResult_CallsBoardCleanerClearAll()
        {
            var boardCleaner = new StubBoardCleaner();
            var c = MakeController(boardCleaner: boardCleaner);

            c.GoTitleFromResult();

            Assert.AreEqual(1, boardCleaner.ClearAllCount);
        }

        [Test]
        public void GoTitleFromResult_CallsTimerReset()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.GoTitleFromResult();

            Assert.AreEqual(1, timer.ResetCount);
        }

        [Test]
        public void GoTitleFromResult_CallsTimerStopAll()
        {
            var timer = new StubTimer();
            var c = MakeController(timer: timer);

            c.GoTitleFromResult();

            Assert.AreEqual(1, timer.StopAllCount);
        }
    }
}
