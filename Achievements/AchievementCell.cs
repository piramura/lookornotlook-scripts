using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Piramura.LookOrNotLook.Achievements
{
    public class AchievementCell : MonoBehaviour, IHoverable, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image lockOverlay;

        private AchievementDefinition def;
        private AchievementManager manager;

        public void Setup(AchievementDefinition def, AchievementManager manager)
        {
            // もし再Setupされることがあるなら、前の購読を解除してから差し替える
            if (this.manager != null)
                this.manager.OnUnlocked -= OnUnlocked;

            this.def = def;
            this.manager = manager;

            icon.sprite = def.icon;
            Refresh();
            if (this.manager != null)
                this.manager.OnUnlocked += OnUnlocked;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }
        private void OnDestroy()
        {
            // 破棄時に購読解除（地味に大事）
            if (manager != null)
                manager.OnUnlocked -= OnUnlocked;
        }
        private void OnClick()
        {
            LogState("[Click]");
        }

        private void OnUnlocked(AchievementDefinition unlockedDef)
        {
            if (unlockedDef == def)
                Refresh();
        }

        private void Refresh()
        {
            var state = manager.GetState(def.id);
            bool unlocked = state != null && state.unlocked;

            icon.color = unlocked ? Color.white : Color.gray;
            lockOverlay.gameObject.SetActive(!unlocked);
        }
        // ===== Hover Interface =====
        public void OnHoverEnter()
        {
            Debug.Log($"[Hover Enter] {def.title}");
        }

        public void OnHoverExit()
        {
            Debug.Log("[Hover Exit]");
        }
        // EventSystemのHoverをIHoverableに橋渡し
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit();
        }
        private void LogState(string prefix)
        {
            var state = manager.GetState(def.id);

            if (state != null && state.unlocked)
                Debug.Log($"{prefix} 実績解除済み: {def.title}");
            else
                Debug.Log($"{prefix} 実績未解除");
        }
    }
}