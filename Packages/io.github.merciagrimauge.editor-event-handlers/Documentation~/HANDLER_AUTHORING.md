# イベントハンドラーの作成ガイド

この資料は、条件プロバイダーの内部実装やイベントディスパッチャーのソースを読まずにイベントハンドラーを作るための入口です。イベントハンドラーは通知型を購読し、その通知を受けたときだけ同期処理します。条件プロバイダーは標準パッケージ・第三者パッケージ・自分のパッケージのいずれが提供しても同じ契約で接続できます。

## 使用する条件を選ぶ

| 必要な入力 | 使用する通知型 / 購読 | 編集範囲 |
|---|---|---|
| アバターPrefabが新しく配置された | `AvatarPlacement.Editor.AvatarPlaced` / `Subscribe<T>` | `context.Root == notification.Avatar` |
| アバター直下に配置された別対象に、名前にbreastを含むシェイプがある | `AvatarPlacement.Editor.AvatarChildBreastBlendShapesPlaced` / `Subscribe<T>` | 親アバター。配置対象そのものではない |
| 同じ入力が複数の条件プロバイダーにすべて一致した | `SubscribeAll` / `IEventHandler<CompositeEvent>` | すべての条件プロバイダーのRootが同じ場合のみ実行 |
| 同じ入力が複数の条件プロバイダーのいずれかに一致した | `SubscribeAny` / `IEventHandler<CompositeEvent>` | 型一覧で最初に一致した条件プロバイダーのRoot |
| 上記にない意味条件 | 第三者の条件プロバイダーまたは独自の条件プロバイダーの通知型 | 条件プロバイダーの公開契約で確認 |

標準条件プロバイダーはAvatar Placementパッケージが自動登録します。イベントハンドラーから同じ条件プロバイダーを登録し直しません。[標準通知の全プロパティと検出範囲](../../io.github.merciagrimauge.avatar-placement/Documentation~/API_REFERENCE.md)を参照してください。VRChat SDKのDescriptor型がない場合は標準条件プロバイダーが登録されません。

`AvatarPlaced AND AvatarChildBreastBlendShapesPlaced`は「配置したアバター自身に胸シェイプがある」という条件にはなりません。前者はアバター自身の作成、後者は別対象の直下配置なので、同じ入力で一致する意味条件ではありません。その用途には「作成対象自身の胸シェイプ」を調べる別の条件プロバイダーが必要です。

第三者の条件プロバイダーを購読するために必要なのは、公開通知型とそのアセンブリ、入力の意味、payloadの仕様、Rootの契約、登録方法です。条件プロバイダーのprivate/internalなクラスを参照しません。必要な情報が欠けていれば条件プロバイダーの作者へ契約の提供を求めます。型名・GameObject名・シェイプ名から意味を推測して埋めません。

## Editor用アセンブリを用意する

標準条件プロバイダーを購読するイベントハンドラーのasmdef例です。サンプルは配布パッケージへ自動で取り込まれる実装ではありません。利用する機能として必要なものだけを、自分のEditor専用アセンブリへ追加してください。

```json
{
  "name": "Example.AvatarFeature.Editor",
  "references": ["EditorEventHandlers.Editor", "AvatarPlacement.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

| 利用形態 | 必要な参照 |
|---|---|
| イベントハンドラーと第三者の通知契約だけ | `EditorEventHandlers.Editor`と通知型を定義するアセンブリ |
| 標準アバター条件のイベントハンドラー | 上記asmdefの2アセンブリ |
| 後述の独自の条件プロバイダーを含むAND例 | 上記2つと`Example.Events.Contracts` |
| MAを設定するイベントハンドラー | 使用する条件の参照に加え`nadena.dev.modular-avatar.core` |

asmdefのreferencesはアセンブリ名です。パッケージ導入の依存宣言とは別に設定します。標準条件プロバイダーのパッケージIDは`io.github.merciagrimauge.avatar-placement`、イベントディスパッチャーは`io.github.merciagrimauge.editor-event-handlers`です。第三者の条件プロバイダーの通知型を使うイベントハンドラーには、その契約を含むパッケージも必要です。

条件プロバイダーの実装が未登録でも、通知型のアセンブリが存在すればイベントハンドラーを登録できます。通知型のアセンブリ自体がない場合はイベントハンドラーがコンパイルできません。「待機する」ことと「依存パッケージがなくてもコンパイルできる」ことを混同しないでください。

## 単一条件のイベントハンドラーを作る

次のブロックは1つの完全なイベントハンドラー実装です。アバターPrefab配置時、同名の子がなければ子を作ります。生成対象の名前は例示用です。

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

登録順は条件プロバイダーの初期化順に依存しません。条件プロバイダーが不在なら待機し、その後条件プロバイダーが登録されても過去の配置は再送しません。Editor起動・ドメインリロードでは上の初期化処理を再実行します。購読トークンはEditorドメイン内で保持され、UI設定と異常無効化は別に永続保存されます。

`Id`は製品ごとの安定した値に置き換えます。同じ通知型の購読IDは重複できません。ランダム値や起動ごとに変わるIDでは、保存した有効/無効・期限が次回の登録と結び付かなくなります。IDを変えて異常無効化を回避しません。

動的な機能では所有者が`EventSubscription`を保持し、終了時にEditorメインスレッドで`Dispose()`します。参照を手放すだけでは解除されません。ドメインの間ずっと必要な上の購読では終了時の明示解除は不要です。

## 検出済みブレンドシェイプを使うイベントハンドラー

次は標準条件プロバイダーの一覧を使う完全なイベントハンドラーです。検出一覧のMesh/Index/Nameを再確認してから、対象Rendererの`updateWhenOffscreen`を設定します。製品の同期処理を実装するものではありません。

```csharp
using System.Collections.Generic;
using AvatarPlacement.Editor;
using EditorEventHandlers.Editor;
using UnityEditor;
using UnityEngine;

