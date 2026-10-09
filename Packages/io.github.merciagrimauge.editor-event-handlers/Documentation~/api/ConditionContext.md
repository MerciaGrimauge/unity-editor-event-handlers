# `ConditionContext`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

判定中の入力・バッチ番号・期限です。

## 定義

```csharp
public sealed class ConditionContext
```

評価中の Editor メインスレッドでだけ使ってください。後の処理のために保持しないでください。

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public EditorChange Change { get; }](#member-db4f61621513) | 評価対象の入力変更です。 |
| [public long BatchId { get; }](#member-62a4a3a7a4ed) | 重複排除に使う一時的なバッチ番号です。永続的な識別子ではありません。 |
| [public TimeSpan TimeLimit { get; }](#member-2be2d9f3b629) | この呼び出しの開始時に保持した、個別の期限です。 |
| [public TimeSpan Elapsed { get; }](#member-989c9754c54b) | このコンテキストの作成からの経過時間です。 |
| [public void CheckDeadline()](#member-802010a3c07f) | Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。 |

<a id="member-db4f61621513"></a>

## `Change`

```csharp
public EditorChange Change { get; }
```

評価対象の入力変更です。

<a id="member-62a4a3a7a4ed"></a>

## `BatchId`

```csharp
public long BatchId { get; }
```

重複排除に使う一時的なバッチ番号です。永続的な識別子ではありません。

<a id="member-2be2d9f3b629"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

この呼び出しの開始時に保持した、個別の期限です。

<a id="member-989c9754c54b"></a>

## `Elapsed`

```csharp
public TimeSpan Elapsed { get; }
```

このコンテキストの作成からの経過時間です。

<a id="member-802010a3c07f"></a>

## `CheckDeadline`

```csharp
public void CheckDeadline()
```

Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | 別スレッド、または条件評価の終了後に呼び出した場合です。 |
| `EditorEventHandlers.Editor.ConditionDeadlineExceededException` | 経過時間が呼び出しの期限に達した場合です。 |

## 使用上の注意

管理側が判定ごとに生成するsealed classです。公開コンストラクターはありません。


コンテキストを後の通知へ持ち越さないでください。判定終了後のCheckDeadlineは拒否します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
