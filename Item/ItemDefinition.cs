using UnityEngine;

namespace Piramura.LookOrNotLook.Item
{
    [CreateAssetMenu(menuName = "LookOrNotLook/Item Definition", fileName = "ItemDef_")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "ITEM_001";   // ユニークID（称号・図鑑に使う）
        [SerializeField] private string displayName = "Meta Quest 3";

        [Header("Gameplay")]
        [SerializeField] private ItemCategory category = ItemCategory.Gadget;
        [SerializeField] private int value = 1000;
        [SerializeField] private bool isForbidden = false;

        [SerializeField] private float collectSeconds = 2.0f;   // ★追加：取得に必要な秒数
        public float CollectSeconds => collectSeconds;          // ★追加
        [SerializeField] private int penaltyValue = 500;

        public int PenaltyValue => penaltyValue;


        [Header("Visual")]
        [SerializeField] private GameObject prefab;            // 3Dモデル（任意・後でOK）
        [SerializeField] private Sprite icon;                  // UI用（任意・後でOK）

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemCategory Category => category;
        public int Value => value;
        public bool IsForbidden => isForbidden;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 最低限の事故防止（空のIDだけは避ける）
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name.ToUpperInvariant();
        }
#endif
    }
}
