# `HandlerStatus`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

ハンドラーの終了状態です。

## 定義

```csharp
public enum HandlerStatus
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [Unspecified](#member-80f6130a2fd4) | No explicit completion was reported. |
| [Succeeded](#member-1230b83d04d0) | The handler completed successfully. |
| [Skipped](#member-b7ea9828303d) | The handler declined the notification without tracked edits. |
| [Failed](#member-e23fa635deb3) | The handler failed; tracked edits are subject to rollback. |
| [Cancelled](#member-210c14ad8134) | The handler cancelled; tracked edits are subject to rollback. |

<a id="member-80f6130a2fd4"></a>

## `Unspecified`

```csharp
Unspecified
```

No explicit completion was reported.

<a id="member-1230b83d04d0"></a>

## `Succeeded`

```csharp
Succeeded
```

The handler completed successfully.

<a id="member-b7ea9828303d"></a>

## `Skipped`

```csharp
Skipped
```

The handler declined the notification without tracked edits.

<a id="member-e23fa635deb3"></a>

## `Failed`

```csharp
Failed
```

The handler failed; tracked edits are subject to rollback.

<a id="member-210c14ad8134"></a>

## `Cancelled`

```csharp
Cancelled
```

The handler cancelled; tracked edits are subject to rollback.

## 使用上の注意

`Unspecified = 0`、`Succeeded = 1`、`Skipped = 2`、`Failed = 3`、`Cancelled = 4`です。ハンドラーは[HandlerResult](HandlerResult.md)のfactoryで明示的な結果を返します。defaultまたは不正な値は失敗として扱います。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
