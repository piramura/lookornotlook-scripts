using System;

namespace Piramura.LookOrNotLook.Game.Overheat
{
public interface IOverheatService
    {
        int Combo { get; }
        float ForbiddenChance01 { get; }   // 0..1
        event Action<int, float> Changed;  // (combo, chance)

        void Reset();
        void OnCollect(bool isForbidden);
    }
}