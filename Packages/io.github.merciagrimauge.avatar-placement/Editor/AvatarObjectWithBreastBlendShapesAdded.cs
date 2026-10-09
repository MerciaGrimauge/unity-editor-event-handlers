using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>アバターの階層に追加された対象から、名前に breast を含むブレンドシェイプを検出した通知です。</summary>
    /// <remarks>存在だけを判定し、検出一覧は保持しません。具体的な対象の探索とシェイプの対応付けは購読者が行います。</remarks>
    public readonly struct AvatarObjectWithBreastBlendShapesAdded
    {
        /// <summary>追加先のアバターで、ハンドラーの編集範囲のルートです。</summary>
        /// <remarks>使用直前に == null または != null で未設定と破棄済みを確認してください。</remarks>
        public GameObject Avatar { get; }

        /// <summary>作成または親変更でアバターの階層に追加された対象です。</summary>
        /// <remarks>直下の子に限りません。使用直前に == null または != null で未設定と破棄済みを確認してください。</remarks>
        public GameObject AddedObject { get; }

        /// <summary>アバターと追加対象を保存します。</summary>
        /// <param name="avatar">編集範囲となるアバターです。</param>
        /// <param name="addedObject">アバターの階層へ追加された対象です。</param>
        public AvatarObjectWithBreastBlendShapesAdded(GameObject avatar, GameObject addedObject)
        {
            Avatar = avatar;
            AddedObject = addedObject;
        }
    }
}