namespace Example.BreastFeature.Editor
{
    [InitializeOnLoad]
    internal static class BreastRendererRegistration
    {
        private static readonly EventSubscription Subscription;

        static BreastRendererRegistration()
        {
            Subscription = EditorEvents.Subscribe<AvatarChildBreastBlendShapesPlaced>(new Handler());
        }

        private sealed class Handler : IEventHandler<AvatarChildBreastBlendShapesPlaced>
        {
            public string Id => "example.breast-feature.renderer-settings";

            public HandlerResult Execute(HandlerContext<AvatarChildBreastBlendShapesPlaced> context)
            {
                context.CheckDeadline();
                var notice = context.Event;
                if (notice.Avatar == null || notice.Avatar != context.Root
                    || notice.PlacedObject == null || notice.PlacedObject.transform.parent != context.Root.transform)
                    return HandlerResult.Skip("Placement changed before execution.");

                // Validate first, then edit. Skip must not follow tracked edits.
                var targets = new HashSet<SkinnedMeshRenderer>();
                foreach (var shape in notice.BreastBlendShapes)
                {
                    context.CheckDeadline();
                    var renderer = shape.Renderer;
                    if (renderer == null || shape.Mesh == null || renderer.sharedMesh != shape.Mesh
                        || shape.Mesh.GetBlendShapeIndex(shape.Name) != shape.Index
                        || (renderer.transform != notice.PlacedObject.transform
                            && !renderer.transform.IsChildOf(notice.PlacedObject.transform)))
                        return HandlerResult.Skip("Detected shape is no longer valid.");
                    if (!renderer.updateWhenOffscreen) targets.Add(renderer);
                }
                if (targets.Count == 0) return HandlerResult.Skip("Already configured.");
                foreach (var renderer in targets)
                {
                    context.CheckDeadline();
                    context.Modify(renderer, () => renderer.updateWhenOffscreen = true);
                }
                return HandlerResult.Success();
            }
        }
    }
}
```

検出結果は現在のUnityオブジェクトへの参照です。先に呼ばれたイベントハンドラーがMesh・階層・設定を変更する場合があるため、使用時に必要な状態を確認します。上の例は条件プロバイダーが渡したRendererだけを使い、アバターやシーン全体を再走査しません。同じRendererの重複編集も避けます。

## ANDを購読するイベントハンドラー

次の例は[独自の条件プロバイダーガイド](CONDITION_AUTHORING.md)の公開通知型`Example.Events.CreatedObjectWithAudioSource`を使います。その契約アセンブリを参照して、このイベントハンドラーだけをコンパイルできます。イベントハンドラーから条件プロバイダーを登録するコードは不要です。

```csharp
using System;
using AvatarPlacement.Editor;
using EditorEventHandlers.Editor;
using Example.Events;
using UnityEditor;

namespace Example.CompositeFeature.Editor
{
    [InitializeOnLoad]
    internal static class AvatarAudioRegistration
    {
        private static readonly EventSubscription Subscription;

