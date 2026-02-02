# このリポジトリについて
このリポジトリは、個人開発している Unity VR ゲーム LookOrNotLook の C# Scripts 部分を切り出したものです。

## 目的
- Unity プロジェクト全体（アセットや依存関係）を公開せずに、コードだけをどこでも閲覧できる状態にする
- 他の人にレビューを依頼しやすくする（リンクひとつで見てもらえる）
- 開発環境がノートPC→デスクトップPC中心に移ったが、必要に応じて ノートPCでもコードを書けるようにする
- public に置くことで「見られても恥ずかしくないコードを書く」ための 修行環境にする

この README は、このリポジトリの概要と、個人開発での運用方法をメモする目的で書いています。
## 運用方法（Submodule運用）
このリポジトリは、private の Unity プロジェクト側に Git submodule として取り込み、開発に利用します。
## 更新手順
- submodule を初めて使うので更新のずれに気を付ける。
- submodule 側（このリポジトリ）で pull / 変更 → commit → push
- private Unity プロジェクト側の親リポジトリで submodule の更新を反映（commit）
### ポイント：
- submodule 内で更新しただけでは、親リポジトリ側には反映されません
- 親リポジトリは「submodule の参照先コミット」が更新されるので、親側でも必ずコミットします
## ディレクトリ構成（2026/02/02）
<pre>
lookornotlook-scripts/
├── Achievements/
│   ├── AchievementManager.cs
│   ├── AchievementDefinition.cs
│   ├── AchievementRuntimeState.cs
│   ├── AchievementGridBuilder.cs
│   └── interface/
│       └── IHoverable.cs
│
├── Audio/
│   ├── ISfxService.cs
│   ├── SfxService.cs
│   ├── SfxConfig.cs
│   └── SfxSource.cs
│
├── Common/
│   ├── AsyncMB.cs
│   └── BillboardToCamera.cs
│
├── Composition/
│   └── GameLifetimeScope.cs
│
├── Debug/
│   └── GazeDebugView.cs
│
├── Game/
│   ├── GameManager.cs
│   ├── GameLoop.cs
│   ├── GameController.cs
│   ├── GameInputCoordinator.cs
│   ├── ResultFlow.cs
│   ├── State/
│   │   ├── GamePhase.cs
│   │   ├── GameStateService.cs
│   │   └── IGameStateService.cs
│   ├── Timer/
│   │   ├── ITimerService.cs
│   │   └── TimerService.cs
│   └── Overheat/
│       ├── IOverheatService.cs
│       └── OverheatService.cs
│
├── Gaze/
│   ├── GazeManager.cs
│   ├── GazeTarget.cs
│   ├── GazeRayVisualizer.cs
│   └── GazeReticleSdfView.cs
│
├── Item/
│   ├── ItemDefinition.cs
│   ├── ItemSpawner.cs
│   ├── ItemProgress.cs
│   ├── ItemSlot.cs
│   └── BoardCleaner.cs
│
├── Logic/
│   ├── IScoreService.cs
│   ├── ScoreService.cs
│   ├── IAchievementService.cs
│   ├── AchievementService.cs
│   └── SeeingLogic.cs
│
├── Reaction/
│   └── ItemReaction.cs
│
├── Save/
│   ├── ISaveService.cs
│   ├── PlayerPrefsSaveService.cs
│   └── SaveCoordinator.cs
│
├── Session/
│   └── GameSession.cs
│
└── UI/
    ├── Achievement/
    │   ├── AchievementPresenter.cs
    │   └── AchievementView.cs
    ├── Result/
    │   ├── ResultPresenter.cs
    │   └── ResultView.cs
    ├── Title/
    │   ├── TitlePresenter.cs
    │   └── TitleView.cs
    ├── ScorePresenter.cs
    ├── TimePresenter.cs
    ├── ProgressBarView.cs
    └── UIHoverForwarder.cs
</pre>

## 各フォルダの説明
... 後ほど書いていく
