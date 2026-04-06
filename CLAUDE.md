# CLAUDE.md — LookOrNotLook Scripts セッションガイド

## このファイルについて

Claude Code セッションで作業する際の前提・制約・ルールをまとめたファイルです。
セッション冒頭に必ず参照してください。

---

## リポジトリの位置づけ（Submodule）

このリポジトリは private Unity プロジェクト `lookornotlook` の Git submodule として `/Assets/LookOrNotLook/Scripts` 以下にマウントされています。

- Namespace 目標ルール: `Piramura.LookOrNotLook.<Domain>[.<Layer>]`
  - 既知の例外: なし（全ファイルが目標ルールに準拠済み）
  - 新規ファイルは目標ルールに従う
- `Composition/GameLifetimeScope.cs` の `RegisterComponentInHierarchy` はシーン側 Prefab に依存するため、このリポジトリ単体では動作確認不可
- Scene / Prefab への参照や SerializedField の実配線は親リポジトリ側で行う
- Unity を private 側で開くと `.meta` ファイルが生成される。`.meta` が必要なら **この repo 側** で commit して管理する
- C# script 本体を private repo 側に移動してはいけない（source of truth はこの repo）

---

## 作業分類（2列ルール）

このリポジトリで TODO や作業提案を出すときは、**必ず以下の2列に分けること**。

| このリポジトリ単独で完結 | 親リポジトリが必要 |
|---|---|
| CLAUDE.md / README.md / overall.md の更新 | Scene / Prefab / SerializedField の配線 |
| フォルダ構成・命名の整理 | GameLifetimeScope の実動作確認 |
| GameLoop の責務分離・リファクタ | 視線入力・UI の PlayMode テスト |
| 純C#ロジックの抽出 | 実機（Quest）での Integration 確認 |
| Interface / サービス境界の設計 | OVR/MetaXR 依存の変更検証 |
| テスト基盤の設計・コード整備 | Unity Test Framework の実行確認 |
| Interface の追加・GameLoop の依存型変更 | DI 登録変更後の実動作確認（GameLifetimeScope） |

### TODO 出力フォーマット

セッション内で TODO を出力するときは必ず次の形式にする:

```markdown
## TODO

| 単独で完結 | 親リポジトリ必要 |
|---|---|
| ... | ... |
```

---

## コミット手順

```
1. このリポジトリ（submodule）で commit & push
2. 親リポジトリ側で submodule 参照を更新して commit
```

> 逆順厳禁：親が古い参照を指したまま進まないように、必ず submodule 側を先にコミットする。

---

## 標準確認手順（compile / Test Runner / scene smoke）

変更の種類によって必要なステップが変わる。

### C# のみ変更した場合（毎回）

1. このリポジトリ（submodule）で commit & push
2. 親リポジトリ側で Unity を開き、**【compile】** エラーがないことを確認
3. **【Test Runner】** EditMode テストを実行し、今回触った範囲が全グリーンであることを確認
4. 問題なければ親リポジトリ側で submodule 参照更新を commit

### Scene / Prefab / DI 登録も変更した場合（上記に追加）

5. **【scene smoke】** `Title → Playing → Result → Retry → Title` を目視確認
6. Meta XR Simulator で視線入力の基本動作を確認（任意・最終確認用）

> Meta XR Simulator は毎回の開発ループで必須にしない。DI 変更や新 Prefab 配線後の最終確認用。

---

## アーキテクチャ原則

詳細は `overall.md` を参照。以下はセッション中の早引き用。

**Layer 依存方向（一方向のみ許可）:**
```
Presentation → GameFlow → Logic → Input → Infrastructure
```

- 逆依存禁止（下位層は上位層を知らない）
- 非同期メソッドは `CancellationToken` を受け取る（**新規・変更コードから適用**。既存の未適用箇所はリファクタ時に対応）
- サービスはすべて Interface を持つ（`IScoreService`, `ITimerService`, `IGameStateService`, `IOverheatService`, `ISfxService`, `IAchievementService`, `ISaveService`, `IGamePhaseController`, `IItemCollectFlow`）
- DI 登録は `Composition/GameLifetimeScope.cs` に集約（分散禁止）
- Namespace 目標ルール: `Piramura.LookOrNotLook.<Domain>[.<Layer>]`（既知例外は上記「リポジトリの位置づけ」参照）

---

## GameLoop の責務分離（完了）

`Game/GameLoop.cs` はかつて収集フロー・フォーカス・フェーズ遷移・後処理を一身に担っていた（技術負債）。
段階的な抽出を経て、現在の `GameLoop` の責務は以下のみ:

