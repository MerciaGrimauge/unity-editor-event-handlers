# `BreastBlendShape`

- 名前空間: `AvatarPlacement.Editor`
- アセンブリ: `AvatarPlacement.Editor`
- 対象: Unity Editor

Renderer・Mesh・インデックス・名前の検出結果です。

## 定義

```csharp
public readonly struct BreastBlendShape
```

Renderer and Mesh are current Unity references. Recheck the mesh before using its captured index.

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public SkinnedMeshRenderer Renderer { get; }](#member-2b0774ca0458) | Renderer using the detected shape; may later change or be destroyed. |
| [public Mesh Mesh { get; }](#member-cd6794a606dd) | Shared mesh at detection time; a Unity reference for inspection. |
| [public int Index { get; }](#member-5d00e1f7b712) | Blend shape index at detection time; mesh changes can invalidate its meaning. |
| [public string Name { get; }](#member-a5c41d7b322f) | Blend shape name captured at detection time. |

<a id="member-2b0774ca0458"></a>

## `Renderer`

```csharp
public SkinnedMeshRenderer Renderer { get; }
```

Renderer using the detected shape; may later change or be destroyed.

<a id="member-cd6794a606dd"></a>

## `Mesh`

```csharp
public Mesh Mesh { get; }
```

Shared mesh at detection time; a Unity reference for inspection.

<a id="member-5d00e1f7b712"></a>

## `Index`

```csharp
public int Index { get; }
```

Blend shape index at detection time; mesh changes can invalidate its meaning.

<a id="member-a5c41d7b322f"></a>

## `Name`

```csharp
public string Name { get; }
```

Blend shape name captured at detection time.

## 使用上の注意

readonly structです。公開コンストラクターはありません。


すべて読み取り専用です。Renderer/MeshはUnity参照であり、後のハンドラで変更・破棄される場合があります。Meshを交換・変更すればIndexの意味も変わるため、使用時に必要な状態を確認してください。

## 関連資料

[API一覧](../../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
