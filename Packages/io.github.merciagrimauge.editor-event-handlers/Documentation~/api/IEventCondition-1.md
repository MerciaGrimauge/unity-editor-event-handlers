# `IEventCondition<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

入力を読み取り、通知と編集範囲を返す条件のインターフェースです。

## 定義

```csharp
public interface IEventCondition<TEvent>
```

Do not edit Unity objects, use asynchronous work, or reenter the editor event loop during evaluation.

実装して登録するインターフェースです。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [string Id { get; }](#member-e3dfb9a780c3) | Stable, nonblank registration identifier. Its getter must be lightweight. |
| [EditorChangeKind Changes { get; }](#member-01c05260a6b5) | Required supported change flags; None and unknown flags are rejected at registration. |
| [bool TryMatch(ConditionContext context, out ConditionMatch&lt;TEvent&gt; match)](#member-e2fd46501259) | Evaluates the change on the editor main thread without editing it. |

<a id="member-e3dfb9a780c3"></a>

## `Id`

```csharp
string Id { get; }
```

Stable, nonblank registration identifier. Its getter must be lightweight.

<a id="member-01c05260a6b5"></a>

## `Changes`

```csharp
EditorChangeKind Changes { get; }
```

Required supported change flags; None and unknown flags are rejected at registration.

<a id="member-e2fd46501259"></a>

## `TryMatch`

```csharp
bool TryMatch(ConditionContext context, out ConditionMatch<TEvent> match)
```

Evaluates the change on the editor main thread without editing it.

### 引数

| 名前 | 説明 |
|---|---|
| `context` | This invocation's input and independent deadline. |
| `match` | Notification and editable hierarchy root when the return value is true. |

### 戻り値

True for a match; false leaves the output unused.

## 使用上の注意


条件は同期・読み取り専用です。Unity変更、直接Undo操作、非同期処理、モーダル表示、イベントループ再入を行わないでください。Unity参照を公開するため、読み取り専用性を強制するサンドボックスではありません。

購読者のない条件は評価しません。入力変更1件・条件1件につき1回評価し、同じ結果を購読者へ共有します。全入力の条件評価が終わってからハンドラを実行します。falseの場合も戻った後に期限を確認します。例外・期限超過・不正な一致結果はその条件を無効化し、対応する購読は待機します。

通知値も複数の購読者で共有します。通知型はreadonly structや読み取り専用の一覧などで定義し、ハンドラへ必要な検出結果を渡してください。Unity参照の存続・状態は実行時に変わり得ます。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
