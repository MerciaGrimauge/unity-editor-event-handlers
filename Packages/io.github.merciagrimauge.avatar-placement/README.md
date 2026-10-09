# Avatar Placement

アバターのシーン配置と、胸のブレンドシェイプに関する条件を提供します。
[Editor Event Handlers](../io.github.merciagrimauge.editor-event-handlers/README.md) と VRChat SDK が必要です。

## 利用者向け

VRChat SDK の Descriptor 型を検出すると、条件は自動登録されます。
導入と管理画面の操作は、[リポジトリの README](../../README.md#利用者向け) を参照してください。

## 開発者向け

### 通知を選ぶ

- **`AvatarAddedToScene`**：Descriptor を持つアバター Prefab が、通常シーンに新しく配置された通知です。`Avatar` がアバターのルートです。
- **`AvatarHasBreastBlendShapes`**：配置入力に対応するアバター側に、胸シェイプがある場合の通知です。`Avatar` が編集範囲です。
- **`AvatarObjectWithBreastBlendShapesAdded`**：アバターの階層へ作成・移動された対象に、胸シェイプがある場合の通知です。直下に限りません。`Avatar` が編集範囲、`AddedObject` が追加対象です。

胸シェイプは、名前に `breast` を含むものを大文字・小文字を区別せず探します。
非アクティブな対象も探索し、`Armature` 以下と入れ子の別アバターを除外します。
アバター側の判定では追加対象の階層も除外し、追加対象だけに胸シェイプがある場合は成立させません。

どちらの胸シェイプ条件も、1つ見つかれば探索を終了します。
具体的なレンダラー・シェイプの探索、衣装の判定、対応付けは利用側で行ってください。

### 実装例

アバター側と追加対象側の両方に胸シェイプがある場合に、子オブジェクトを作る例です。
シーンへのアバター配置だけを扱う場合は、`AvatarAddedToScene` を単一購読します。

`Assets/ExampleFeature/Editor/ExampleFeature.Editor.asmdef` を作成します。

```json
{
  "name": "ExampleFeature.Editor",
  "references": ["EditorEventHandlers.Editor", "AvatarPlacement.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

次のコードを `Assets/ExampleFeature/Editor/ExampleHandler.cs` に保存します。

```csharp
using AvatarPlacement.Editor;
using EditorEventHandlers.Editor;
using UnityEditor;

[InitializeOnLoad]
internal static class ExampleRegistration
{
    private static readonly EventSubscription Subscription;

    static ExampleRegistration()
    {
        Subscription = EditorEvents.SubscribeAll(new Handler(), new[]
        {
            typeof(AvatarHasBreastBlendShapes),
            typeof(AvatarObjectWithBreastBlendShapesAdded)
        });
    }

    private sealed class Handler : IEventHandler<CompositeEvent>
    {
        public string Id => "example.breast-shape-feature";

        public HandlerResult Execute(HandlerContext<CompositeEvent> context)
        {
            context.CheckDeadline();
            if (!context.Event.TryGet<AvatarObjectWithBreastBlendShapesAdded>(out var added)
                || added.AddedObject == null)
                return HandlerResult.Skip("Target is unavailable.");
            if (context.Root.transform.Find("ExampleAdornment") != null)
                return HandlerResult.Skip("Already configured.");

            context.CreateChild("ExampleAdornment");
            return HandlerResult.Success();
        }
    }
}
```

### API の変更

`AvatarPlaced` は `AvatarAddedToScene` に変更しました。`Avatar` プロパティとシーン配置の判定範囲は同じです。
`AvatarChildBreastBlendShapesPlaced` と検出一覧型 `BreastBlendShape` は廃止し、追加対象の存在判定は `AvatarObjectWithBreastBlendShapesAdded` に置き換えました。

[登録・実行の基本](../io.github.merciagrimauge.editor-event-handlers/README.md#開発者向け) / [シーン配置の通知](Editor/AvatarAddedToSceneCondition.cs) / [アバター側の条件](Editor/AvatarBreastBlendShapesCondition.cs) / [追加対象の通知](Editor/AvatarObjectWithBreastBlendShapesAdded.cs)

## ライセンス

[MIT](LICENSE)。
