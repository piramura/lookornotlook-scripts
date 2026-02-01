// using UnityEngine;
// using VContainer;

// namespace Piramura.LookOrNotLook.Gaze
// {
//     public sealed class GazeRayVisualizer : MonoBehaviour
//     {
//         [SerializeField] private LineRenderer line;
//         [SerializeField] private bool showOnlyWhenAssist = false; // 使うなら

//         private GazeManager gaze;

//         [Inject]
//         public void Construct(GazeManager gaze) => this.gaze = gaze;

//         private void Awake()
//         {
//             if (line == null) line = GetComponent<LineRenderer>();
//             line.useWorldSpace = true;
//         }

//         private void LateUpdate()
//         {
//             if (line == null || gaze == null) return;

//             // GazeManagerが持つ“真実”を描く
//             var s = gaze.DebugState;

//             // showOnlyWhenAssist を使うなら gazeManager.assistMode を公開するか設定サービスに寄せる
//             line.positionCount = 2;
//             line.SetPosition(0, s.rayStart);
//             line.SetPosition(1, s.rayEnd);
//         }
//     }
// }
//  ↑ いったんコメントアウトしておくつかわないから