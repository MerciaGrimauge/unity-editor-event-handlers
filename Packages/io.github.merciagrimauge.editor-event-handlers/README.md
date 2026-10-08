# Editor Event Handlers

Unity Editorの変更を条件プロバイダーで判定し、型付きイベントハンドラーへ共有するEditor専用ライブラリです。ディスパッチャーは登録・結果共有・配送順・期限・失敗時の無効化とUndo対応の確定/復元を管理します。アバターや特定SDKの意味判定は条件プロバイダー側で行います。

## ドキュメント

- [クイックスタート](Documentation~/QUICKSTART.md)
- [型・メンバー別APIリファレンス](Documentation~/API_REFERENCE.md)
- [イベントハンドラーガイド](Documentation~/HANDLER_AUTHORING.md)
- [条件プロバイダーガイド](Documentation~/CONDITION_AUTHORING.md)
- [実行契約とライフサイクル](Documentation~/CONTRACTS.md)

利用側のEditor専用asmdefは`EditorEventHandlers.Editor`を参照します。標準アバター通知を使う場合はAvatar Placementと`AvatarPlacement.Editor`も必要です。通知型の契約アセンブリがあれば、条件が登録される前でも購読できます。

## 動作範囲

単一・AND・OR購読を登録できます。必要な条件を入力ごとに1回評価して結果を共有し、バッチ全体の判定後に購読順で実行します。ORの短絡は保存済み結果の選択に適用します。

登録数は条件登録と購読の合計100件です。各呼び出しの期限は既定100 ms、ユーザー設定範囲1〜100 msです。管理ウィンドウは`Tools / MerciaGrimauge / Editor Event Handlers`です。手動設定と異常無効化はEditor再起動後も保持します。

編集と復元にはHandlerContextのAPIを使います。追跡済み変更以外の状態は復元対象に含みません。メインスレッドで戻らない処理を強制停止する機構はありません。Undo/Redoの通知を除外しますが、すべての変更の発生元をユーザー操作だけに限定する仕組みではありません。既存シーンの一括探索はありません。

Unity 2022とUnity 6.4以降の識別子APIの差を内部で吸収します。依存SDKの互換性は別途確認が必要です。

ライセンスは[MIT](LICENSE)です。本ソフトウェアは現状のまま提供します。
