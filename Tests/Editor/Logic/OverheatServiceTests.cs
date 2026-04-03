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
    }
}
