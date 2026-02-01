using UnityEngine;

namespace Piramura.LookOrNotLook.Game
{
    public sealed class BoardPlacerToPlayer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private OVRCameraRig rig;

        [Header("Targets")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private Transform uiRoot;

        [Header("Placement - Board")]
        [SerializeField] private float boardDistance = 2.2f;
        [SerializeField] private float boardHeightOffset = 0.0f;

        [Header("Placement - UI")]
        [SerializeField] private float uiDistance = 1.6f;
        [SerializeField] private float uiHeightOffset = 0.0f;

        public void PlaceBoardAndUiInFrontOfPlayer()
        {
            Transform eye = GetCenterEye();
            if (eye == null) return;

            Vector3 forward = GetFlatForward(eye);
            Vector3 boardPos = CalcPosition(eye, forward, boardDistance, boardHeightOffset);
            Vector3 uiPos    = CalcPosition(eye, forward, uiDistance, uiHeightOffset);

            PlaceAt(boardRoot, boardPos, forward);
            PlaceAt(uiRoot, uiPos, forward);
        }

        private Transform GetCenterEye()
        {
            if (rig == null)
            {
                Debug.LogWarning("[BoardPlacerToPlayer] rig is null");
                return null;
            }

            Transform eye = rig.centerEyeAnchor;
            if (eye == null)
            {
                Debug.LogWarning("[BoardPlacerToPlayer] centerEyeAnchor is null");
                return null;
            }

            return eye;
        }

        private static Vector3 GetFlatForward(Transform eye)
        {
            Vector3 forward = eye.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            return forward.normalized;
        }

        private static Vector3 CalcPosition(Transform eye, Vector3 forward, float distance, float heightOffset)
        {
            Vector3 pos = eye.position + forward * distance;
            pos.y = eye.position.y + heightOffset;
            return pos;
        }

        private static void PlaceAt(Transform target, Vector3 pos, Vector3 forward)
        {
            if (target == null) return;

            target.position = pos;

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

            target.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
