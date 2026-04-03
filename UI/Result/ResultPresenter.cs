using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Logic;
using UnityEngine;
using VContainer;

namespace Piramura.LookOrNotLook.UI.Result
{
    public sealed class ResultPresenter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ResultView view;

        [Header("Optional: Hide HUD Root")]
        [SerializeField] private Canvas hudCanvas; // 既存HUDのCanvasを刺せるなら

        private IGameStateService state;
        private IScoreService score;
        private IAchievementService achievement;
        private IResultFlow resultFlow;

        [Inject]
        public void Construct(
            IGameStateService state,
            IScoreService score,
            IAchievementService achievement,
            IResultFlow resultFlow)
        {
            this.state = state;
            this.score = score;
            this.achievement = achievement;
            this.resultFlow = resultFlow;
        }

        private void Awake()
        {
            if (view == null) view = GetComponent<ResultView>();
        }

        private void OnEnable()
        {
            if (state != null) state.Changed += OnPhaseChanged;

            if (view != null)
            {
                view.RetryClicked += OnRetryClicked;
                view.TitleClicked += OnTitleClicked;
            }

            // 初期反映
            OnPhaseChanged(state != null ? state.Phase : GamePhase.TitleScreen);
        }

        private void OnDisable()
        {
            if (state != null) state.Changed -= OnPhaseChanged;

            if (view != null)
            {
                view.RetryClicked -= OnRetryClicked;
                view.TitleClicked -= OnTitleClicked;
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            bool isResult = (phase == GamePhase.Result);

            // HUDを隠す（任意）
            if (hudCanvas != null) hudCanvas.enabled = !isResult;

            if (view == null) return;

            view.SetVisible(isResult);

            if (isResult)
            {
                view.SetScore(score != null ? score.Score : 0);//CurrentじゃなくてScoreでいいよね？
                view.SetTitle(achievement != null ? achievement.CurrentAchievement : "");
                view.SetInteractable(true);
            }
        }

        private void OnRetryClicked()
        {
            if (view != null) view.SetInteractable(false);
            resultFlow?.Retry();
        }

        private void OnTitleClicked()
        {
            if (view != null) view.SetInteractable(false);
            resultFlow?.GoTitle();
        }
    }
}
