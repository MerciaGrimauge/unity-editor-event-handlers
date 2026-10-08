using System;
using System.Collections.Generic;
using EditorEventHandlers.Editor;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    internal sealed class AvatarChildBreastBlendShapeCondition : IEventCondition<AvatarChildBreastBlendShapesPlaced>
    {
        private readonly Type _descriptor;
        private readonly HashSet<EditorObjectId> _seen = new HashSet<EditorObjectId>();
        private long _batch = -1;
        private readonly List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
        private readonly Dictionary<Mesh, ShapeName[]> _meshMatches = new Dictionary<Mesh, ShapeName[]>();
        private readonly List<ShapeName> _names = new List<ShapeName>();
        private readonly List<BreastBlendShape> _matches = new List<BreastBlendShape>();

        private readonly struct ShapeName
        {
            internal readonly int Index;
            internal readonly string Name;
            internal ShapeName(int index, string name) { Index = index; Name = name; }
        }

        public string Id => "io.github.merciagrimauge.avatar-placement.child-breast-blend-shapes";
        public EditorChangeKind Changes => EditorChangeKind.Created | EditorChangeKind.ParentChanged;
        internal AvatarChildBreastBlendShapeCondition(Type descriptor) { _descriptor = descriptor; }

        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarChildBreastBlendShapesPlaced> match)
        {
            match = default;
            context.CheckDeadline();
            if (_batch != context.BatchId) { _seen.Clear(); _batch = context.BatchId; }
            var change = context.Change;
            if (change.Kind != EditorChangeKind.Created && change.Kind != EditorChangeKind.ParentChanged) return false;
            var target = change.GameObject;
            if (!SceneObjectFilter.IsEditable(target) || EditorSceneId.FromScene(target.scene) != change.SceneId) return false;
            // An avatar placed under another avatar is not a placed object.
            if (target.TryGetComponent(_descriptor, out _)) return false;
            var parent = target.transform.parent;
            if (parent == null || !parent.TryGetComponent(_descriptor, out _)) return false;
            if (change.Kind == EditorChangeKind.ParentChanged && (change.NewParentId != EditorObjectId.FromObject(parent.gameObject)
                || change.PreviousParentId == change.NewParentId)) return false;
            if (!_seen.Add(EditorObjectId.FromObject(target))) return false;

            try
            {
                target.GetComponentsInChildren(true, _renderers);
                context.CheckDeadline();
                foreach (var renderer in _renderers)
                {
                    context.CheckDeadline();
                    if (renderer == null || renderer.sharedMesh == null) continue;
                    var mesh = renderer.sharedMesh;
                    if (!_meshMatches.TryGetValue(mesh, out var shapes))
                    {
                        _names.Clear();
                        var count = mesh.blendShapeCount;
                        for (var i = 0; i < count; i++)
                        {
                            if ((i & 15) == 0) context.CheckDeadline();
                            var name = mesh.GetBlendShapeName(i);
                            if (name != null && name.IndexOf("breast", StringComparison.OrdinalIgnoreCase) >= 0)
                                _names.Add(new ShapeName(i, name));
                        }
                        shapes = _names.Count == 0 ? Array.Empty<ShapeName>() : _names.ToArray();
                        _meshMatches.Add(mesh, shapes);
                    }
                    foreach (var shape in shapes)
                        _matches.Add(new BreastBlendShape(renderer, mesh, shape.Index, shape.Name));
                }
                context.CheckDeadline();
                if (_matches.Count == 0) return false;
                var notification = new AvatarChildBreastBlendShapesPlaced(parent.gameObject, target, _matches.ToArray());
                match = new ConditionMatch<AvatarChildBreastBlendShapesPlaced>(notification, parent.gameObject);
                return true;
            }
            finally { _renderers.Clear(); _meshMatches.Clear(); _names.Clear(); _matches.Clear(); }
        }
    }
}
