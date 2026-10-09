using System.Collections.Generic;
using UnityEngine;

namespace AvatarPlacement.Editor
{
    /// <summary>大文字・小文字を区別せず、名前に breast を含むブレンドシェイプを1つ検出した結果です。</summary>
    /// <remarks>Renderer と Mesh は現在の Unity オブジェクトへの参照です。保存したインデックスを使う前にメッシュを再確認してください。</remarks>
    public readonly struct BreastBlendShape
    {
        /// <summary>検出したブレンドシェイプを使うレンダラーです。後で状態が変わったり破棄されたりする場合があります。</summary>
        /// <remarks>使用直前に == null または != null で存続を確認してください。Unity の比較は未設定と破棄済みの両方を検出します。is null、?.、?? は破棄済み判定の代替にはなりません。</remarks>
        public SkinnedMeshRenderer Renderer { get; }
        /// <summary>検出時の共有メッシュです。状態の確認に使う Unity 参照です。</summary>
        /// <remarks>使用直前に == null または != null で存続を確認してください。Unity の比較は未設定と破棄済みの両方を検出します。is null、?.、?? は破棄済み判定の代替にはなりません。</remarks>
        public Mesh Mesh { get; }
        /// <summary>検出時のブレンドシェイプのインデックスです。メッシュが変わると無効になる場合があります。</summary>
        public int Index { get; }
        /// <summary>検出時に保存したブレンドシェイプ名です。</summary>
        public string Name { get; }

        /// <summary>検出時のレンダラー・メッシュ・インデックス・名前を保存します。</summary>
        /// <param name="renderer">検出したブレンドシェイプを使うレンダラーです。</param>
        /// <param name="mesh">検出時にレンダラーが使っていた共有メッシュです。</param>
        /// <param name="index">検出時のブレンドシェイプのインデックスです。</param>
        /// <param name="name">保存するブレンドシェイプ名です。</param>
        internal BreastBlendShape(SkinnedMeshRenderer renderer, Mesh mesh, int index, string name)
        { Renderer = renderer; Mesh = mesh; Index = index; Name = name; }
    }

    /// <summary>アバターの直下に作成または移動された子オブジェクトから、名前に breast を含むブレンドシェイプを検出した通知です。</summary>
    /// <remarks>名前の一致だけでは、衣装であることやアバターとの互換性を判断できません。Unity 参照の編集には HandlerContext を使ってください。</remarks>
    public sealed class AvatarChildBreastBlendShapesPlaced
    {
        /// <summary>ハンドラーの編集範囲のルートとして選んだ、直上の親アバターです。</summary>
        /// <remarks>使用直前に == null または != null で存続を確認してください。Unity の比較は未設定と破棄済みの両方を検出します。is null、?.、?? は破棄済み判定の代替にはなりません。</remarks>
        public GameObject Avatar { get; }
        /// <summary>作成または親を変更された直下の子オブジェクトです。Prefab インスタンスである必要はありません。</summary>
        /// <remarks>使用直前に == null または != null で存続を確認してください。Unity の比較は未設定と破棄済みの両方を検出します。is null、?.、?? は破棄済み判定の代替にはなりません。</remarks>
        public GameObject PlacedObject { get; }
        /// <summary>読み取り専用の検出結果です。後続のハンドラーが使う前に Unity 参照の状態が変わる場合があります。</summary>
        public IReadOnlyList<BreastBlendShape> BreastBlendShapes { get; }

        /// <summary>親アバターと配置対象を保持し、検出結果を読み取り専用の一覧にします。</summary>
        /// <param name="avatar">編集範囲のルートとなる親アバターです。</param>
        /// <param name="placedObject">アバター直下に作成または移動された対象です。</param>
        /// <param name="shapes">検出したブレンドシェイプの一覧です。</param>
        internal AvatarChildBreastBlendShapesPlaced(GameObject avatar, GameObject placedObject, BreastBlendShape[] shapes)
        { Avatar = avatar; PlacedObject = placedObject; BreastBlendShapes = System.Array.AsReadOnly(shapes); }
    }
}
