using System;
using System.Collections.Generic;
using UnityEditor;

namespace EditorEventHandlers.Editor
{
    // Native notifications only. Semantic detection belongs to registered conditions.
    internal sealed class UnityChangeSource : IEditorChangeSource
    {
        private readonly Action<IReadOnlyList<EditorChange>> _dispatch;
        private readonly List<EditorChange> Pending = new List<EditorChange>();
        private readonly HashSet<EditorChange> Seen = new HashSet<EditorChange>();
        private readonly List<EditorChange> Work = new List<EditorChange>();
        private ulong _nativeKinds;
        private bool _active, _scheduled, _undoFence, _flushing;
        internal UnityChangeSource(Action<IReadOnlyList<EditorChange>> dispatch) { _dispatch = dispatch; }

        // Every EventDispatcher.Kinds member needs a mapping here and a case in OnChangesPublished.
        public void SetKinds(EditorChangeKind kinds)
        {
            _nativeKinds = 0;
            AddKind(kinds, EditorChangeKind.Created, ObjectChangeKind.CreateGameObjectHierarchy);
            AddKind(kinds, EditorChangeKind.ParentChanged, ObjectChangeKind.ChangeGameObjectParent);
            AddKind(kinds, EditorChangeKind.PropertiesChanged, ObjectChangeKind.ChangeGameObjectOrComponentProperties);
            AddKind(kinds, EditorChangeKind.StructureChanged, ObjectChangeKind.ChangeGameObjectStructure);
            AddKind(kinds, EditorChangeKind.HierarchyChanged, ObjectChangeKind.ChangeGameObjectStructureHierarchy);
            AddKind(kinds, EditorChangeKind.ChildrenReordered, ObjectChangeKind.ChangeChildrenOrder);
            AddKind(kinds, EditorChangeKind.Destroyed, ObjectChangeKind.DestroyGameObjectHierarchy);
            AddKind(kinds, EditorChangeKind.PrefabUpdated, ObjectChangeKind.UpdatePrefabInstances);
            SetActive(_nativeKinds != 0);
        }
        private void AddKind(EditorChangeKind kinds, EditorChangeKind kind, ObjectChangeKind native)
        { if ((kinds & kind) != 0) _nativeKinds |= 1UL << (int)native; }

        private void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            if (active)
            {
                ObjectChangeEvents.changesPublished += OnChangesPublished;
                Undo.undoRedoEvent += OnUndoRedo;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                AssemblyReloadEvents.beforeAssemblyReload += CancelPending;
                EditorApplication.quitting += CancelPending;
            }
            else
            {
                CancelPending();
                ObjectChangeEvents.changesPublished -= OnChangesPublished;
                Undo.undoRedoEvent -= OnUndoRedo;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                AssemblyReloadEvents.beforeAssemblyReload -= CancelPending;
                EditorApplication.quitting -= CancelPending;
            }
        }

        internal void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            var canEditChecked = false;
            for (var i = 0; i < stream.length; i++)
            {
                var kind = stream.GetEventType(i);
                if ((int)kind < 0 || (int)kind >= 64 || (_nativeKinds & (1UL << (int)kind)) == 0) continue;
                if (_undoFence) return;
                if (!canEditChecked)
                { if (!EventDispatcher.CanEdit()) return; canEditChecked = true; }
                switch (kind)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        stream.GetCreateGameObjectHierarchyEvent(i, out var created);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.Created, new EditorObjectId(created.entityId), new EditorSceneId(created.scene.handle))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.Created, new EditorObjectId(created.instanceId), new EditorSceneId(created.scene.handle))); break;
#endif
                    case ObjectChangeKind.ChangeGameObjectParent:
                        stream.GetChangeGameObjectParentEvent(i, out var parent);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.ParentChanged, new EditorObjectId(parent.entityId), new EditorSceneId(parent.newScene.handle),
                            new EditorSceneId(parent.previousScene.handle), new EditorObjectId(parent.previousParentEntityId), new EditorObjectId(parent.newParentEntityId))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.ParentChanged, new EditorObjectId(parent.instanceId), new EditorSceneId(parent.newScene.handle),
                            new EditorSceneId(parent.previousScene.handle), new EditorObjectId(parent.previousParentInstanceId), new EditorObjectId(parent.newParentInstanceId))); break;
