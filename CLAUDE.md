# CLAUDE.md — LookOrNotLook Scripts セッションガイド

## このファイルについて

Claude Code セッションで作業する際の前提・制約・ルールをまとめたファイルです。
セッション冒頭に必ず参照してください。

---

## リポジトリの位置づけ（Submodule）

このリポジトリは private Unity プロジェクト `lookornotlook` の Git submodule として `/Assets/Scripts/` 以下にマウントされています。

- Namespace 目標ルール: `Piramura.LookOrNotLook.<Domain>[.<Layer>]`
  - 既知の例外: `Session/GameSession.cs` は namespace なし、`Common/AsyncMB.cs` は `Piramura.Common`
  - 新規ファイルは目標ルールに従う。既存例外はリファクタ時に個別判断する
- `Composition/GameLifetimeScope.cs` の `RegisterComponentInHierarchy` はシーン側 Prefab に依存するため、このリポジトリ単体では動作確認不可
- Scene / Prefab への参照や SerializedField の実配線は親リポジトリ側で行う

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

## GameLoop の現状と責務分離方針

`Game/GameLoop.cs` は現在 `IStartable + ITickable` を実装し、以下の責務を一身に担っている（既知の技術負債）:

1. ボード状態管理（`slotObjects`, `freeIndices`）
2. フォーカスキャッシュ（`currentProgress`, `currentCollectable`, `currentReaction`）
3. アイテム選択ロジック（Overheat 確率込み）← 純C#、抽出優先
4. フェーズ遷移エントリポイント（`StartGameFromTitle`, `RetryFromResult`, `EnterResult`, `GoTitleFromResult`）
5. 取得後処理の調停（演出 → スコア確定 → 後処理）

**分離候補クラス（このリポジトリ単独で設計・実装可能）:**

| クラス候補 | 責務 | テスト可否 |
|---|---|---|
| `ItemSelectionPolicy` | `SelectDefinitionWithOverheat` 純C#化 | ○ |
| `FocusTracker` | フォーカスキャッシュと変化検知 | △（MonoBehaviour依存あり） |
| `BoardSlotManager` | `slotObjects`, `freeIndices` 管理 | △（インデックス部分は純C#） |
| `GamePhaseController` | フェーズ遷移メソッド群 | △ |

> `Game/GazeCollectCoordinator.cs` は public 側 `GameLifetimeScope` では未配線。private 側 Scope の有無は確認不可。

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
Tests/EditMode/
├── Logic/
│   ├── ScoreServiceTests.cs
│   ├── AchievementServiceTests.cs
│   └── OverheatServiceTests.cs
├── Game/
│   ├── GameSessionTests.cs
│   └── ItemSelectionPolicyTests.cs
└── LookOrNotLook.Tests.asmdef   ← このリポジトリ側に置く
```

> `LookOrNotLook.Tests.asmdef` はこのリポジトリに配置する（テストコードと同居するため）。Unity Test Framework パッケージの有効化は親リポジトリ側で行う。

---

## コード規約チェックリスト

以下のルールは**新規・変更コードに適用する**。既存の未適用箇所（`GameLoop.cs:221` の `.Forget()` 等）はリファクタ時に個別対応する。

- [ ] `sealed` をデフォルトにする（MonoBehaviour・interface・abstract を除く）
- [ ] 非同期メソッドには `CancellationToken token` 引数
- [ ] `Forget()` を使う場合は理由コメントを付ける
- [ ] `GetComponent` は Tick 外でキャッシュする
- [ ] `Debug.Log` には `[ClassName]` プレフィックスを付ける
