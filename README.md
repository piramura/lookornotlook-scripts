# このリポジトリについて

このリポジトリは、個人開発している Unity VR ゲーム LookOrNotLook の C# Scripts 部分を切り出したものです。

## 目的

- Unity プロジェクト全体（アセットや依存関係）を公開せずに、コードだけをどこでも閲覧できる状態にする
- 他の人にレビューを依頼しやすくする（リンクひとつで見てもらえる）
- 開発環境がノートPC→デスクトップPC中心に移ったが、必要に応じて ノートPCでもコードを書けるようにする
- public に置くことで「見られても恥ずかしくないコードを書く」ための 修行環境にする

この README は、このリポジトリの概要と、個人開発での運用方法をメモする目的で書いています。

## Claude Code セッション

Claude Code で作業する際は [`CLAUDE.md`](./CLAUDE.md) を参照してください。作業分類・コミット手順・TODO フォーマット・アーキテクチャ原則がまとまっています。

## 運用方法（Submodule運用）

このリポジトリは、private の Unity プロジェクト側に Git submodule として取り込み、開発に利用します。

## 更新手順

- submodule を初めて使うので更新のずれに気を付ける。
- submodule 側（このリポジトリ）で pull / 変更 → commit → push
- private Unity プロジェクト側の親リポジトリで submodule の更新を反映（commit）

### ポイント：

- submodule 内で更新しただけでは、親リポジトリ側には反映されません
- 親リポジトリは「submodule の参照先コミット」が更新されるので、親側でも必ずコミットします
- C# script 本体の source of truth はこの repo。private repo 側に script を移動しない
- Unity を private 側で開くと `.meta` が生成される。必要なら**この repo 側**で管理する

## アーキテクチャ概要

Layer 依存方向（一方向のみ許可）:

```
Presentation → GameFlow → Logic → Input → Infrastructure
```

詳細は [`overall.md`](./overall.md) を参照。

## ディレクトリ構成

