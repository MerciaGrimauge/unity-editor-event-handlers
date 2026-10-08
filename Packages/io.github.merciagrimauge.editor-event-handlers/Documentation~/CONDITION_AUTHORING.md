# 条件プロバイダーの作成・公開ガイド

条件プロバイダーの追加はライブラリ管理者だけに限定されません。第三者も自分のEditorアセンブリで`IEventCondition<TEvent>`を実装し、公開された`EditorEvents.RegisterCondition<TEvent>`で登録できます。イベントディスパッチャーの変更や継承、型別のハードコード、管理APIの開放は不要です。

## 通知契約をイベントハンドラーと共有する

イベントハンドラーは条件クラスではなく、厳密な通知型`TEvent`を購読します。通知型はpublicとして共有し、条件プロバイダーの判定実装はinternalにできます。同じ名前の型をイベントハンドラーがコピーして定義しても別の型なので配送されません。条件プロバイダーとイベントハンドラーは同じ契約アセンブリを参照してください。

| 配布構成 | 使用方法 |
|---|---|
| 標準条件プロバイダーのパッケージに通知型と実装を同梱 | イベントハンドラーは標準パッケージを参照し、型を購読。標準条件プロバイダーが自動登録 |
| 第三者の条件プロバイダーが通知型と実装を同梱 | イベントハンドラーはそのパッケージを参照。作者が提供する登録方法を使用 |
| 契約と判定実装を別アセンブリに分離 | イベントハンドラーは契約だけを参照。実装が未登録なら待機。複数提供者は条件プロバイダーを1つだけ選択 |

**1つの通知型に登録できる条件プロバイダーは1つ**です。異なるIdにしても同じTEventの2件目は拒否します。標準`AvatarPlaced`と別の意味で判定したい場合は、新しい通知型を定義します。標準条件プロバイダーの置き換えを自動で奪うAPIはありません。イベントハンドラーの購読は複数登録できます。

## 完全例: 公開通知型

この例は「Createdの対象自身にAudioSourceがある」を検出します。アバターやPrefabには限定しません。契約を`Example.Events.Contracts`アセンブリへ置くasmdef例です。

```json
{
  "name": "Example.Events.Contracts",
  "references": [],
  "includePlatforms": ["Editor"],
  "autoReferenced": false
}
```

次のコードは契約アセンブリで単独コンパイルできます。イベントディスパッチャーや条件プロバイダーの実装へ依存しません。

```csharp
using UnityEngine;

namespace Example.Events
{
    /// <summary>A created scene object currently has an AudioSource on itself.</summary>
    public readonly struct CreatedObjectWithAudioSource
    {
        /// <summary>Created object selected as the transaction root.</summary>
        public GameObject Object { get; }
        /// <summary>AudioSource on the object at detection time; recheck before editing.</summary>
        public AudioSource Audio { get; }

        /// <summary>Creates a notification value without publishing it.</summary>
        /// <param name="obj">Created object.</param>
        /// <param name="audio">Its detected AudioSource.</param>
        public CreatedObjectWithAudioSource(GameObject obj, AudioSource audio)
        { Object = obj; Audio = audio; }
    }
}
```

## 完全例: 条件プロバイダーの実装と登録

別の`Example.Events.Provider.Editor`アセンブリに判定・登録を置きます。上の契約とイベントディスパッチャーを参照します。

