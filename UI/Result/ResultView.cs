using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Piramura.LookOrNotLook.UI.Result
{
    public sealed class ResultView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private Canvas rootCanvas;

        [Header("Texts")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text titleText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;

        public event Action RetryClicked;
        public event Action TitleClicked;

        private void Awake()
        {
            if (rootCanvas == null) rootCanvas = GetComponentInChildren<Canvas>(true);
            SetVisible(false);

            if (retryButton != null) retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            if (titleButton != null) titleButton.onClick.AddListener(() => TitleClicked?.Invoke());
        }

        public void SetVisible(bool visible)
        {
            if (rootCanvas != null) rootCanvas.enabled = visible;
            else gameObject.SetActive(visible);
        }

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title ?? "";
        }

        public void SetInteractable(bool interactable)
        {
            if (retryButton != null) retryButton.interactable = interactable;
            if (titleButton != null) titleButton.interactable = interactable;
        }
    }
}
