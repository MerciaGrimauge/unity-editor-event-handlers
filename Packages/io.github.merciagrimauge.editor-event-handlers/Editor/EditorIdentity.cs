using System;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if UNITY_6000_4_OR_NEWER
using NativeObjectId = UnityEngine.EntityId;
using NativeSceneId = UnityEngine.SceneManagement.SceneHandle;
#else
using NativeObjectId = System.Int32;
using NativeSceneId = System.Int32;
#endif

namespace EditorEventHandlers.Editor
{
    /// <summary>内部値を公開しない、一時的な Unity オブジェクト識別子です。シリアライズしたり、値からオブジェクトの状態を推測したりしないでください。</summary>
    /// <remarks>破棄後に識別子が再利用される場合があります。Unity のセッションやドメインをまたいで保持しないでください。比較時はオブジェクトへの参照を取得しません。</remarks>
    public readonly struct EditorObjectId : IEquatable<EditorObjectId>
    {
        private readonly NativeObjectId _value;
        internal EditorObjectId(NativeObjectId value) { _value = value; }

        /// <summary>識別子が既定値以外かどうかです。オブジェクトはすでに破棄されている場合があります。</summary>
        public bool IsValid => !_value.Equals(default(NativeObjectId));

        /// <summary>Editor のメインスレッドで存続しているオブジェクトの識別子を取得します。null または破棄済みなら default を返します。</summary>
        /// <param name="target">識別子を取得するオブジェクトです。</param>
        /// <returns>一時的な識別子です。null または破棄済みのオブジェクトなら default です。</returns>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public static EditorObjectId FromObject(Object target)
        {
            EventDispatcher.RequireMainThread();
            if (target == null) return default;
#if UNITY_6000_4_OR_NEWER
            return new EditorObjectId(target.GetEntityId());
#else
            return new EditorObjectId(target.GetInstanceID());
#endif
        }

        /// <summary>Editor のメインスレッドで識別子からオブジェクトへの参照を取得します。取得できない場合は null を返します。</summary>
        /// <returns>現在の Unity オブジェクトです。識別子が default または参照を取得できない場合は null です。</returns>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public Object Resolve()
        {
            EventDispatcher.RequireMainThread();
            if (!IsValid) return null;
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(_value);
#else
            return EditorUtility.InstanceIDToObject(_value);
#endif
        }

        /// <summary>Unity オブジェクトへの参照を取得せず、識別子全体を比較します。どのスレッドでも比較できます。</summary>
        /// <param name="other">比較対象の識別子です。</param>
        /// <returns>両方の識別子が等しいかどうかです。</returns>
        public bool Equals(EditorObjectId other) => _value.Equals(other._value);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorObjectId other && Equals(other);
        /// <summary>一時的なコレクションに使うハッシュ値です。一意または永続的なオブジェクト識別子ではありません。</summary>
        public override int GetHashCode() => _value.GetHashCode();
        /// <summary>オブジェクトへの参照を取得せず、識別子が等しいか比較します。</summary>
        public static bool operator ==(EditorObjectId left, EditorObjectId right) => left.Equals(right);
        /// <summary>オブジェクトへの参照を取得せず、識別子が異なるか比較します。</summary>
        public static bool operator !=(EditorObjectId left, EditorObjectId right) => !left.Equals(right);
    }

    /// <summary>内部値を公開しない、一時的な Unity シーン識別子です。値が等しくても、シーンが読み込み済みであるとは限りません。</summary>
    /// <remarks>Unity のセッションやドメインをまたいで保存しないでください。比較時はシーンの存在や読み込み状態を確認しません。</remarks>
    public readonly struct EditorSceneId : IEquatable<EditorSceneId>
    {
        private readonly NativeSceneId _value;
        internal EditorSceneId(NativeSceneId value) { _value = value; }
        /// <summary>識別子が既定値以外かどうかです。シーンがすでに存在しない、または読み込み済みではない場合があります。</summary>
        public bool IsValid => !_value.Equals(default(NativeSceneId));
        /// <summary>Editor のメインスレッドでシーンの識別子を取得します。無効なシーンなら default を返します。</summary>
        /// <param name="scene">識別子を取得するシーンです。</param>
        /// <returns>一時的な識別子です。無効なシーンなら default です。</returns>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public static EditorSceneId FromScene(Scene scene)
        {
            EventDispatcher.RequireMainThread();
            return scene.IsValid() ? new EditorSceneId(scene.handle) : default;
        }
        /// <summary>シーンの読み込み状態を確認せず、識別子全体を比較します。どのスレッドでも比較できます。</summary>
        /// <param name="other">比較対象の識別子です。</param>
        /// <returns>両方の識別子が等しいかどうかです。</returns>
        public bool Equals(EditorSceneId other) => _value.Equals(other._value);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorSceneId other && Equals(other);
        /// <summary>一時的なコレクションに使うハッシュ値です。一意または永続的なシーン識別子ではありません。</summary>
        public override int GetHashCode() => _value.GetHashCode();
        /// <summary>シーンの存在や読み込み状態を確認せず、識別子が等しいか比較します。</summary>
        public static bool operator ==(EditorSceneId left, EditorSceneId right) => left.Equals(right);
        /// <summary>シーンの存在や読み込み状態を確認せず、識別子が異なるか比較します。</summary>
        public static bool operator !=(EditorSceneId left, EditorSceneId right) => !left.Equals(right);
    }
}
