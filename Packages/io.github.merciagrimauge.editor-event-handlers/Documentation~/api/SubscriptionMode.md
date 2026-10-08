# `SubscriptionMode`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

単一・全条件一致・いずれか一致の購読モードです。

## 定義

```csharp
public enum SubscriptionMode
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [Single](#member-5ac4effcc717) | One exact notification type. |
| [All](#member-18134acabfe9) | All declared conditions must match the same editable root. |
| [Any](#member-8ae40056d068) | The first matching declared condition supplies the notification and editable root. |

<a id="member-5ac4effcc717"></a>

## `Single`

```csharp
Single
```

One exact notification type.

<a id="member-18134acabfe9"></a>

## `All`

```csharp
All
```

All declared conditions must match the same editable root.

<a id="member-8ae40056d068"></a>

## `Any`

```csharp
Any
```

The first matching declared condition supplies the notification and editable root.

## 使用上の注意

enumです。`Single = 0` は単一条件、`All = 1` は全条件一致、`Any = 2` はいずれか一致を表します。管理側が購読トークンへ設定します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
