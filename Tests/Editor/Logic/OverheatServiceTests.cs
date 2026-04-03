using NUnit.Framework;
using Piramura.LookOrNotLook.Game.Overheat;

namespace Piramura.LookOrNotLook.Tests.Logic
{
    public class OverheatServiceTests
    {
        private OverheatService service;

        [SetUp]
        public void SetUp()
        {
            service = new OverheatService();
        }

        [Test]
        public void InitialState_Combo0_Chance005()
        {
            Assert.AreEqual(0, service.Combo);
            Assert.AreEqual(0.05f, service.ForbiddenChance01, 0.0001f);
        }

        [Test]
        public void OnCollect_Normal_IncrementsCombo()
        {
            service.OnCollect(false);
            Assert.AreEqual(1, service.Combo);
            Assert.AreEqual(0.07f, service.ForbiddenChance01, 0.0001f);
        }

        [Test]
        public void OnCollect_Forbidden_ResetsCombo()
        {
            service.OnCollect(false);
            service.OnCollect(false);
            service.OnCollect(true);
            Assert.AreEqual(0, service.Combo);
            Assert.AreEqual(0.05f, service.ForbiddenChance01, 0.0001f);
        }

        [Test]
        public void ForbiddenChance_CapsAt035()
        {
            for (int i = 0; i < 15; i++)
                service.OnCollect(false);
            Assert.AreEqual(0.35f, service.ForbiddenChance01, 0.0001f);

            service.OnCollect(false);
            Assert.AreEqual(0.35f, service.ForbiddenChance01, 0.0001f);
        }

        [Test]
        public void Reset_SetsComboToZero()
        {
            service.OnCollect(false);
            service.OnCollect(false);
            service.Reset();
            Assert.AreEqual(0, service.Combo);
        }

        [Test]
        public void Reset_SetsChanceToBase()
        {
            for (int i = 0; i < 10; i++)
                service.OnCollect(false);
            service.Reset();
            Assert.AreEqual(0.05f, service.ForbiddenChance01, 0.0001f);
        }

        [Test]
        public void Reset_FiresChangedEvent()
        {
            service.OnCollect(false);
            int callCount = 0;
            service.Changed += (_, _) => callCount++;
            service.Reset();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void OnCollect_Normal_FiresChangedEvent()
        {
            int callCount = 0;
            service.Changed += (_, _) => callCount++;
            service.OnCollect(false);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void OnCollect_Forbidden_FiresChangedEvent()
        {
            int callCount = 0;
            service.Changed += (_, _) => callCount++;
            service.OnCollect(true);
            Assert.AreEqual(1, callCount);
        }
    }
}
