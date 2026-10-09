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
| [Single](#member-5ac4effcc717) | 完全一致で扱う1つの通知型です。 |
| [All](#member-18134acabfe9) | 指定した全条件が一致し、同じ編集範囲のルートを返す必要があります。 |
| [Any](#member-8ae40056d068) | 指定順で最初に一致した条件の通知と編集範囲のルートを使います。 |

<a id="member-5ac4effcc717"></a>

## `Single`

```csharp
Single
```

完全一致で扱う1つの通知型です。

<a id="member-18134acabfe9"></a>

## `All`

```csharp
All
```

指定した全条件が一致し、同じ編集範囲のルートを返す必要があります。

<a id="member-8ae40056d068"></a>

## `Any`

```csharp
Any
```

指定順で最初に一致した条件の通知と編集範囲のルートを使います。

## 使用上の注意

enumです。`Single = 0` は単一条件、`All = 1` は全条件一致、`Any = 2` はいずれか一致を表します。管理側が購読トークンへ設定します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
