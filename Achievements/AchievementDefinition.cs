using UnityEngine;

namespace Piramura.LookOrNotLook.Achievements
{
    [CreateAssetMenu(menuName = "LookOrNotLook/Achievement Definition", fileName = "Ach_")]
    public class AchievementDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;

        [Header("UI")]
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Simple Condition")]
        public int targetValue = 1; // 数値目標
    }
}
