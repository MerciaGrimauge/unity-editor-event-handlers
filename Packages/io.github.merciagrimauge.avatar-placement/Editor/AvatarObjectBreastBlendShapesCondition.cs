using System;
using EditorEventHandlers.Editor;

namespace AvatarPlacement.Editor
{
    /// <summary>アバターの階層へ追加された対象に、胸シェイプがあるかを判定します。</summary>
    internal sealed class AvatarObjectBreastBlendShapesCondition : IEventCondition<AvatarObjectWithBreastBlendShapesAdded>
    {
        /// <summary>アバターを識別する Descriptor コンポーネントの型です。</summary>
        private readonly Type _descriptor;

        /// <inheritdoc />
        public string Id => "io.github.merciagrimauge.avatar-placement.added-object-breast-blend-shapes";

        /// <inheritdoc />
        public EditorChangeKind Changes => EditorChangeKind.Created | EditorChangeKind.ParentChanged;

        /// <summary>アバターを識別する型を保持します。</summary>
        /// <param name="descriptor">アバターの Descriptor コンポーネントの型です。</param>
        internal AvatarObjectBreastBlendShapesCondition(Type descriptor)
        {
            _descriptor = descriptor;
        }

        /// <summary>直下に限定せず追加対象を探し、その階層に胸シェイプが1つでもあれば一致します。</summary>
        /// <param name="context">入力変更と判定期限を持つコンテキストです。</param>
        /// <param name="match">一致した通知と、編集範囲となるアバターです。</param>
        /// <returns>追加対象に該当するシェイプがある場合は true、それ以外は false です。</returns>
        /// <remarks>名前の一致は衣装や互換性の保証ではありません。アバター自身の配置と Armature 以下のシェイプは対象外です。</remarks>
        /// <exception cref="ConditionDeadlineExceededException">探索中に判定期限を超えた場合です。</exception>
        public bool TryMatch(ConditionContext context, out ConditionMatch<AvatarObjectWithBreastBlendShapesAdded> match)
        {
            match = default;
            if (!AvatarBlendShapeSearch.TryGetAvatar(context, _descriptor, out var target, out var avatar))
                return false;
            if (target == avatar)
                return false;
            if (!AvatarBlendShapeSearch.ContainsBreastShape(context, target, _descriptor))
                return false;

            match = new ConditionMatch<AvatarObjectWithBreastBlendShapesAdded>(
                new AvatarObjectWithBreastBlendShapesAdded(avatar, target), avatar);
            return true;
        }
    }
}
