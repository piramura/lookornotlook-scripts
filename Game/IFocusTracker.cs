using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    public interface IFocusTracker
    {
        int FocusedSlotIndex { get; }
        GameObject Tick(float dt);
        void Clear();
    }
}
