using UnityEngine;
using VContainer;
namespace Piramura.LookOrNotLook.Gaze
{
    /// <summary>
    /// 視線イベントを受け取り、
    /// 「今は進行していいか？」＝「今もそのターゲットを見ているか」を判定するロジック層
    /// </summary>
    public class SeeingLogic : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GazeManager gazeManager;
        
        [Header("Optional: Mismatch Grace")]
        [Tooltip("ActiveTargetから視線が外れたとき、難病でActiveTargetを解除するか（0で即解除）")]
        [SerializeField] private float m_MismatchGraceSeconds= 0.1f;
        private GazeTarget m_CurrentTarget;
        private bool m_IsGazing;
        private float m_MismatchTimer;

        /// <summary>
        /// 現在のActiveTargetに対して「進行していい」か（＝今も見ているか）
        /// </summary>
        public bool CanProgress { get; private set; }


        /// <summary>
        /// 現在のアクティブターゲット（OnSeeingStartで確定したもの）
        /// </summary>
        public GazeTarget ActiveTarget => m_CurrentTarget;
        [Inject]
        public void Construct(GazeManager injected)
        {
            // インスペクタ優先、無ければDI
            if (gazeManager == null)
                gazeManager = injected;
        }


        private void Awake()
        {
            CanProgress = false;
            m_IsGazing = false;
        }

        private void OnEnable()
        {
            gazeManager.OnSeeingStart += HandleSeeingStart;
            gazeManager.OnSeeingEnd += HandleSeeingEnd;
        }

        private void OnDisable()
        {
            gazeManager.OnSeeingStart -= HandleSeeingStart;
            gazeManager.OnSeeingEnd -= HandleSeeingEnd;
        }

        private void Update()
        {
            if (!m_IsGazing || m_CurrentTarget == null)
            {
                CanProgress = false;
                return;
            }

            // 重要：GazeManagerは「別ターゲットを見てもOnSeeingEndが来ない」ことがあるので
            // Raw一致で“いま本当に見ているか”を判定する
            bool rawHitSame = (gazeManager.CurrentRawTarget == m_CurrentTarget);

            if (rawHitSame)
            {
                m_MismatchTimer = 0f;
                CanProgress = true;
                return;
            }

            // Rawがズレた → 進行停止（ItemProgress.Tick(false,dt)でリセットされる）
            CanProgress = false;

            // 一定時間ズレたらActiveTarget自体も解除して、UIも自然に消えるようにする
            m_MismatchTimer += Time.deltaTime;
            if (m_MismatchTimer >= m_MismatchGraceSeconds)
            {
                m_CurrentTarget = null;
                m_IsGazing = false;
                m_MismatchTimer = 0f;
            }
        }

        private void HandleSeeingStart(GazeTarget target)
        {
            m_CurrentTarget = target;
            m_IsGazing = (target != null);
            m_MismatchTimer = 0f;

            // OnSeeingStartが来た瞬間から進行開始できる（＝ゲージ開始）
            CanProgress = (target != null);

        }

        private void HandleSeeingEnd(GazeTarget target)
        {
            if (m_CurrentTarget != target) return;

            m_CurrentTarget = null;
            m_IsGazing = false;
            m_MismatchTimer = 0f;
            CanProgress = false;
        }
    }
}
