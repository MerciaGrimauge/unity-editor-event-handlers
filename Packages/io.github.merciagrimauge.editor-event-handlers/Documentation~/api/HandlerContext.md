# `HandlerContext`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

期限の確認・編集範囲・Undo対応の編集APIです。

## 定義

```csharp
public abstract class HandlerContext
```

実行後の再利用、Unity オブジェクトへの直接書き込み、遅延編集を行わないでください。Undo 記録は階層全体の複製ではありません。Unity や編集処理の例外は呼び出し元へ伝播する場合があります。

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public GameObject Root { get; }](#member-ede288ee2642) | 条件が選んだ編集可能なシーン階層のルートです。状態の確認に使い、編集にはコンテキストのメソッドを使ってください。 |
| [public TimeSpan TimeLimit { get; }](#member-e1ec18c3ad2d) | 呼び出し開始時に保持した、個別の実行期限です。 |
| [public TimeSpan Elapsed { get; }](#member-35985a8812d5) | コンテキスト作成からの経過時間です。取得しても期限は延長されません。 |
| [public void CheckDeadline()](#member-1c4cd4889a28) | Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。 |
| [public Component AddComponent(GameObject target, Type componentType)](#member-e5937470cfda) | 編集可能な階層内の GameObject に Component を追加し、Undo に記録します。 |
| [public T AddComponent&lt;T&gt;(GameObject target) where T : Component](#member-0e3f39ab69cd) | 編集可能な階層に指定型の Component を追加し、Undo に記録します。 |
| [public void Modify(UnityEngine.Object target, Action edit)](#member-0385478dd0c3) | 編集範囲内の GameObject または Component を記録し、シリアライズ可能なプロパティを同期的に編集します。 |
| [public GameObject CreateChild(string name, GameObject parent = null)](#member-63974b68e1e3) | 編集可能なシーン階層に子の GameObject を作成し、Undo に記録します。 |
| [public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null)](#member-4f8ca8cdf6f8) | Root のシーンに Prefab アセットを子としてインスタンス化し、Undo に記録します。 |
| [public void SetParent(GameObject child, GameObject parent)](#member-045592dcbb96) | 編集可能な階層内で子孫の親を変更し、Undo に記録します。 |
| [public void Destroy(UnityEngine.Object target)](#member-b972d4f48498) | 編集可能な階層内の子の GameObject または許可された Component を破棄し、Undo に記録します。 |

<a id="member-ede288ee2642"></a>

## `Root`

```csharp
public GameObject Root { get; }
```

条件が選んだ編集可能なシーン階層のルートです。状態の確認に使い、編集にはコンテキストのメソッドを使ってください。

<a id="member-e1ec18c3ad2d"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

呼び出し開始時に保持した、個別の実行期限です。

<a id="member-35985a8812d5"></a>

## `Elapsed`

```csharp
public TimeSpan Elapsed { get; }
```

コンテキスト作成からの経過時間です。取得しても期限は延長されません。

<a id="member-1c4cd4889a28"></a>

## `CheckDeadline`

```csharp
public void CheckDeadline()
```

Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Editor のメインスレッド以外、または呼び出しの終了後に実行した場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 経過時間が実行期限に達した場合です。 |

<a id="member-e5937470cfda"></a>

## `AddComponent`

```csharp
public Component AddComponent(GameObject target, Type componentType)
```

編集可能な階層内の GameObject に Component を追加し、Undo に記録します。

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Root、または同じシーンに属する子孫です。 |
| `componentType` | Component の派生型です。 |

### 戻り値

追加した Component です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 対象が編集範囲外、または指定型が Component の派生型ではない場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、Root の喪失、または Unity が Component を追加できなかった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

<a id="member-0e3f39ab69cd"></a>

## `AddComponent<T>`

```csharp
public T AddComponent<T>(GameObject target) where T : Component
```

編集可能な階層に指定型の Component を追加し、Undo に記録します。

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `T` | 追加する Component の型です。 |

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Root、または同じシーンに属する子孫です。 |

### 戻り値

追加した指定型の Component です。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 対象が編集範囲外の場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、Root の喪失、または Unity が Component を追加できなかった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

<a id="member-0385478dd0c3"></a>

## `Modify`

```csharp
public void Modify(UnityEngine.Object target, Action edit)
```

編集範囲内の GameObject または Component を記録し、シリアライズ可能なプロパティを同期的に編集します。

### 引数

| 名前 | 説明 |
|---|---|
| `target` | プロパティを記録して編集するオブジェクトです。 |
| `edit` | この対象のプロパティだけを編集する処理です。 |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 対象が編集範囲内のシーンにある GameObject または Component ではない場合です。 |
| `System.ArgumentNullException` | 編集処理が null の場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

### 備考

作成・破棄・親子関係の変更には専用のコンテキストメソッドを使ってください。別のオブジェクトを編集する場合は、個別に Modify を呼び出してください。

<a id="member-63974b68e1e3"></a>

## `CreateChild`

```csharp
public GameObject CreateChild(string name, GameObject parent = null)
```

編集可能なシーン階層に子の GameObject を作成し、Undo に記録します。

### 引数

| 名前 | 説明 |
|---|---|
| `name` | 新しい GameObject に付ける名前です。 |
| `parent` | Root またはその子孫です。null を指定すると Root を使います。 |

### 戻り値

作成した子オブジェクトです。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 親が編集範囲外の場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

<a id="member-4f8ca8cdf6f8"></a>

## `InstantiatePrefab`

```csharp
public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null)
```

Root のシーンに Prefab アセットを子としてインスタンス化し、Undo に記録します。

### 引数

| 名前 | 説明 |
|---|---|
| `prefab` | インスタンス化する Prefab アセットです。 |
| `parent` | Root またはその子孫です。null を指定すると Root を使います。 |

### 戻り値

作成した Prefab インスタンスです。

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 入力が Prefab アセットではない場合、または親が編集範囲外の場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

<a id="member-045592dcbb96"></a>

## `SetParent`

```csharp
public void SetParent(GameObject child, GameObject parent)
```

編集可能な階層内で子孫の親を変更し、Undo に記録します。

### 引数

| 名前 | 説明 |
|---|---|
| `child` | 移動する子孫です。Root 自体は指定できません。 |
| `parent` | Root または別の子孫です。親子関係が循環する指定はできません。 |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 対象が編集範囲外、子が Root 自体、または親子関係が循環する場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

<a id="member-b972d4f48498"></a>

## `Destroy`

```csharp
public void Destroy(UnityEngine.Object target)
```

編集可能な階層内の子の GameObject または許可された Component を破棄し、Undo に記録します。

### 引数

| 名前 | 説明 |
|---|---|
| `target` | 子孫の GameObject または Component です。Root と Transform コンポーネントは破棄できません。 |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | 対象が編集範囲外、Root 自体、または Transform の場合です。 |
| `System.InvalidOperationException` | 別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。 |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | 編集の前後で呼び出しの期限に達した場合です。 |

## 使用上の注意

基底型は抽象クラスで、ジェネリック型は継承できないクラスです。公開コンストラクターはなく、外部で継承・生成するための入口ではありません。ジェネリック型は以下の全APIに加え `TEvent Event` を提供します。


対象はRootまたはその子階層のGameObject/Componentで、Rootと同じシーンにあり、永続アセットではない必要があります。Modifyのeditは記録したtargetのプロパティだけを変更し、作成・削除・親変更には専用APIを使います。別オブジェクトも変更する場合は個別にModifyしてください。

主な例外は以下です。管理側がExecute中の例外を結果へ変換し、追跡済み変更を復元します。


対象の破棄状態によってはUnity由来のMissingReferenceException等が発生します。特定のUnityエラーをすべて固定の例外型へ正規化するAPIではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
