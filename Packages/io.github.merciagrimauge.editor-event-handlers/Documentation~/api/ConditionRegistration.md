# `ConditionRegistration`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

条件登録の状態・解除トークンです。

## 定義

```csharp
public sealed class ConditionRegistration : IDisposable
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public string Id { get; }](#member-2f04af916893) | Identifier captured at registration. |
| [public Type EventType { get; }](#member-42254476742f) | Exact notification type produced by this condition. |
| [public EditorChangeKind Changes { get; }](#member-fcaf429032a1) | Change categories captured at registration. |
| [public TimeSpan TimeLimit { get; }](#member-30d93ae2400a) | Current user-configured limit; each invocation captures its starting value. |
| [public bool IsDisposed { get; }](#member-777c2705164a) | Whether the condition has been explicitly removed. |
| [public bool IsEnabled { get; }](#member-c18aee63ed88) | Whether user settings and fault policy currently enable the condition. |
| [public string DisabledReason { get; }](#member-cb89bf583b82) | Reason for disabling; initially empty for an enabled condition. |
| [public TimeSpan LastDuration { get; }](#member-733c4dff35b9) | Most recent evaluation duration; zero before first evaluation. |
| [public void Dispose()](#member-158e3eb82bbe) | Removes this condition and releases its slot without removing its subscribers; repeated calls do nothing. |

<a id="member-2f04af916893"></a>

## `Id`

```csharp
public string Id { get; }
```

Identifier captured at registration.

<a id="member-42254476742f"></a>

## `EventType`

```csharp
public Type EventType { get; }
```

Exact notification type produced by this condition.

<a id="member-fcaf429032a1"></a>

## `Changes`

```csharp
public EditorChangeKind Changes { get; }
```

Change categories captured at registration.

<a id="member-30d93ae2400a"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

Current user-configured limit; each invocation captures its starting value.

<a id="member-777c2705164a"></a>

## `IsDisposed`

```csharp
public bool IsDisposed { get; }
```

Whether the condition has been explicitly removed.

<a id="member-c18aee63ed88"></a>

## `IsEnabled`

```csharp
public bool IsEnabled { get; }
```

Whether user settings and fault policy currently enable the condition.

<a id="member-cb89bf583b82"></a>

## `DisabledReason`

```csharp
public string DisabledReason { get; }
```

Reason for disabling; initially empty for an enabled condition.

<a id="member-733c4dff35b9"></a>

## `LastDuration`

```csharp
public TimeSpan LastDuration { get; }
```

Most recent evaluation duration; zero before first evaluation.

<a id="member-158e3eb82bbe"></a>

## `Dispose`

```csharp
public void Dispose()
```

Removes this condition and releases its slot without removing its subscribers; repeated calls do nothing.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

## 使用上の注意

公開コンストラクターはありません。`Id:string`、`EventType:Type`、`Changes:EditorChangeKind`、`TimeLimit:TimeSpan`、`IsDisposed:bool`、`IsEnabled:bool`、`DisabledReason:string`、`LastDuration:TimeSpan` を読み取れます。LastDurationは初回判定前はゼロです。

`void Dispose()` は条件を解除して枠を解放します。メインスレッド専用で、別スレッドならInvalidOperationExceptionです。同じトークンの再Disposeは何もしません。対応する購読を解除せず、待機へ移します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
