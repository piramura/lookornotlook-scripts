using System;
using Piramura.LookOrNotLook.Item;

namespace Piramura.LookOrNotLook.Logic
{
    public interface IAchievementService
    {
        string CurrentAchievement { get; }
        event Action<string> Changed;

        void Reset();
        void OnCollect(ItemDefinition def, int scoreDelta);
    }
}
