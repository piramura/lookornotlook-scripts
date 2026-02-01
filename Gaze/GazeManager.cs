using System;
using System.Collections.Generic;
using UnityEngine;

namespace Piramura.LookOrNotLook.Gaze
{
    public class GazeManager : MonoBehaviour
    {
        [Header("Ray Settings")]
        [SerializeField] private Camera gazeCamera;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private bool assistMode = true;
        [SerializeField] private float assistRadius = 0.03f; // 3cm くらい

        [Header("Timing")]
        [Tooltip("Seeingが成立するまでの注視秒数")]
        [SerializeField] private float dwellSeconds = 0.5f;

        [Tooltip("一時的な見失いを許容する秒数")]
        [SerializeField] private float missGraceSeconds = 0.5f;//テスト用

        // 最小イベント
        public event Action<GazeTarget> OnSeeingStart;
        public event Action<GazeTarget> OnSeeingEnd;

         // コア状態
        public GazeTarget CurrentRawTarget { get; private set; }
        public GazeTarget CurrentSeeingTarget { get; private set; }
        // 内部状態
        private GazeTarget rawCandidate;   // 「Seeingになり得る候補」
        private float candidateStartTime; // 候補を見始めた時刻
        private float missTimer;
        // デバッグ状態
        [Header("Debug")]
        public GazeDebugState DebugState { get; private set; }
        public event Action<GazeDebugState> OnDebugUpdated;
        [SerializeField] private bool drawDebugRay = false;
        [SerializeField] private float rayLength = 10f;


        void Awake()
        {
            if (gazeCamera == null) gazeCamera = Camera.main;
        }

