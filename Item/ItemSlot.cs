using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    public sealed class ItemSlot : MonoBehaviour
    {
        public int Index { get; private set; } = -1;
        public void SetIndex(int index) => Index = index;
    }
}
