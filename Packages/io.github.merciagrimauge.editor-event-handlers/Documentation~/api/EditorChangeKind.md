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
| [None = 0](#member-84cb4ed6a844) | 変更の種類を指定しない値です。条件の変更フラグとしては登録できません。 |
| [Created = 1](#member-5f71f985ed09) | GameObject の階層が作成されました。 |
| [ParentChanged = 2](#member-cf96fa0396b0) | GameObject の親または所属シーンが変更されました。 |
| [PropertiesChanged = 4](#member-544472d517b9) | GameObject または Component のプロパティが変更されました。 |
| [StructureChanged = 8](#member-721160c562df) | GameObject のコンポーネント構成が変更されました。 |
| [HierarchyChanged = 16](#member-17b2ee79ed89) | GameObject の階層構造が変更されました。 |
| [ChildrenReordered = 32](#member-500cfd21a724) | 子オブジェクトの順序が変更されました。 |
| [Destroyed = 64](#member-5f01b0e58280) | GameObject の階層が破棄されました。 |
| [PrefabUpdated = 128](#member-b38825d75834) | Prefab インスタンスが更新されました。 |

<a id="member-84cb4ed6a844"></a>

## `None`

```csharp
None = 0
```

変更の種類を指定しない値です。条件の変更フラグとしては登録できません。

<a id="member-5f71f985ed09"></a>

## `Created`

```csharp
Created = 1
```

GameObject の階層が作成されました。

<a id="member-cf96fa0396b0"></a>

## `ParentChanged`

```csharp
ParentChanged = 2
```

GameObject の親または所属シーンが変更されました。

<a id="member-544472d517b9"></a>

## `PropertiesChanged`

```csharp
PropertiesChanged = 4
```

GameObject または Component のプロパティが変更されました。

<a id="member-721160c562df"></a>

## `StructureChanged`

```csharp
StructureChanged = 8
```

GameObject のコンポーネント構成が変更されました。

<a id="member-17b2ee79ed89"></a>

## `HierarchyChanged`

```csharp
HierarchyChanged = 16
```

GameObject の階層構造が変更されました。

<a id="member-500cfd21a724"></a>

## `ChildrenReordered`

```csharp
ChildrenReordered = 32
```

子オブジェクトの順序が変更されました。

<a id="member-5f01b0e58280"></a>

## `Destroyed`

```csharp
Destroyed = 64
```

GameObject の階層が破棄されました。

<a id="member-b38825d75834"></a>

## `PrefabUpdated`

```csharp
PrefabUpdated = 128
```

Prefab インスタンスが更新されました。

## 使用上の注意

`[Flags]` enumです。入力の `EditorChange.Kind` は単一種類、条件の `Changes` には組み合わせを指定します。


通知種類はユーザー入力の発生元を表しません。他ツールのUndo対応変更も届き得ます。SceneOpened/SceneLoadedの通知、シーン全体の初期走査はありません。

## 関連資料

[API一覧](../API_REFERENCE.md) / [実行契約](../CONTRACTS.md) / [ハンドラーガイド](../HANDLER_AUTHORING.md) / [条件ガイド](../CONDITION_AUTHORING.md)
