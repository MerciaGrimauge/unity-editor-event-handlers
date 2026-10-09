using System;
using System.Collections.Generic;
using EditorEventHandlers.Editor;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>アバター直下への作成・親変更から、名前に breast を含むブレンドシェイプを検出する条件です。</summary>
    internal sealed class AvatarChildBreastBlendShapeCondition : IEventCondition<AvatarChildBreastBlendShapesPlaced>
    {
        /// <summary>アバターのルートを識別する Descriptor コンポーネントの型です。</summary>
        private readonly Type _descriptor;
        /// <summary>同じ入力バッチで評価済みのオブジェクト識別子です。</summary>
        private readonly HashSet<EditorObjectId> _seen = new HashSet<EditorObjectId>();
        /// <summary>重複排除の状態が属する入力バッチの識別子です。</summary>
        private long _batch = -1;
        /// <summary>子孫のレンダラーを列挙する際に再利用する作業用リストです。</summary>
        private readonly List<SkinnedMeshRenderer> _renderers = new List<SkinnedMeshRenderer>();
        /// <summary>1回の条件評価内で再利用する、メッシュごとのブレンドシェイプ名です。評価終了時に破棄します。</summary>
        private readonly Dictionary<Mesh, ShapeName[]> _meshMatches = new Dictionary<Mesh, ShapeName[]>();
        /// <summary>一つのメッシュから該当名を集める際に再利用する作業用リストです。</summary>
        private readonly List<ShapeName> _names = new List<ShapeName>();
        /// <summary>対象オブジェクトについて検出した一致結果の作業用リストです。</summary>
        private readonly List<BreastBlendShape> _matches = new List<BreastBlendShape>();

        /// <summary>1回の判定で再利用する、メッシュ内のブレンドシェイプ名とインデックスです。</summary>
        private readonly struct ShapeName
        {
            /// <summary>検出時のメッシュ内のブレンドシェイプのインデックスです。</summary>
            internal readonly int Index;
            /// <summary>検出時のブレンドシェイプ名です。</summary>
            internal readonly string Name;
            /// <summary>ブレンドシェイプのインデックスと名前を保存します。</summary>
            /// <param name="index">検出時のブレンドシェイプのインデックスです。</param>
            /// <param name="name">保存するブレンドシェイプ名です。</param>
            internal ShapeName(int index, string name) { Index = index; Name = name; }
        }

        /// <inheritdoc />
        public string Id => "io.github.merciagrimauge.avatar-placement.child-breast-blend-shapes";
        /// <inheritdoc />
        public EditorChangeKind Changes => EditorChangeKind.Created | EditorChangeKind.ParentChanged;
        /// <summary>アバターを識別する Descriptor 型を保持して条件を作成します。</summary>
        /// <param name="descriptor">アバターを識別する Descriptor コンポーネントの型です。</param>
        internal AvatarChildBreastBlendShapeCondition(Type descriptor) { _descriptor = descriptor; }

        /// <summary>入力対象の直上にアバターがある場合に子階層のメッシュを調べ、検出結果と親アバターを返します。</summary>
        /// <param name="context">入力変更と、この条件評価の期限を確認するコンテキストです。</param>
        /// <param name="match">一致した場合の通知と編集範囲のルートです。不一致の場合は使いません。</param>
        /// <returns>条件が一致して有効な通知とルートを返す場合は true、それ以外は false です。</returns>
        /// <remarks>通知の参照は現在の状態です。対象の重複排除は同じ入力バッチ内、メッシュ結果の再利用は1回の条件評価内に限り、永続的な履歴として保存しません。</remarks>
        /// <exception cref="ConditionDeadlineExceededException">判定の区切りで期限超過を検出した場合です。</exception>
        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarChildBreastBlendShapesPlaced> match)
        {
            match = default;
            context.CheckDeadline();
            if (_batch != context.BatchId) { _seen.Clear(); _batch = context.BatchId; }
            var change = context.Change;
            if (change.Kind != EditorChangeKind.Created && change.Kind != EditorChangeKind.ParentChanged) return false;
            var target = change.GameObject;
            if (!SceneObjectFilter.IsEditable(target) || EditorSceneId.FromScene(target.scene) != change.SceneId) return false;
            // 別のアバターの直下に配置されたアバター自身は、直下配置通知の対象にしません。
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
