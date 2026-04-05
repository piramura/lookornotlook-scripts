using Cysharp.Threading.Tasks;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Game.State;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// フレームティック処理とフェーズ遷移ファサードを担当する。
    /// 収集フロー（ロック・演出・確定・後処理）は ItemCollectFlow に委譲する。
    /// </summary>
    public sealed class GameLoop : ITickable
    {
        private readonly IGamePhaseController controller;
        private readonly IFocusTracker focusTracker;
        private readonly IItemCollectFlow collectFlow;
        private readonly ITimerService timer;
        private readonly IGameStateService state;

        // Tick ゲート（同フレーム内での相互排他）
        // GamePhaseController.Start() が TitleScreen を設定するまで Tick を止める
        private bool finished = true;

        public GameLoop(
            IGamePhaseController controller,
            IFocusTracker focusTracker,
            IItemCollectFlow collectFlow,
            ITimerService timer,
            IGameStateService state)
        {
            this.controller = controller;
            this.focusTracker = focusTracker;
            this.collectFlow = collectFlow;
            this.timer = timer;
            this.state = state;
        }

        public void Tick()
        {
            if (state.Phase != GamePhase.Playing) return; // ★フェーズゲート
            // 時間切れなら一度だけ終了処理して止める
            if (!finished && timer != null && timer.IsTimeUp)
            {
                finished = true;
                controller.EnterResult();
                return;
            }

            if (finished) return;

            var completed = focusTracker.Tick(UnityEngine.Time.deltaTime);
            // Forget: 収集フローはフレームティックと独立して非同期並走させる。ロックと CancellationToken でセッション安全を担保済み
            if (completed != null) collectFlow.ExecuteAsync(completed, () => finished).Forget();
        }

        // ---- フェーズ遷移ファサード（外部 API は変わらない）----

        public void StartGameFromTitle()
        {
            finished = true;
            controller.EnterPlaying();
            finished = false;
        }

        public void RetryFromResult()     => StartGameFromTitle();
        public void DebugResetToPlaying() => StartGameFromTitle();
        public void GoTitleFromResult()   => controller.GoTitleFromResult();
    }
}
