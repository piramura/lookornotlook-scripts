using System;
using NUnit.Framework;
using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Session;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class CollectGuardTests
    {
        private sealed class StubTimer : ITimerService
        {
            public bool IsTimeUp { get; set; }
            public float DurationSeconds => 60f;
            public float RemainingSeconds => IsTimeUp ? 0f : 60f;
            public event Action<float> OnRemainingChanged { add { } remove { } }
            public event Action OnTimeUp { add { } remove { } }
            public void Reset() { }
            public void StartTimer() { }
            public void StopAll() { }
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenFinished()
        {
            using var session = new GameSession();
            session.BeginNewSession();
            Assert.IsFalse(CollectGuard.IsValid(session.Token, session.Version, () => true, null, session));
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenTokenCancelled()
        {
            using var session = new GameSession();
            session.BeginNewSession();
            int ver = session.Version;
            var cancelledToken = session.Token;
            session.EndSession(); // token を先にキャプチャしてからキャンセル
            Assert.IsFalse(CollectGuard.IsValid(cancelledToken, ver, () => false, null, session));
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenTimerIsTimeUp()
        {
            using var session = new GameSession();
            session.BeginNewSession();
            var timer = new StubTimer { IsTimeUp = true };
            Assert.IsFalse(CollectGuard.IsValid(session.Token, session.Version, () => false, timer, session));
        }

        [Test]
        public void IsValid_ReturnsTrue_WhenTimerIsNull()
        {
            // timer == null はタイムアップガードをスキップする（NullReferenceException にならない）
            using var session = new GameSession();
            session.BeginNewSession();
            Assert.IsTrue(CollectGuard.IsValid(session.Token, session.Version, () => false, timer: null, session));
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenVersionMismatch()
        {
            using var session = new GameSession();
            session.BeginNewSession(); // Version = 1
            int staleVer = session.Version - 1; // 0 ≠ 1
            Assert.IsFalse(CollectGuard.IsValid(session.Token, staleVer, () => false, null, session));
        }

        [Test]
        public void IsValid_ReturnsTrue_WhenAllConditionsPass()
        {
            using var session = new GameSession();
            session.BeginNewSession();
            var timer = new StubTimer { IsTimeUp = false };
            Assert.IsTrue(CollectGuard.IsValid(session.Token, session.Version, () => false, timer, session));
        }
    }
}
