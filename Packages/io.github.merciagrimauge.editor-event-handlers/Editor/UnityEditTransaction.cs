using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    // Unity の Undo と Prefab 状態を扱います。アクセスは範囲を限定したハンドラーコンテキストに限ります。
    /// <summary>1回のハンドラー呼び出しについて、編集範囲の確認と Undo・Prefab の確定・復元を担当します。</summary>
    internal sealed class UnityEditTransaction
    {
        /// <summary>Undo グループの名前に含めるハンドラー識別子です。</summary>
        private readonly string _handlerId;
        /// <summary>編集範囲と期限の確認を提供する、この呼び出しのコンテキストです。</summary>
        private readonly HandlerContext _context;
        /// <summary>編集前の状態を Undo に記録済みのオブジェクト識別子です。</summary>
        private readonly HashSet<EditorObjectId> _recorded = new HashSet<EditorObjectId>();
        /// <summary>復元時に適用する、編集前の Prefab の変更記録です。</summary>
        private readonly Dictionary<EditorObjectId, PrefabState> _prefabStates = new Dictionary<EditorObjectId, PrefabState>();
        /// <summary>このトランザクションで作成したオブジェクトの識別子です。</summary>
        private readonly HashSet<EditorObjectId> _created = new HashSet<EditorObjectId>();
        /// <summary>この編集をまとめる Undo グループ番号です。開始前は -1 です。</summary>
        private int _group = -1;
        /// <summary>この編集記録が扱うシーン階層のルートです。</summary>
        private GameObject Root { get; }
        /// <summary>Undo グループが開始され、追跡対象の編集があるかどうかです。</summary>
        internal bool HasChanges => _group >= 0;

        /// <summary>編集前の Prefab の変更記録と、復元時に参照を解決するための識別子を保持します。</summary>
        private readonly struct PrefabState
        {
            /// <summary>編集前に保存した Prefab インスタンスの変更記録です。</summary>
            internal readonly PropertyModification[] Properties;
            /// <summary>保存した変更記録内のオブジェクト参照に対応する識別子です。</summary>
            internal readonly EditorObjectId[] ReferenceIds;
            /// <summary>Prefab の変更記録を保持し、各オブジェクト参照の一時的な識別子を保存します。</summary>
            /// <param name="properties">編集前の Prefab の変更記録です。null は空の一覧として扱います。</param>
            internal PrefabState(PropertyModification[] properties)
            {
                Properties = properties ?? Array.Empty<PropertyModification>();
                ReferenceIds = new EditorObjectId[Properties.Length];
                for (var i = 0; i < Properties.Length; i++)
                    if (Properties[i].objectReference != null) ReferenceIds[i] = EditorObjectId.FromObject(Properties[i].objectReference);
            }
        }

        /// <summary>編集範囲のルート、ハンドラー識別子、期限を確認するコンテキストを保持します。</summary>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <param name="handlerId">Undo グループの名前に使うハンドラー識別子です。</param>
        /// <param name="context">このハンドラー呼び出しの編集範囲と期限を確認するコンテキストです。</param>
        internal UnityEditTransaction(GameObject root, string handlerId, HandlerContext context)
        { Root = root; _handlerId = handlerId; _context = context; }

        // 対象が属する GameObject を返します。GameObject と Component だけを許可します。
        /// <summary>期限とルートの存続を確認し、対象が同じシーンの編集範囲内の GameObject または Component であることを要求します。</summary>
        /// <param name="target">同じシーンの編集範囲内にあることを確認する GameObject または Component です。</param>
        /// <returns>確認済み対象が属する GameObject です。</returns>
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

        /// <summary>既存の Prefab インスタンスの変更記録を1回だけ保存します。今回作成した階層は保存対象から除外します。</summary>
        /// <param name="target">編集前の Prefab インスタンスの状態を保存する対象です。</param>
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

        /// <summary>最初の編集で Undo グループを開始し、必要な Prefab の編集前の状態を保存します。</summary>
        /// <param name="target">追加で Prefab の状態を保存する対象です。null なら Root のみを保存します。</param>
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

        /// <summary>対象と型を確認して Component を Undo 対応で追加し、編集後の期限を確認します。</summary>
        /// <param name="target">Component を追加する、編集範囲内の GameObject です。</param>
        /// <param name="componentType">追加する Component の派生型です。</param>
        /// <returns>追加された Component です。</returns>
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

        // 各既存オブジェクトを、最初のプロパティ編集前に1回だけ記録します。
        // 作成・破棄・親変更には別の専用コンテキストメソッドを使います。
        /// <summary>対象を初回の編集前に記録し、同期編集後に Prefab の変更記録と期限を確認します。</summary>
        /// <param name="target">プロパティを編集する、編集範囲内の GameObject または Component です。</param>
        /// <param name="edit">指定対象のプロパティだけを同期的に編集する処理です。</param>
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

        /// <summary>編集範囲内に子の GameObject を作成し、作成と親子関係を Undo に記録します。</summary>
        /// <param name="name">作成する GameObject の名前です。</param>
        /// <param name="parent">作成先となる同じ編集範囲内の親です。null なら Root を使います。</param>
        /// <returns>作成した子の GameObject です。</returns>
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

        /// <summary>Prefab アセットを編集範囲のシーンに作成し、作成と親子関係を Undo に記録します。</summary>
        /// <param name="prefab">インスタンス化する Prefab アセットです。</param>
        /// <param name="parent">作成先となる同じ編集範囲内の親です。null なら Root を使います。</param>
        /// <returns>作成した Prefab インスタンスです。</returns>
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

        /// <summary>ルート移動や循環を拒否し、編集範囲内の子孫の親変更を Undo に記録します。</summary>
        /// <param name="child">親を変更する子孫です。Root 自体は指定できません。</param>
        /// <param name="parent">同じ編集範囲内の移動先の親です。</param>
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

        /// <summary>ルートと Transform の破棄を拒否し、編集範囲内の対象を Undo 対応で破棄します。</summary>
        /// <param name="target">破棄する、Root を除く編集範囲内の GameObject または Component です。</param>
        internal void Destroy(Object target)
        {
            var owner = RequireTarget(target);
            if (target == Root || target is Transform)
                throw new ArgumentException("The transaction root and Transform components may not be destroyed.", nameof(target));
            BeginChanges(owner);
            Undo.DestroyObjectImmediate(target);
            _context.CheckDeadline();
        }

        /// <summary>記録した変更を確定または復元します。復元時は Prefab の変更記録とオブジェクト参照も復元します。</summary>
        /// <param name="commit">true なら記録した変更を確定し、false なら復元します。</param>
        internal void Finish(bool commit)
        {
            if (_group < 0) return;
            // この編集記録が有効な間は非同期処理・yield・無関係な Editor 処理を挟みません。
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