```json
{
  "name": "Example.Events.Provider.Editor",
  "references": ["EditorEventHandlers.Editor", "Example.Events.Contracts"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

```csharp
using EditorEventHandlers.Editor;
using Example.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Example.Events.Provider.Editor
{
    [InitializeOnLoad]
    internal static class AudioConditionRegistration
    {
        private static readonly ConditionRegistration Registration;

        static AudioConditionRegistration()
        {
            Registration = EditorEvents.RegisterCondition<CreatedObjectWithAudioSource>(new Condition());
        }

        private sealed class Condition : IEventCondition<CreatedObjectWithAudioSource>
        {
            public string Id => "example.events.created-object-with-audio-source";
            public EditorChangeKind Changes => EditorChangeKind.Created;

            public bool TryMatch(ConditionContext context,
                out ConditionMatch<CreatedObjectWithAudioSource> match)
            {
                match = default;
                context.CheckDeadline();
                var change = context.Change;
                if (change.Kind != EditorChangeKind.Created) return false;
                var obj = change.GameObject;
                if (obj == null || !obj.scene.IsValid() || !obj.scene.isLoaded
                    || EditorUtility.IsPersistent(obj) || EditorSceneManager.IsPreviewScene(obj.scene)
                    || PrefabStageUtility.GetPrefabStage(obj) != null
                    || EditorSceneId.FromScene(obj.scene) != change.SceneId
                    || !obj.TryGetComponent<AudioSource>(out var audio))
                    return false;
                context.CheckDeadline();
                match = new ConditionMatch<CreatedObjectWithAudioSource>(
                    new CreatedObjectWithAudioSource(obj, audio), obj);
                return true;
            }
        }
    }
}
```

同じ入力を`AvatarPlaced`とANDで購読すると、「配置されたアバター自身にAudioSourceがある」場合に同じRootで一致します。[イベントハンドラーのAND実装例](HANDLER_AUTHORING.md)はこの契約だけを参照します。条件プロバイダーのクラスや登録トークンをイベントハンドラーへ公開する必要はありません。

この例でイベントハンドラーを契約だけでコンパイルするためのasmdefは、`EditorEventHandlers.Editor`、`AvatarPlacement.Editor`、`Example.Events.Contracts`をreferencesに指定します。条件プロバイダー実装の`Example.Events.Provider.Editor`はイベントハンドラーのコンパイル依存ではありません。

## 条件プロバイダーの実装契約

| 項目 | 必要な設計 |
|---|---|
| `Id` | 空白以外の安定した値。保存するユーザー設定・異常無効化の識別に使用 |
| `Changes` | 必要な公開EditorChangeKindのみ。None・未知ビットは登録不可。登録時に値を保持 |
| `TryMatch` | 同期・読み取り専用。falseでは出力は未使用、trueでは有効な通知とRootを返す |
| payload | public通知型。readonly struct、読み取り専用プロパティ/一覧を推奨。参照型の通知はnull不可 |
| Root | 通常の読み込み済みシーンのGameObject。アセット、Prefabステージ、プレビューシーンは対象外 |
| 対象の選び方 | 入力で変更された対象から必要な範囲だけを判定。全シーン走査を判定の既定にしない |
| コスト | getterは軽量にし、判定ループ・高コスト処理の区切りでCheckDeadline |
| 寿命 | tokenを保持し、動的な所有機能が終了する場合だけEditorメインスレッドでDispose |

Unityオブジェクトを変更したり、直接Undo操作・非同期編集・遅延編集・モーダル表示・イベントループ再入を行いません。参照を渡せるAPIですが、読み取り専用性を強制する隔離機構ではありません。通知を作っても任意に発火させるpublic APIはありません。

通知のRootはイベントハンドラーの編集範囲を決めます。例えば子が追加された条件で親アバターをRootにすれば、イベントハンドラーは親アバター以下を編集できます。payloadに別対象の参照を含めても、その対象がRoot外なら編集範囲は広がりません。複合ANDで使う条件プロバイダーは、同じ入力に対して他の条件プロバイダーと同じRootを選べるかを契約に示します。

入力のKind・ID・親遷移は通知時の情報ですが、Unity参照は判定時/実行時の現在状態です。SceneId、NewParentId等が意味条件に必要なら現在の対象と照合します。`EditorObjectId.IsValid`は対象が現存する証明ではなく、IDはセッションをまたいで保存しません。

条件に有効な購読者がいなければ評価されません。単一/AND/ORの多数のイベントハンドラーが同じ条件プロバイダーを使っても入力ごとの評価は1回です。全バッチの条件プロバイダー評価後にイベントハンドラーが実行されます。条件プロバイダーは呼び出し回数や順序に依存した通知蓄積を行わず、必要な状態をpayloadへ渡します。重複排除のためBatchIdを使う場合はその範囲と失効を契約へ記載します。

判定の例外・期限超過・不正なtrue結果では条件プロバイダーを異常無効化します。falseでも戻った後に期限を確認します。依存するイベントハンドラーは待機し、ORの別の枝で故障を迂回しません。初期値/上限100 ms、ユーザー設定1〜100 ms、条件登録と購読の合計100件です。条件プロバイダー自身が上限や設定を変更するpublic APIはありません。

SDK等が必要な条件プロバイダーは、どの依存があれば登録するかを示します。依存のない環境を対応済みと扱わず、不在で未登録になるのか、コンパイル依存なのかを区別します。標準条件プロバイダーの自動登録を追加の条件プロバイダー側から重ねて実行しません。

## イベントハンドラーの作者へ公開する条件仕様

各条件プロバイダーは次の表を埋めた資料を、通知契約と一緒に提供してください。イベントハンドラーの作者がイベントディスパッチャー/条件プロバイダーのソースを読まずに購読方式を決められることを目的にします。

| 項目 | 上の例の公開仕様 |
|---|---|
| パッケージ / アセンブリ | 例示用契約`Example.Events.Contracts`。実製品ではパッケージIDと導入条件も記載 |
| 完全修飾通知型 | `Example.Events.CreatedObjectWithAudioSource` |
| 条件ID | `example.events.created-object-with-audio-source` |
| 登録方法 / 寿命 | ProviderアセンブリのInitializeOnLoadによる自動登録、Editorドメイン内 |
| 入力Kind | Createdのみ |
| 判定の意味 | Created対象自身が通常シーンにあり、その時点でAudioSourceを持つ |
| 除外 | アセット、Prefabステージ、プレビュー、ロード前、通知シーンから移動した対象 |
| payload | Object=作成対象、Audio=そのAudioSource。Unity参照は後で変わり得る |
| transaction Root | Object自身 |
| 重複 / 再通知 | 独自のバッチ内重複排除なし。シーンロード・後からのAudioSource追加・過去配置の再送なし |
| ANDの利用 | 同じCreated入力で同じRootを返す条件と結合可能 |
| 性能 / キャッシュ | 対象1つのコンポーネント検索のみ。通知履歴・全シーンキャッシュなし |
| 依存 / バージョン | Unity Editorとイベントディスパッチャー。標準アバター条件やSDKを条件プロバイダー自身では要求しない |
| 失敗時 | イベントディスパッチャーの期限/異常無効化へ従う。再起動で故障を解除しない |

通知型を変える場合、既存イベントハンドラーのコンパイル依存と厳密型の購読が変わります。同じ型の意味やRootを変更する場合も契約変更として扱います。新しい意味条件を既存型の名前だけで偽装しません。

## 条件プロバイダーの確認項目

入力の意味に一致/不一致する対象、対象の破棄・移動、シーン/Prefabステージ除外、通知payloadとRoot、独自の重複排除・キャッシュ失効、期限・異常無効化を確認します。イベントハンドラーが先に登録された場合、複数のイベントハンドラーが評価を共有する場合、同じ通知型の条件プロバイダーの二重登録拒否も確認します。

仕様を公開しただけで入力を検証済みとは扱いません。イベントディスパッチャーにはシーンロード通知、初期全シーン探索、ユーザー操作だけの厳密な識別がありません。追加の条件プロバイダーはイベントディスパッチャーが公開するUnity変更Kindの範囲内で意味を定義します。新しいネイティブ通知源が必要な場合は、条件プロバイダーの登録だけで実現する範囲とは区別します。

[API reference](API_REFERENCE.md) / [イベントハンドラーの作成ガイド](HANDLER_AUTHORING.md) / [実行契約](CONTRACTS.md)
