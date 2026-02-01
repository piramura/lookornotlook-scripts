using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    public interface IBoardCleaner
    {
        void ClearAll();
    }

    public sealed class BoardCleaner : MonoBehaviour, IBoardCleaner
    {
        [SerializeField] private Transform boardRoot;

        public void ClearAll()
        {
            if (boardRoot == null) boardRoot = transform;

            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                var child = boardRoot.GetChild(i);
                Destroy(child.gameObject);
            }
        }
    }
}
