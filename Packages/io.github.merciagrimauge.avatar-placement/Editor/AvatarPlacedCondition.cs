using System;
using EditorEventHandlers.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace AvatarPlacement.Editor
{
    // Public semantic contract. Consumers do not need the condition implementation.
    /// <summary>Notification that the standard condition detected an avatar Prefab instance root being created.</summary>
    /// <remarks>The Unity reference exposes current state for inspection; edit through the supplied HandlerContext.</remarks>
    public readonly struct AvatarPlaced
    {
        /// <summary>Detected avatar Prefab instance root; it may change or be destroyed before later handlers use it.</summary>
        public GameObject Avatar { get; }
        /// <summary>Creates a notification value without validating or publishing it.</summary>
        /// <param name="avatar">Avatar reference stored in the value.</param>
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
