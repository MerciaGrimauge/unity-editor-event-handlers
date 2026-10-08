using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    // Unity-specific undo and prefab state. Access is limited to the scoped handler context.
    internal sealed class UnityEditTransaction
    {
        private readonly string _handlerId;
        private readonly HandlerContext _context;
        private readonly HashSet<EditorObjectId> _recorded = new HashSet<EditorObjectId>();
        private readonly Dictionary<EditorObjectId, PrefabState> _prefabStates = new Dictionary<EditorObjectId, PrefabState>();
        private readonly HashSet<EditorObjectId> _created = new HashSet<EditorObjectId>();
        private int _group = -1;
        private GameObject Root { get; }
        internal bool HasChanges => _group >= 0;

        private readonly struct PrefabState
        {
            internal readonly PropertyModification[] Properties;
            internal readonly EditorObjectId[] ReferenceIds;
            internal PrefabState(PropertyModification[] properties)
            {
                Properties = properties ?? Array.Empty<PropertyModification>();
                ReferenceIds = new EditorObjectId[Properties.Length];
                for (var i = 0; i < Properties.Length; i++)
                    if (Properties[i].objectReference != null) ReferenceIds[i] = EditorObjectId.FromObject(Properties[i].objectReference);
            }
        }

        internal UnityEditTransaction(GameObject root, string handlerId, HandlerContext context)
        { Root = root; _handlerId = handlerId; _context = context; }

        // Returns the GameObject that owns the target; only GameObjects and Components pass.
        private GameObject RequireTarget(Object target)
        {
            _context.CheckDeadline();
            if (Root == null) throw new InvalidOperationException("The transaction root no longer exists.");
            var obj = target as GameObject;
            if (target is Component component) obj = component.gameObject;
            if (obj == null || EditorUtility.IsPersistent(target) || obj.scene != Root.scene
                || (obj != Root && !obj.transform.IsChildOf(Root.transform)))
                throw new ArgumentException("Only the transaction root and its scene hierarchy may be edited.", nameof(target));
            return obj;
        }

        private void CapturePrefabState(GameObject target)
        {
            var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
            if (instanceRoot == null) return;
            var id = EditorObjectId.FromObject(instanceRoot);
            if (_created.Contains(id) || _prefabStates.ContainsKey(id)) return;
            for (var ancestor = instanceRoot.transform; ancestor != null && ancestor != Root.transform; ancestor = ancestor.parent)
                if (_created.Contains(EditorObjectId.FromObject(ancestor.gameObject))) return;
            _prefabStates.Add(id, new PrefabState(PrefabUtility.GetPropertyModifications(instanceRoot)));
        }

        private void BeginChanges(GameObject target = null)
        {
            if (_group < 0)
            {
                CapturePrefabState(Root);
                Undo.IncrementCurrentGroup();
                _group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Editor event: " + _handlerId);
            }
            if (target != null) CapturePrefabState(target);
        }

        internal Component AddComponent(GameObject target, Type componentType)
        {
            RequireTarget(target);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                throw new ArgumentException("A Unity component type is required.", nameof(componentType));
            BeginChanges(target);
            var added = Undo.AddComponent(target, componentType);
            if (added == null) throw new InvalidOperationException("Unity did not add the requested component.");
            _context.CheckDeadline();
            return added;
        }

        // Captures each existing object once, before its first property edit.
        // Creation, destruction and parenting must use the other context methods.
        internal void Modify(Object target, Action edit)
        {
            var owner = RequireTarget(target);
            if (edit == null) throw new ArgumentNullException(nameof(edit));
            BeginChanges(owner);
            if (_recorded.Add(EditorObjectId.FromObject(target))) Undo.RegisterCompleteObjectUndo(target, "Editor event properties");
            edit();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            _context.CheckDeadline();
        }

        internal GameObject CreateChild(string name, GameObject parent = null)
        {
            parent = parent != null ? parent : Root;
            RequireTarget(parent);
            BeginChanges(parent);
            var child = new GameObject(name);
            _created.Add(EditorObjectId.FromObject(child));
            SceneManager.MoveGameObjectToScene(child, Root.scene);
            Undo.RegisterCreatedObjectUndo(child, "Editor event child");
            Undo.SetTransformParent(child.transform, parent.transform, "Editor event parenting");
            _context.CheckDeadline();
            return child;
        }

        internal GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null)
        {
            parent = parent != null ? parent : Root;
            RequireTarget(parent);
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new ArgumentException("A prefab asset is required.", nameof(prefab));
            BeginChanges(parent);
            var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Root.scene);
            _created.Add(EditorObjectId.FromObject(child));
            Undo.RegisterCreatedObjectUndo(child, "Editor event prefab");
            Undo.SetTransformParent(child.transform, parent.transform, "Editor event parenting");
            _context.CheckDeadline();
            return child;
        }

        internal void SetParent(GameObject child, GameObject parent)
        {
            RequireTarget(child);
            RequireTarget(parent);
            if (child == Root) throw new ArgumentException("The transaction root may not be reparented.", nameof(child));
            if (child == parent || parent.transform.IsChildOf(child.transform))
                throw new ArgumentException("Parenting would create a cycle.", nameof(parent));
            BeginChanges(child);
            CapturePrefabState(parent);
            Undo.SetTransformParent(child.transform, parent.transform, "Editor event parenting");
            _context.CheckDeadline();
        }

        internal void Destroy(Object target)
        {
            var owner = RequireTarget(target);
            if (target == Root || target is Transform)
                throw new ArgumentException("The transaction root and Transform components may not be destroyed.", nameof(target));
            BeginChanges(owner);
            Undo.DestroyObjectImmediate(target);
            _context.CheckDeadline();
        }

        internal void Finish(bool commit)
        {
            if (_group < 0) return;
            // No async/yield or unrelated editor work is allowed while this transaction is open.
            Undo.FlushUndoRecordObjects();
            try
            {
                if (commit) Undo.CollapseUndoOperations(_group);
                else
                {
                    Undo.RevertAllDownToGroup(_group);
                    foreach (var state in _prefabStates)
                    {
                        var instanceRoot = state.Key.Resolve() as GameObject;
                        if (instanceRoot == null) throw new InvalidOperationException("A recorded prefab instance was not restored.");
                        var saved = state.Value;
                        var restored = new PropertyModification[saved.Properties.Length];
                        for (var i = 0; i < restored.Length; i++)
                        {
                            var property = saved.Properties[i];
                            var reference = saved.ReferenceIds[i].IsValid ? saved.ReferenceIds[i].Resolve() : null;
                            if (saved.ReferenceIds[i].IsValid && reference == null)
                                throw new InvalidOperationException("A recorded prefab property reference was not restored.");
                            restored[i] = new PropertyModification
                            { target = property.target, propertyPath = property.propertyPath, value = property.value, objectReference = reference };
                        }
                        PrefabUtility.SetPropertyModifications(instanceRoot, restored);
                    }
                }
            }
            finally { Undo.IncrementCurrentGroup(); }
        }
    }
}