<pre>
lookornotlook-scripts/
├── Achievements/               # 実績システム（定義・状態管理・UI構築）
│   ├── AchievementCell.cs
│   ├── AchievementDefinition.cs
│   ├── AchievementGridBuilder.cs
│   ├── AchievementManager.cs
│   ├── AchievementRuntimeState.cs
│   ├── AchievementTestDriver.cs
│   └── interface/
│       └── IHoverable.cs
│
├── Audio/                      # SEサービス（Interface + 実装）
│   ├── ISfxService.cs
│   ├── SfxConfig.cs
│   ├── SfxService.cs
│   └── SfxSource.cs
│
├── Common/                     # 汎用ユーティリティ（ドメイン依存なし）
│   ├── AsyncMB.cs
│   └── BillboardToCamera.cs
│
├── Composition/                # DI Composition Root（VContainer）
│   └── GameLifetimeScope.cs
│
├── Debug/                      # デバッグ表示ツール
│   └── GazeDebugView.cs
│
├── Game/                       # ゲーム進行のオーケストレーション
│   ├── GameLoop.cs             # フレームティック処理とフェーズ遷移ファサード（ITickable）
│   ├── GameManager.cs          # 設定値コンテナ（ItemPool, RefreshRadius等）
│   ├── GameInputCoordinator.cs # フェーズ別の入力ルーティング
│   ├── GamePhaseController.cs  # フェーズ遷移シーケンス（IStartable, IGamePhaseController）
│   ├── IGamePhaseController.cs # フェーズ遷移 interface
│   ├── ItemCollectFlow.cs      # 非同期収集フロー（ロック・ガード・演出・確定）
│   ├── IItemCollectFlow.cs     # 収集フロー interface
│   ├── FocusTracker.cs         # フォーカスキャッシュと変化検知（IFocusTracker）
│   ├── IFocusTracker.cs        # フォーカス追跡 interface
│   ├── CollectGuard.cs         # 収集確定前の安全ガード（static internal）
│   ├── BoardSlotManager.cs     # 盤面スロット管理（IBoardSlotManager）
│   ├── IBoardSlotManager.cs    # スロット管理 interface
│   ├── BoardPlacerToPlayer.cs  # プレイヤーに向けてボードを配置（IBoardPlacerToPlayer）
│   ├── IBoardPlacerToPlayer.cs # ボード配置 interface
│   ├── ItemSelectionPolicy.cs  # Overheat 込みアイテム選択ロジック
│   ├── ResultFlow.cs           # リザルト画面の遷移フロー
│   ├── TimeUpSfxCoordinator.cs # タイムアップ時のSE制御
│   ├── State/                  # ゲームフェーズ状態管理
│   │   ├── GamePhase.cs
│   │   ├── IGameStateService.cs
│   │   └── GameStateService.cs
│   ├── Timer/                  # ゲームタイマー
│   │   ├── ITimerService.cs
│   │   └── TimerService.cs
│   └── Overheat/               # コンボ連続取得による難易度変調
│       ├── IOverheatService.cs
│       └── OverheatService.cs
│
├── Gaze/                       # 視線入力の処理（低レイヤー）
│   ├── GazeDebugState.cs
│   ├── GazeManager.cs          # レイキャスト + 注視検出
│   ├── GazeRayVisualizer.cs
│   ├── GazeReticleSdfView.cs
│   ├── GazeTarget.cs
│   └── SeeingLogic.cs          # dwell/ミスマッチ判定（MonoBehaviour）
│
├── Item/                       # 収集アイテムシステム
│   ├── BoardCleaner.cs         # スポーン済みアイテム全削除（IBoardCleaner を同ファイルで定義）
│   ├── CollectableItem.cs
│   ├── ItemCategory.cs
│   ├── ItemDefinition.cs       # ScriptableObject：アイテム設定値
│   ├── ItemLayout.cs
│   ├── ItemProgress.cs         # アイテムごとの注視タイマー
│   ├── ItemSlot.cs
│   └── ItemSpawner.cs
│
├── Logic/                      # ゲームルール・スコア・実績判定（純C#）
│   ├── IAchievementService.cs
│   ├── AchievementService.cs   # 純C#（テスト可）
│   ├── IScoreService.cs
│   └── ScoreService.cs         # 純C#（テスト可）
│
├── Reaction/                   # アイテム取得時のビジュアル反応
│   └── ItemReactiion.cs
│
├── Save/                       # セーブ・ロード（Interface + PlayerPrefs実装）
│   ├── ISaveService.cs
│   ├── PlayerPrefsSaveService.cs
│   └── SaveCoordinator.cs
│
├── Session/                    # 非同期キャンセルのライフタイム管理
│   └── GameSession.cs
│
├── Tests/                      # EditMode テスト（NUnit + Unity Test Framework）
│   └── Editor/
│       ├── Logic/              # ScoreService / AchievementService / OverheatService
│       ├── Game/               # ItemSelectionPolicy / CollectGuard / ItemCollectFlow
│       │                       # FocusTracker / GamePhaseController / GameLoop
│       │                       # GameStateService / TimerService
│       ├── Save/               # SaveCoordinator
│       └── Session/            # GameSession
│
└── UI/                         # MVP パターンによる UI 層
    ├── Achievement/
    │   ├── AchievementPresenter.cs
    │   └── AchievementView.cs
    ├── Result/
    │   ├── ResultPresenter.cs
    │   └── ResultView.cs
    ├── Title/
    │   ├── TitlePresenter.cs
    │   └── TitleView.cs
    ├── ComboPopup.cs
    ├── ComboPopupSpawner.cs
    ├── ProgressBarView.cs
    ├── ScorePresenter.cs
    ├── ScoreView.cs
    ├── TimePresenter.cs
    ├── TimeView.cs
    └── UIHoverForwarder.cs
</pre>
