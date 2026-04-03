using NUnit.Framework;
using System.Threading;

namespace Piramura.LookOrNotLook.Tests.Session
{
    public class GameSessionTests
    {
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            session = new GameSession();
        }

        [TearDown]
        public void TearDown()
        {
            session.Dispose();
        }

        // ---- 初期状態 ----

        [Test]
        public void Initial_Version_Is_Zero()
        {
            Assert.AreEqual(0, session.Version);
        }

        [Test]
        public void Initial_IsAlive_Is_False()
        {
            Assert.IsFalse(session.IsAlive);
        }

        [Test]
        public void Initial_Token_Is_CancellationNone()
        {
            Assert.AreEqual(CancellationToken.None, session.Token);
        }

        // ---- BeginNewSession ----

        [Test]
        public void BeginNewSession_IncrementsVersion()
        {
            session.BeginNewSession();
            Assert.AreEqual(1, session.Version);
        }

        [Test]
        public void BeginNewSession_MultipleCallsAccumulatesVersion()
        {
            session.BeginNewSession();
            session.BeginNewSession();
            session.BeginNewSession();
            Assert.AreEqual(3, session.Version);
        }

        [Test]
        public void BeginNewSession_SetsIsAliveTrue()
        {
            session.BeginNewSession();
            Assert.IsTrue(session.IsAlive);
        }

        [Test]
        public void BeginNewSession_ProvidesNonCancelledToken()
        {
            session.BeginNewSession();
            Assert.AreNotEqual(CancellationToken.None, session.Token);
            Assert.IsFalse(session.Token.IsCancellationRequested);
        }

        [Test]
        public void BeginNewSession_CancelsPreviousToken()
        {
            session.BeginNewSession();
            var previousToken = session.Token;

            session.BeginNewSession();

            Assert.IsTrue(previousToken.IsCancellationRequested);
        }

        // ---- EndSession ----

        [Test]
        public void EndSession_SetsIsAliveFalse()
        {
            session.BeginNewSession();
            session.EndSession();
            Assert.IsFalse(session.IsAlive);
        }

        [Test]
        public void EndSession_CancelsActiveToken()
        {
            session.BeginNewSession();
            var token = session.Token;

            session.EndSession();

            Assert.IsTrue(token.IsCancellationRequested);
        }

        [Test]
        public void EndSession_TokenBecomesNone()
        {
            session.BeginNewSession();
            session.EndSession();
            Assert.AreEqual(CancellationToken.None, session.Token);
        }

        [Test]
        public void EndSession_DoesNotResetVersion()
        {
            session.BeginNewSession();
            session.EndSession();
            Assert.AreEqual(1, session.Version);
        }

        [Test]
        public void EndSession_BeforeAnySession_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => session.EndSession());
            Assert.IsFalse(session.IsAlive);
        }

        // ---- 冪等性 ----

        [Test]
        public void EndSession_IsIdempotent()
        {
            session.BeginNewSession();
            session.EndSession();

            Assert.DoesNotThrow(() => session.EndSession());
            Assert.AreEqual(CancellationToken.None, session.Token);
            Assert.IsFalse(session.IsAlive);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            session.BeginNewSession();
            session.Dispose();

            Assert.DoesNotThrow(() => session.Dispose());
        }

        // ---- Dispose ----

        [Test]
        public void Dispose_BehavesLikeEndSession()
        {
            session.BeginNewSession();
            var token = session.Token;

            session.Dispose();

            Assert.IsTrue(token.IsCancellationRequested);
            Assert.IsFalse(session.IsAlive);
            Assert.AreEqual(CancellationToken.None, session.Token);
        }
    }
}
