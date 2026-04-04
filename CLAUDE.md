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

## Unity 側の最終確認フロー

submodule 側の変更は、**毎回 Play 検証までやるのではなく**、以下の軽い smoke で最終確認する。

1. このリポジトリ（submodule）で commit & push
2. 親リポジトリ側で Unity を開き、compile error がないことを確認
3. `Tests/Editor/` の EditMode テストが Test Runner に表示されることを確認
4. EditMode テストを実行し、少なくとも今回触った範囲のテストが通ることを確認
5. scene smoke として `Title -> Playing -> Result -> Retry -> Title` を一通り確認
6. 問題なければ親リポジトリ側で submodule 参照更新を commit

> Meta XR Simulator は **最終確認用の smoke** として使う。毎回の開発ループで必須にしない。

---

## アーキテクチャ原則

詳細は `overall.md` を参照。以下はセッション中の早引き用。

**Layer 依存方向（一方向のみ許可）:**
```
Presentation → GameFlow → Logic → Input → Infrastructure
```

- 逆依存禁止（下位層は上位層を知らない）
- 非同期メソッドは `CancellationToken` を受け取る（**新規・変更コードから適用**。既存の未適用箇所はリファクタ時に対応）
- サービスはすべて Interface を持つ（`IScoreService`, `ITimerService`, `IGameStateService`, `IOverheatService`, `ISfxService`, `IAchievementService`, `ISaveService`）
- DI 登録は `Composition/GameLifetimeScope.cs` に集約（分散禁止）
- Namespace 目標ルール: `Piramura.LookOrNotLook.<Domain>[.<Layer>]`（既知例外は上記「リポジトリの位置づけ」参照）

---

## GameLoop の責務分離（完了）

`Game/GameLoop.cs` はかつて収集フロー・フォーカス・フェーズ遷移・後処理を一身に担っていた（技術負債）。
段階的な抽出を経て、現在の `GameLoop` の責務は以下のみ:

- `Tick()` — フェーズゲート / タイムアップ検知 / `focusTracker.Tick()` 呼び出し / `collectFlow.ExecuteAsync()` 起動
- フェーズ遷移ファサード — `StartGameFromTitle`, `RetryFromResult`, `DebugResetToPlaying`, `GoTitleFromResult`

**ctor 依存（5つ）:** `GamePhaseController` / `FocusTracker` / `ItemCollectFlow` / `ITimerService` / `IGameStateService`

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

**対象サービス（優先度順）:**

| サービス | テスト対象の例 |
|---|---|
| `Logic/ScoreService.cs` | `Add`, `Reset`, `Changed` イベント |
| `Logic/AchievementService.cs` | 称号判定の境界値 |
| `Game/Overheat/OverheatService.cs` | コンボ計算、`ForbiddenChance01` の上下限 |
| `Session/GameSession.cs` | `BeginNewSession` / `Version` インクリメント |
| `ItemSelectionPolicy`（分離後） | Overheat 確率ロジック |

**テスト配置予定:**
```
Tests/Editor/
├── Logic/
│   ├── ScoreServiceTests.cs
│   ├── AchievementServiceTests.cs
│   └── OverheatServiceTests.cs
├── Game/
│   ├── ItemSelectionPolicyTests.cs
│   └── ItemCollectFlowTests.cs   （Unity依存あり・PlayMode or smoke）
└── Session/
    └── GameSessionTests.cs       （作成済み）
```

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
