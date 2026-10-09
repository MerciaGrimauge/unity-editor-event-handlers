# `IEventHandler<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

通知を処理して終了状態を返すハンドラーのインターフェースです。

## 定義

```csharp
public interface IEventHandler<TEvent>
```

コンテキストの編集メソッドを使ってください。Unity の遅延編集の予約や、Editor のイベントループへの再入は行わないでください。

実装して登録するインターフェースです。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [string Id { get; }](#member-6358b7e39cbb) | 空白でない安定した識別子です。同じ通知型の購読内で一意にしてください。 |
| [HandlerResult Execute(HandlerContext&lt;TEvent&gt; context)](#member-407370ebf925) | Editor のメインスレッドで、個別の期限内に通知を処理します。 |

<a id="member-6358b7e39cbb"></a>

## `Id`

```csharp
string Id { get; }
```

空白でない安定した識別子です。同じ通知型の購読内で一意にしてください。

<a id="member-407370ebf925"></a>

## `Execute`

```csharp
HandlerResult Execute(HandlerContext<TEvent> context)
```

Editor のメインスレッドで、個別の期限内に通知を処理します。

### 引数

| 名前 | 説明 |
|---|---|
| `context` | 通知、編集範囲を限定した編集メソッド、呼び出しの期限です。 |

### 戻り値

成功・スキップ・失敗・キャンセルのいずれかを明示した結果です。

## 使用上の注意


async void、バックグラウンドでのUnity編集、終了後の遅延編集、直接Undo操作、イベントループ再入は使用しません。変更にはコンテキストAPIを使います。公開参照への直接書き込みを防ぐ隔離機構はありません。

### `HandlerStatus` / `HandlerResult`

HandlerStatusは `Unspecified = 0, Succeeded = 1, Skipped = 2, Failed = 3, Cancelled = 4` です。HandlerResultは読み取り専用の構造体で、公開プロパティは `HandlerStatus Status`、`string Message` です。各生成メソッドは、理由が null の場合に空文字列へ変換します。


ハンドラーの例外・期限超過はFailureへ変換し、`OperationCanceledException` はCancelledへ変換します。復元が成功すれば次の購読を実行できます。復元・確定の例外、または実行後のRoot喪失では全体を即時停止します。

Successを返しても、実行後にRootが失われていればLastResultをFailureへ変更し、そのイベントハンドラーを異常無効として保存します。Context外で破壊されたRootの復元を意味しません。

### `HandlerContext` / `HandlerContext<TEvent>`

基底型は抽象クラスで、ジェネリック型は継承できないクラスです。公開コンストラクターはなく、外部で継承・生成するための入口ではありません。ジェネリック型は以下の全APIに加え `TEvent Event` を提供します。


対象はRootまたはその子階層のGameObject/Componentで、Rootと同じシーンにあり、永続アセットではない必要があります。Modifyのeditは記録したtargetのプロパティだけを変更し、作成・削除・親変更には専用APIを使います。別オブジェクトも変更する場合は個別にModifyしてください。

主な例外は以下です。管理側がExecute中の例外を結果へ変換し、追跡済み変更を復元します。


対象の破棄状態によってはUnity由来のMissingReferenceException等が発生します。特定のUnityエラーをすべて固定の例外型へ正規化するAPIではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
