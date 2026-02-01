namespace Piramura.LookOrNotLook.UI.TitleScreen
{
    using System;
    using Piramura.LookOrNotLook.Game.State;
    using UnityEngine;
    using VContainer;

    public sealed class TitlePresenter : MonoBehaviour
    {
        [SerializeField] private TitleView view;

        private IGameStateService state;

        [Inject]
        public void Construct(IGameStateService state) => this.state = state;

        private void Awake()
        {
            if (view == null) view = GetComponentInChildren<TitleView>(true);
        }

        private void OnEnable()
        {
            if (state != null) state.Changed += OnChanged;
            OnChanged(state != null ? state.Phase : GamePhase.TitleScreen);
        }

        private void OnDisable()
        {
            if (state != null) state.Changed -= OnChanged;
        }

        private void OnChanged(GamePhase phase)
        {
            if (view == null) return;
            view.SetVisible(phase == GamePhase.TitleScreen);
        }
    }
}
