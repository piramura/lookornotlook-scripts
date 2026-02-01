using UnityEngine;
using Piramura.LookOrNotLook.Item;

namespace Piramura.LookOrNotLook.Gaze
{
    public class GazeTarget : MonoBehaviour
    {
        [SerializeField] private ItemDefinition definition;
        public ItemDefinition Definition => definition;

        // 進行ゲージを付けたいなら（任意）
        [SerializeField] private Piramura.LookOrNotLook.UI.ItemProgressBar progressBar;
        public Piramura.LookOrNotLook.UI.ItemProgressBar ProgressBar => progressBar;
    }
}
