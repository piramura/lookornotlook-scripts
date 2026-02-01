using UnityEngine;
using VContainer;

namespace Piramura.LookOrNotLook.Gaze
{
    public sealed class GazeReticleSdfView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform quad;
        [SerializeField] private Renderer quadRenderer;

        [Header("Placement")]
        [SerializeField] private float surfaceOffset = 0.01f;
        [SerializeField] private float baseScale = 0.03f;

        [Header("Ring Param")]
        [SerializeField] private float radiusMin = 0.08f;
        [SerializeField] private float radiusMax = 0.35f;

        [Header("Visibility")]
        [SerializeField] private bool hideWhenNoHit = true;
        [SerializeField] private float visibleGraceSeconds = 0.08f; // チラつき防止

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        private GazeManager gaze;
        private Camera cam;
        private MaterialPropertyBlock mpb;

        private float visibleTimer;

        [Inject]
        public void Construct(GazeManager gaze) => this.gaze = gaze;

        private void Awake()
        {
            cam = Camera.main;
            if (quad == null) quad = transform;
            if (quadRenderer == null) quadRenderer = quad.GetComponent<Renderer>();

            quad.localScale = Vector3.one * baseScale;
            quad.gameObject.SetActive(false);

            mpb = new MaterialPropertyBlock();
            Debug.Log($"[Reticle] quad={quad!=null} renderer={quadRenderer!=null} gazeInjected={gaze!=null}");

        }

        private void LateUpdate()
        {
            Debug.Log("[Reticle] LateUpdate");

            if (gaze == null || quadRenderer == null || quad == null) return;

            var s = gaze.DebugState;

            // hasHit 判定：hitDistance を使う（DebugState側に bool を持たせてもOK）
            bool hasHit = s.hitDistance > 0f; // ← GazeManager側で「非ヒット時は 0」にしておくのが大事

            if (hasHit) visibleTimer = visibleGraceSeconds;
            else visibleTimer -= Time.deltaTime;

            bool visible = !hideWhenNoHit ? true : (visibleTimer > 0f);

            if (!visible)
            {
                Hide();
                return;
            }
            if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
            

            // 位置：必ず “面法線で押し出す” → カメラに迫らない
            Vector3 pos = s.hitPoint + s.hitNormal * surfaceOffset;
            quad.position = pos;

            
            // 面に貼る：法線方向を向く
            quad.rotation = Quaternion.LookRotation(s.hitNormal);
            

            // リング半径（dwell進行度）
            float t = s.dwell01; // ← GazeDebugStateに dwell01 が必要
            float radius = Mathf.Lerp(radiusMax, radiusMin, t);

            quadRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(RadiusId, radius);
            quadRenderer.SetPropertyBlock(mpb);
        }
        private void Hide()
        {
            if (quad.gameObject.activeSelf)
                quad.gameObject.SetActive(false);
        }
    }
}
