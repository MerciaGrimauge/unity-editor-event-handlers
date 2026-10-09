# Avatar Placement APIリファレンス

標準の条件プロバイダーが公開する通知型です。名前空間とアセンブリは`AvatarPlacement.Editor`、すべてEditor専用です。判定の実装は内部型です。

| 型 | 通知・データ |
|---|---|
| [AvatarPlaced](api/AvatarPlaced.md) | Descriptorを持つアバターPrefabの新規配置 |
| [AvatarChildBreastBlendShapesPlaced](api/AvatarChildBreastBlendShapesPlaced.md) | アバター直下に配置された対象の、名前にbreastを含むシェイプ |
| [BreastBlendShape](api/BreastBlendShape.md) | Renderer・Mesh・インデックス・シェイプ名の検出結果 |

VRChat SDKのDescriptor型を検出すると、標準の2条件を自動登録します。利用するハンドラーが再登録する必要はありません。SDKがない環境では未登録となり、購読は待機します。

[共通API一覧](../../io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md) / [クイックスタート](../../io.github.merciagrimauge.editor-event-handlers/Documentation~/QUICKSTART.md)

[パッケージ概要](../README.md)