        void Update()
        {
            if (gazeCamera == null) return;
            float dt = Time.deltaTime;
            var cam = gazeCamera.transform;
            
            // near clip の少し前からRayを出す（見えない問題を潰したい）これいる？

            float startOffset = Mathf.Max(0.05f, gazeCamera.nearClipPlane + 0.01f);
            Vector3 origin = cam.position + cam.forward * startOffset;
            Vector3 dir = cam.forward;
            
            // RaycastHit hit を受け取る
            bool hasHit = RaycastTarget(origin, dir, out GazeTarget hitTarget, out RaycastHit hit);
            CurrentRawTarget = hitTarget;

            // Vector3 end = hasHit ? hitPoint : origin + dir * rayLength;
            // UpdateDebugState(origin, end, hasHit);

            // ※SphereCastのヒット点を出したいなら RaycastTarget側で hit.point を返す設計にする
            if(hitTarget == null)
            {
                missTimer += dt;
                if(missTimer >= missGraceSeconds)
                {
                    rawCandidate = null;

                    if(CurrentSeeingTarget != null)
                    {
                        OnSeeingEnd?.Invoke(CurrentSeeingTarget);
                        CurrentSeeingTarget = null;
                    }
                    missTimer = 0f;
                }
            }
            else
            {
                // 当たってる
                missTimer = 0f;
                // 注視候補が変わった
                if(hitTarget != rawCandidate)
                {
                    // Seeing中は候補を更新しない（あなたの仕様のまま）
                    if(CurrentSeeingTarget == null)
                    {
                        rawCandidate = hitTarget;
                        candidateStartTime = Time.time;
                    }
                }
                else
                {
                    // 同じ候補を見続けている
                    float elapsed = Time.time - candidateStartTime;
                    if(elapsed >= dwellSeconds && CurrentSeeingTarget != hitTarget)
                    {
                        if(CurrentSeeingTarget != hitTarget)
                        {
                            // Seeing成立（遷移時だけ）
                            if(CurrentSeeingTarget != null)
                            {
                                OnSeeingEnd?.Invoke(CurrentSeeingTarget);
                            }
                            CurrentSeeingTarget = hitTarget;
                            OnSeeingStart?.Invoke(hitTarget);
                        }
                    }
                }
            }
            // ====== DebugState更新（最後に1回だけ） ======
            Vector3 end = hasHit ? hit.point : origin + dir * rayLength;
            UpdateDebugState(origin, dir, hasHit, hit);

            if (drawDebugRay)
                Debug.DrawRay(origin, dir * (hasHit ? hit.distance : rayLength), Color.cyan);
            // UpdateDebugState(origin, dir, hasHit, hit);
            // if (drawDebugRay)
            // {
            //     UnityEngine.Debug.DrawRay(
            //         cam.position,
            //         cam.forward * rayLength,
            //         Color.cyan
            //     );
            // }
            // CurrentRawTarget = hitTarget;
            
            // // 1) 何にもあたってない
            // if(hitTarget == null)
            // {
            //     missTimer += dt;
                
            //     if(missTimer >= missGraceSeconds)
            //     {
            //         rawCandidate = null;

            //         if(CurrentSeeingTarget != null)
            //         {
            //             OnSeeingEnd?.Invoke(CurrentSeeingTarget);
            //             CurrentSeeingTarget = null;
            //         }
            //         missTimer = 0f;
            //     }
            //     UpdateDebugState(origin, end, hasHit);
            //     return;
            // }

            // // 2) 当たってる：見失いタイマーをリセット
            // missTimer = 0f;

            // // 3) 注視候補が変わった
            // if (hitTarget != rawCandidate)
            // {
            //     // Seeing中は候補を更新しない
            //     if (CurrentSeeingTarget == null)
            //     {
            //         rawCandidate = hitTarget;
            //         candidateStartTime = Time.time;
            //     }
            //     UpdateDebugState(origin, end, hasHit);
            //     return;
            // }

            // // 4) 同じ候補を見続けている
            // float elapsed = Time.time - candidateStartTime;

            // if (elapsed < dwellSeconds)
            // {
            //     UpdateDebugState(origin, end, hasHit);
            //     return;
            // }

            // // 5) Seeing成立
            // if (CurrentSeeingTarget == hitTarget)
            // {
            //     UpdateDebugState(origin, end, hasHit);
            //     return;
            // }

            // // 旧Seeingがあれば終了
            // if (CurrentSeeingTarget != null)
            // {
            //     OnSeeingEnd?.Invoke(CurrentSeeingTarget);
            // }

            // 新Seeing開始
            // CurrentSeeingTarget = hitTarget;
            // OnSeeingStart?.Invoke(hitTarget);
            // UpdateDebugState(origin, dir, hasHit, hit);
        }
        private void UpdateDebugState(
            Vector3 origin, Vector3 dir,
            bool hasHit, RaycastHit hit)
        {
            float dwellElapsed = rawCandidate != null ? Time.time - candidateStartTime : 0f;

            if (hasHit)
            {
                Vector3 hitPoint = hit.point;
                Vector3 hitNormal = hit.normal;
                float hitDistance = hit.distance;
                Vector3 end = hitPoint;

                DebugState = GazeDebugState.CreateHit(
                    origin, end,
                    hitPoint, hitNormal, hitDistance,
                    CurrentRawTarget, rawCandidate, CurrentSeeingTarget,
                    dwellElapsed, dwellSeconds,
                    missTimer, missGraceSeconds);
            }
            else
            {
                Vector3 end = origin + dir * maxDistance;

                DebugState = GazeDebugState.CreateNoHit(
                    origin, end,
                    CurrentRawTarget, rawCandidate, CurrentSeeingTarget,
                    dwellElapsed, dwellSeconds,
                    missTimer, missGraceSeconds);
            }

            OnDebugUpdated?.Invoke(DebugState);
        }


        private bool RaycastTarget(
            Vector3 origin, Vector3 dir,
            out GazeTarget target,
            out RaycastHit hit)
        {
            bool hitPhysics = assistMode
                ? Physics.SphereCast(origin, assistRadius, dir, out hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore)
                : Physics.Raycast(origin, dir, out hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore);

            if (hitPhysics)
            {
                target = hit.collider.GetComponentInParent<GazeTarget>();
                return true;
            }

            target = null;
            return false;
        }



    }
}
