using UnityEngine;
using UnityEngine.EventSystems;

namespace Piramura.LookOrNotLook.UI
{
    /// <summary>
    /// uGUI(EventSystem)のPointerEnter/Exitを、IHoverableへ転送するだけ。
    /// マウスでもVRのRayでも同じイベントが来るので、Hover実装が1本化できる。
    /// </summary>
    public sealed class UIHoverForwarder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private IHoverable hoverable;

        private void Awake()
        {
            // Button配下に付ける想定なので、親から拾う
            hoverable = GetComponentInParent<IHoverable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hoverable?.OnHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hoverable?.OnHoverExit();
        }
    }
}
