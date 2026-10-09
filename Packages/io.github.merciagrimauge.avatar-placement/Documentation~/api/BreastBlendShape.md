# `BreastBlendShape`

- 名前空間: `AvatarPlacement.Editor`
- アセンブリ: `AvatarPlacement.Editor`
- 対象: Unity Editor

Renderer・Mesh・インデックス・名前の検出結果です。

## 定義

```csharp
public readonly struct BreastBlendShape
```

Renderer と Mesh は現在の Unity オブジェクトへの参照です。保存したインデックスを使う前にメッシュを再確認してください。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public SkinnedMeshRenderer Renderer { get; }](#member-2b0774ca0458) | 検出したブレンドシェイプを使うレンダラーです。後で状態が変わったり破棄されたりする場合があります。 |
| [public Mesh Mesh { get; }](#member-cd6794a606dd) | 検出時の共有メッシュです。状態の確認に使う Unity 参照です。 |
| [public int Index { get; }](#member-5d00e1f7b712) | 検出時のブレンドシェイプのインデックスです。メッシュが変わると無効になる場合があります。 |
| [public string Name { get; }](#member-a5c41d7b322f) | 検出時に保存したブレンドシェイプ名です。 |

<a id="member-2b0774ca0458"></a>

## `Renderer`

```csharp
public SkinnedMeshRenderer Renderer { get; }
```

検出したブレンドシェイプを使うレンダラーです。後で状態が変わったり破棄されたりする場合があります。

<a id="member-cd6794a606dd"></a>

## `Mesh`

```csharp
public Mesh Mesh { get; }
```

検出時の共有メッシュです。状態の確認に使う Unity 参照です。

<a id="member-5d00e1f7b712"></a>

## `Index`

```csharp
public int Index { get; }
```

検出時のブレンドシェイプのインデックスです。メッシュが変わると無効になる場合があります。

<a id="member-a5c41d7b322f"></a>

## `Name`

```csharp
public string Name { get; }
```

検出時に保存したブレンドシェイプ名です。

## 使用上の注意

読み取り専用の構造体です。公開コンストラクターはありません。


すべて読み取り専用です。Renderer/MeshはUnity参照であり、後のハンドラーで変更・破棄される場合があります。Meshを交換・変更すればIndexの意味も変わるため、使用時に必要な状態を確認してください。

## 関連資料

[API一覧](../../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
