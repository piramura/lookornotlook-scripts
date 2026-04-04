using NUnit.Framework;
using Piramura.LookOrNotLook.Game;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class FocusTrackerTests
    {
        private const float Delta = 1e-4f;

        [Test]
        public void GetDwellSpeedMultiplier_Combo0_ReturnsBaseSpeed()
        {
            // combo なし → 倍率 1.0（加速なし）
            Assert.AreEqual(1.0000f, FocusTracker.GetDwellSpeedMultiplier(0), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo1_ReturnsIncreasedSpeed()
        {
            // コンボ開始で微加速
            Assert.AreEqual(1.0714f, FocusTracker.GetDwellSpeedMultiplier(1), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo5_ReturnsMidpoint()
        {
            Assert.AreEqual(1.5000f, FocusTracker.GetDwellSpeedMultiplier(5), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo10_ReturnsPreFloorMax()
        {
            // combo=10 はまだ下限未到達（dwell=0.40）
            Assert.AreEqual(3.0000f, FocusTracker.GetDwellSpeedMultiplier(10), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo11_ReturnsFloorSpeed()
        {
            // combo=11 で minDwell=0.35 に到達（初めて上限速度に達する）
            Assert.AreEqual(3.4286f, FocusTracker.GetDwellSpeedMultiplier(11), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_LargeCombo_ReturnsSameAsFloor()
        {
            // combo が増え続けても速度は上がらない
            Assert.AreEqual(3.4286f, FocusTracker.GetDwellSpeedMultiplier(100), Delta);
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo10_IsLessThanCombo11()
        {
            // combo=11 で下限到達 → combo=10 より速い
            Assert.Less(FocusTracker.GetDwellSpeedMultiplier(10), FocusTracker.GetDwellSpeedMultiplier(11));
        }

        [Test]
        public void GetDwellSpeedMultiplier_Combo11_EqualsCombo100()
        {
            // 下限到達後は combo が増えても速度は変わらない
            Assert.AreEqual(
                FocusTracker.GetDwellSpeedMultiplier(11),
                FocusTracker.GetDwellSpeedMultiplier(100),
                Delta);
        }
    }
}
