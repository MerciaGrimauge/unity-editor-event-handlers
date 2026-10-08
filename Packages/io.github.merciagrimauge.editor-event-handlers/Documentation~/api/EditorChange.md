# `EditorChange`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

Unity変更の種類・識別子・現在の対象参照です。

## 定義

```csharp
public readonly struct EditorChange : IEquatable<EditorChange>
```

Resolved Unity references expose current state for inspection; they may be null. This does not identify the user who caused a change.

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public EditorChangeKind Kind { get; }](#member-8f625f1983eb) | The single category of the input notification. |
| [public EditorObjectId ObjectId { get; }](#member-2ad54d145ab4) | Temporary identity of the notification's target. |
| [public EditorSceneId SceneId { get; }](#member-f5e32bc32e9b) | Notification-time scene identity; for a parent change, the new scene. |
| [public EditorSceneId PreviousSceneId { get; }](#member-c28d6fd26eca) | Previous scene for parent changes; the same as SceneId for destruction; otherwise default. |
| [public EditorObjectId PreviousParentId { get; }](#member-5aaee8265f2f) | Previous parent for parent changes or last parent for destruction; otherwise default. |
| [public EditorObjectId NewParentId { get; }](#member-b46e44022508) | New parent for parent changes; otherwise default. |
| [public UnityEngine.Object Target { get; }](#member-74b60158dc49) | Resolves the current target on the editor main thread, or null if unavailable. |
| [public GameObject GameObject { get; }](#member-b26a07e929b0) | Resolves the target GameObject, or a Component's owning GameObject, on the editor main thread; otherwise null. |
| [public GameObject PreviousParent { get; }](#member-2376e7f1650f) | Resolves the previous parent's current GameObject on the editor main thread, or null. |
| [public GameObject NewParent { get; }](#member-6b7440a7d128) | Resolves the new parent's current GameObject on the editor main thread, or null. |
| [public bool Equals(EditorChange other)](#member-720334c64a6e) | Compares the category and all stored identities without resolving Unity objects. |
| [public override bool Equals(object obj)](#member-7dad5a325d0e) | Overrides System.Object.Equals(object). |
| [public override int GetHashCode()](#member-9df9da68d15f) | Hash of the category and all identities for temporary collections. |

<a id="member-8f625f1983eb"></a>

## `Kind`

```csharp
public EditorChangeKind Kind { get; }
```

The single category of the input notification.

<a id="member-2ad54d145ab4"></a>

## `ObjectId`

```csharp
public EditorObjectId ObjectId { get; }
```

Temporary identity of the notification's target.

<a id="member-f5e32bc32e9b"></a>

## `SceneId`

```csharp
public EditorSceneId SceneId { get; }
```

Notification-time scene identity; for a parent change, the new scene.

<a id="member-c28d6fd26eca"></a>

## `PreviousSceneId`

```csharp
public EditorSceneId PreviousSceneId { get; }
```

Previous scene for parent changes; the same as SceneId for destruction; otherwise default.

<a id="member-5aaee8265f2f"></a>

## `PreviousParentId`

```csharp
public EditorObjectId PreviousParentId { get; }
```

Previous parent for parent changes or last parent for destruction; otherwise default.

<a id="member-b46e44022508"></a>

## `NewParentId`

```csharp
public EditorObjectId NewParentId { get; }
```

New parent for parent changes; otherwise default.

<a id="member-74b60158dc49"></a>

## `Target`

```csharp
public UnityEngine.Object Target { get; }
```

Resolves the current target on the editor main thread, or null if unavailable.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-b26a07e929b0"></a>

## `GameObject`

```csharp
public GameObject GameObject { get; }
```

Resolves the target GameObject, or a Component's owning GameObject, on the editor main thread; otherwise null.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-2376e7f1650f"></a>

## `PreviousParent`

```csharp
public GameObject PreviousParent { get; }
```

Resolves the previous parent's current GameObject on the editor main thread, or null.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-6b7440a7d128"></a>

## `NewParent`

```csharp
public GameObject NewParent { get; }
```

Resolves the new parent's current GameObject on the editor main thread, or null.

### 例外

| 型 | 発生条件 |
|---|---|
| `System.InvalidOperationException` | Called outside the editor main thread. |

<a id="member-720334c64a6e"></a>

## `Equals`

```csharp
public bool Equals(EditorChange other)
```

Compares the category and all stored identities without resolving Unity objects.

### 引数

| 名前 | 説明 |
|---|---|
| `other` | Change to compare. |

### 戻り値

Whether every stored field is equal.

<a id="member-7dad5a325d0e"></a>

## `Equals`

```csharp
public override bool Equals(object obj)
```

System.Object.Equals(object)をオーバーライドし、同じ識別子かを比較します。

<a id="member-9df9da68d15f"></a>

## `GetHashCode`

```csharp
public override int GetHashCode()
```

Hash of the category and all identities for temporary collections.

## 使用上の注意

`readonly struct : IEquatable<EditorChange>`。管理側が作成する変更のスナップショットです。公開コンストラクターはありません。


Target/GameObject/親の解決はメインスレッド専用です。通知時の種類・IDは保存されますが、対象のプロパティを凍結しません。参照先の状態は判定・実行時点のものです。Destroyedの対象自身を通常は解決できません。

`Equals(EditorChange)`、`Equals(object)`、`GetHashCode()` はKindと全識別子を比較します。これが同じ更新までの重複排除の単位です。異なる親遷移は別の入力です。defaultの変更は有効な入力通知ではありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
