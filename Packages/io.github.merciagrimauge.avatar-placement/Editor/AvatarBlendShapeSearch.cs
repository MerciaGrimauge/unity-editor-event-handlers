using System;
using System.Collections.Generic;
using EditorEventHandlers.Editor;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>配置入力からアバターを特定し、胸シェイプの有無を探索します。</summary>
    internal static class AvatarBlendShapeSearch
    {
        /// <summary>配置入力の現在の対象と、最も近いアバターを取得します。</summary>
        /// <param name="context">入力変更と判定期限を持つコンテキストです。</param>
        /// <param name="descriptor">アバターを識別するコンポーネントの型です。</param>
        /// <param name="target">作成または親変更された対象です。</param>
        /// <param name="avatar">対象自身または祖先にある最も近いアバターです。</param>
        /// <returns>通常シーンの有効な配置入力とアバターがある場合は true、それ以外は false です。</returns>
        /// <exception cref="ConditionDeadlineExceededException">祖先をたどる途中で判定期限を超えた場合です。</exception>
        internal static bool TryGetAvatar(ConditionContext context, Type descriptor, out GameObject target, out GameObject avatar)
        {
            target = null;
            avatar = null;
            context.CheckDeadline();
            var change = context.Change;
            if (change.Kind != EditorChangeKind.Created && change.Kind != EditorChangeKind.ParentChanged)
                return false;
            target = change.GameObject;
            if (!SceneObjectFilter.IsEditable(target) || EditorSceneId.FromScene(target.scene) != change.SceneId)
                return false;

            if (change.Kind == EditorChangeKind.ParentChanged)
            {
                var parent = target.transform.parent;
                var currentParent = parent == null ? default : EditorObjectId.FromObject(parent.gameObject);
                if (change.NewParentId != currentParent)
                    return false;
                if (change.PreviousParentId == change.NewParentId && change.PreviousSceneId == change.SceneId)
                    return false;
            }

            for (var ancestor = target.transform; ancestor != null; ancestor = ancestor.parent)
            {
                context.CheckDeadline();
                if (!ancestor.TryGetComponent(descriptor, out _))
                    continue;
                avatar = ancestor.gameObject;
                return SceneObjectFilter.IsEditable(avatar);
            }
            return false;
        }

        /// <summary>対象階層から該当名を探し、最初の一致で探索を終了します。</summary>
        /// <param name="context">探索期限を確認するコンテキストです。</param>
        /// <param name="root">探索の起点です。</param>
        /// <param name="descriptor">入れ子の別アバターを除外するための識別型です。</param>
        /// <param name="excluded">この階層を除外する場合の起点です。指定しなければ追加の除外を行いません。</param>
        /// <returns>名前に breast を含むブレンドシェイプが1つでもあれば true、それ以外は false です。</returns>
        /// <remarks>大文字・小文字を区別しません。非アクティブな対象も探索し、Armature 以下と入れ子の別アバターを除外します。</remarks>
        /// <exception cref="ConditionDeadlineExceededException">階層やシェイプの探索中に判定期限を超えた場合です。</exception>
        internal static bool ContainsBreastShape(ConditionContext context, GameObject root, Type descriptor, Transform excluded = null)
        {
            var renderers = new List<SkinnedMeshRenderer>();
            var rootTransform = root.transform;
            for (var node = rootTransform; node != null;)
            {
                context.CheckDeadline();
                var skip = node == excluded || string.Equals(node.name, "Armature", StringComparison.OrdinalIgnoreCase)
                    || (node != rootTransform && node.TryGetComponent(descriptor, out _));
                if (!skip)
                {
                    renderers.Clear();
                    node.GetComponents(renderers);
                    foreach (var renderer in renderers)
                    {
                        context.CheckDeadline();
                        if (renderer == null || renderer.sharedMesh == null)
                            continue;
                        var mesh = renderer.sharedMesh;
                        for (var index = 0; index < mesh.blendShapeCount; index++)
                        {
                            context.CheckDeadline();
                            var name = mesh.GetBlendShapeName(index);
                            if (name != null && name.IndexOf("breast", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                    }
                }
                node = Next(node, rootTransform, !skip, context);
            }
            return false;
        }

        /// <summary>再帰呼び出しを使わず、深さ優先探索の次の対象を取得します。</summary>
        /// <param name="node">現在の探索対象です。</param>
        /// <param name="root">この階層の外へ出ないための起点です。</param>
        /// <param name="descend">現在の対象の子も探索する場合は true です。</param>
        /// <param name="context">祖先をたどる間も期限を確認するコンテキストです。</param>
        /// <returns>次の探索対象です。階層を調べ終えた場合は null です。</returns>
        private static Transform Next(Transform node, Transform root, bool descend, ConditionContext context)
        {
            if (descend && node.childCount > 0)
                return node.GetChild(0);
            while (node != root)
            {
                context.CheckDeadline();
                var parent = node.parent;
                if (parent == null)
                    return null;
                var nextIndex = node.GetSiblingIndex() + 1;
                if (nextIndex < parent.childCount)
                    return parent.GetChild(nextIndex);
                node = parent;
            }
            return null;
        }
    }
}
