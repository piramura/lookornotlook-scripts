using Piramura.LookOrNotLook.Logic;
using UnityEngine;
using VContainer;

namespace Piramura.LookOrNotLook.UI
{
    public sealed class ScorePresenter : MonoBehaviour
    {
        [SerializeField] private ScoreView view;

        private IScoreService score;

        [Inject]
        public void Construct(IScoreService score)
        {
            this.score = score;
        }

        private void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<ScoreView>(true);
        }

        private void OnEnable()
        {
            if (score == null) return;
            score.Changed += OnScoreChanged;
            // 初期表示
            OnScoreChanged(score.Score);
        }

        private void OnDisable()
        {
            if (score == null) return;
            score.Changed -= OnScoreChanged;
        }

        private void OnScoreChanged(int value)
        {
            if (view == null) return;
            view.SetScore(value);
        }
    }
}
