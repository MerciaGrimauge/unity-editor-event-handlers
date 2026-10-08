# Unity Editor Event Handlers

Unity Editorの変更を条件ごとに判定し、複数の拡張機能で共有するライブラリです。条件プロバイダーが型付き通知を生成し、イベントハンドラーが購読します。イベントディスパッチャーが登録、順序、期限、無効化、Undo対応の確定・復元を担当します。

単一条件に加え、全条件一致のAND・いずれか一致のOR購読を登録できます。同じ入力の判定結果を各購読で共有し、バッチ全体の条件評価後にイベントハンドラーを順番に呼びます。ORは保存済み結果の選択だけを短絡します。

| パッケージ | バージョン | 内容 |
|---|---|---|
| [Editor Event Handlers](Packages/io.github.merciagrimauge.editor-event-handlers/README.md) | 0.3.0 | 汎用条件・購読の管理、内部Unity接続、opaque識別子、限定されたUndo復元 |
| [Avatar Placement Condition](Packages/io.github.merciagrimauge.avatar-placement/README.md) | 0.2.4 | アバターPrefab配置、アバター直下への胸シェイプ名を持つ対象の配置の検出 |

AAO Trace And Optimizeの自動付与は別の[AAO Auto Attach](https://github.com/MerciaGrimauge/aao-auto-attach)で提供します。最適化そのものを実行するライブラリではありません。

## ドキュメント

- [クイックスタート](Packages/io.github.merciagrimauge.editor-event-handlers/Documentation~/QUICKSTART.md)
- [APIリファレンス](Packages/io.github.merciagrimauge.editor-event-handlers/Documentation~/API_REFERENCE.md)
- [イベントハンドラーガイド](Packages/io.github.merciagrimauge.editor-event-handlers/Documentation~/HANDLER_AUTHORING.md)
- [条件プロバイダーガイド](Packages/io.github.merciagrimauge.editor-event-handlers/Documentation~/CONDITION_AUTHORING.md)
- [実行契約とライフサイクル](Packages/io.github.merciagrimauge.editor-event-handlers/Documentation~/CONTRACTS.md)
- [構成・責務・シーケンス図](docs/ARCHITECTURE.md)
- [テストケースと検証範囲](docs/TEST_CASES.md)

## 導入と動作範囲

各パッケージのフォルダーをUnityのPackages以下へ導入できます。既存の同名パッケージと重複させないでください。パッケージ管理ツールによる導入では、公開された配布物と依存宣言に従います。VRChat SDK、Avatar Optimizer、NDMFの取得・導入・更新は利用者またはパッケージ管理ツールが担当します。

Unity 2022の旧IDとUnity 6.4以降のEntityId/SceneHandleを内部で切り替えます。公開APIは `EditorObjectId` / `EditorSceneId` で統一します。Unityのバージョン差を吸収する設計であり、SDKを含めたUnity 6対応を意味しません。各バージョンでの検証状況は別途確認が必要です。

Unity 2022.3.22f1とUnity 6000.6.0f1のSDKなし共通ライブラリで、現行APIの66 assertionがそれぞれ成功しています。順序指定APIの不在、単一/AND/OR配送、結果共有、依存失効、Root喪失時の打ち切りを確認しました。現行ガイドの完全な実装例6件も両環境でコンパイルしています。SDKを含むUnity 6連携と実際のGUI操作は未検証です。[検証範囲](docs/TEST_CASES.md)を参照してください。

条件とハンドラの期限は各1〜100 ms、初期値100 msです。共有のバッチ時間予算はありません。登録数は条件と購読の合計100件です。登録側に期限変更・再有効化APIはありません。メインスレッド上の同期処理で、戻らない処理を強制killする機構はありません。

管理ウィンドウは `Tools / MerciaGrimauge / Editor Event Handlers` です。登録の有効/無効、期限、対応条件、条件の元ソースを確認できます。Undo/Redo通知は除外しますが、すべての変更の発生元をユーザー操作と他ツールに分ける仕組みではありません。既存シーンの一括走査は行いません。

手動設定と異常による無効化はプロジェクトごとのEditorPrefsに保存し、Editor再起動後にも適用します。異常無効化の解除は管理ウィンドウから有効化して行います。

## ライセンス

MIT。本ソフトウェアは現状のまま提供します。[LICENSE](LICENSE)を参照してください。SDKやAvatar Optimizerのコード・素材は同梱していません。
