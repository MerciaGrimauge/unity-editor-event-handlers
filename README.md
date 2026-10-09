# Unity Editor Event Handlers

Unity Editor の拡張機能を開発するためのライブラリです。Editor の変更を条件で判定し、登録された処理を呼び出します。
アバターのシーン配置や胸のブレンドシェイプを検出する条件も含まれます。

## 利用者向け

### 導入

1. GitHub の **Code → Download ZIP** からソースを取得します。
2. `Packages/io.github.merciagrimauge.editor-event-handlers` を、Unity プロジェクトの `Packages/` にコピーします。
3. アバター条件を使う場合は、`Packages/io.github.merciagrimauge.avatar-placement` もコピーし、対応する VRChat SDK を導入します。

同名パッケージを重複して導入しないでください。拡張機能に導入手順がある場合は、その手順に従ってください。

### 設定・動作確認

**Tools → MerciaGrimauge → Editor Event Handlers** で、条件とハンドラーの有効・無効、期限、直近の結果を確認できます。
設定はプロジェクトごとに保存されます。失敗によって無効になった登録は、原因を確認してからこの画面で有効化します。

Unity Editor 専用です。アバター条件は VRChat SDK がある場合に登録されます。
既存シーンの一括走査や、シーンを開いた際の再通知は行いません。
Unity 2022.3 を基準とし、SDK を含む Unity 6 の連携は未検証です。

## 開発者向け

用途に応じて、各パッケージの README を参照してください。

- [Editor Event Handlers](Packages/io.github.merciagrimauge.editor-event-handlers/README.md)：条件登録、単一・AND・OR 購読、Editor アセンブリの準備。
- [Avatar Placement](Packages/io.github.merciagrimauge.avatar-placement/README.md)：アバター通知の選び方、胸シェイプ条件の AND 購読、実装例。

API の引数・戻り値・例外は、ソースの日本語 XML コメントで確認できます。
AAO Trace And Optimize の自動付与は、別パッケージの [AAO Auto Attach](https://github.com/MerciaGrimauge/aao-auto-attach) で提供します。

## ライセンス

[MIT](LICENSE)。VRChat SDK や Avatar Optimizer のコード・素材は同梱していません。
