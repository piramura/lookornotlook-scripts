using System.Reflection;
using NUnit.Framework;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using UnityEngine;

namespace Piramura.LookOrNotLook.Tests.Logic
{
    public class AchievementServiceTests
    {
        private AchievementService service;

        [SetUp]
        public void SetUp()
        {
            service = new AchievementService();
        }

        // ItemDefinition (ScriptableObject) をテスト用に生成するヘルパー
        private static ItemDefinition MakeItem(ItemCategory category, bool isForbidden = false)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition)
                .GetField("category", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(def, category);
            typeof(ItemDefinition)
                .GetField("isForbidden", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(def, isForbidden);
            return def;
        }

        [Test]
        public void InitialAchievement_IsBeginner()
        {
            Assert.AreEqual("Beginner", service.CurrentAchievement);
        }

        [Test]
        public void OneNormalCollect_BecomesCollector()
        {
            service.OnCollect(MakeItem(ItemCategory.Food), 10);
            Assert.AreEqual("Collector", service.CurrentAchievement);
        }

        [Test]
        public void TenGadgets_BecomesGadgetHunter()
        {
            for (int i = 0; i < 10; i++)
                service.OnCollect(MakeItem(ItemCategory.Gadget), 10);
            Assert.AreEqual("Gadget Hunter", service.CurrentAchievement);
        }

        [Test]
        public void TwentyNormalCollects_BecomesMasterCollector()
        {
            for (int i = 0; i < 20; i++)
                service.OnCollect(MakeItem(ItemCategory.Food), 10);
            Assert.AreEqual("Master Collector", service.CurrentAchievement);
        }

        [Test]
        public void ThreeForbiddenCollects_BecomesRealityCheck()
        {
            for (int i = 0; i < 3; i++)
                service.OnCollect(MakeItem(ItemCategory.Reality, isForbidden: true), -5);
            Assert.AreEqual("Reality Check", service.CurrentAchievement);
        }

        [Test]
        public void RealityCheck_TakesPriorityOverMasterCollector()
        {
            for (int i = 0; i < 20; i++)
                service.OnCollect(MakeItem(ItemCategory.Food), 10);
            Assert.AreEqual("Master Collector", service.CurrentAchievement);

            for (int i = 0; i < 3; i++)
                service.OnCollect(MakeItem(ItemCategory.Reality, isForbidden: true), -5);
            Assert.AreEqual("Reality Check", service.CurrentAchievement);
        }

        [Test]
        public void Reset_ReturnsToBeginner()
        {
            service.OnCollect(MakeItem(ItemCategory.Food), 10);
            service.Reset();
            Assert.AreEqual("Beginner", service.CurrentAchievement);
        }

        [Test]
        public void OnCollect_TitleChange_FiresChangedEvent()
        {
            string received = null;
            service.Changed += t => received = t;
            service.OnCollect(MakeItem(ItemCategory.Food), 10);
            Assert.AreEqual("Collector", received);
        }

        [Test]
        public void OnCollect_SameTitle_DoesNotFireChangedEvent()
        {
            service.OnCollect(MakeItem(ItemCategory.Food), 10); // → Collector
            int callCount = 0;
            service.Changed += _ => callCount++;
            service.OnCollect(MakeItem(ItemCategory.Food), 10); // still Collector
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void OnCollect_Null_DoesNotThrowAndDoesNotChangeState()
        {
            int callCount = 0;
            service.Changed += _ => callCount++;

            Assert.DoesNotThrow(() => service.OnCollect(null, 0));

            Assert.AreEqual("Beginner", service.CurrentAchievement);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void OnCollect_19NormalItems_NotYetMasterCollector()
        {
            for (int i = 0; i < 19; i++)
                service.OnCollect(MakeItem(ItemCategory.Food), 10);
            Assert.AreNotEqual("Master Collector", service.CurrentAchievement);
        }

        [Test]
        public void OnCollect_2ForbiddenItems_NotYetRealityCheck()
        {
            for (int i = 0; i < 2; i++)
                service.OnCollect(MakeItem(ItemCategory.Reality, isForbidden: true), -5);
            Assert.AreNotEqual("Reality Check", service.CurrentAchievement);
        }

        [Test]
        public void ForbiddenItem_NotCountedTowardTotalCollected()
        {
            // 通常 19 個 + forbidden 1 個では Master Collector にならない
            for (int i = 0; i < 19; i++)
                service.OnCollect(MakeItem(ItemCategory.Food), 10);
            service.OnCollect(MakeItem(ItemCategory.Reality, isForbidden: true), -5);
            Assert.AreNotEqual("Master Collector", service.CurrentAchievement);
        }

        [Test]
        public void Reset_FiresChangedEvent_WhenTitleWasNotBeginner()
        {
            service.OnCollect(MakeItem(ItemCategory.Food), 10); // → Collector
            int callCount = 0;
            service.Changed += _ => callCount++;
            service.Reset();
            Assert.AreEqual("Beginner", service.CurrentAchievement);
            Assert.AreEqual(1, callCount);
        }
    }
}
