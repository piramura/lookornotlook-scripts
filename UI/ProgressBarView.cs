using UnityEngine;
using UnityEngine.UI;

namespace Piramura.LookOrNotLook.UI
{
    public class ItemProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }

        public void SetProgress01(float value)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = Mathf.Clamp01(value);
        }

        public void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.enabled = visible;
        }
        public void ResetBar()
        {
            fillImage.fillAmount = 0f;
        }
    }
}
