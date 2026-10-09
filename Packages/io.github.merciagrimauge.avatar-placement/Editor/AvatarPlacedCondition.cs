using System;
using EditorEventHandlers.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace AvatarPlacement.Editor
{
    // Public semantic contract. Consumers do not need the condition implementation.
    /// <summary>標準条件がアバター Prefab インスタンスのルートの新規作成を検出した通知です。</summary>
    /// <remarks>Unity 参照は現在の状態の確認に使います。編集には渡された HandlerContext を使ってください。</remarks>
    public readonly struct AvatarPlaced
    {
        /// <summary>検出したアバター Prefab インスタンスのルートです。後続のハンドラーが使う前に、状態が変わったり破棄されたりする場合があります。</summary>
        public GameObject Avatar { get; }
        /// <summary>通知値を作成します。値の検証や通知の送信は行いません。</summary>
        /// <param name="avatar">通知値に保存するアバターへの参照です。</param>
        public AvatarPlaced(GameObject avatar) { Avatar = avatar; }
    }

    // Shared by both avatar conditions: a loaded, ordinary scene object outside assets, Prefab Mode and preview scenes.
    internal static class SceneObjectFilter
    {
        internal static bool IsEditable(GameObject obj) => obj != null && obj.scene.IsValid() && obj.scene.isLoaded
            && !EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.scene)
            && PrefabStageUtility.GetPrefabStage(obj) == null;
    }

    // Registers both conditions only when the SDK descriptor type exists. The tokens stay registered for
    // the whole editor domain, so they are kept here and never disposed.
    [InitializeOnLoad]
    internal static class AvatarConditionRegistration
    {
        private static readonly ConditionRegistration Registration;
        private static readonly ConditionRegistration BreastRegistration;
        internal static readonly AvatarPlacedCondition Condition;
        internal static readonly AvatarChildBreastBlendShapeCondition BreastCondition;
        static AvatarConditionRegistration()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<Component>())
                if (type.FullName == "VRC.SDKBase.VRC_AvatarDescriptor")
                {
                    Condition = new AvatarPlacedCondition(type);
                    Registration = EditorEvents.RegisterCondition<AvatarPlaced>(Condition);
                    BreastCondition = new AvatarChildBreastBlendShapeCondition(type);
                    BreastRegistration = EditorEvents.RegisterCondition<AvatarChildBreastBlendShapesPlaced>(BreastCondition);
                    break;
                }
        }
    }

    internal sealed class AvatarPlacedCondition : IEventCondition<AvatarPlaced>
    {
        private readonly Type _descriptor;
        public string Id => "local.avatar-placement.created";
        public EditorChangeKind Changes => EditorChangeKind.Created;
        internal AvatarPlacedCondition(Type descriptor) { _descriptor = descriptor; }
        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarPlaced> match)
        {
            context.CheckDeadline();
            var change = context.Change;
            var obj = change.GameObject;
            if (change.Kind == EditorChangeKind.Created && IsEligible(obj) && EditorSceneId.FromScene(obj.scene) == change.SceneId)
            {
                match = new ConditionMatch<AvatarPlaced>(new AvatarPlaced(obj), obj);
                return true;
            }
            match = default; return false;
        }
        internal bool IsEligible(GameObject obj)
        {
            if (!SceneObjectFilter.IsEditable(obj)) return false;
            if (!obj.TryGetComponent(_descriptor, out _)) return false;
            return PrefabUtility.IsPartOfPrefabInstance(obj) && PrefabUtility.IsAnyPrefabInstanceRoot(obj);
        }
    }
}
