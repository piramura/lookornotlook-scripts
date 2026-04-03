using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Item;
using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    public sealed class ItemSelectionPolicy
    {
        private readonly IOverheatService overheat;

        public ItemSelectionPolicy(IOverheatService overheat)
        {
            this.overheat = overheat;
        }

        public ItemDefinition Select(ItemDefinition[] pool)
        {
            if (pool == null || pool.Length == 0) return null;

            var def = pool[Random.Range(0, pool.Length)];

            float p = overheat != null ? overheat.ForbiddenChance01 : 0f;
            if (Random.value < p)
            {
                ItemDefinition picked = null;
                int count = 0;

                for (int i = 0; i < pool.Length; i++)
                {
                    var d = pool[i];
                    if (d != null && d.IsForbidden)
                    {
                        count++;
                        // reservoir sampling（1パスで等確率に1つ選ぶ）
                        if (Random.Range(0, count) == 0)
                            picked = d;
                    }
                }

                def = picked;
            }

            // Forbidden が pool にない場合のフォールバック
            if (def == null)
                def = pool[Random.Range(0, pool.Length)];

            return def;
        }
    }
}
