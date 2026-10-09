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
| [Unspecified](#member-80f6130a2fd4) | 終了状態が明示されていません。 |
| [Succeeded](#member-1230b83d04d0) | ハンドラーが正常終了しました。 |
| [Skipped](#member-b7ea9828303d) | 記録対象の編集を行わずに、ハンドラーが通知の処理をスキップしました。 |
| [Failed](#member-e23fa635deb3) | ハンドラーが失敗しました。記録した変更は復元の対象です。 |
| [Cancelled](#member-210c14ad8134) | ハンドラーがキャンセルしました。記録した変更は復元の対象です。 |

<a id="member-80f6130a2fd4"></a>

## `Unspecified`

```csharp
Unspecified
```

終了状態が明示されていません。

<a id="member-1230b83d04d0"></a>

## `Succeeded`

```csharp
Succeeded
```

ハンドラーが正常終了しました。

<a id="member-b7ea9828303d"></a>

## `Skipped`

```csharp
Skipped
```

記録対象の編集を行わずに、ハンドラーが通知の処理をスキップしました。

<a id="member-e23fa635deb3"></a>

## `Failed`

```csharp
Failed
```

ハンドラーが失敗しました。記録した変更は復元の対象です。

<a id="member-210c14ad8134"></a>

## `Cancelled`

```csharp
Cancelled
```

ハンドラーがキャンセルしました。記録した変更は復元の対象です。

## 使用上の注意

`Unspecified = 0`、`Succeeded = 1`、`Skipped = 2`、`Failed = 3`、`Cancelled = 4`です。ハンドラーは[HandlerResult](HandlerResult.md)の生成メソッドで明示的な結果を返します。defaultまたは不正な値は失敗として扱います。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
