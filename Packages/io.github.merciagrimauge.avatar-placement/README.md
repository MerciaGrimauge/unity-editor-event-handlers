# Avatar Placement

アバターPrefabの配置と、アバター直下に配置された対象の胸シェイプ名を検出する標準条件プロバイダーです。Editor Event Handlersへ条件を登録し、公開通知型を購読するハンドラーへ結果を渡します。

## 通知

| 通知型 | 判定対象 | 編集範囲 |
|---|---|---|
| [AvatarPlaced](Documentation~/api/AvatarPlaced.md) | 新規作成された、Descriptorを持つアバターPrefabのルート | アバター |
| [AvatarChildBreastBlendShapesPlaced](Documentation~/api/AvatarChildBreastBlendShapesPlaced.md) | アバター直下に作成/移動された別対象から検出した、名前にbreastを含むシェイプ | 親アバター |

後者は衣装であることや、アバター本体とのシェイプ互換性を判定しません。アバター自身の胸シェイプを探す条件でもありません。検出範囲・入力・除外・通知プロパティは各型のリファレンスを参照してください。

## 利用方法

Editor専用asmdefで`EditorEventHandlers.Editor`と`AvatarPlacement.Editor`を参照します。VRChat SDKのDescriptor型を検出すると標準の2条件を自動登録します。購読側が条件を登録し直す必要はありません。SDKがない環境では条件が未登録となり、購読は待機します。

[クイックスタート](../io.github.merciagrimauge.editor-event-handlers/Documentation~/QUICKSTART.md) / [イベントハンドラーガイド](../io.github.merciagrimauge.editor-event-handlers/Documentation~/HANDLER_AUTHORING.md) / [通知API一覧](Documentation~/API_REFERENCE.md)

既存シーンの初期探索やシーン読み込みによる再通知はありません。SDKを含むUnity 6でのアバター検出は対応SDKによる検証が必要です。

ライセンスは[MIT](LICENSE)です。本ソフトウェアは現状のまま提供します。
