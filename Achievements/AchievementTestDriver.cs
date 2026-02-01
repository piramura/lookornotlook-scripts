using UnityEngine;

namespace Piramura.LookOrNotLook.Achievements
{
    public class AchievementTestDriver : MonoBehaviour
    {
        [SerializeField] private AchievementManager manager;
        [SerializeField] private string id = "ach_test";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                manager.AddProgress(id, 1);
                var st = manager.GetState(id);
                Debug.Log($"[Test] {id}: {st.progress} / unlocked={st.unlocked}");
            }
        }
    }
}
