# `IEventHandler<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

通知を処理して終了状態を返すハンドラーのインターフェースです。

## 定義

```csharp
public interface IEventHandler<TEvent>
```

Use context edit methods, and do not schedule delayed Unity edits or reenter the editor event loop.

実装して登録するインターフェースです。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [string Id { get; }](#member-6358b7e39cbb) | Stable, nonblank identifier, unique among subscriptions for this notification type. |
| [HandlerResult Execute(HandlerContext&lt;TEvent&gt; context)](#member-407370ebf925) | Handles the notification on the editor main thread within its independent deadline. |

<a id="member-6358b7e39cbb"></a>

## `Id`

```csharp
string Id { get; }
```

Stable, nonblank identifier, unique among subscriptions for this notification type.

<a id="member-407370ebf925"></a>

## `Execute`

```csharp
HandlerResult Execute(HandlerContext<TEvent> context)
```

Handles the notification on the editor main thread within its independent deadline.

### 引数

| 名前 | 説明 |
|---|---|
| `context` | Notification, scoped editing methods, and invocation deadline. |

### 戻り値

An explicit success, skip, failure, or cancellation result.

## 使用上の注意


async void、バックグラウンドでのUnity編集、終了後の遅延編集、直接Undo操作、イベントループ再入は使用しません。変更にはコンテキストAPIを使います。公開参照への直接書き込みを防ぐ隔離機構はありません。

### `HandlerStatus` / `HandlerResult`

HandlerStatusは `Unspecified = 0, Succeeded = 1, Skipped = 2, Failed = 3, Cancelled = 4` です。HandlerResultはreadonly structで、公開プロパティは `HandlerStatus Status`、`string Message` です。各factoryでnullの理由は空文字へ変換します。


ハンドラの例外・期限超過はFailureへ変換し、`OperationCanceledException` はCancelledへ変換します。復元が成功すれば次の購読を実行できます。復元・確定の例外、または実行後のRoot喪失では全体を即時停止します。

Successを返しても、実行後にRootが失われていればLastResultをFailureへ変更し、そのイベントハンドラーを異常無効として保存します。Context外で破壊されたRootの復元を意味しません。

### `HandlerContext` / `HandlerContext<TEvent>`

基底はabstract class、generic型はsealed classです。公開コンストラクターはなく、外部で継承・生成するための入口ではありません。generic型は以下の全APIに加え `TEvent Event` を提供します。


対象はRootまたはその子階層のGameObject/Componentで、Rootと同じシーンにあり、永続アセットではない必要があります。Modifyのeditは記録したtargetのプロパティだけを変更し、作成・削除・親変更には専用APIを使います。別オブジェクトも変更する場合は個別にModifyしてください。

主な例外は以下です。管理側がExecute中の例外を結果へ変換し、追跡済み変更を復元します。


対象の破棄状態によってはUnity由来のMissingReferenceException等が発生します。特定のUnityエラーをすべて固定の例外型へ正規化するAPIではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
