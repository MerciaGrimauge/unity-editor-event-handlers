# `EditorEvents`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

条件登録・単一購読・AND/OR購読の入口です。

## 定義

```csharp
public static class EditorEvents
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public static ConditionRegistration RegisterCondition&lt;TEvent&gt;(IEventCondition&lt;TEvent&gt; condition)](#member-71a21c90ff6e) | Registers one condition for the exact notification type, within the combined registration limit. |
| [public static EventSubscription Subscribe&lt;TEvent&gt;(IEventHandler&lt;TEvent&gt; handler)](#member-95ae3c191f3d) | Subscribes a synchronous handler; it waits while no active condition exists for this exact type. |
| [public static EventSubscription SubscribeAll(IEventHandler&lt;CompositeEvent&gt; handler, Type[] eventTypes)](#member-0bff08207ebc) | Subscribes when all declared conditions match the same input change and editable root. |
| [public static EventSubscription SubscribeAny(IEventHandler&lt;CompositeEvent&gt; handler, Type[] eventTypes)](#member-faa7de1e9c83) | Subscribes when any declared condition matches the same input change. |

<a id="member-71a21c90ff6e"></a>

## `RegisterCondition<T>`

```csharp
public static ConditionRegistration RegisterCondition<TEvent>(IEventCondition<TEvent> condition)
```

Registers one condition for the exact notification type, within the combined registration limit.

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `TEvent` | Exact notification type. |

### 引数

| 名前 | 説明 |
|---|---|
| `condition` | Read-only condition; its registration getters must be lightweight. |

### 戻り値

A removal token and read-only condition state.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | The condition is null. |
| `System.ArgumentException` | The identifier or change flags are invalid, or this type already has a condition. |
| `System.InvalidOperationException` | Called outside the editor main thread or the registration limit has been reached. |

### 備考

Getter exceptions propagate to the caller. Dropping the token does not unregister the condition.

<a id="member-95ae3c191f3d"></a>

## `Subscribe<T>`

```csharp
public static EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler)
```

Subscribes a synchronous handler; it waits while no active condition exists for this exact type.

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `TEvent` | Exact notification type. |

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | Handler with a lightweight identifier getter. |

### 戻り値

A removal token and read-only subscription state.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | The handler is null. |
| `System.ArgumentException` | The identifier is blank or already subscribed for this notification type. |
| `System.InvalidOperationException` | Called outside the editor main thread or the registration limit has been reached. |

### 備考

Identifier getter exceptions propagate. Dropping the token does not remove the subscription. Handler execution order is unspecified.

<a id="member-0bff08207ebc"></a>

## `SubscribeAll`

```csharp
public static EventSubscription SubscribeAll(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
```

Subscribes when all declared conditions match the same input change and editable root.

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | Synchronous composite handler with a stable identifier. |
| `eventTypes` | One to 100 distinct, closed notification types in declaration order; copied at registration. |

### 戻り値

A removal token and read-only subscription state.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | The handler or type array is null. |
| `System.ArgumentException` | Types or identifier are invalid, or the composite identifier is already registered. |
| `System.InvalidOperationException` | Called outside the editor main thread or the combined registration limit has been reached. |

### 備考

Every required condition must be present and enabled. All required evaluations precede all handlers in a batch. Handler execution order is unspecified.

<a id="member-faa7de1e9c83"></a>

## `SubscribeAny`

```csharp
public static EventSubscription SubscribeAny(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
```

Subscribes when any declared condition matches the same input change.

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | Synchronous composite handler with a stable identifier. |
| `eventTypes` | One to 100 distinct, closed notification types in declaration order; copied at registration. |

### 戻り値

A removal token and read-only subscription state.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | The handler or type array is null. |
| `System.ArgumentException` | Types or identifier are invalid, or the composite identifier is already registered. |
| `System.InvalidOperationException` | Called outside the editor main thread or the combined registration limit has been reached. |

### 備考

Every required condition must be present and enabled. Evaluations do not short-circuit; result selection stops at the first match and exposes only that result. Handler execution order is unspecified.

## 使用上の注意


登録時に実装の `Id` と、条件の `Changes` を読みます。そのgetterが投げた例外は呼び出し元へ伝播します。登録枠はgetterを呼ぶ前に予約し、失敗時に解放します。getter内の無限ループを強制停止する機構はありません。getterは定数相当の軽い処理にしてください。

上限は条件プロバイダーの条件とイベントハンドラーの購読の合計100件です。無効・待機中の登録、進行中の登録予約も含みます。上限到達時はgetterを読まず拒否します。条件1件と、それを購読するハンドラ2件なら3枠を使用します。Dispose後の枠は再利用できます。

型の継承や同じ名前による通知の互換扱いはありません。`IEventCondition<BaseEvent>` は `IEventHandler<DerivedEvent>` を起動しません。型とIDは別の概念で、型が配送先、IDがその登録の設定・失敗記録の識別子です。

複合購読は1件につき1枠です。依存する条件プロバイダーもそれぞれ1枠を使います。通知型一覧は1〜100型で、null要素・重複型・void・参照渡し型・ポインター型・未確定のgeneric型・`CompositeEvent`を拒否します。通知型一覧は登録時にコピーし、後から渡した配列を編集しても購読の条件は変わりません。AND/ORの混在や入れ子、過去入力との結合は扱いません。単一条件は`Subscribe<TEvent>`で購読します。

複合購読の通知型は `CompositeEvent` です。同じIDの複合購読は、モードや型一覧が異なっていても重複として拒否します。永続設定も `CompositeEvent` とIDで識別するため、解除後に条件構成だけを変えて同じIDで再登録すると設定を引き継ぎます。

ハンドラーの実行順は公開契約に含めません。順序・優先度を指定する引数はありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
