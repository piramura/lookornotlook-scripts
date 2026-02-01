using TMPro;
using UnityEngine;
using Piramura.LookOrNotLook.Gaze;
using VContainer;

namespace Piramura.LookOrNotLook.Dev
{
    public class GazeDebugView : MonoBehaviour
    {
        [SerializeField] private TMP_Text debugText;

        private GazeManager gazeManager;

        [Inject]
        public void Construct(GazeManager gazeManager)
        {
            this.gazeManager = gazeManager;
        }

        private void OnEnable()
        {
            if (gazeManager == null) return;
            gazeManager.OnDebugUpdated += UpdateView;
        }

        private void OnDisable()
        {
            if (gazeManager == null) return;
            gazeManager.OnDebugUpdated -= UpdateView;
        }

        private void UpdateView(GazeDebugState state)
        {
            if (debugText == null) return;
            debugText.text = state.ToString();
        }
    }
}
