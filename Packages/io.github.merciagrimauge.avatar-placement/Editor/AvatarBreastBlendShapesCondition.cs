using System;
using EditorEventHandlers.Editor;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>アバターが、名前に breast を含むブレンドシェイプを持つ場合の通知です。</summary>
    /// <remarks>存在だけを判定します。対象のレンダラーやシェイプの探索は購読者が行います。</remarks>
    public readonly struct AvatarHasBreastBlendShapes
    {
        /// <summary>胸シェイプが見つかったアバターで、ハンドラーの編集範囲のルートです。</summary>
        /// <remarks>使用直前に == null または != null で未設定と破棄済みを確認してください。</remarks>
        public GameObject Avatar { get; }

        /// <summary>通知対象のアバターを保存します。</summary>
        /// <param name="avatar">胸シェイプが見つかったアバターです。</param>
        public AvatarHasBreastBlendShapes(GameObject avatar)
        {
            Avatar = avatar;
        }
    }

    /// <summary>配置入力に対応するアバターの胸シェイプの有無を判定します。</summary>
    internal sealed class AvatarBreastBlendShapesCondition : IEventCondition<AvatarHasBreastBlendShapes>
    {
        /// <summary>アバターを識別する Descriptor コンポーネントの型です。</summary>
        private readonly Type _descriptor;

        /// <inheritdoc />
        public string Id => "io.github.merciagrimauge.avatar-placement.avatar-breast-blend-shapes";

        /// <inheritdoc />
        public EditorChangeKind Changes => EditorChangeKind.Created | EditorChangeKind.ParentChanged;

        /// <summary>アバターを識別する型を保持します。</summary>
        /// <param name="descriptor">アバターの Descriptor コンポーネントの型です。</param>
        internal AvatarBreastBlendShapesCondition(Type descriptor)
        {
            _descriptor = descriptor;
        }

        /// <summary>同じ配置入力のアバターを調べ、胸シェイプが1つでもあれば一致します。</summary>
        /// <param name="context">入力変更と判定期限を持つコンテキストです。</param>
        /// <param name="match">一致した通知と、編集範囲となるアバターです。</param>
        /// <returns>アバター側に該当するシェイプがある場合は true、それ以外は false です。</returns>
        /// <remarks>Armature 以下と別アバターは除外します。追加対象のシェイプだけでアバター側の条件が成立しないよう、追加対象の階層も除外します。</remarks>
        /// <exception cref="ConditionDeadlineExceededException">探索中に判定期限を超えた場合です。</exception>
        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarHasBreastBlendShapes> match)
        {
            match = default;
            if (!AvatarBlendShapeSearch.TryGetAvatar(context, _descriptor, out var target, out var avatar))
                return false;
            var excluded = target == avatar ? null : target.transform;
            if (!AvatarBlendShapeSearch.ContainsBreastShape(context, avatar, _descriptor, excluded))
                return false;

            match = new ConditionMatch<AvatarHasBreastBlendShapes>(new AvatarHasBreastBlendShapes(avatar), avatar);
            return true;
        }
    }
}
