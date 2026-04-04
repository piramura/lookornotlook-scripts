1. 目的とスコープ
インターン選考で5分以内に“設計できる／遊べる”が伝わるVR視線入力ゲームを完成させる。
2. 体験フローと状態遷移
Title→Playing→Result（Retry/Title）の3状態で、遷移条件は Start / TimeUp or Fail / Retry or Title とする
3. システム構成と責務境界
依存方向は UI → GameState → Logic → Input のみ許可し、逆依存は全面禁止とする。
Inputは視線の生データを提供するだけで、Focus/進行/判定はGameが持つ（Inputは上位層を知らない）
4. 技術選定
1プレイ完走率100%、ソフトロック0、目標FPS維持、非同期は必ずToken管理、依存はInterface経由のみ。

## フォルダ方針（Hybrid）

- Domain（機能）を軸に置き、必要最小限のLayerで分ける
- 依存方向は Presentation → GameFlow → Logic → Input → Infrastructure の順のみ許可
- 逆依存は禁止（下位層は上位層を知らない）
- 外部SDK（OVR/MetaXR/Firebase/Audio/Save）は Infrastructure に隔離
- Common/Shared は最小限（肥大化禁止）

## Layerの責務定義

- Presentation: UI表示とユーザー操作のみ。状態変更は命令でFlowへ渡す
- GameFlow: 状態遷移と進行のオーケストレーション（「いつ何をするか」）
- Logic: ルール・計算・状態更新の中核（Unity/SDK依存を持たない）
- Input: 生の入力を正規化して提供するだけ（上位層を知らない）
- Infrastructure: 外部SDKやI/Oに関する実装

## 依存ルール（明文化）

- UIはLogic/Inputの内部状態を直接変更しない
- LogicはUI/Flowを直接参照しない
- InputとInfrastructureは上位層に依存しない
- Flowは「寿命管理（Token）と遷移」を必ず握る
- 非同期処理は必ず CancellationToken を受け取る
- FlowはInfrastructureを直接参照しない（DIで注入）

## フォルダ配置ルール（運用）
- Features/<Domain>/{Flow,Logic,Presentation,Input} に集約
- Infrastructure/ に外部SDKとI/O
- Composition/ にDIのComposition Rootを集約（分散禁止）
- Shared/ は汎用のみ（ドメイン依存やSDK依存は置かない）
## Namespaceルール（境界一致）
- Rootは Piramura.LookOrNotLook に固定する（目標ルール）
- 次にDomainを付ける
- 必要な場合のみLayerを付与する
- フォルダ構造と完全一致はさせない（移動時のnamespace変更を最小化）
- 既知の例外: なし（全ファイルが目標ルールに準拠済み）

## 実装と方針の乖離メモ（既知の技術負債）

### GameLoop.cs の責務集中
`Game/GameLoop.cs` はボード状態管理・フォーカスキャッシュ・アイテム選択・フェーズ遷移・取得後処理を一手に担っており、上記のレイヤー方針から逸脱している。段階的な責務分離を予定している。分離方針の詳細は `CLAUDE.md` を参照。

### SeeingLogic.cs のレイヤー配置
`Logic/SeeingLogic.cs` は `Logic/` フォルダに配置されているが、`GazeManager` のイベントを購読する `MonoBehaviour` であり、厳密には Input に近い。移動するかどうかは要検討。

