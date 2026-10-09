# Editor Event Handlers

Unity Editor の変更を判定する条件と、その結果を処理するハンドラーを接続するライブラリです。
アバター固有の条件は [Avatar Placement](../io.github.merciagrimauge.avatar-placement/README.md) が提供します。

## 利用者向け

導入と管理画面の操作は、[リポジトリの README](../../README.md#利用者向け) を参照してください。

## 開発者向け

### Editor アセンブリを用意する

例として、`Assets/ExampleFeature/Editor/ExampleFeature.Editor.asmdef` を作成します。

```json
{
  "name": "ExampleFeature.Editor",
  "references": ["EditorEventHandlers.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

スクリプトは同じフォルダー以下に置きます。例えば `Assets/ExampleFeature/Editor/ExampleHandler.cs` です。
アバター通知を使う場合は、`references` に `AvatarPlacement.Editor` も追加します。

### 条件を登録・購読する

- **条件を提供する**：`IEventCondition<TEvent>` を実装し、`EditorEvents.RegisterCondition<TEvent>()` で登録します。
- **1条件を購読する**：`IEventHandler<TEvent>` を実装し、`EditorEvents.Subscribe<TEvent>()` で登録します。
- **すべての条件に一致したら処理する**：`IEventHandler<CompositeEvent>` と `EditorEvents.SubscribeAll()` を使います。
- **いずれかの条件に一致したら処理する**：`IEventHandler<CompositeEvent>` と `EditorEvents.SubscribeAny()` を使います。

登録トークンを保持し、不要になったら `Dispose()` で解除します。
条件が未登録・無効の場合、依存する購読は待機します。条件より先にハンドラーを登録することもできます。

AND は同じ入力で全条件が一致し、同じ編集範囲を返す場合に実行します。異なる入力の結果は組み合わせません。
OR は指定順で最初に一致した通知を選びます。通知値は `CompositeEvent.TryGet<TEvent>()` で取得します。

### ハンドラーの実行

編集には `HandlerContext` のメソッドを使い、`HandlerResult.Success()`・`Skip()`・`Failure()`・`Cancel()` で結果を返します。
`Skip()` は編集前に返してください。ハンドラー間の実行順は保証しません。

登録・解除・判定・実行は Editor のメインスレッドで行います。
判定とハンドラーは同期処理で、`CheckDeadline()` による期限確認が必要です。期限は管理画面で 1〜100 ms に設定できます。
失敗時の復元対象はコンテキストを通じた追跡済み変更です。直接編集や外部ファイルの変更は含みません。

[アバター通知の実装例](../io.github.merciagrimauge.avatar-placement/README.md#実装例) / [API の入口](Editor/Contracts.cs) / [編集 API](Editor/HandlerContext.cs)

## ライセンス

[MIT](LICENSE)。
