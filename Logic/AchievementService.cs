using System.Collections.Generic;
using Piramura.LookOrNotLook.Item;

namespace Piramura.LookOrNotLook.Logic
{
    public sealed class AchievementService : IAchievementService
    {
        public string CurrentAchievement { get; private set; } = "Beginner";
        public event System.Action<string> Changed;

        private int totalCollected;
        private int forbiddenCount;
        private readonly Dictionary<ItemCategory, int> categoryCounts = new();

        public void Reset()
        {
            totalCollected = 0;
            forbiddenCount = 0;
            categoryCounts.Clear();
            SetTitle("Beginner");
        }

        public void OnCollect(ItemDefinition def, int scoreDelta)
        {
            if (def == null) return;

            if (def.IsForbidden) forbiddenCount++;
            else totalCollected++;

            if (!categoryCounts.ContainsKey(def.Category))
                categoryCounts[def.Category] = 0;
            categoryCounts[def.Category]++;

            // ★称号ルール（最小版：あとで増やせる）
            // 優先度高い順
            if (forbiddenCount >= 3)
            {
                SetTitle("Reality Check");
                return;
            }

            if (totalCollected >= 20)
            {
                SetTitle("Master Collector");
                return;
            }

            if (categoryCounts.TryGetValue(ItemCategory.Gadget, out var g) && g >= 10)
            {
                SetTitle("Gadget Hunter");
                return;
            }

            if (totalCollected >= 1)
            {
                SetTitle("Collector");
                return;
            }

            SetTitle("Beginner");
        }

        private void SetTitle(string title)
        {
            if (CurrentAchievement == title) return;
            CurrentAchievement = title;
            Changed?.Invoke(CurrentAchievement);
        }
    }
}
