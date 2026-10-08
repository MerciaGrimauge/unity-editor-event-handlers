# `ConditionContext`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

判定中の入力・バッチ番号・期限です。

## 定義

```csharp
public sealed class ConditionContext
```

Use on the editor main thread during evaluation only; do not retain for later work.

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public EditorChange Change { get; }](#member-db4f61621513) | Input change being evaluated. |
| [public long BatchId { get; }](#member-62a4a3a7a4ed) | Temporary batch number for deduplication; not a persistent identity. |
| [public TimeSpan TimeLimit { get; }](#member-2be2d9f3b629) | Independent deadline captured when this invocation starts. |
| [public TimeSpan Elapsed { get; }](#member-989c9754c54b) | Time elapsed since this context was created. |
| [public void CheckDeadline()](#member-802010a3c07f) | Checks the editor thread, invocation lifetime, and cooperative deadline. |

<a id="member-db4f61621513"></a>

## `Change`

```csharp
public EditorChange Change { get; }
```

Input change being evaluated.

<a id="member-62a4a3a7a4ed"></a>

## `BatchId`

```csharp
public long BatchId { get; }
```

Temporary batch number for deduplication; not a persistent identity.

<a id="member-2be2d9f3b629"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

Independent deadline captured when this invocation starts.

<a id="member-989c9754c54b"></a>

## `Elapsed`

```csharp
public TimeSpan Elapsed { get; }
```

Time elapsed since this context was created.

<a id="member-802010a3c07f"></a>

## `CheckDeadline`

```csharp
public void CheckDeadline()
```

Checks the editor thread, invocation lifetime, and cooperative deadline.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called on another thread or after evaluation ended. |
| `EditorEventHandlers.Editor.ConditionDeadlineExceededException` | Elapsed time has reached the invocation limit. |

## 使用上の注意

管理側が判定ごとに生成するsealed classです。公開コンストラクターはありません。


コンテキストを後の通知へ持ち越さないでください。判定終了後のCheckDeadlineは拒否します。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
