using UnityEngine;

namespace Piramura.LookOrNotLook.Gaze
{
    [System.Serializable]
    public struct GazeDebugState
    {
        // ---- Target state (logic) ----
        public GazeTarget rawTarget;
        public GazeTarget candidateTarget;
        public GazeTarget seeingTarget;

        // ---- Timing ----
        public float dwellElapsed;
        public float dwellRequired;   // 0ならdwell無効扱い
        public float dwell01;         // 0..1 (View用)
        public float missTimer;
        public float missGrace;

        // ---- Physics hit (visual) ----
        public bool hasHit;           // ← Viewが最も欲しい
        public Vector3 rayStart;
        public Vector3 rayEnd;        // 線の終端（当たってればhitPoint、当たってなければ最大距離）
        public Vector3 hitPoint;      // 当たってれば有効
        public Vector3 hitNormal;     // 当たってれば有効
        public float hitDistance;     // 当たってれば >0

        public static GazeDebugState CreateNoHit(
            Vector3 rayStart, Vector3 rayEnd,
            GazeTarget raw, GazeTarget candidate, GazeTarget seeing,
            float dwellElapsed, float dwellRequired, float missTimer, float missGrace)
        {
            float dwell01 = (candidate != null && dwellRequired > 0f)
                ? Mathf.Clamp01(dwellElapsed / dwellRequired)
                : 0f;

            return new GazeDebugState
            {
                rawTarget = raw,
                candidateTarget = candidate,
                seeingTarget = seeing,
                dwellElapsed = dwellElapsed,
                dwellRequired = dwellRequired,
                dwell01 = dwell01,
                missTimer = missTimer,
                missGrace = missGrace,

                hasHit = false,
                rayStart = rayStart,
                rayEnd = rayEnd,
                hitPoint = rayEnd,
                hitNormal = Vector3.zero,
                hitDistance = 0f,
            };
        }

        public static GazeDebugState CreateHit(
            Vector3 rayStart, Vector3 rayEnd,
            Vector3 hitPoint, Vector3 hitNormal, float hitDistance,
            GazeTarget raw, GazeTarget candidate, GazeTarget seeing,
            float dwellElapsed, float dwellRequired, float missTimer, float missGrace)
        {
            float dwell01 = (candidate != null && dwellRequired > 0f)
                ? Mathf.Clamp01(dwellElapsed / dwellRequired)
                : 0f;

            return new GazeDebugState
            {
                rawTarget = raw,
                candidateTarget = candidate,
                seeingTarget = seeing,
                dwellElapsed = dwellElapsed,
                dwellRequired = dwellRequired,
                dwell01 = dwell01,
                missTimer = missTimer,
                missGrace = missGrace,

                hasHit = true,
                rayStart = rayStart,
                rayEnd = rayEnd,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                hitDistance = hitDistance,
            };
        }
    }
}
