using System;
using System.Collections.Generic;
using Piramura.LookOrNotLook.Item;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Piramura.LookOrNotLook.Game
{
    public sealed class BoardSlotManager
    {
        private readonly ItemSpawner spawner;
        private readonly ItemLayout layout;
        private readonly ItemSelectionPolicy policy;
        private readonly GameManager config;

        private GameObject[] slotObjects;
        private readonly List<int> freeIndices = new();
        private readonly List<int> aroundBuffer = new();

        public BoardSlotManager(
            ItemSpawner spawner,
            ItemLayout layout,
            ItemSelectionPolicy policy,
            GameManager config)
        {
            this.spawner = spawner;
            this.layout = layout;
            this.policy = policy;
            this.config = config;
        }

        /// <summary>既存スロットを全 Destroy し、空の状態に初期化する。Start() と ResetGame() で使う。</summary>
        public void Reset()
        {
            if (slotObjects != null)
            {
                for (int i = 0; i < slotObjects.Length; i++)
                {
                    if (slotObjects[i] != null)
                    {
                        UnityEngine.Object.Destroy(slotObjects[i]);
                        slotObjects[i] = null;
                    }
                }
            }

            slotObjects = new GameObject[layout.Count];

            freeIndices.Clear();
            for (int i = 0; i < layout.Count; i++)
                freeIndices.Add(i);
        }

        /// <summary>freeIndices が尽きるまでランダムにスポーンする。</summary>
        public void SpawnAll()
        {
            while (freeIndices.Count > 0)
                SpawnRandom();
        }

        /// <summary>指定スロットにアイテムをスポーンする。</summary>
        public bool SpawnAt(int index)
        {
            if (!freeIndices.Contains(index)) return false;

            var def = policy.Select(config.ItemPool);
            if (def == null) return false;

            var go = spawner.SpawnAt(index, def.Prefab);

            freeIndices.Remove(index);
            slotObjects[index] = go;

            go.GetComponent<ItemSlot>().SetIndex(index);
            go.GetComponent<CollectableItem>().SetDefinition(def);

            return true;
        }

        /// <summary>
        /// スロットを空きとして登録する。GameObject の Destroy はしない
        /// （収集演出で既に非表示化済みの前提）。
        /// </summary>
        public void FreeSlot(int index)
        {
            if (slotObjects != null && index >= 0 && index < slotObjects.Length)
                slotObjects[index] = null;

            if (!freeIndices.Contains(index))
                freeIndices.Add(index);
        }

        /// <summary>
        /// centerIndex の周辺スロットを Destroy して再スポーンする。
        /// focusedSlotIndex が周辺範囲内に含まれる場合、Destroy の前に onFocusHit を呼ぶ。
        /// </summary>
        public void RefreshAround(int centerIndex, int focusedSlotIndex = -1, Action onFocusHit = null)
        {
            layout.GetIndicesAround(centerIndex, config.RefreshRadius, aroundBuffer, includeCenter: false);

            // Destroy 前にフォーカスクリアを通知（コンポーネントがまだ生きているうちに呼ぶ）
            if (focusedSlotIndex >= 0 && aroundBuffer.Contains(focusedSlotIndex))
                onFocusHit?.Invoke();

            for (int i = 0; i < aroundBuffer.Count; i++)
            {
                int idx = aroundBuffer[i];

                if (slotObjects != null && slotObjects[idx] != null)
                {
                    UnityEngine.Object.Destroy(slotObjects[idx]);
                    slotObjects[idx] = null;
                }

                if (!freeIndices.Contains(idx))
                    freeIndices.Add(idx);
            }

            for (int i = 0; i < aroundBuffer.Count; i++)
                SpawnAt(aroundBuffer[i]);
        }

        private bool SpawnRandom()
        {
            if (freeIndices.Count == 0) return false;
            int pick = Random.Range(0, freeIndices.Count);
            return SpawnAt(freeIndices[pick]);
        }
    }
}
