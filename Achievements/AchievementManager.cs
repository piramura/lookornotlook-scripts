using System;
using System.Collections.Generic;
using UnityEngine;

namespace Piramura.LookOrNotLook.Achievements
{
    public class AchievementManager : MonoBehaviour
    {
        [SerializeField] private AchievementDefinition[] definitions;

        private readonly Dictionary<string, AchievementRuntimeState> states = new();
        private readonly Dictionary<string, AchievementDefinition> defById = new();

        public event Action<AchievementDefinition> OnUnlocked;

        private void Awake()
        {
            states.Clear();
            defById.Clear();

            foreach (var def in definitions)
            {
                if (def == null) continue;

                if (string.IsNullOrWhiteSpace(def.id))
                {
                    Debug.LogWarning($"[AchievementManager] Definition '{def.name}' has empty id. Skipped.");
                    continue;
                }

                if (defById.ContainsKey(def.id))
                {
                    Debug.LogWarning($"[AchievementManager] Duplicate id '{def.id}'. Skipped '{def.name}'.");
                    continue;
                }

                defById.Add(def.id, def);
                states.Add(def.id, new AchievementRuntimeState());
            }
        }

        public void AddProgress(string id, int value = 1)
        {
            if (!states.TryGetValue(id, out var state))
            {
                Debug.LogWarning($"[AchievementManager] Unknown id: {id}");
                return;
            }

            if (state.unlocked) return;

            state.progress += value;

            var def = defById[id];
            if (state.progress >= def.targetValue)
            {
                state.unlocked = true;
                Debug.Log($"[AchievementManager] Unlocked: {def.title} ({def.id})");
                OnUnlocked?.Invoke(def);
            }
        }

        public AchievementRuntimeState GetState(string id)
        {
            if (!states.TryGetValue(id, out var state)) return null;
            return state;
        }

        public IReadOnlyList<AchievementDefinition> GetDefinitions()
        {
            return definitions;
        }
    }
}