#endif
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var properties);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.PropertiesChanged, new EditorObjectId(properties.entityId), new EditorSceneId(properties.scene.handle))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.PropertiesChanged, new EditorObjectId(properties.instanceId), new EditorSceneId(properties.scene.handle))); break;
#endif
                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(i, out var structure);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.StructureChanged, new EditorObjectId(structure.entityId), new EditorSceneId(structure.scene.handle))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.StructureChanged, new EditorObjectId(structure.instanceId), new EditorSceneId(structure.scene.handle))); break;
#endif
                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var hierarchy);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.HierarchyChanged, new EditorObjectId(hierarchy.entityId), new EditorSceneId(hierarchy.scene.handle))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.HierarchyChanged, new EditorObjectId(hierarchy.instanceId), new EditorSceneId(hierarchy.scene.handle))); break;
#endif
                    case ObjectChangeKind.ChangeChildrenOrder:
                        stream.GetChangeChildrenOrderEvent(i, out var order);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.ChildrenReordered, new EditorObjectId(order.entityId), new EditorSceneId(order.scene.handle))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.ChildrenReordered, new EditorObjectId(order.instanceId), new EditorSceneId(order.scene.handle))); break;
#endif
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyed);
#if UNITY_6000_4_OR_NEWER
                        Enqueue(new EditorChange(EditorChangeKind.Destroyed, new EditorObjectId(destroyed.entityId), new EditorSceneId(destroyed.scene.handle),
                            new EditorSceneId(destroyed.scene.handle), new EditorObjectId(destroyed.parentEntityId))); break;
#else
                        Enqueue(new EditorChange(EditorChangeKind.Destroyed, new EditorObjectId(destroyed.instanceId), new EditorSceneId(destroyed.scene.handle),
                            new EditorSceneId(destroyed.scene.handle), new EditorObjectId(destroyed.parentInstanceId))); break;
#endif
                    case ObjectChangeKind.UpdatePrefabInstances:
                        stream.GetUpdatePrefabInstancesEvent(i, out var prefab);
#if UNITY_6000_4_OR_NEWER
                        foreach (var id in prefab.entityIds)
#else
                        foreach (var id in prefab.instanceIds)
#endif
                        {
                            Enqueue(new EditorChange(EditorChangeKind.PrefabUpdated, new EditorObjectId(id), new EditorSceneId(prefab.scene.handle)));
                        }
                        break;
                }
            }
            if (Pending.Count == 0 || _scheduled) return;
            _scheduled = true; EditorApplication.update += Flush;
        }
        private void Enqueue(EditorChange change)
        { if (Seen.Add(change)) Pending.Add(change); }

        internal void Flush()
        {
            EditorApplication.update -= Flush; _scheduled = false;
            if (!_active || _undoFence || !EventDispatcher.CanEdit()) { Pending.Clear(); Seen.Clear(); return; }
            Work.Clear(); Work.AddRange(Pending); Pending.Clear(); Seen.Clear();
            _flushing = true;
            try { _dispatch(Work); }
            finally { _flushing = false; Work.Clear(); }
        }
        private void OnUndoRedo(in UndoRedoInfo info)
        {
            CancelPending(); _undoFence = true;
            EditorApplication.update -= ReleaseUndoFence;
            EditorApplication.update += ReleaseUndoFence;
        }
        private void ReleaseUndoFence()
        { EditorApplication.update -= ReleaseUndoFence; _undoFence = false; }
        private void OnPlayModeChanged(PlayModeStateChange state) => CancelPending();
        private void CancelPending()
        {
            EditorApplication.update -= Flush; EditorApplication.update -= ReleaseUndoFence;
            _scheduled = false; _undoFence = false; Pending.Clear(); Seen.Clear();
            // Route changes during dispatch can deactivate this source while Flush still reads Work; Flush clears it.
            if (!_flushing) Work.Clear();
        }
        public void Dispose() => SetKinds(EditorChangeKind.None);
    }
}
