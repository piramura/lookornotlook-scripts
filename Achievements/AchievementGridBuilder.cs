using UnityEngine;

namespace Piramura.LookOrNotLook.Achievements
{
    public class AchievementGridBuilder : MonoBehaviour
    {
        [SerializeField] private AchievementManager manager;
        [SerializeField] private AchievementCell cellPrefab;
        [SerializeField] private Transform contentRoot;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            foreach (var def in manager.GetDefinitions())
            {
                var cell = Instantiate(cellPrefab, contentRoot);
                cell.Setup(def, manager);
            }
        }
    }
}
