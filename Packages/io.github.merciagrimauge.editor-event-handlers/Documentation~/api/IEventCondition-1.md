# `IEventCondition<TEvent>`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

入力を読み取り、通知と編集範囲を返す条件のインターフェースです。

## 定義

```csharp
public interface IEventCondition<TEvent>
```

評価中は Unity オブジェクトの編集、非同期処理、Editor のイベントループへの再入を行わないでください。

実装して登録するインターフェースです。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [string Id { get; }](#member-e3dfb9a780c3) | 空白でない安定した登録識別子です。ゲッターは軽量な処理にしてください。 |
| [EditorChangeKind Changes { get; }](#member-01c05260a6b5) | 評価対象の変更フラグです。None や未対応のフラグは登録時に拒否します。 |
| [bool TryMatch(ConditionContext context, out ConditionMatch&lt;TEvent&gt; match)](#member-e2fd46501259) | Editor のメインスレッドで、対象を編集せずに変更を評価します。 |

<a id="member-e3dfb9a780c3"></a>

## `Id`

```csharp
string Id { get; }
```

空白でない安定した登録識別子です。ゲッターは軽量な処理にしてください。

<a id="member-01c05260a6b5"></a>

## `Changes`

```csharp
EditorChangeKind Changes { get; }
```

評価対象の変更フラグです。None や未対応のフラグは登録時に拒否します。

<a id="member-e2fd46501259"></a>

## `TryMatch`

```csharp
bool TryMatch(ConditionContext context, out ConditionMatch<TEvent> match)
```

Editor のメインスレッドで、対象を編集せずに変更を評価します。

### 引数

| 名前 | 説明 |
|---|---|
| `context` | この呼び出しの入力と、個別の期限です。 |
| `match` | 戻り値が true のときの通知と、編集可能な階層のルートです。 |

### 戻り値

一致した場合は true です。false の場合、出力値は使いません。

## 使用上の注意


条件は同期・読み取り専用です。Unity変更、直接Undo操作、非同期処理、モーダル表示、イベントループ再入を行わないでください。Unity参照を公開するため、読み取り専用性を強制するサンドボックスではありません。

購読者のない条件は評価しません。入力変更1件・条件1件につき1回評価し、同じ結果を購読者へ共有します。全入力の条件評価が終わってからハンドラーを実行します。falseの場合も戻った後に期限を確認します。例外・期限超過・不正な一致結果はその条件を無効化し、対応する購読は待機します。

通知値も複数の購読者で共有します。通知型は読み取り専用の構造体や読み取り専用の一覧などで定義し、ハンドラーへ必要な検出結果を渡してください。Unity参照の存続・状態は実行時に変わり得ます。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