- `Tick()` — フェーズゲート / タイムアップ検知 / `focusTracker.Tick()` 呼び出し / `collectFlow.ExecuteAsync()` 起動
- フェーズ遷移ファサード — `StartGameFromTitle`, `RetryFromResult`, `DebugResetToPlaying`, `GoTitleFromResult`

**ctor 依存（5つ）:** `IGamePhaseController` / `IFocusTracker` / `IItemCollectFlow` / `ITimerService` / `IGameStateService`

**GameLoop から抽出済みのクラス:**

| クラス | 責務 |
|---|---|
| `ItemSelectionPolicy` | Overheat 込みアイテム選択 |
| `BoardSlotManager` | 盤面状態管理（スロット・スポーン） |
| `FocusTracker` | フォーカスキャッシュと変化検知 |
| `GamePhaseController` | フェーズ遷移シーケンス（Playing / Result / TitleScreen） |
| `ItemCollectFlow` | 非同期収集フロー（ロック・ガード・演出・確定・後処理） |

---

## テスト基盤の方針

純C#サービスは NUnit + Unity Test Framework (EditMode) でテスト可能。

**作成済みテスト:**

| テストファイル | 対象 | テスト対象の例 |
|---|---|---|
| `Logic/ScoreServiceTests.cs` | `ScoreService` | `Add`, `Reset`, `Changed` イベント |
| `Logic/AchievementServiceTests.cs` | `AchievementService` | 称号判定の境界値 |
| `Logic/OverheatServiceTests.cs` | `OverheatService` | コンボ計算、`ForbiddenChance01` の上下限 |
| `Session/GameSessionTests.cs` | `GameSession` | `BeginNewSession` / `Version` インクリメント |
| `Game/ItemSelectionPolicyTests.cs` | `ItemSelectionPolicy` | Overheat 確率ロジック |
| `Game/CollectGuardTests.cs` | `CollectGuard` | finished / token / timer.IsTimeUp / session.Version の4ガード条件 |
| `Game/ItemCollectFlowTests.cs` | `ItemCollectFlow.PostCommit` | score / achievement / sfx / overheat / comboPopup の副作用分岐 |
| `Game/FocusTrackerTests.cs` | `FocusTracker.GetDwellSpeedMultiplier` | dwell 速度倍率の計算・上下限 |
| `Game/GamePhaseControllerTests.cs` | `GamePhaseController` | 各フェーズ遷移のシーケンスと呼び出し順 |
| `Game/GameLoopTests.cs` | `GameLoop` | フェーズゲート / タイムアップ / finished ガード / ファサード委譲 |
| `Game/GameStateServiceTests.cs` | `GameStateService` | 初期フェーズ / SetPhase / 重複フェーズガード / Changed イベント発火 |

**テスト配置（現状）:**
```
Tests/Editor/
├── Logic/
│   ├── ScoreServiceTests.cs
│   ├── AchievementServiceTests.cs
│   └── OverheatServiceTests.cs
├── Game/
│   ├── ItemSelectionPolicyTests.cs
│   ├── CollectGuardTests.cs
│   ├── ItemCollectFlowTests.cs
│   ├── FocusTrackerTests.cs
│   ├── GamePhaseControllerTests.cs
│   ├── GameLoopTests.cs
│   └── GameStateServiceTests.cs
└── Session/
    └── GameSessionTests.cs
```

**`internal` メンバーのテスト方法:**
`AssemblyInfo.cs`（`Scripts/` 直下）に `[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]` を追加済み。
`internal static` なロジック（`CollectGuard.IsValid` 等）と `internal` なメソッド（`ItemCollectFlow.PostCommit` 等）は EditMode テストから直接呼べる。

**EditMode テストの限界と PlayMode 境界:**
- `GameLoop.Tick()` の条件分岐は interface 経由のため EditMode で確認済み
- `ExecuteAsync` 全体（`GameObject` + `GetComponent` 依存）は PlayMode テスト対象
- `comboPopup` ビジュアル・`ItemReaction` アニメーションは private 側 smoke で確認

> 現在は runtime asmdef を導入せず、`Tests/Editor/` を `Assembly-CSharp-Editor` に載せて運用する。runtime asmdef 導入は別タスク。
> Unity Test Framework パッケージの有効化と Test Runner 実行確認は親リポジトリ側で行う。

---

## コード規約チェックリスト

以下のルールは**新規・変更コードに適用する**。既存の未適用箇所はリファクタ時に個別対応する。

- [ ] `sealed` をデフォルトにする（MonoBehaviour・interface・abstract を除く）
- [ ] 非同期メソッドには `CancellationToken token` 引数
- [ ] `Forget()` を使う場合は理由コメントを付ける
- [ ] `GetComponent` は Tick 外でキャッシュする
- [ ] `Debug.Log` には `[ClassName]` プレフィックスを付ける
