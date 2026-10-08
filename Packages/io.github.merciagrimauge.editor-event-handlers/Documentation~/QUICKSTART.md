# クイックスタート

アバターPrefabが配置されたとき、子オブジェクトを1つ追加するイベントハンドラーを作ります。標準条件の判定コードを書く必要はありません。

## アセンブリを用意する

共通パッケージとAvatar Placement、対応するVRChat SDKを導入します。Editor専用のasmdefに次の参照を設定します。パッケージの取得・導入は利用者側で行います。

```json
{
  "name": "Example.AvatarFeature.Editor",
  "references": ["EditorEventHandlers.Editor", "AvatarPlacement.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

## ハンドラーを登録する

次のコードを同じアセンブリに置きます。`Subscribe<AvatarPlaced>`が通知を購読し、`Execute`が変更を行い、`HandlerResult`が終了状態を伝えます。通知型が定義されていれば条件の登録前でも購読できます。

```csharp
using AvatarPlacement.Editor;
using EditorEventHandlers.Editor;
using UnityEditor;

namespace Example.AvatarFeature.Editor
{
    [InitializeOnLoad]
    internal static class AvatarAdornmentRegistration
    {
        private static readonly EventSubscription Subscription;

        static AvatarAdornmentRegistration()
        {
            Subscription = EditorEvents.Subscribe<AvatarPlaced>(
                new Handler());
        }

        private sealed class Handler : IEventHandler<AvatarPlaced>
        {
            public string Id => "example.avatar-feature.adornment";

            public HandlerResult Execute(HandlerContext<AvatarPlaced> context)
            {
                context.CheckDeadline();
                if (context.Event.Avatar == null || context.Event.Avatar != context.Root)
                    return HandlerResult.Skip("Avatar is no longer available.");
                if (context.Root.transform.Find("ExampleAdornment") != null)
                    return HandlerResult.Skip("Already configured.");
                context.CreateChild("ExampleAdornment");
                return HandlerResult.Success();
            }
        }
    }
}
```

## 配置して確認する

通常のシーンへDescriptorを持つアバターPrefabを新しく配置すると、指定した子が追加されます。既に同名の子がある場合は処理をスキップします。既存シーンの一括探索やSceneLoaded通知はありません。

管理ウィンドウ`Tools / MerciaGrimauge / Editor Event Handlers`で、登録状態・条件・有効/無効・期限を確認できます。条件が未登録または無効なら購読は待機します。失敗による無効化は再起動後も残り、UIから再有効化します。

編集には`context`のメソッドを使います。追跡済み変更は失敗時に復元の対象になります。直接変更したUnity参照や外部ファイルはその対象に含まれません。各呼び出しの期限は既定100 ms、UI設定範囲は1〜100 msです。

## 次に読む資料

- [EditorEvents](api/EditorEvents.md): 登録・単一購読・AND/OR購読
- [IEventHandler<TEvent>](api/IEventHandler-1.md): ハンドラーのインターフェース
- [HandlerContext<TEvent>](api/HandlerContext-1.md): 通知と編集API
- [イベントハンドラーガイド](HANDLER_AUTHORING.md): 通知の選び方、第三者の通知契約、実装例
- [条件プロバイダーガイド](CONDITION_AUTHORING.md): 独自条件の追加
- [実行契約とライフサイクル](CONTRACTS.md): 配送順・期限・永続設定・復元
