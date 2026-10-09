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
| [public TEvent Event { get; }](#member-1b0989ef308a) | 一致した購読者間で共有する通知です。参照型の通知には null を指定できません。 |
| [public GameObject Root { get; }](#member-ff4c32ce6008) | 購読者がコンテキストを通じて編集できる、読み込み済みの通常シーンにある既存の階層です。 |
| [public ConditionMatch(TEvent notification, GameObject transactionRoot)](#member-b5bfdf949266) | 一致結果を作成します。一致した結果の妥当性は、評価後にディスパッチャーが確認します。 |

<a id="member-1b0989ef308a"></a>

## `Event`

```csharp
public TEvent Event { get; }
```

一致した購読者間で共有する通知です。参照型の通知には null を指定できません。

<a id="member-ff4c32ce6008"></a>

## `Root`

```csharp
public GameObject Root { get; }
```

購読者がコンテキストを通じて編集できる、読み込み済みの通常シーンにある既存の階層です。

<a id="member-b5bfdf949266"></a>

## コンストラクター

```csharp
public ConditionMatch(TEvent notification, GameObject transactionRoot)
```

一致結果を作成します。一致した結果の妥当性は、評価後にディスパッチャーが確認します。

### 引数

| 名前 | 説明 |
|---|---|
| `notification` | 配送する通知です。 |
| `transactionRoot` | 編集可能な階層のルートです。階層全体の状態を複製するものではありません。 |

## 使用上の注意

読み取り専用の構造体です。


コンストラクター自体は内容を検証しません。管理側がtrueの結果を受け取った時点で検証します。Rootは通知の対象と同一である必要はありません。たとえば子の追加通知で、編集範囲を親アバターにできます。全階層の保存を意味しません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
