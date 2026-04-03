using System;
using System.Reflection;
using NUnit.Framework;
using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Item;
using UnityEngine;

namespace Piramura.LookOrNotLook.Tests.Game
{
    public class ItemSelectionPolicyTests
    {
        // ForbiddenChance01 を外から直接指定できる stub
        private sealed class StubOverheat : IOverheatService
        {
            public int Combo => 0;
            public float ForbiddenChance01 { get; set; }
            public event Action<int, float> Changed { add { } remove { } }
            public void Reset() { }
            public void OnCollect(bool isForbidden) { }
        }

        private static ItemDefinition MakeItem(bool isForbidden)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            typeof(ItemDefinition)
                .GetField("isForbidden", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(def, isForbidden);
            return def;
        }

        [Test]
        public void Select_NullPool_ReturnsNull()
        {
            var policy = new ItemSelectionPolicy(null);
            Assert.IsNull(policy.Select(null));
        }

        [Test]
        public void Select_EmptyPool_ReturnsNull()
        {
            var policy = new ItemSelectionPolicy(null);
            Assert.IsNull(policy.Select(new ItemDefinition[0]));
        }

        [Test]
        public void Select_NormalPool_ReturnsNonNull()
        {
            var pool = new[] { MakeItem(false), MakeItem(false) };
            var policy = new ItemSelectionPolicy(null);
            Assert.IsNotNull(policy.Select(pool));
        }

        [Test]
        public void Select_ForbiddenChance1_ReturnsForbiddenItem()
        {
            var normal   = MakeItem(false);
            var forbidden = MakeItem(true);
            var pool = new[] { normal, forbidden };

            var stub = new StubOverheat { ForbiddenChance01 = 1f };
            var policy = new ItemSelectionPolicy(stub);

            // Random.state を退避してシードを固定し、テスト後に復元する
            var savedState = Random.state;
            try
            {
                Random.InitState(42);
                var result = policy.Select(pool);
                Assert.IsTrue(result.IsForbidden);
            }
            finally
            {
                Random.state = savedState;
            }
        }

        [Test]
        public void Select_NoOverheat_NeverForceForbidden()
        {
            // overheat = null → chance = 0 → forbidden が選ばれることはない
            // （pool が forbidden のみなら null 経由のフォールバックで forbidden が返る可能性があるので
            //   通常アイテムのみのプールでテストする）
            var pool = new[] { MakeItem(false), MakeItem(false) };
            var policy = new ItemSelectionPolicy(null);

            var savedState = Random.state;
            try
            {
                Random.InitState(0);
                var result = policy.Select(pool);
                Assert.IsFalse(result.IsForbidden);
            }
            finally
            {
                Random.state = savedState;
            }
        }
    }
}
