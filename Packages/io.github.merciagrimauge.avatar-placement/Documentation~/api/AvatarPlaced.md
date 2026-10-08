# `AvatarPlaced`

- 名前空間: `AvatarPlacement.Editor`
- アセンブリ: `AvatarPlacement.Editor`
- 対象: Unity Editor

Descriptorを持つアバターPrefabの新規配置通知です。

## 定義

```csharp
public readonly struct AvatarPlaced
```

The Unity reference exposes current state for inspection; edit through the supplied HandlerContext.

## メンバー一覧

| メンバー | 概要 |
|---|---|
| [public GameObject Avatar { get; }](#member-16809f5bc0cd) | Detected avatar Prefab instance root; it may change or be destroyed before later handlers use it. |
| [public AvatarPlaced(GameObject avatar)](#member-ed6470b05960) | Creates a notification value without validating or publishing it. |

<a id="member-16809f5bc0cd"></a>

## `Avatar`

```csharp
public GameObject Avatar { get; }
```

Detected avatar Prefab instance root; it may change or be destroyed before later handlers use it.

<a id="member-ed6470b05960"></a>

## `Constructor`

```csharp
public AvatarPlaced(GameObject avatar)
```

Creates a notification value without validating or publishing it.

### 引数

| 名前 | 説明 |
|---|---|
| `avatar` | Avatar reference stored in the value. |

## 使用上の注意

readonly structです。


標準条件IDは `local.avatar-placement.created`、入力はCreatedです。対象自身に `VRC.SDKBase.VRC_AvatarDescriptor` 派生Componentがあり、Prefabインスタンスのルートであることを確認します。通常の読み込み済みシーン、非永続、Prefabステージ外、プレビューシーン外で、通知のシーンから移動していないことが必要です。非アクティブでも対象です。

トランザクションRootはAvatarです。SDKのDescriptor型が見つかったときだけ自動登録します。購読者のない条件は評価しません。コンテナの子や既存シーンのアバターを走査しません。AAOの有無、アセット名・パス・GUIDは条件に含めません。

publicコンストラクターで値を作っても、管理側に通知を投入するpublic APIはありません。

## 関連資料

[API一覧](../../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
