# `AvatarChildBreastBlendShapesPlaced`

- 名前空間: `AvatarPlacement.Editor`
- アセンブリ: `AvatarPlacement.Editor`
- 対象: Unity Editor

アバター直下の対象から検出した胸シェイプ名の通知です。

## 定義

```csharp
public sealed class AvatarChildBreastBlendShapesPlaced
```

名前の一致だけでは、衣装であることやアバターとの互換性を判断できません。Unity 参照の編集には HandlerContext を使ってください。

公開コンストラクターはありません。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public GameObject Avatar { get; }](#member-767b9e7fca4c) | ハンドラーの編集範囲のルートとして選んだ、直上の親アバターです。 |
| [public GameObject PlacedObject { get; }](#member-ec42c4419a59) | 作成または親を変更された直下の子オブジェクトです。Prefab インスタンスである必要はありません。 |
| [public IReadOnlyList&lt;BreastBlendShape&gt; BreastBlendShapes { get; }](#member-f3f06144b0ae) | 読み取り専用の検出結果です。後続のハンドラーが使う前に Unity 参照の状態が変わる場合があります。 |

<a id="member-767b9e7fca4c"></a>

## `Avatar`

```csharp
public GameObject Avatar { get; }
```

ハンドラーの編集範囲のルートとして選んだ、直上の親アバターです。

<a id="member-ec42c4419a59"></a>

## `PlacedObject`

```csharp
public GameObject PlacedObject { get; }
```

作成または親を変更された直下の子オブジェクトです。Prefab インスタンスである必要はありません。

<a id="member-f3f06144b0ae"></a>

## `BreastBlendShapes`

```csharp
public IReadOnlyList<BreastBlendShape> BreastBlendShapes { get; }
```

読み取り専用の検出結果です。後続のハンドラーが使う前に Unity 参照の状態が変わる場合があります。

## 使用上の注意

sealed classです。公開コンストラクターはありません。

標準条件IDは`io.github.merciagrimauge.avatar-placement.child-breast-blend-shapes`です。標準パッケージがSDKのDescriptor型を検出した場合に自動登録し、イベントハンドラーが登録を重ねる必要はありません。


入力はCreatedまたはParentChangedです。対象自身がDescriptorを持つアバターなら除外します。対象はPrefabに限定しません。親変更では現在の親・シーンが通知の変更後の値と一致する必要があります。作成では現在のシーンが通知値と一致する必要があります。Avatar/PlacedObjectは通常の読み込み済みシーンのオブジェクトである必要があります。トランザクションRootはAvatarです。

対象自身とその子階層のSkinnedMeshRendererを、非アクティブを含めて検索します。sharedMeshのブレンドシェイプ名に `breast` がOrdinalIgnoreCaseで部分一致したものを一覧にします。アバター本体やシーン全体は走査しません。頂点・フレームデータの読み取りやウェイト変更はありません。

同じバッチの同一対象は1回だけ検出・通知します。同じMeshの名前走査は1対象の判定中に共有し、判定後にキャッシュを破棄します。シーン読み込み、再コンパイル、後からのMesh交換・ウェイト変更による再通知はありません。アバターPrefabに元から含まれる子の一括探索も行いません。

名前に基づく検出です。衣装であること、変形部位、アバター側との対応・互換性を判定しません。条件実装はこのパッケージのEditorフォルダーにあります。

## 関連資料

[API一覧](../../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
