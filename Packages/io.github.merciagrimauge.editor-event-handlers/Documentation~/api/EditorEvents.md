# `EditorEvents`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

条件登録・単一購読・AND/OR購読の入口です。

## 定義

```csharp
public static class EditorEvents
```

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public static ConditionRegistration RegisterCondition&lt;TEvent&gt;(IEventCondition&lt;TEvent&gt; condition)](#member-71a21c90ff6e) | 条件登録と購読の合計上限内で、指定した通知型に条件を1件登録します。 |
| [public static EventSubscription Subscribe&lt;TEvent&gt;(IEventHandler&lt;TEvent&gt; handler)](#member-95ae3c191f3d) | 同期的なハンドラーを購読登録します。同じ通知型の有効な条件がない間は待機します。 |
| [public static EventSubscription SubscribeAll(IEventHandler&lt;CompositeEvent&gt; handler, Type[] eventTypes)](#member-0bff08207ebc) | 指定した全条件が同じ入力変更に一致し、同じ編集範囲のルートを返す場合に処理する購読を登録します。 |
| [public static EventSubscription SubscribeAny(IEventHandler&lt;CompositeEvent&gt; handler, Type[] eventTypes)](#member-faa7de1e9c83) | 指定したいずれかの条件が同じ入力変更に一致した場合に処理する購読を登録します。 |

<a id="member-71a21c90ff6e"></a>

## `RegisterCondition<T>`

```csharp
public static ConditionRegistration RegisterCondition<TEvent>(IEventCondition<TEvent> condition)
```

条件登録と購読の合計上限内で、指定した通知型に条件を1件登録します。

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `TEvent` | 通知の型です。型は完全一致で扱います。 |

### 引数

| 名前 | 説明 |
|---|---|
| `condition` | 読み取り専用の条件です。登録時に使うゲッターは軽量な処理にしてください。 |

### 戻り値

登録を解除するトークンと、読み取り専用の条件登録状態です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | condition が null の場合です。 |
| `System.ArgumentException` | 識別子や変更フラグが無効、またはこの通知型にすでに条件が登録されている場合です。 |
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合、または登録数が上限に達した場合です。 |

### 備考

ゲッターの例外は呼び出し元へ伝播します。トークンを手放しても条件登録は解除されません。

<a id="member-95ae3c191f3d"></a>

## `Subscribe<T>`

```csharp
public static EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler)
```

同期的なハンドラーを購読登録します。同じ通知型の有効な条件がない間は待機します。

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `TEvent` | 通知の型です。型は完全一致で扱います。 |

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | 識別子のゲッターが軽量なハンドラーです。 |

### 戻り値

購読を解除するトークンと、読み取り専用の購読状態です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | ハンドラーが null の場合です。 |
| `System.ArgumentException` | 識別子が空白、または同じ通知型ですでに購読登録されている場合です。 |
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合、または登録数が上限に達した場合です。 |

### 備考

識別子のゲッターの例外は呼び出し元へ伝播します。トークンを手放しても購読は解除されません。ハンドラー間の実行順は規定しません。

<a id="member-0bff08207ebc"></a>

## `SubscribeAll`

```csharp
public static EventSubscription SubscribeAll(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
```

指定した全条件が同じ入力変更に一致し、同じ編集範囲のルートを返す場合に処理する購読を登録します。

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | 安定した識別子を持つ、同期的な複合通知ハンドラーです。 |
| `eventTypes` | 型引数が確定した、重複のない通知型を指定順に1〜100件渡します。登録時にコピーします。 |

### 戻り値

購読を解除するトークンと、読み取り専用の購読状態です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | ハンドラーまたは型の配列が null の場合です。 |
| `System.ArgumentException` | 型一覧や識別子が無効、または複合購読の識別子がすでに登録されている場合です。 |
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合、または条件登録と購読の合計が上限に達した場合です。 |

### 備考

必要な全条件が登録済みで有効である必要があります。バッチ内の全条件評価が終わってからハンドラーを実行します。ハンドラー間の実行順は規定しません。

<a id="member-faa7de1e9c83"></a>

## `SubscribeAny`

```csharp
public static EventSubscription SubscribeAny(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
```

指定したいずれかの条件が同じ入力変更に一致した場合に処理する購読を登録します。

### 引数

| 名前 | 説明 |
|---|---|
| `handler` | 安定した識別子を持つ、同期的な複合通知ハンドラーです。 |
| `eventTypes` | 型引数が確定した、重複のない通知型を指定順に1〜100件渡します。登録時にコピーします。 |

### 戻り値

購読を解除するトークンと、読み取り専用の購読状態です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentNullException` | ハンドラーまたは型の配列が null の場合です。 |
| `System.ArgumentException` | 型一覧や識別子が無効、または複合購読の識別子がすでに登録されている場合です。 |
| `System.InvalidOperationException` | Editor のメインスレッド以外で呼び出した場合、または条件登録と購読の合計が上限に達した場合です。 |

### 備考

必要な全条件が登録済みで有効である必要があります。評価は途中で省略しません。結果の選択は最初の一致で止まり、その結果だけを公開します。ハンドラー間の実行順は規定しません。

## 使用上の注意


登録時に実装の `Id` と、条件の `Changes` を読みます。そのゲッターが投げた例外は呼び出し元へ伝播します。登録枠はゲッターを呼ぶ前に予約し、失敗時に解放します。ゲッター内の無限ループを強制停止する機構はありません。ゲッターは定数相当の軽い処理にしてください。

上限は条件プロバイダーの条件とイベントハンドラーの購読の合計100件です。無効・待機中の登録、進行中の登録予約も含みます。上限到達時はゲッターを読まず拒否します。条件1件と、それを購読するハンドラー2件なら3枠を使用します。Dispose後の枠は再利用できます。

型の継承や同じ名前による通知の互換扱いはありません。`IEventCondition<BaseEvent>` は `IEventHandler<DerivedEvent>` を起動しません。型とIDは別の概念で、型が配送先、IDがその登録の設定・失敗記録の識別子です。

複合購読は1件につき1枠です。依存する条件プロバイダーもそれぞれ1枠を使います。通知型一覧は1〜100型で、null要素・重複型・void・参照渡し型・ポインター型・未確定のジェネリック型・`CompositeEvent`を拒否します。通知型一覧は登録時にコピーし、後から渡した配列を編集しても購読の条件は変わりません。AND/ORの混在や入れ子、過去入力との結合は扱いません。単一条件は`Subscribe<TEvent>`で購読します。

複合購読の通知型は `CompositeEvent` です。同じIDの複合購読は、モードや型一覧が異なっていても重複として拒否します。永続設定も `CompositeEvent` とIDで識別するため、解除後に条件構成だけを変えて同じIDで再登録すると設定を引き継ぎます。

ハンドラーの実行順は公開契約に含めません。順序・優先度を指定する引数はありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
