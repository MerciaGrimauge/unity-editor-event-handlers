using System.Collections.Generic;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>大文字・小文字を区別せず、名前に breast を含むブレンドシェイプを1つ検出した結果です。</summary>
    /// <remarks>Renderer と Mesh は現在の Unity オブジェクトへの参照です。保存したインデックスを使う前にメッシュを再確認してください。</remarks>
    public readonly struct BreastBlendShape
    {
        /// <summary>検出したブレンドシェイプを使うレンダラーです。後で状態が変わったり破棄されたりする場合があります。</summary>
        public SkinnedMeshRenderer Renderer { get; }
        /// <summary>検出時の共有メッシュです。状態の確認に使う Unity 参照です。</summary>
        public Mesh Mesh { get; }
        /// <summary>検出時のブレンドシェイプのインデックスです。メッシュが変わると無効になる場合があります。</summary>
        public int Index { get; }
        /// <summary>検出時に保存したブレンドシェイプ名です。</summary>
        public string Name { get; }

        internal BreastBlendShape(SkinnedMeshRenderer renderer, Mesh mesh, int index, string name)
        { Renderer = renderer; Mesh = mesh; Index = index; Name = name; }
    }

    /// <summary>アバターの直下に作成または移動された子オブジェクトから、名前に breast を含むブレンドシェイプを検出した通知です。</summary>
    /// <remarks>名前の一致だけでは、衣装であることやアバターとの互換性を判断できません。Unity 参照の編集には HandlerContext を使ってください。</remarks>
    public sealed class AvatarChildBreastBlendShapesPlaced
    {
        /// <summary>ハンドラーの編集範囲のルートとして選んだ、直上の親アバターです。</summary>
        public GameObject Avatar { get; }
        /// <summary>作成または親を変更された直下の子オブジェクトです。Prefab インスタンスである必要はありません。</summary>
        public GameObject PlacedObject { get; }
        /// <summary>読み取り専用の検出結果です。後続のハンドラーが使う前に Unity 参照の状態が変わる場合があります。</summary>
        public IReadOnlyList<BreastBlendShape> BreastBlendShapes { get; }

        internal AvatarChildBreastBlendShapesPlaced(GameObject avatar, GameObject placedObject, BreastBlendShape[] shapes)
        { Avatar = avatar; PlacedObject = placedObject; BreastBlendShapes = System.Array.AsReadOnly(shapes); }
    }
}
