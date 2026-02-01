using System.Collections.Generic;
using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    public class ItemLayout : MonoBehaviour
    {
        public enum LayoutMode
        {
            GridPlane,
            ArcPanel,
            Ring
        }

        [Header("Layout")]
        [SerializeField] private LayoutMode mode = LayoutMode.ArcPanel;

        [SerializeField] private int rows = 3;
        [SerializeField] private int columns = 4;

        public int Rows => rows;
        public int Columns => columns;
        public int Count => rows * columns;

        [Header("Grid / ArcPanel Size (local units)")]
        [SerializeField] private float width = 2.0f;
        [SerializeField] private float height = 1.0f;

        [Header("ArcPanel")]
        [SerializeField] private float arcAngleDeg = 60f;

        [Header("Ring")]
        [Tooltip("リングの半径（boardRootのローカル原点を中心にする）")]
        [SerializeField] private float ringRadius = 2.0f;

        [Tooltip("360で一周。120なら扇形に並ぶ")]
        [SerializeField] private float ringAngleDeg = 360f;

        [Tooltip("中央の向き(度)。0=+Z(正面)。90=+X")]
        [SerializeField] private float ringCenterDeg = 0f;

        [Tooltip("楕円にしたいならON（Z方向を潰す）")]
        [SerializeField] private bool ringEllipse = true;

        [Range(0.1f, 2f)]
        [SerializeField] private float ringEllipseZScale = 0.7f;

        private Vector3[] itemLocalPositions;

        private void Awake()
        {
            Rebuild();
        }

    #if UNITY_EDITOR
            private void OnValidate()
            {
                // Inspector変更時にも追従（EditorだけでOK）
                if (rows < 1) rows = 1;
                if (columns < 1) columns = 1;
                Rebuild();
            }
    #endif

        public void Rebuild()
        {
            int count = rows * columns;
            if (itemLocalPositions == null || itemLocalPositions.Length != count)
                itemLocalPositions = new Vector3[count];

            for (int i = 0; i < count; i++)
                itemLocalPositions[i] = CalcLocalPosition(i);
        }

        public Vector3 GetLocalPosition(int index) => itemLocalPositions[index];

        public Quaternion GetLocalRotation(int index)
        {
            // “中心(0,0,0)を見る”向き（Ring/ArcPanel向き）
            var p = GetLocalPosition(index);
            Vector3 dir = (-p);
            dir.y = 0f; // ロール/ピッチ抑制（必要なら外してOK）
            if (dir.sqrMagnitude < 0.0001f) return Quaternion.identity;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private Vector3 CalcLocalPosition(int index)
        {
            int col = index % columns;
            int row = index / columns;

            // 行の高さ（共通）
            float dy = (rows <= 1) ? 0f : height / (rows - 1);
            float y0 = -height * 0.5f;
            float y = y0 + dy * row;

            switch (mode)
            {
                case LayoutMode.GridPlane:
                {
                    float dx = (columns <= 1) ? 0f : width / (columns - 1);
                    float x0 = -width * 0.5f;
                    float x = x0 + dx * col;
                    return new Vector3(x, y, 0f);
                }

                case LayoutMode.ArcPanel:
                {
                    float dx = (columns <= 1) ? 0f : width / (columns - 1);
                    float x0 = -width * 0.5f;
                    float x = x0 + dx * col;

                    float thetaRad = arcAngleDeg * Mathf.Deg2Rad;
                    float radius = width / (2f * Mathf.Sin(thetaRad * 0.5f));
                    float xNorm = (width <= 0.0001f) ? 0f : x / (width * 0.5f);
                    float theta = xNorm * (thetaRad * 0.5f);

                    float curvedX = radius * Mathf.Sin(theta);
                    float curvedZ = radius * (1f - Mathf.Cos(theta));
                    return new Vector3(curvedX, y, curvedZ);
                }

                case LayoutMode.Ring:
                {
                    // 360のときは「columns」で割る（最後が重ならないように）
                    float stepDeg = Mathf.Approximately(ringAngleDeg, 360f)
                        ? ringAngleDeg / columns
                        : (columns <= 1 ? 0f : ringAngleDeg / (columns - 1));

                    // 中央を ringCenterDeg に合わせて左右に広げる
                    float startDeg = ringCenterDeg - ringAngleDeg * 0.5f;
                    float angDeg = startDeg + stepDeg * col;
                    float angRad = angDeg * Mathf.Deg2Rad;

                    float x = Mathf.Sin(angRad) * ringRadius;
                    float z = Mathf.Cos(angRad) * ringRadius;

                    if (ringEllipse)
                        z *= ringEllipseZScale;

                    return new Vector3(x, y, z);
                }

                default:
                    return Vector3.zero;
            }
        }

        public List<int> GetIndicesAround(int centerIndex, int range, List<int> results, bool includeCenter = true)
        {
            results.Clear();

            int centerRow = centerIndex / columns;
            int centerCol = centerIndex % columns;

            for (int dr = -range; dr <= range; dr++)
            {
                for (int dc = -range; dc <= range; dc++)
                {
                    if (!includeCenter && dr == 0 && dc == 0) continue;

                    int r = centerRow + dr;
                    int c = centerCol + dc;

                    if (r < 0 || r >= rows) continue;

                    if (mode == LayoutMode.Ring)
                    {
                        // 横だけ巻く
                        c = (c % columns + columns) % columns;
                    }
                    else
                    {
                        if (c < 0 || c >= columns) continue;
                    }

                    int idx = r * columns + c;
                    results.Add(idx);
                }
            }

            return results;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rows <= 0 || columns <= 0) return;

            int count = rows * columns;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = CalcLocalPosition(i);
                Gizmos.DrawSphere(transform.TransformPoint(p), 0.03f);
            }
        }
#endif
    }
}
