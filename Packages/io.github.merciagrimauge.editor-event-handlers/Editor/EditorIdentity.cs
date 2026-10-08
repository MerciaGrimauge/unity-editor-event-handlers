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
    /// <summary>Opaque, temporary Unity object identity. Do not serialize or infer object state from it.</summary>
    /// <remarks>Identities may be reused after destruction. Do not retain across Unity sessions or domains. Comparisons do not resolve objects.</remarks>
    public readonly struct EditorObjectId : IEquatable<EditorObjectId>
    {
        private readonly NativeObjectId _value;
        internal EditorObjectId(NativeObjectId value) { _value = value; }

        /// <summary>Whether this is a non-default identity; the object may already have been destroyed.</summary>
        public bool IsValid => !_value.Equals(default(NativeObjectId));

        /// <summary>Captures a live object's identity on the editor main thread. Null or destroyed objects return default.</summary>
        /// <param name="target">Object to identify.</param>
        /// <returns>Temporary identity, or default for null or a destroyed object.</returns>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
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

        /// <summary>Resolves the identity on the editor main thread. Returns null when it cannot be resolved.</summary>
        /// <returns>Current Unity object, or null for default or an unavailable identity.</returns>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
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

        /// <summary>Compares complete identities without resolving Unity objects; any thread may compare.</summary>
        /// <param name="other">Identity to compare.</param>
        /// <returns>Whether both identities are equal.</returns>
        public bool Equals(EditorObjectId other) => _value.Equals(other._value);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorObjectId other && Equals(other);
        /// <summary>Hash for temporary collections; not a unique or persistent object identifier.</summary>
        public override int GetHashCode() => _value.GetHashCode();
        /// <summary>Compares identities for equality without resolving objects.</summary>
        public static bool operator ==(EditorObjectId left, EditorObjectId right) => left.Equals(right);
        /// <summary>Compares identities for inequality without resolving objects.</summary>
        public static bool operator !=(EditorObjectId left, EditorObjectId right) => !left.Equals(right);
    }

    /// <summary>Opaque, temporary Unity scene identity. Equality does not establish that a scene is loaded.</summary>
    /// <remarks>Do not persist across Unity sessions or domains. Comparisons do not check scene existence or loading.</remarks>
    public readonly struct EditorSceneId : IEquatable<EditorSceneId>
    {
        private readonly NativeSceneId _value;
        internal EditorSceneId(NativeSceneId value) { _value = value; }
        /// <summary>Whether this is a non-default identity; the scene may no longer exist or be loaded.</summary>
        public bool IsValid => !_value.Equals(default(NativeSceneId));
        /// <summary>Captures a scene's identity on the editor main thread. Invalid scenes return default.</summary>
        /// <param name="scene">Scene to identify.</param>
        /// <returns>Temporary identity, or default for an invalid scene.</returns>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public static EditorSceneId FromScene(Scene scene)
        {
            EventDispatcher.RequireMainThread();
            return scene.IsValid() ? new EditorSceneId(scene.handle) : default;
        }
        /// <summary>Compares complete identities without checking scene loading; any thread may compare.</summary>
        /// <param name="other">Identity to compare.</param>
        /// <returns>Whether both identities are equal.</returns>
        public bool Equals(EditorSceneId other) => _value.Equals(other._value);
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorSceneId other && Equals(other);
        /// <summary>Hash for temporary collections; not a unique or persistent scene identifier.</summary>
        public override int GetHashCode() => _value.GetHashCode();
        /// <summary>Compares identities for equality without checking scene existence or loading.</summary>
        public static bool operator ==(EditorSceneId left, EditorSceneId right) => left.Equals(right);
        /// <summary>Compares identities for inequality without checking scene existence or loading.</summary>
        public static bool operator !=(EditorSceneId left, EditorSceneId right) => !left.Equals(right);
    }
}
