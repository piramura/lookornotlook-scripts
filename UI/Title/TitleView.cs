namespace Piramura.LookOrNotLook.UI.TitleScreen
{
    using UnityEngine;

    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        public void SetVisible(bool visible)
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null) canvas.enabled = visible;
        }
    }
}
