# `EditorChangeKind`

- 名前空間: `EditorEventHandlers.Editor`
- アセンブリ: `EditorEventHandlers.Editor`
- 対象: Unity Editor

条件が必要とするUnity通知種類のフラグです。

## 定義

```csharp
[Flags] public enum EditorChangeKind
```

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [None = 0](#member-84cb4ed6a844) | No category; cannot be registered as a condition's change mask. |
| [Created = 1](#member-5f71f985ed09) | A GameObject hierarchy was created. |
| [ParentChanged = 2](#member-cf96fa0396b0) | A GameObject's parent or scene changed. |
| [PropertiesChanged = 4](#member-544472d517b9) | A GameObject or Component's properties changed. |
| [StructureChanged = 8](#member-721160c562df) | A GameObject's Component structure changed. |
| [HierarchyChanged = 16](#member-17b2ee79ed89) | A GameObject hierarchy's structure changed. |
| [ChildrenReordered = 32](#member-500cfd21a724) | Children were reordered. |
| [Destroyed = 64](#member-5f01b0e58280) | A GameObject hierarchy was destroyed. |
| [PrefabUpdated = 128](#member-b38825d75834) | A prefab instance was updated. |

<a id="member-84cb4ed6a844"></a>

## `None`

```csharp
None = 0
```

No category; cannot be registered as a condition's change mask.

<a id="member-5f71f985ed09"></a>

## `Created`

```csharp
Created = 1
```

A GameObject hierarchy was created.

<a id="member-cf96fa0396b0"></a>

## `ParentChanged`

```csharp
ParentChanged = 2
```

A GameObject's parent or scene changed.

<a id="member-544472d517b9"></a>

## `PropertiesChanged`

```csharp
PropertiesChanged = 4
```

A GameObject or Component's properties changed.

<a id="member-721160c562df"></a>

## `StructureChanged`

```csharp
StructureChanged = 8
```

A GameObject's Component structure changed.

<a id="member-17b2ee79ed89"></a>

## `HierarchyChanged`

```csharp
HierarchyChanged = 16
```

A GameObject hierarchy's structure changed.

<a id="member-500cfd21a724"></a>

## `ChildrenReordered`

```csharp
ChildrenReordered = 32
```

Children were reordered.

<a id="member-5f01b0e58280"></a>

## `Destroyed`

```csharp
Destroyed = 64
```

A GameObject hierarchy was destroyed.

<a id="member-b38825d75834"></a>

## `PrefabUpdated`

```csharp
PrefabUpdated = 128
```

A prefab instance was updated.

## 使用上の注意

`[Flags]` enumです。入力の `EditorChange.Kind` は単一種類、条件の `Changes` には組み合わせを指定します。


通知種類はユーザー入力の発生元を表しません。他ツールのUndo対応変更も届き得ます。SceneOpened/SceneLoadedの通知、シーン全体の初期走査はありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
