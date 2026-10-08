# `EventSubscription`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

購読の状態・直近結果・解除トークンです。

## 定義

```csharp
public sealed class EventSubscription : IDisposable
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public string Id { get; }](#member-1bb8bf4ed03d) | Identifier captured at registration. |
| [public Type EventType { get; }](#member-ad6c4d89651d) | Handler payload type; CompositeEvent for an All or Any subscription. |
| [public SubscriptionMode Mode { get; }](#member-2a926cc77565) | Combination mode captured at registration. |
| [public IReadOnlyList&lt;Type&gt; ConditionTypes { get; }](#member-27f4f9333f99) | Immutable condition notification types, in declaration order. |
| [public TimeSpan TimeLimit { get; }](#member-856f94e40421) | Current user-configured limit; each invocation captures its starting value. |
| [public bool IsDisposed { get; }](#member-df1809a6b779) | Whether the subscription has been explicitly removed. |
| [public bool IsEnabled { get; }](#member-336e62e8fb71) | Whether user settings and fault policy currently enable the subscription. |
| [public bool IsActive { get; }](#member-68eeaaf5b414) | Whether enabled, not disposed, and backed by every required active condition. Read on the editor main thread. |
| [public string DisabledReason { get; }](#member-b64ca99fd124) | Reason for disabling; initially empty for an enabled subscription. |
| [public HandlerResult LastResult { get; }](#member-9aba67b54815) | Most recent completion result; Unspecified before the first invocation. |
| [public TimeSpan LastDuration { get; }](#member-ea8f72146cc0) | Most recent invocation duration, excluding transaction finalization; zero before first use. |
| [public void Dispose()](#member-f6917fdae8ca) | Removes this subscription and releases its registration slot; repeated calls do nothing. |

<a id="member-1bb8bf4ed03d"></a>

## `Id`

```csharp
public string Id { get; }
```

Identifier captured at registration.

<a id="member-ad6c4d89651d"></a>

## `EventType`

```csharp
public Type EventType { get; }
```

Handler payload type; CompositeEvent for an All or Any subscription.

<a id="member-2a926cc77565"></a>

## `Mode`

```csharp
public SubscriptionMode Mode { get; }
```

Combination mode captured at registration.

<a id="member-27f4f9333f99"></a>

## `ConditionTypes`

```csharp
public IReadOnlyList<Type> ConditionTypes { get; }
```

Immutable condition notification types, in declaration order.

<a id="member-856f94e40421"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

Current user-configured limit; each invocation captures its starting value.

<a id="member-df1809a6b779"></a>

## `IsDisposed`

```csharp
public bool IsDisposed { get; }
```

Whether the subscription has been explicitly removed.

<a id="member-336e62e8fb71"></a>

## `IsEnabled`

```csharp
public bool IsEnabled { get; }
```

Whether user settings and fault policy currently enable the subscription.

<a id="member-68eeaaf5b414"></a>

## `IsActive`

```csharp
public bool IsActive { get; }
```

Whether enabled, not disposed, and backed by every required active condition. Read on the editor main thread.

<a id="member-b64ca99fd124"></a>

## `DisabledReason`

```csharp
public string DisabledReason { get; }
```

Reason for disabling; initially empty for an enabled subscription.

<a id="member-9aba67b54815"></a>

## `LastResult`

```csharp
public HandlerResult LastResult { get; }
```

Most recent completion result; Unspecified before the first invocation.

<a id="member-ea8f72146cc0"></a>

## `LastDuration`

```csharp
public TimeSpan LastDuration { get; }
```

Most recent invocation duration, excluding transaction finalization; zero before first use.

<a id="member-f6917fdae8ca"></a>

## `Dispose`

```csharp
public void Dispose()
```

Removes this subscription and releases its registration slot; repeated calls do nothing.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

## 使用上の注意

公開コンストラクターはありません。


`void Dispose()` は購読を解除し枠を解放します。メインスレッド専用、別スレッドならInvalidOperationException、再Disposeは何もしません。

管理側が実装と登録を強参照します。トークンを手放しただけでは解除されません。登録はEditorドメイン内の寿命を持ち、InitializeOnLoad等で必要に応じて再登録します。バッチ開始時の登録構成で評価し、新規登録は次バッチから対象です。解除・無効化は後続処理へ反映します。依存条件が解除・無効化・別登録に置き換えられた場合、その条件の保存済み結果は使用しません。Anyでも未選択の依存条件の失効は当該配送を止めます。過去通知は再送しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
