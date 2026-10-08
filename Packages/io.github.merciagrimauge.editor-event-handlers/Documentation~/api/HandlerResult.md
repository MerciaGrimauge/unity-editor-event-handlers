# `HandlerResult`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

成功・スキップ・失敗・キャンセルの終了結果です。

## 定義

```csharp
public readonly struct HandlerResult
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public HandlerStatus Status { get; }](#member-03c844cc7f93) | Explicit completion state; the default value is Unspecified. |
| [public string Message { get; }](#member-c9c6eea44821) | Optional explanation; factory methods normalize null to an empty string. |
| [public static HandlerResult Success()](#member-913735074221) | Reports successful completion. Tracked edits are committed if all execution checks pass. |
| [public static HandlerResult Skip(string reason = null)](#member-7c9e0dd7a368) | Reports no work. A skip after tracked edits is treated as failure. |
| [public static HandlerResult Failure(string reason)](#member-63096106b7e3) | Reports failure; the dispatcher attempts tracked rollback and disables the subscription. |
| [public static HandlerResult Cancel(string reason = null)](#member-69cac07e885f) | Reports cancellation; the dispatcher attempts tracked rollback and disables the subscription. |

<a id="member-03c844cc7f93"></a>

## `Status`

```csharp
public HandlerStatus Status { get; }
```

Explicit completion state; the default value is Unspecified.

<a id="member-c9c6eea44821"></a>

## `Message`

```csharp
public string Message { get; }
```

Optional explanation; factory methods normalize null to an empty string.

<a id="member-913735074221"></a>

## `Success`

```csharp
public static HandlerResult Success()
```

Reports successful completion. Tracked edits are committed if all execution checks pass.

### 戻り値

A successful result.

<a id="member-7c9e0dd7a368"></a>

## `Skip`

```csharp
public static HandlerResult Skip(string reason = null)
```

Reports no work. A skip after tracked edits is treated as failure.

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | Optional explanation. |

### 戻り値

A skipped result.

<a id="member-63096106b7e3"></a>

## `Failure`

```csharp
public static HandlerResult Failure(string reason)
```

Reports failure; the dispatcher attempts tracked rollback and disables the subscription.

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | Failure explanation; null becomes an empty string. |

### 戻り値

A failed result.

<a id="member-69cac07e885f"></a>

## `Cancel`

```csharp
public static HandlerResult Cancel(string reason = null)
```

Reports cancellation; the dispatcher attempts tracked rollback and disables the subscription.

### 引数

| 名前 | 説明 |
|---|---|
| `reason` | Optional explanation. |

### 戻り値

A cancelled result.

## 使用上の注意

HandlerStatusは `Unspecified = 0, Succeeded = 1, Skipped = 2, Failed = 3, Cancelled = 4` です。HandlerResultはreadonly structで、公開プロパティは `HandlerStatus Status`、`string Message` です。各factoryでnullの理由は空文字へ変換します。


ハンドラの例外・期限超過はFailureへ変換し、`OperationCanceledException` はCancelledへ変換します。復元が成功すれば次の購読を実行できます。復元・確定の例外、または実行後のRoot喪失では全体を即時停止します。

Successを返しても、実行後にRootが失われていればLastResultをFailureへ変更し、そのイベントハンドラーを異常無効として保存します。Context外で破壊されたRootの復元を意味しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
