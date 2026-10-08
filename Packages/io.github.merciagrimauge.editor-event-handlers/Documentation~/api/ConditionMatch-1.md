# `ConditionMatch<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

一致した通知値と編集範囲のルートです。

## 定義

```csharp
public readonly struct ConditionMatch<TEvent>
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public TEvent Event { get; }](#member-1b0989ef308a) | Notification shared with matching subscribers; reference-type notifications must not be null. |
| [public GameObject Root { get; }](#member-ff4c32ce6008) | Existing, ordinary loaded scene hierarchy that subscribers may edit through their contexts. |
| [public ConditionMatch(TEvent notification, GameObject transactionRoot)](#member-b5bfdf949266) | Creates a match value. The dispatcher validates successful matches after evaluation. |

<a id="member-1b0989ef308a"></a>

## `Event`

```csharp
public TEvent Event { get; }
```

Notification shared with matching subscribers; reference-type notifications must not be null.

<a id="member-ff4c32ce6008"></a>

## `Root`

```csharp
public GameObject Root { get; }
```

Existing, ordinary loaded scene hierarchy that subscribers may edit through their contexts.

<a id="member-b5bfdf949266"></a>

## `Constructor`

```csharp
public ConditionMatch(TEvent notification, GameObject transactionRoot)
```

Creates a match value. The dispatcher validates successful matches after evaluation.

### 引数

| 名前 | 説明 |
|---|---|
| `notification` | Notification to deliver. |
| `transactionRoot` | Root of the editable hierarchy; this does not snapshot the entire hierarchy. |

## 使用上の注意

readonly structです。


コンストラクター自体は内容を検証しません。管理側がtrueの結果を受け取った時点で検証します。Rootは通知の対象と同一である必要はありません。たとえば子の追加通知で、編集範囲を親アバターにできます。全階層の保存を意味しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
