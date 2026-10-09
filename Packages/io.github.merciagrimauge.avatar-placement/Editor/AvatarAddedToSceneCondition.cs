using System;
using EditorEventHandlers.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace AvatarPlacement.Editor
{
    // 公開通知の契約です。購読者が条件の内部実装を参照する必要はありません。
    /// <summary>標準条件がアバター Prefab インスタンスのルートの新規作成を検出した通知です。</summary>
    /// <remarks>Unity 参照は現在の状態の確認に使います。編集には渡された HandlerContext を使ってください。</remarks>
    public readonly struct AvatarAddedToScene
    {
        /// <summary>検出したアバター Prefab インスタンスのルートです。後続のハンドラーが使う前に、状態が変わったり破棄されたりする場合があります。</summary>
        /// <remarks>使用直前に == null または != null で存続を確認してください。Unity の比較は未設定と破棄済みの両方を検出します。is null、?.、?? は破棄済み判定の代替にはなりません。</remarks>
        public GameObject Avatar { get; }
        /// <summary>通知値を作成します。値の検証や通知の送信は行いません。</summary>
        /// <param name="avatar">通知値に保存するアバターへの参照です。</param>
        public AvatarAddedToScene(GameObject avatar) { Avatar = avatar; }
    }

    // アバター条件で共用します。読み込み済みの通常シーンを対象とし、アセット・Prefab モード・プレビューシーンを除外します。
    /// <summary>アバター条件で共用する、通常シーン内の編集可能なオブジェクトの判定です。</summary>
    internal static class SceneObjectFilter
    {
        /// <summary>対象が存続し、読み込み済みの通常シーンに属する編集可能なオブジェクトかを確認します。</summary>
        /// <param name="obj">条件に適合するかを確認する現在の GameObject です。null や破棄済みなら不適合です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        /// <remarks>Unity の比較演算子で null と破棄済みの両方を除外します。is null や ?. はこの存続確認の代替にはなりません。</remarks>
        internal static bool IsEditable(GameObject obj) => obj != null && obj.scene.IsValid() && obj.scene.isLoaded
            && !EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.scene)
            && PrefabStageUtility.GetPrefabStage(obj) == null;
    }

    // SDK の Descriptor 型がある場合だけ標準条件を登録します。登録は Editor ドメイン中維持するため、
    // トークンをここで保持し、Dispose は呼び出しません。
    /// <summary>VRChat SDK の Descriptor 型がある場合に標準条件を登録し、Editor ドメイン内で保持します。</summary>
    [InitializeOnLoad]
    internal static class AvatarConditionRegistration
    {
        /// <summary>アバターのシーン配置を検出する条件の登録トークンです。</summary>
        private static readonly ConditionRegistration Registration;
        /// <summary>追加対象の胸シェイプを検出する条件の登録トークンです。</summary>
        private static readonly ConditionRegistration BreastRegistration;
        /// <summary>アバター側の胸シェイプを判定する条件の登録トークンです。</summary>
        private static readonly ConditionRegistration AvatarBreastRegistration;
        /// <summary>登録して管理画面にも公開する配置検出の実装です。</summary>
        internal static readonly AvatarAddedToSceneCondition Condition;
        /// <summary>登録して管理画面にも公開するブレンドシェイプ検出の実装です。</summary>
        internal static readonly AvatarObjectBreastBlendShapesCondition BreastCondition;
        /// <summary>アバター側の胸シェイプを判定する実装です。</summary>
        internal static readonly AvatarBreastBlendShapesCondition AvatarBreastCondition;
        /// <summary>Descriptor 型を探索し、見つかった場合だけシーン配置・アバターの胸シェイプ・追加対象の胸シェイプの条件を登録します。</summary>
        static AvatarConditionRegistration()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<Component>())
                if (type.FullName == "VRC.SDKBase.VRC_AvatarDescriptor")
                {
                    Condition = new AvatarAddedToSceneCondition(type);
                    Registration = EditorEvents.RegisterCondition<AvatarAddedToScene>(Condition);
                    AvatarBreastCondition = new AvatarBreastBlendShapesCondition(type);
                    AvatarBreastRegistration = EditorEvents.RegisterCondition<AvatarHasBreastBlendShapes>(AvatarBreastCondition);
                    BreastCondition = new AvatarObjectBreastBlendShapesCondition(type);
                    BreastRegistration = EditorEvents.RegisterCondition<AvatarObjectWithBreastBlendShapesAdded>(BreastCondition);
                    break;
                }
        }
    }

    /// <summary>Descriptor を持つアバター Prefab インスタンスのルートの新規作成を検出する条件です。</summary>
    internal sealed class AvatarAddedToSceneCondition : IEventCondition<AvatarAddedToScene>
    {
        /// <summary>アバターのルートを識別する Descriptor コンポーネントの型です。</summary>
        private readonly Type _descriptor;
        /// <inheritdoc />
        public string Id => "local.avatar-placement.created";
        /// <inheritdoc />
        public EditorChangeKind Changes => EditorChangeKind.Created;
        /// <summary>アバターを識別する Descriptor 型を保持して条件を作成します。</summary>
        /// <param name="descriptor">アバターを識別する Descriptor コンポーネントの型です。</param>
        internal AvatarAddedToSceneCondition(Type descriptor) { _descriptor = descriptor; }
        /// <summary>新規作成された対象がアバター Prefab のルートで、通知時のシーンに残っている場合に一致結果を返します。</summary>
        /// <param name="context">入力変更と、この条件評価の期限を確認するコンテキストです。</param>
        /// <param name="match">一致した場合の通知と編集範囲のルートです。不一致の場合は使いません。</param>
        /// <returns>条件が一致して有効な通知とルートを返す場合は true、それ以外は false です。</returns>
        /// <remarks>既存シーンの初期走査は行いません。通知時と現在のシーンが一致する、新規作成入力だけを対象にします。</remarks>
        /// <exception cref="ConditionDeadlineExceededException">判定の区切りで期限超過を検出した場合です。</exception>
        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarAddedToScene> match)
        {
            context.CheckDeadline();
            var change = context.Change;
            var obj = change.GameObject;
            if (change.Kind == EditorChangeKind.Created && IsEligible(obj) && EditorSceneId.FromScene(obj.scene) == change.SceneId)
            {
                match = new ConditionMatch<AvatarAddedToScene>(new AvatarAddedToScene(obj), obj);
                return true;
            }
            match = default; return false;
        }
        /// <summary>通常シーンの対象が Descriptor を持つ Prefab インスタンスのルートかを確認します。</summary>
        /// <param name="obj">条件に適合するかを確認する現在の GameObject です。null や破棄済みなら不適合です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal bool IsEligible(GameObject obj)
        {
            if (!SceneObjectFilter.IsEditable(obj)) return false;
            if (!obj.TryGetComponent(_descriptor, out _)) return false;
            return PrefabUtility.IsPartOfPrefabInstance(obj) && PrefabUtility.IsAnyPrefabInstanceRoot(obj);
        }
    }
}
