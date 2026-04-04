using System;

namespace Piramura.LookOrNotLook.Game
{
    public interface IBoardSlotManager
    {
        void Reset();
        void SpawnAll();
        bool SpawnAt(int index);
        void FreeSlot(int index);
        void RefreshAround(int centerIndex, int focusedSlotIndex = -1, Action onFocusHit = null);
    }
}
