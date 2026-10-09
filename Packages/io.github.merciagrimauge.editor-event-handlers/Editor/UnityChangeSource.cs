using System;
using System.Collections.Generic;
using UnityEditor;

namespace EditorEventHandlers.Editor
{
    // Unity の変更通知だけを扱います。通知の意味を判断する処理は登録された条件が担当します。
    /// <summary>Unity の変更通知を公開入力へ変換し、重複排除と更新時のバッチ配送を担当します。</summary>
    internal sealed class UnityChangeSource : IEditorChangeSource
    {
        /// <summary>まとめた入力変更を同期的に配送する処理です。</summary>
        private readonly Action<IReadOnlyList<EditorChange>> _dispatch;
        /// <summary>次の Editor 更新まで保留する入力変更です。</summary>
        private readonly List<EditorChange> Pending = new List<EditorChange>();
        /// <summary>保留中の入力変更の重複を除くための集合です。</summary>
        private readonly HashSet<EditorChange> Seen = new HashSet<EditorChange>();
        /// <summary>配送中の変更を保持し、保留リストとの干渉を避ける作業用リストです。</summary>
        private readonly List<EditorChange> Work = new List<EditorChange>();
        /// <summary>購読中の公開入力に対応する Unity の通知種類のビット集合です。</summary>
        private ulong _nativeKinds;
        /// <summary>通知購読、次回配送の予約、Undo による入力の抑止、配送実行の各状態です。</summary>
        private bool _active, _scheduled, _undoFence, _flushing;
        /// <summary>評価用の配送先を保持します。通知の購読は必要な種類が設定されたときに開始します。</summary>
        /// <param name="dispatch">次の Editor 更新で同期的に入力バッチを渡す配送先です。</param>
        internal UnityChangeSource(Action<IReadOnlyList<EditorChange>> dispatch) { _dispatch = dispatch; }

        // EventDispatcher.Kinds の各種類には、この対応表と OnChangesPublished の分岐が必要です。
        /// <summary>公開入力の種類を Unity の通知ビットへ変換し、必要な種類がある間だけ通知を購読します。</summary>
        /// <param name="kinds">購読が必要とする公開入力の種類です。None なら購読を終了します。</param>
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
        /// <summary>要求された公開種類に対応する Unity 通知のビットを購読対象へ追加します。</summary>
        /// <param name="kinds">現在要求されている公開入力の変更フラグです。</param>
        /// <param name="kind">対応を確認する公開入力の種類です。</param>
        /// <param name="native">公開入力に対応する Unity の通知種類です。</param>
        private void AddKind(EditorChangeKind kinds, EditorChangeKind kind, ObjectChangeKind native)
        { if ((kinds & kind) != 0) _nativeKinds |= 1UL << (int)native; }

        /// <summary>Unity 変更・Undo・モード変更・終了のイベントを購読または解除します。</summary>
        /// <param name="active">通知元の購読を有効にする場合は true です。</param>
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

        /// <summary>必要な Unity 通知だけを変更値へ変換し、編集可能状態を確認して次の更新へ配送を予約します。</summary>
        /// <param name="stream">Unity が公開した変更通知のストリームです。</param>
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
        /// <summary>同じ種類と全識別子の変更を重複排除して保留入力へ追加します。</summary>
        /// <param name="change">通知時点の種類と識別子を保持する入力変更です。</param>
        private void Enqueue(EditorChange change)
        { if (Seen.Add(change)) Pending.Add(change); }

        /// <summary>保留入力を配送用の一覧へ移し、編集可能な場合だけ同期配送して作業一覧を解放します。</summary>
        internal void Flush()
        {
            EditorApplication.update -= Flush; _scheduled = false;
            if (!_active || _undoFence || !EventDispatcher.CanEdit()) { Pending.Clear(); Seen.Clear(); return; }
            Work.Clear(); Work.AddRange(Pending); Pending.Clear(); Seen.Clear();
            _flushing = true;
            try { _dispatch(Work); }
            finally { _flushing = false; Work.Clear(); }
        }
        /// <summary>保留入力を破棄し、次の Editor 更新まで Undo・Redo に伴う通知を除外します。</summary>
        /// <param name="info">Undo・Redo の通知情報です。この処理では個別の情報を使いません。</param>
        private void OnUndoRedo(in UndoRedoInfo info)
        {
            CancelPending(); _undoFence = true;
            EditorApplication.update -= ReleaseUndoFence;
            EditorApplication.update += ReleaseUndoFence;
        }
        /// <summary>更新の購読を解除し、Undo・Redo 通知の一時的な除外を終了します。</summary>
        private void ReleaseUndoFence()
        { EditorApplication.update -= ReleaseUndoFence; _undoFence = false; }
        /// <summary>Play モードの切り替えに伴い、保留入力と更新予約を取り消します。</summary>
        /// <param name="state">切り替わった Play モードです。どの状態への遷移でも保留入力を破棄します。</param>
        private void OnPlayModeChanged(PlayModeStateChange state) => CancelPending();
        /// <summary>更新予約・Undo 除外・保留入力を取り消します。配送中の作業一覧は配送側の終了処理に任せます。</summary>
        private void CancelPending()
        {
            EditorApplication.update -= Flush; EditorApplication.update -= ReleaseUndoFence;
            _scheduled = false; _undoFence = false; Pending.Clear(); Seen.Clear();
            // 配送構成の変更で通知元が無効になっても、配送中の Work は Flush が読み終えてからクリアします。
            if (!_flushing) Work.Clear();
        }
        /// <summary>通知の購読を終了し、保留入力を取り消します。</summary>
        public void Dispose() => SetKinds(EditorChangeKind.None);
    }
}
