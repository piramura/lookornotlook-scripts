using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Logic;
using UnityEngine;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Game
{
    /// <summary>
    /// フェーズ遷移シーケンス（Playing / Result / TitleScreen）を担当する。
    /// GameLoop.Tick() のフレーム処理とは独立した「状態遷移オーケストレーター」。
    /// </summary>
    public sealed class GamePhaseController : IStartable
    {
        private readonly IGameSession session;
        private readonly IGameStateService state;
        private readonly ITimerService timer;
        private readonly IScoreService score;
        private readonly IAchievementService achievement;
        private readonly IOverheatService overheat;
        private readonly BoardSlotManager boardSlotManager;
        private readonly IBoardCleaner boardCleaner;
        private readonly BoardPlacerToPlayer boardPlacerToPlayer;
        private readonly FocusTracker focusTracker;

        public GamePhaseController(
            IGameSession session,
            IGameStateService state,
            ITimerService timer,
            IScoreService score,
            IAchievementService achievement,
            IOverheatService overheat,
            BoardSlotManager boardSlotManager,
            IBoardCleaner boardCleaner,
            BoardPlacerToPlayer boardPlacerToPlayer,
            FocusTracker focusTracker)
        {
            this.session = session;
            this.state = state;
            this.timer = timer;
            this.score = score;
            this.achievement = achievement;
            this.overheat = overheat;
            this.boardSlotManager = boardSlotManager;
            this.boardCleaner = boardCleaner;
            this.boardPlacerToPlayer = boardPlacerToPlayer;
            this.focusTracker = focusTracker;
        }

        /// <summary>起動時の TitleScreen 初期化。</summary>
        public void Start()
        {
            Debug.Log("[GamePhaseController] Start");
            overheat.Reset();
            state.SetPhase(GamePhase.TitleScreen);
            timer.StopAll();
            timer.Reset();
            boardCleaner.ClearAll();
            boardSlotManager.Reset();
        }

        /// <summary>Playing フェーズ開始シーケンス。旧セッション終了・全リセット・新セッション開始を含む。</summary>
        public void EnterPlaying()
        {
            timer.StopAll();
            session.EndSession();         // 旧セッション終了（旧タスク全キャンセル）

            focusTracker.Clear();
            boardCleaner.ClearAll();

            // 盤とUIをプレイヤー正面に配置
            boardPlacerToPlayer.PlaceBoardAndUiInFrontOfPlayer();

            // リセットシーケンス
            overheat.Reset();
            session.BeginNewSession();    // 新セッション（Version++）
            boardSlotManager.Reset();
            score.Reset();
            achievement.Reset();
            timer.Reset();
            boardSlotManager.SpawnAll();

            state.SetPhase(GamePhase.Playing);
            timer.StartTimer();
        }

        /// <summary>Result フェーズ遷移シーケンス。</summary>
        public void EnterResult()
        {
            session.EndSession();
            timer.StopAll();
            focusTracker.Clear();
            boardCleaner.ClearAll();
            state.SetPhase(GamePhase.Result);
        }

        /// <summary>Result → TitleScreen 遷移シーケンス。</summary>
        public void GoTitleFromResult()
        {
            boardCleaner.ClearAll();
            timer.Reset();
            timer.StopAll();
            state.SetPhase(GamePhase.TitleScreen);
        }
    }
}
