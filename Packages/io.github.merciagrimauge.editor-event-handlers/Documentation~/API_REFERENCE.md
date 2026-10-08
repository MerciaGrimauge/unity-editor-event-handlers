# API reference

Unity Editorで条件を登録し、型付き通知を購読するためのAPIです。型名からメンバー、引数、戻り値、例外を参照できます。すべてEditor専用です。

- 初めて使う場合: [クイックスタート](QUICKSTART.md)
- ハンドラーを実装する場合: [イベントハンドラーガイド](HANDLER_AUTHORING.md)
- 独自条件を提供する場合: [条件プロバイダーガイド](CONDITION_AUTHORING.md)
- 配送・期限・設定保存・復元の仕様: [実行契約とライフサイクル](CONTRACTS.md)
- ブラウザーで検索する場合: 同じフォルダーの[APIブラウザー](index.html)をダウンロードして開きます。外部への接続は不要です。

## 役割

| 役割 | 英語名 | API・実装 |
|---|---|---|
| 条件プロバイダー | Condition Provider | `IEventCondition<TEvent>`を実装し、入力を判定して通知と編集範囲を返す |
| イベントディスパッチャー | Event Dispatcher | 登録・判定結果の共有・配送・実行ポリシーを管理。公開入口は`EditorEvents`、内部実装は`EventDispatcher` |
| イベントハンドラー | Event Handler | `IEventHandler<TEvent>`を実装し、通知を購読して処理結果を返す |

通知型と購読トークンは別の概念です。通知型は配送先を、トークンは登録の状態と解除を表します。ディスパッチャーは公開の継承・差し替え対象ではありません。

## 名前空間・アセンブリ

| 名前空間 / asmdef参照名 | パッケージ |
|---|---|
| `EditorEventHandlers.Editor` | `io.github.merciagrimauge.editor-event-handlers` |
| `AvatarPlacement.Editor` | `io.github.merciagrimauge.avatar-placement` |

登録・解除・判定・実行・Unity参照の解決はEditorメインスレッドで行います。期限と有効状態の変更は管理ウィンドウの操作です。登録APIのオプションには含めません。

## 登録と購読

| 型 | 概要 |
|---|---|
| [EditorEvents](api/EditorEvents.md) | 条件登録・単一購読・AND/OR購読の入口 |
| [SubscriptionMode](api/SubscriptionMode.md) | 単一・全条件一致・いずれか一致の購読モード |
| [CompositeEvent](api/CompositeEvent.md) | AND/ORで選択した条件の通知を型ごとに取得 |
| [EventSubscription](api/EventSubscription.md) | 購読の状態・直近結果・解除トークン |
| [ConditionRegistration](api/ConditionRegistration.md) | 条件登録の状態・解除トークン |

## 条件プロバイダー

| 型 | 概要 |
|---|---|
| [`IEventCondition<TEvent>`](api/IEventCondition-1.md) | 入力を読み取り、通知と編集範囲を返す条件のインターフェース |
| [ConditionContext](api/ConditionContext.md) | 判定中の入力・バッチ番号・期限 |
| [`ConditionMatch<TEvent>`](api/ConditionMatch-1.md) | 一致した通知値と編集範囲のルート |
| [ConditionDeadlineExceededException](api/ConditionDeadlineExceededException.md) | 条件の協調的な期限超過 |

## イベントハンドラー

| 型 | 概要 |
|---|---|
| [`IEventHandler<TEvent>`](api/IEventHandler-1.md) | 通知を処理して終了状態を返すハンドラーのインターフェース |
| [`HandlerContext<TEvent>`](api/HandlerContext-1.md) | 型付き通知と、基底型から継承する編集API |
| [HandlerContext](api/HandlerContext.md) | 期限の確認・編集範囲・Undo対応の編集API |
| [HandlerResult](api/HandlerResult.md) | 成功・スキップ・失敗・キャンセルの終了結果 |
| [HandlerStatus](api/HandlerStatus.md) | ハンドラーの終了状態 |
| [HandlerDeadlineExceededException](api/HandlerDeadlineExceededException.md) | ハンドラーの協調的な期限超過 |

## Unity変更と識別子

| 型 | 概要 |
|---|---|
| [EditorChange](api/EditorChange.md) | Unity変更の種類・識別子・現在の対象参照 |
| [EditorChangeKind](api/EditorChangeKind.md) | 条件が必要とするUnity通知種類のフラグ |
| [EditorObjectId](api/EditorObjectId.md) | Unityのバージョン差を吸収する一時的なオブジェクト識別子 |
| [EditorSceneId](api/EditorSceneId.md) | Unityのバージョン差を吸収する一時的なシーン識別子 |

## 標準アバター通知

| 型 | 概要 |
|---|---|
| [AvatarPlaced](../../io.github.merciagrimauge.avatar-placement/Documentation~/api/AvatarPlaced.md) | Descriptorを持つアバターPrefabの新規配置通知 |
| [AvatarChildBreastBlendShapesPlaced](../../io.github.merciagrimauge.avatar-placement/Documentation~/api/AvatarChildBreastBlendShapesPlaced.md) | アバター直下の対象から検出した胸シェイプ名の通知 |
| [BreastBlendShape](../../io.github.merciagrimauge.avatar-placement/Documentation~/api/BreastBlendShape.md) | Renderer・Mesh・インデックス・名前の検出結果 |