        static AvatarAudioRegistration()
        {
            Subscription = EditorEvents.SubscribeAll(new Handler(),
                new Type[] { typeof(AvatarPlaced), typeof(CreatedObjectWithAudioSource) });
        }

        private sealed class Handler : IEventHandler<CompositeEvent>
        {
            public string Id => "example.composite-feature.avatar-audio";

            public HandlerResult Execute(HandlerContext<CompositeEvent> context)
            {
                context.CheckDeadline();
                if (!context.Event.TryGet<AvatarPlaced>(out var avatar)
                    || !context.Event.TryGet<CreatedObjectWithAudioSource>(out var audio)
                    || avatar.Avatar == null || avatar.Avatar != context.Root
                    || audio.Object != context.Root || audio.Audio == null
                    || audio.Audio.gameObject != context.Root)
                    return HandlerResult.Skip("Required object is no longer available.");
                if (!audio.Audio.playOnAwake) return HandlerResult.Skip("Already configured.");
                context.Modify(audio.Audio, () => audio.Audio.playOnAwake = false);
                return HandlerResult.Success();
            }
        }
    }
}
```

型一覧は空でない、重複のない1〜100型で指定します。`null`、`void`、byref、pointer、open generic、`CompositeEvent`は依存型にできません。入れ子のAND/OR式はありません。登録時に一覧をコピーするため、後から配列を変更しても登録内容は変わりません。

ANDは**同じ1入力での全一致**です。別の配置や別バッチの一致を蓄積しません。すべての条件プロバイダーが同じRootを返す必要があります。追加条件は「元の対象にどんな性質があるか」を同じ入力から判定する形にすると再利用できます。

## ORを購読するイベントハンドラー

次の完全例は標準条件プロバイダーの2種類を購読し、選択された通知に応じて現在の対象を確認します。読み取り専用の処理なので`Skip`を返します。

```csharp
using System;
using AvatarPlacement.Editor;
using EditorEventHandlers.Editor;
using UnityEditor;
using UnityEngine;

namespace Example.AnyFeature.Editor
{
    [InitializeOnLoad]
    internal static class AnyPlacementRegistration
    {
        private static readonly EventSubscription Subscription;

        static AnyPlacementRegistration()
        {
            Subscription = EditorEvents.SubscribeAny(new Handler(), new Type[]
            {
                typeof(AvatarPlaced), typeof(AvatarChildBreastBlendShapesPlaced)
            });
        }

        private sealed class Handler : IEventHandler<CompositeEvent>
        {
            public string Id => "example.any-feature.placement-reader";

