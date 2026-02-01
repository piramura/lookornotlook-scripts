using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private ItemLayout layout;
        [SerializeField] private Transform boardRoot;
        public Transform BoardRoot => boardRoot != null ? boardRoot : transform;
        

        public GameObject SpawnAt(int index, GameObject prefab)
        {
            if (layout == null || prefab == null)
            {
                UnityEngine.Debug.LogError("[ItemSpawner] Missing reference");
                return null;
            }

            // Parentだけ先に決めて、ローカルで配置する
            var go = Instantiate(prefab, boardRoot);

            go.transform.localPosition = layout.GetLocalPosition(index);
            go.transform.localRotation = layout.GetLocalRotation(index);

            return go;
        }
    }
}
