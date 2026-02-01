using UnityEngine;

namespace Piramura.LookOrNotLook.Common
{
    public sealed class BillboardToCamera : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null && Camera.main != null)
                targetCamera = Camera.main.transform;

            if (targetCamera == null) return;

            var dir = transform.position - targetCamera.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }
}
