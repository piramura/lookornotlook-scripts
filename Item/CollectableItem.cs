using UnityEngine;
using Piramura.LookOrNotLook.Gaze;
using Piramura.LookOrNotLook.Reaction;
namespace Piramura.LookOrNotLook.Item
{
    [RequireComponent(typeof(GazeTarget))]
    [RequireComponent(typeof(ItemProgress))]
    [RequireComponent(typeof(ItemReaction))]
    [RequireComponent(typeof(ItemSlot))]

    public class CollectableItem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition definition;
        public ItemDefinition Definition => definition;
        public void SetDefinition(ItemDefinition def)
        {
            definition = def;
            // ★追加：Definitionの値をProgressへ反映
            var progress = GetComponent<ItemProgress>();
            if (progress != null && def != null)
            {
                progress.SetRequiredTime(def.CollectSeconds);
            }
        }
    }
}
