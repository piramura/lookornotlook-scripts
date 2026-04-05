using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Game.Timer;
using UnityEngine;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class GameLoopTests
    {
        // ---- Stubs ----

        private sealed class StubState : IGameStateService
        {
            public GamePhase Phase { get; private set; }
            public event Action<GamePhase> Changed { add { } remove { } }
            public void SetPhase(GamePhase next) => Phase = next;
        }

        private sealed class StubTimer : ITimerService
        {
            public float DurationSeconds => 60f;
            public float RemainingSeconds => 60f;
            public bool IsTimeUp { get; set; }
            public event Action<float> OnRemainingChanged { add { } remove { } }
            public event Action OnTimeUp { add { } remove { } }
            public void Reset() { }
            public void StartTimer() { }
            public void StopAll() { }
        }

        private sealed class StubFocusTracker : IFocusTracker
        {
            public int FocusedSlotIndex => -1;
            public int TickCallCount { get; private set; }
            public GameObject ReturnOnTick { get; set; }
            public void Clear() { }
            public GameObject Tick(float dt) { TickCallCount++; return ReturnOnTick; }
        }

        private sealed class StubCollectFlow : IItemCollectFlow
        {
            public int ExecuteCallCount { get; private set; }
            public GameObject LastItem { get; private set; }
            public Func<bool> LastIsFinished { get; private set; }
            public UniTask ExecuteAsync(GameObject item, Func<bool> isFinished)
            {
                ExecuteCallCount++;
                LastItem = item;
                LastIsFinished = isFinished;
                return UniTask.CompletedTask;
            }
        }

        /// <summary>
        /// StubController: 呼び出しを記録し、EnterPlaying では StubState を Playing に進める
        /// （GameLoop.Tick のフェーズゲートを通過させるため）。
        /// </summary>
        private sealed class StubController : IGamePhaseController
        {
            private readonly StubState _state;
            public int EnterPlayingCount { get; private set; }
            public int EnterResultCount { get; private set; }
            public int GoTitleCount { get; private set; }

            public StubController(StubState state) => _state = state;

            public void EnterPlaying()
            {
                EnterPlayingCount++;
                _state.SetPhase(GamePhase.Playing);
            }

            public void EnterResult()
            {
                EnterResultCount++;
                _state.SetPhase(GamePhase.Result);
            }

            public void GoTitleFromResult()
            {
                GoTitleCount++;
                _state.SetPhase(GamePhase.TitleScreen);
            }
        }

        // ---- Factory ----

        private StubState _state;
        private StubTimer _timer;
        private StubFocusTracker _focusTracker;
        private StubCollectFlow _collectFlow;
        private StubController _controller;

        [SetUp]
        public void SetUp()
        {
            _state = new StubState();
            _timer = new StubTimer();
            _focusTracker = new StubFocusTracker();
            _collectFlow = new StubCollectFlow();
            _controller = new StubController(_state);
        }

        private GameLoop MakeLoop() =>
            new GameLoop(_controller, _focusTracker, _collectFlow, _timer, _state);

        // ---- Tick: フェーズゲート ----

        [Test]
        public void Tick_DoesNothing_WhenPhaseIsNotPlaying()
        {
            // state.Phase は初期値 TitleScreen（SetPhase 未呼び出し）
            var loop = MakeLoop();

            loop.Tick();

            Assert.AreEqual(0, _focusTracker.TickCallCount);
            Assert.AreEqual(0, _controller.EnterResultCount);
        }

        [Test]
        public void Tick_DoesNothing_WhenPhaseIsResult()
        {
            _state.SetPhase(GamePhase.Result);
            var loop = MakeLoop();

            loop.Tick();

            Assert.AreEqual(0, _focusTracker.TickCallCount);
            Assert.AreEqual(0, _controller.EnterResultCount);
        }

        // ---- Tick: finished ガード ----

        [Test]
        public void Tick_DoesNotCallFocusTracker_WhenFinishedIsTrue()
        {
            // finished は初期値 true
            _state.SetPhase(GamePhase.Playing);
            var loop = MakeLoop();

            loop.Tick();

            Assert.AreEqual(0, _focusTracker.TickCallCount);
        }

        [Test]
        public void Tick_CallsFocusTracker_WhenFinishedIsFalse()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle(); // finished = false, phase = Playing

            loop.Tick();

            Assert.AreEqual(1, _focusTracker.TickCallCount);
        }

        // ---- Tick: タイムアップ ----

        [Test]
        public void Tick_CallsEnterResult_WhenTimeUp()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle(); // finished = false, phase = Playing
            _timer.IsTimeUp = true;

            loop.Tick();

            Assert.AreEqual(1, _controller.EnterResultCount);
        }

        [Test]
        public void Tick_DoesNotCallFocusTracker_WhenTimeUp()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle();
            _timer.IsTimeUp = true;

            loop.Tick();

            Assert.AreEqual(0, _focusTracker.TickCallCount);
        }

        [Test]
        public void Tick_EnterResult_CalledOnlyOnce_OnRepeatedTicks()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle();
            _timer.IsTimeUp = true;

            loop.Tick();
            loop.Tick();
            loop.Tick();

            Assert.AreEqual(1, _controller.EnterResultCount);
        }

        // ---- Tick: collectFlow 呼び出し ----

        [Test]
        public void Tick_DoesNotCallExecuteAsync_WhenCompletedIsNull()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle();
            _focusTracker.ReturnOnTick = null;

            loop.Tick();

            Assert.AreEqual(0, _collectFlow.ExecuteCallCount);
        }

        [Test]
        public void Tick_CallsExecuteAsync_WhenCompletedIsNotNull()
        {
            var go = new GameObject();
            var loop = MakeLoop();
            loop.StartGameFromTitle();
            _focusTracker.ReturnOnTick = go;

            loop.Tick();

            Assert.AreEqual(1, _collectFlow.ExecuteCallCount);
            Assert.AreEqual(go, _collectFlow.LastItem);

            UnityEngine.Object.DestroyImmediate(go);
        }

        // ---- ファサード: StartGameFromTitle ----

        [Test]
        public void StartGameFromTitle_CallsEnterPlaying()
        {
            var loop = MakeLoop();

            loop.StartGameFromTitle();

            Assert.AreEqual(1, _controller.EnterPlayingCount);
        }

        [Test]
        public void StartGameFromTitle_AllowsTickToFlowThrough()
        {
            var loop = MakeLoop();
            loop.StartGameFromTitle();

            loop.Tick();

            Assert.AreEqual(1, _focusTracker.TickCallCount);
        }

        // ---- ファサード: RetryFromResult ----

        [Test]
        public void RetryFromResult_CallsEnterPlaying()
        {
            var loop = MakeLoop();

            loop.RetryFromResult();

            Assert.AreEqual(1, _controller.EnterPlayingCount);
        }

        [Test]
        public void RetryFromResult_AllowsTickToFlowThrough()
        {
            var loop = MakeLoop();
            loop.RetryFromResult();

            loop.Tick();

            Assert.AreEqual(1, _focusTracker.TickCallCount);
        }

        // ---- ファサード: DebugResetToPlaying ----

        [Test]
        public void DebugResetToPlaying_CallsEnterPlaying()
        {
            var loop = MakeLoop();

            loop.DebugResetToPlaying();

            Assert.AreEqual(1, _controller.EnterPlayingCount);
        }

        [Test]
        public void DebugResetToPlaying_AllowsTickToFlowThrough()
        {
            var loop = MakeLoop();
            loop.DebugResetToPlaying();

            loop.Tick();

            Assert.AreEqual(1, _focusTracker.TickCallCount);
        }

        // ---- ファサード: GoTitleFromResult ----

        [Test]
        public void GoTitleFromResult_CallsGoTitleOnController()
        {
            var loop = MakeLoop();

            loop.GoTitleFromResult();

            Assert.AreEqual(1, _controller.GoTitleCount);
        }
    }
}
