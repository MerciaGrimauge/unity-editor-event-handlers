# `HandlerContext`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

期限の確認・編集範囲・Undo対応の編集APIです。

## 定義

```csharp
public abstract class HandlerContext
```

Do not reuse after execution, perform direct Unity writes, or use delayed edits. Tracked Undo is not a full hierarchy snapshot. Unity and edit-action exceptions can propagate.

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public GameObject Root { get; }](#member-ede288ee2642) | Condition-selected root of the editable scene hierarchy; inspect it and use context methods for edits. |
| [public TimeSpan TimeLimit { get; }](#member-e1ec18c3ad2d) | Independent execution limit captured at invocation start. |
| [public TimeSpan Elapsed { get; }](#member-35985a8812d5) | Time elapsed since context creation; reading it does not extend the deadline. |
| [public void CheckDeadline()](#member-1c4cd4889a28) | Checks the editor thread, invocation lifetime, and cooperative deadline. |
| [public Component AddComponent(GameObject target, Type componentType)](#member-e5937470cfda) | Adds an Undo-tracked Component to a GameObject in the editable hierarchy. |
| [public T AddComponent&lt;T&gt;(GameObject target) where T : Component](#member-0e3f39ab69cd) | Adds an Undo-tracked Component of the specified type to the editable hierarchy. |
| [public void Modify(UnityEngine.Object target, Action edit)](#member-0385478dd0c3) | Records a scoped GameObject or Component, then synchronously edits its serializable properties. |
| [public GameObject CreateChild(string name, GameObject parent = null)](#member-63974b68e1e3) | Creates an Undo-tracked GameObject as a child in the editable scene hierarchy. |
| [public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null)](#member-4f8ca8cdf6f8) | Instantiates a Prefab asset as an Undo-tracked child in Root's scene. |
| [public void SetParent(GameObject child, GameObject parent)](#member-045592dcbb96) | Reparents an Undo-tracked descendant within the editable hierarchy. |
| [public void Destroy(UnityEngine.Object target)](#member-b972d4f48498) | Destroys an Undo-tracked child GameObject or allowed Component within the editable hierarchy. |

<a id="member-ede288ee2642"></a>

## `Root`

```csharp
public GameObject Root { get; }
```

Condition-selected root of the editable scene hierarchy; inspect it and use context methods for edits.

<a id="member-e1ec18c3ad2d"></a>

## `TimeLimit`

```csharp
public TimeSpan TimeLimit { get; }
```

Independent execution limit captured at invocation start.

<a id="member-35985a8812d5"></a>

## `Elapsed`

```csharp
public TimeSpan Elapsed { get; }
```

Time elapsed since context creation; reading it does not extend the deadline.

<a id="member-1c4cd4889a28"></a>

## `CheckDeadline`

```csharp
public void CheckDeadline()
```

Checks the editor thread, invocation lifetime, and cooperative deadline.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread or after the invocation ended. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | Elapsed time has reached the execution limit. |

<a id="member-e5937470cfda"></a>

## `AddComponent`

```csharp
public Component AddComponent(GameObject target, Type componentType)
```

Adds an Undo-tracked Component to a GameObject in the editable hierarchy.

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Root or a descendant in the same scene. |
| `componentType` | Type derived from Component. |

### 戻り値

The added Component.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The target is outside the scope or the type is not a Component type. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, lost Root, or Unity could not add the Component. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

<a id="member-0e3f39ab69cd"></a>

## `AddComponent<T>`

```csharp
public T AddComponent<T>(GameObject target) where T : Component
```

Adds an Undo-tracked Component of the specified type to the editable hierarchy.

### 型パラメーター

| 名前 | 説明 |
|---|---|
| `T` | Component type to add. |

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Root or a descendant in the same scene. |

### 戻り値

The added typed Component.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The target is outside the editable scope. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, lost Root, or Unity could not add the Component. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

<a id="member-0385478dd0c3"></a>

## `Modify`

```csharp
public void Modify(UnityEngine.Object target, Action edit)
```

Records a scoped GameObject or Component, then synchronously edits its serializable properties.

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Object whose properties are recorded and edited. |
| `edit` | Action that edits only this target's properties. |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The target is not a scene GameObject or Component within the scope. |
| `System.ArgumentNullException` | The edit action is null. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, or lost Root. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

### 備考

Use the dedicated context methods for creation, destruction, and parenting. Other objects require separate Modify calls.

<a id="member-63974b68e1e3"></a>

## `CreateChild`

```csharp
public GameObject CreateChild(string name, GameObject parent = null)
```

Creates an Undo-tracked GameObject as a child in the editable scene hierarchy.

### 引数

| 名前 | 説明 |
|---|---|
| `name` | Name passed to the new GameObject. |
| `parent` | Root or a descendant; null uses Root. |

### 戻り値

The created child.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The parent is outside the editable scope. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, or lost Root. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

<a id="member-4f8ca8cdf6f8"></a>

## `InstantiatePrefab`

```csharp
public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null)
```

Instantiates a Prefab asset as an Undo-tracked child in Root's scene.

### 引数

| 名前 | 説明 |
|---|---|
| `prefab` | Prefab asset to instantiate. |
| `parent` | Root or a descendant; null uses Root. |

### 戻り値

The created Prefab instance.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The input is not a Prefab asset or the parent is outside the editable scope. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, or lost Root. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

<a id="member-045592dcbb96"></a>

## `SetParent`

```csharp
public void SetParent(GameObject child, GameObject parent)
```

Reparents an Undo-tracked descendant within the editable hierarchy.

### 引数

| 名前 | 説明 |
|---|---|
| `child` | Descendant to move; cannot be Root. |
| `parent` | Root or another descendant; cannot create a parenting cycle. |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | An object is outside the scope, child is Root, or parenting would create a cycle. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, or lost Root. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

<a id="member-b972d4f48498"></a>

## `Destroy`

```csharp
public void Destroy(UnityEngine.Object target)
```

Destroys an Undo-tracked child GameObject or allowed Component within the editable hierarchy.

### 引数

| 名前 | 説明 |
|---|---|
| `target` | Descendant GameObject or Component; Root and Transform components cannot be destroyed. |

### 例外

| 型 | 発生条件 |
|---|---|
| `System.ArgumentException` | The target is outside the scope, Root, or a Transform. |
| `System.InvalidOperationException` | Wrong thread, ended invocation, or lost Root. |
| `EditorEventHandlers.Editor.HandlerDeadlineExceededException` | The invocation deadline is reached before or after editing. |

## 使用上の注意

基底はabstract class、generic型はsealed classです。公開コンストラクターはなく、外部で継承・生成するための入口ではありません。generic型は以下の全APIに加え `TEvent Event` を提供します。


対象はRootまたはその子階層のGameObject/Componentで、Rootと同じシーンにあり、永続アセットではない必要があります。Modifyのeditは記録したtargetのプロパティだけを変更し、作成・削除・親変更には専用APIを使います。別オブジェクトも変更する場合は個別にModifyしてください。

主な例外は以下です。管理側がExecute中の例外を結果へ変換し、追跡済み変更を復元します。


対象の破棄状態によってはUnity由来のMissingReferenceException等が発生します。特定のUnityエラーをすべて固定の例外型へ正規化するAPIではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
