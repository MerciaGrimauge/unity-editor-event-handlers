# `AvatarPlaced`

- 名前空間: `AvatarPlacement.Editor`
- アセンブリ: `AvatarPlacement.Editor`
- 対象: Unity Editor

Descriptorを持つアバターPrefabの新規配置通知です。

## 定義

```csharp
public readonly struct AvatarPlaced
```

Unity 参照は現在の状態の確認に使います。編集には渡された HandlerContext を使ってください。

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public GameObject Avatar { get; }](#member-16809f5bc0cd) | 検出したアバター Prefab インスタンスのルートです。後続のハンドラーが使う前に、状態が変わったり破棄されたりする場合があります。 |
| [public AvatarPlaced(GameObject avatar)](#member-ed6470b05960) | 通知値を作成します。値の検証や通知の送信は行いません。 |

<a id="member-16809f5bc0cd"></a>

## `Avatar`

```csharp
public GameObject Avatar { get; }
```

検出したアバター Prefab インスタンスのルートです。後続のハンドラーが使う前に、状態が変わったり破棄されたりする場合があります。

<a id="member-ed6470b05960"></a>

## コンストラクター

```csharp
public AvatarPlaced(GameObject avatar)
```

通知値を作成します。値の検証や通知の送信は行いません。

### 引数

| 名前 | 説明 |
|---|---|
| `avatar` | 通知値に保存するアバターへの参照です。 |

## 使用上の注意

読み取り専用の構造体です。


標準条件IDは `local.avatar-placement.created`、入力はCreatedです。対象自身に `VRC.SDKBase.VRC_AvatarDescriptor` 派生Componentがあり、Prefabインスタンスのルートであることを確認します。通常の読み込み済みシーン、非永続、Prefabステージ外、プレビューシーン外で、通知のシーンから移動していないことが必要です。非アクティブでも対象です。

トランザクションRootはAvatarです。SDKのDescriptor型が見つかったときだけ自動登録します。購読者のない条件は評価しません。コンテナの子や既存シーンのアバターを走査しません。AAOの有無、アセット名・パス・GUIDは条件に含めません。

publicコンストラクターで値を作っても、管理側に通知を投入する公開APIはありません。

## 関連資料

[API一覧](../../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
