using TMPro;
using UnityEngine;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class TimeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeText;

        private void Awake()
        {
            if (timeText == null)
                timeText = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>(true);
        }

        public void SetText(string text)
        {
            if (timeText != null) timeText.text = text;
        }
    }
}
