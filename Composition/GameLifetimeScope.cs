using Piramura.LookOrNotLook.Game;
using Piramura.LookOrNotLook.Gaze;
using Piramura.LookOrNotLook.Session;
using Piramura.LookOrNotLook.Logic;
using Piramura.LookOrNotLook.Item;
using Piramura.LookOrNotLook.Game.Timer;
using Piramura.LookOrNotLook.Audio;
using Piramura.LookOrNotLook.UI;
using Piramura.LookOrNotLook.Game.Overheat;
using Piramura.LookOrNotLook.Game.State;
using Piramura.LookOrNotLook.Save;
using Piramura.LookOrNotLook.UI.Result;
using Piramura.LookOrNotLook.UI.Achievement;
using Piramura.LookOrNotLook.UI.TitleScreen;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Piramura.LookOrNotLook.Composition
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
       protected override void Configure(IContainerBuilder builder)
        {
            // ---- Scene components ----
            builder.RegisterComponentInHierarchy<GazeManager>();
            builder.RegisterComponentInHierarchy<SeeingLogic>();
            builder.RegisterComponentInHierarchy<GameManager>();
            builder.RegisterComponentInHierarchy<ItemSpawner>();
            builder.RegisterComponentInHierarchy<ItemLayout>();

            // ---- UI (MonoBehaviour presenters/views) ----
            builder.RegisterComponentInHierarchy<ScoreView>();
            builder.RegisterComponentInHierarchy<ScorePresenter>();
            builder.RegisterComponentInHierarchy<TimeView>();
            builder.RegisterComponentInHierarchy<TimePresenter>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterComponentInHierarchy<Piramura.LookOrNotLook.Dev.GazeDebugView>();
        #endif

            // ---- Services (pure C#) ----
            builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
            builder.Register<IAchievementService, AchievementService>(Lifetime.Singleton);
            builder.Register<ISaveService, PlayerPrefsSaveService>(Lifetime.Singleton);
            builder.Register<IGameStateService, GameStateService>(Lifetime.Singleton);
            builder.Register<IOverheatService, OverheatService>(Lifetime.Singleton);

            builder.Register<IGameSession, GameSession>(Lifetime.Singleton);

            // ---- Audio ----
            builder.RegisterComponentInHierarchy<SfxSource>();

            // ※ StopAll / session/timerゲート版のSfxServiceにするなら、IGameSession/ITimerServiceも注入する
            builder.Register<ISfxService, SfxService>(Lifetime.Singleton)
                .WithParameter<AudioSource>(r => r.Resolve<SfxSource>().AudioSource)
                .WithParameter<AudioClip>(r => r.Resolve<SfxSource>().Config.collect)
                .WithParameter<AudioClip>(r => r.Resolve<SfxSource>().Config.penalty)
                .WithParameter<AudioClip>(r => r.Resolve<SfxSource>().Config.reset)
                .WithParameter<AudioClip>(r => r.Resolve<SfxSource>().Config.timeUp)
                .WithParameter<AudioClip>(r => r.Resolve<SfxSource>().Config.result);

            builder.RegisterComponentInHierarchy<ComboPopupSpawner>();
            //builder.RegisterComponentInHierarchy<Piramura.LookOrNotLook.Gaze.GazeRayVisualizer>();
            builder.RegisterComponentInHierarchy<GazeReticleSdfView>();
            builder.RegisterComponentInHierarchy<Piramura.LookOrNotLook.Item.BoardCleaner>()
                .As<Piramura.LookOrNotLook.Item.IBoardCleaner>();

            builder.RegisterComponentInHierarchy<Piramura.LookOrNotLook.Game.BoardPlacerToPlayer>();
            builder.Register<ItemSelectionPolicy>(Lifetime.Singleton);
            builder.Register<BoardSlotManager>(Lifetime.Singleton);
            builder.Register<FocusTracker>(Lifetime.Singleton);
            builder.Register<ItemCollectFlow>(Lifetime.Singleton);

            // ---- EntryPoints (Tick順が超重要) ----
            // 1) Timer（最初に時間を確定させる）
            builder.RegisterEntryPoint<TimerService>(Lifetime.Singleton)
                .AsSelf()
                .As<ITimerService>();

            // 2) TimeUp音（TimeUpが出たら即 EndSession/StopAll/PlayTimeUp）
            builder.RegisterEntryPoint<TimeUpSfxCoordinator>(Lifetime.Singleton);

            // 3) フェーズ遷移（IStartable のみ。Tick には参加しない）
            builder.RegisterEntryPoint<GamePhaseController>(Lifetime.Singleton).AsSelf();

            // 4) Game進行（Timerの後に回る）
            // GameLoop（UIから注入するなら AsSelf 必須）
            builder.RegisterEntryPoint<GameLoop>(Lifetime.Singleton).AsSelf();

            // 4) 入力（どこでも良いが、GameLoopより後にしておくと混乱しない）
            // builder.RegisterEntryPoint<GameResetInput>(Lifetime.Singleton);
            builder.Register<ResultFlow>(Lifetime.Singleton).As<IResultFlow>();  // Resultの操作先
            builder.RegisterEntryPoint<GameInputCoordinator>(Lifetime.Singleton).As<ITickable>();

            // ---- Other EntryPoints ----
            builder.RegisterEntryPoint<SaveCoordinator>(Lifetime.Singleton);
            // UI: Result
            builder.RegisterComponentInHierarchy<TitleView>();
            builder.RegisterComponentInHierarchy<TitlePresenter>();

            builder.RegisterComponentInHierarchy<ResultView>();
            builder.RegisterComponentInHierarchy<ResultPresenter>();

            builder.RegisterComponentInHierarchy<AchievementView>();
            builder.RegisterComponentInHierarchy<AchievementPresenter>();

            
        }

    }
}
