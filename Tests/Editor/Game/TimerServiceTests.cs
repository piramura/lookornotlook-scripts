using NUnit.Framework;
using Piramura.LookOrNotLook.Game.Timer;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class TimerServiceTests
    {
        private TimerService timer;

        [SetUp]
        public void SetUp()
        {
            timer = new TimerService();
        }

        // ── 初期状態 ──────────────────────────────────────────────────

        [Test]
        public void DurationSeconds_Is60()
        {
            Assert.AreEqual(60f, timer.DurationSeconds);
        }

        [Test]
        public void Initial_RemainingIsZero_IsTimeUpTrue()
        {
            Assert.AreEqual(0f, timer.RemainingSeconds);
            Assert.IsTrue(timer.IsTimeUp);
        }

        // ── Reset() ───────────────────────────────────────────────────

        [Test]
        public void Reset_SetsRemainingToDuration()
        {
            timer.Reset();

            Assert.AreEqual(60f, timer.RemainingSeconds);
        }

        [Test]
        public void Reset_ClearsIsTimeUp()
        {
            timer.Reset();

            Assert.IsFalse(timer.IsTimeUp);
        }

        [Test]
        public void Reset_FiresOnRemainingChanged()
        {
            float received = -1f;
            timer.OnRemainingChanged += v => received = v;

            timer.Reset();

            Assert.AreEqual(60f, received);
        }

        [Test]
        public void Reset_AfterTimeUp_AllowsRestartViaStartTimer()
        {
            // firedTimeUp=true の状態を作る（初期 RemainingSeconds=0 を利用）
            timer.StartTimer();
            timer.Tick();

            // Reset で firedTimeUp がクリアされることを StartTimer の再発火で確認
            timer.Reset();

            bool fired = false;
            timer.OnRemainingChanged += _ => fired = true;

            timer.StartTimer();

            Assert.IsTrue(fired);
        }

        // ── StartTimer() ──────────────────────────────────────────────

        [Test]
        public void StartTimer_FiresOnRemainingChanged()
        {
            timer.Reset();
            int count = 0;
            timer.OnRemainingChanged += _ => count++;

            timer.StartTimer();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void StartTimer_AfterTimeUp_IsNoOp()
        {
            // firedTimeUp=true の状態を作る
            timer.StartTimer();
            timer.Tick();

            bool fired = false;
            timer.OnRemainingChanged += _ => fired = true;

            timer.StartTimer();

            Assert.IsFalse(fired);
        }

        // ── StopAll() ─────────────────────────────────────────────────

        [Test]
        public void StopAll_AfterStartTimer_PreventsTickFromFiring()
        {
            timer.Reset();
            timer.StartTimer();
            timer.StopAll();

            bool fired = false;
            timer.OnRemainingChanged += _ => fired = true;

            timer.Tick();

            Assert.IsFalse(fired);
        }

        // ── Start() ───────────────────────────────────────────────────

        [Test]
        public void Start_SetsRemainingToDuration_AndStopsTimer()
        {
            timer.Start();

            bool fired = false;
            timer.OnRemainingChanged += _ => fired = true;

            timer.Tick();

            Assert.AreEqual(60f, timer.RemainingSeconds);
            Assert.IsFalse(fired);
        }

        [Test]
        public void Start_ThenTick_DoesNotFireUntilStartTimer()
        {
            timer.Start();

            bool firedByTick = false;
            timer.OnRemainingChanged += _ => firedByTick = true;

            timer.Tick();

            Assert.IsFalse(firedByTick);

            // StartTimer() 後は Tick() で発火するようになる
            timer.StartTimer(); // この呼び出し自体で発火するので購読後にカウントし直す
            bool firedAfterStart = false;
            timer.OnRemainingChanged += _ => firedAfterStart = true;

            timer.Tick();

            Assert.IsTrue(firedAfterStart);
        }

        // ── OnTimeUp ─────────────────────────────────────────────────

        [Test]
        public void Tick_WhenTimeUp_FiresOnTimeUp()
        {
            // 構築直後は RemainingSeconds=0（IsTimeUp=true）
            bool fired = false;
            timer.OnTimeUp += () => fired = true;

            timer.StartTimer();
            timer.Tick();

            Assert.IsTrue(fired);
        }

        [Test]
        public void Tick_AfterTimeUp_IsNoOp_NoDoublefire()
        {
            int count = 0;
            timer.OnTimeUp += () => count++;

            timer.StartTimer();
            timer.Tick(); // 1 回目：OnTimeUp 発火

            timer.Tick(); // 2 回目：no-op のはず

            Assert.AreEqual(1, count);
        }
    }
}