            public HandlerResult Execute(HandlerContext<CompositeEvent> context)
            {
                context.CheckDeadline();
                GameObject placed;
                if (context.Event.TryGet<AvatarPlaced>(out var avatar))
                    placed = avatar.Avatar;
                else if (context.Event.TryGet<AvatarChildBreastBlendShapesPlaced>(out var child))
                    placed = child.PlacedObject;
                else
                    return HandlerResult.Failure("No selected notification.");
                if (placed == null) return HandlerResult.Skip("Placement no longer exists.");
                // Product-specific read-only work can use placed and context.Root here.
                return HandlerResult.Skip("Read-only inspection completed.");
            }
        }
    }
}
```

ORの`TryGet<T>`で取得できるのは、宣言順で最初に一致した1型だけです。全型を必須として読むAND用イベントハンドラーをそのまま転用しません。選択された条件プロバイダーのRootが唯一の編集範囲です。

単一・AND・ORで同じ条件プロバイダーを要求しても、条件プロバイダーの評価は入力ごとに1回です。バッチのすべての条件プロバイダーを評価した後にイベントハンドラーを呼びます。ORの短絡は保存済み結果の選択だけで、条件プロバイダー評価自体を打ち切りません。ORでも必要なすべての条件プロバイダーの登録・有効状態を要求します。未登録・手動無効・異常無効の条件プロバイダーがあればその購読は待機します。

## イベントハンドラーが守る実行・編集契約

| 項目 | イベントハンドラーの実装方法 |
|---|---|
| 実行方法 | Editorメインスレッドで同期完了。`Execute`をasync化しない |
| 正常終了 | `HandlerResult.Success()`。追跡した変更を確定 |
| 対象外・設定済み・検出後に対象が変わった | 変更前に`Skip(reason)` |
| 編集後に継続できない | `Failure(reason)`。追跡済み変更の復元と異常無効化 |
| キャンセル | `Cancel(reason)`も異常無効化。通常の「何もしない」はSkip |
| 未指定結果 | `default(HandlerResult)`は禁止。Failure扱い |
| コンポーネント追加 | `context.AddComponent<T>(target)` |
| プロパティ・シリアライズされたリストの設定 | `context.Modify(target, () => ...)`。delegateで変更するのはtargetだけ |
| 作成・Prefab配置・親変更・削除 | `CreateChild` / `InstantiatePrefab` / `SetParent` / `Destroy` |
| ループ・高コスト処理 | 区切りで`CheckDeadline()`。小さいユーザー設定値でも協調停止できる処理単位にする |
| 再実行 | 現在の設定を確認し、既に目的どおりなら変更しない。イベントハンドラー自身の編集も後の入力になり得る |

編集対象はRootとその子の同一シーンのGameObject/Componentだけです。Mesh、Material等のアセットをModifyする入口ではありません。Rootの削除・親変更、Transformの削除はできません。通知やcontextを保持して後から編集しません。直接Undo操作、遅延編集、モーダルUI、イベントループ再入を行いません。

各条件プロバイダーとイベントハンドラーの期限は独立し、初期値・上限100 ms、利用者のUI設定範囲1〜100 msです。共有バッチ予算はありません。登録側は期限を変更・延長できません。管理側は戻った後にも期限を確認しますが、戻らない同期コードを強制killできません。

ハンドラー間の実行順は公開契約に含めません。順序・優先度・順序の希望を指定するAPIはありません。イベントハンドラーは前後のイベントハンドラーの存在・正常終了・状態保存を前提にしません。条件プロバイダーのpayloadのUnity参照は共有され、完全な状態コピーではありません。

復元対象はContextが追跡したUndo対応変更です。全アバターのメモリ保存ではありません。直接書き込み、外部ファイル、非シリアライズ状態、第三者Componentの副作用や遅延処理は対象外です。確定/復元の例外や実行後のRoot喪失では残りのバッチを即時停止します。Root喪失はイベントハンドラーがSuccessを返してもFailureと異常無効化になります。

## 状態・登録失敗・通知されない場合

`EventSubscription`の公開状態は読み取り専用です。`IsEnabled`はユーザー設定と異常無効化を反映し、`IsActive`はさらに全依存条件プロバイダーの有効登録と全体停止していないことを要求します。`DisabledReason`、`LastResult`、`LastDuration`も確認できます。複合購読の`EventType`は`CompositeEvent`で、依存型は`ConditionTypes`、方式は`Mode`で読みます。

手動の有効/無効、期限、異常無効化はプロジェクト・登録種別・通知型・Idに対応して保存します。Editor再起動や同じIDの再登録で異常を解除しません。利用者が`Tools / MerciaGrimauge / Editor Event Handlers`から有効化すると解除します。複合購読のモード・型一覧を変えても、同じ`CompositeEvent`とIdなら同じ保存設定を使います。イベントハンドラーに再有効化・他者管理のAPIはありません。

| 症状 | 確認すること |
|---|---|
| コンパイルできない | 通知型を含むパッケージとasmdef参照、Editor専用設定 |
| 登録で例外 | IDの空白/重複、不正な型一覧、メインスレッド、条件プロバイダーとイベントハンドラー合計100件上限。例外を無視して登録済み扱いにしない |
| EnabledだがWaiting | 必要な条件プロバイダーの未登録/無効、SDKの不在、全体停止 |
| 有効なまま呼ばれない | 条件プロバイダーの意味条件、対象入力種類、同一入力/Root要件、過去入力を再送しないこと |
| 一度動いた後Disabled | 異常理由、期限、Skip後の編集、未指定結果、例外、Root喪失 |
| 再起動してもDisabled | 意図した保存動作。UIから再有効化して新しい入力で確認 |

Undo/Redoは保留入力を破棄して次のEditor更新まで除外します。Play/移行・コンパイル・アセット更新・Playerビルド・Undo処理中は実行しません。シーンロードや既存シーンの初期走査はありません。他ツールのUndo対応変更も届き得るため、人間による配置だけの証明にはなりません。

## MA Blendshape Syncを設定するイベントハンドラーの設計

MAの1.18.7を対象にした接続情報です。条件プロバイダーが渡す配置対象のRenderer一覧を使い、イベントディスパッチャーへMA依存を追加せずにイベントハンドラーのパッケージへ依存を持たせます。

1. 同期元の身体Rendererと、衣装側の対象Renderer・シェイプ名の対応を決めます。`breast`部分一致は検出条件であり、同名・同部位・互換性の証明ではありません。曖昧な対応はユーザー設定へ委ねます。
2. すべての参照・Mesh・シェイプ名・編集範囲を確認してから編集します。同期先Rendererと同じGameObjectに`ModularAvatarBlendshapeSync`を追加、または既存Componentを使用します。
3. `Bindings`に`BlendshapeBinding`を設定します。`ReferenceMesh`は`new AvatarObjectReference(sourceRenderer.gameObject)`、`Blendshape`は同期元名、`LocalBlendshape`は同期先名です。空のLocalBlendshapeは同期元と同名です。
4. 追加はContext.AddComponent、既存/追加ComponentのBindings編集はContext.Modifyで記録します。既存のユーザー設定を全面上書きせず、同じ同期先への設定競合と再実行を扱います。MAのBinding.Equalsは同期元参照と同期元名だけを比較するため、製品の重複・競合判定をそれだけへ委ねません。
5. `RemapCurve`と`RemapCurveIsValid`はMAの公開データです。恒等/反転等の設定を明確にします。再バインドの`Rebind`と編集更新はinternalで、外部向けの公開同期メソッドではありません。シリアライズ変更後のプレビュー更新を導入対象版で確認し、internalメソッドを反射で呼ぶ設計にしません。
6. 編集時プレビューとビルド後の初期値・アニメーション、Undo/Redo、失敗時のBindings復元を別々に確認します。MAの継続更新・OnValidate・遅延処理はイベントディスパッチャーのイベントハンドラー呼び出し後にも走るため、その時間や副作用までイベントハンドラーの期限/復元範囲に含めません。

MAは編集時に値をコピーし、ビルド時にアニメーションへ同期先のカーブを適用します。同期元→中間→同期先の連鎖同期、VRChat標準の視線/口パクによる正確な同期は対象外です。ビルド時にComponentを生成するNDMF拡張なら、公式の拡張案内に従いGeneratingでMAより前に生成します。Editor配置時のイベントハンドラーとは別の実行入口です。

出典: [公式機能説明](https://modular-avatar.nadena.dev/docs/reference/blendshape-sync)、[1.18.7のComponent/Binding](https://github.com/bdunderscore/modular-avatar/blob/1.18.7/Runtime/ModularAvatarBlendshapeSync.cs)、[参照API](https://github.com/bdunderscore/modular-avatar/blob/1.18.7/Runtime/AvatarObjectReference.cs)、[NDMFからの拡張](https://modular-avatar.nadena.dev/docs/extending)。この節は実装手順と公開データの説明であり、完成したMA同期ハンドラやその動作検証ではありません。

## イベントハンドラーを完成と判断する確認項目

| 確認 | 合格条件 |
|---|---|
| 依存と登録 | 自分のEditorアセンブリでコンパイル。条件プロバイダーのinternal実装・イベントディスパッチャーの管理入口を使わない |
| 条件プロバイダーの任意の登録順 | イベントハンドラー先行では待機、条件プロバイダー登録後の新規入力で実行。既存対象への再送なし |
| 正常な対象 | 必要な変更だけが確定し、明示的なSuccess |
| 不一致・設定済み | 変更なしのSkip、複数回の入力で重複追加なし |
| 検出後の変化 | 破棄・移動・Mesh交換を編集前に扱い、範囲外を編集しない |
| 複合購読 | 同じ入力/RootのAND、先頭結果だけのOR、依存条件プロバイダーなし/無効時の待機 |
| 失敗・期限・キャンセル | 追跡済み変更の復元、原因イベントハンドラーの無効化。別の正常イベントハンドラーを不必要に壊さない |
| 保存とUI | 異常無効化が再起動後にも復元。利用者の再有効化と期限が適用される |
| Undo/Redo・シーンロード | 入力除外/再送なしを前提にし、既存対象への自動一括適用を期待しない |
| サードパーティ設定 | Componentの設定復元と、プレビュー/ビルド結果・副作用を分けて確認 |

この表は新しいイベントハンドラーに適用する確認項目です。掲載だけでそのイベントハンドラーを実行検証済みとは扱いません。イベントディスパッチャーと標準条件プロバイダーの検証範囲は[テストケース](../../../docs/TEST_CASES.md)にあります。

全APIの引数・例外は[API reference](API_REFERENCE.md)、第三者の条件プロバイダーの追加方法は[条件プロバイダーの作成ガイド](CONDITION_AUTHORING.md)、その他の例は[実行契約](CONTRACTS.md)を参照してください。ライセンスは[MIT](../LICENSE)です。
