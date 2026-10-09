using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorEventHandlers.Editor
{
    // この型が Unity API との境界を担当し、登録と配送の方針はディスパッチャーに残します。
    /// <summary>Unity API を使って内部の実行環境契約を実装します。登録・配送の方針はディスパッチャーが担当します。</summary>
    internal sealed class UnityEditorHost : IEditorHost
    {
        /// <summary>Editor のメインスレッドを確認するために初期化時に保存した識別子です。</summary>
        private readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        /// <inheritdoc />
        public bool CanEdit => !EditorApplication.isPlayingOrWillChangePlaymode
            && !EditorApplication.isCompiling && !EditorApplication.isUpdating
            && !BuildPipeline.isBuildingPlayer && !Undo.isProcessing;
        /// <inheritdoc />
        public string SettingsScope => Application.dataPath;

        /// <inheritdoc />
        public void RequireMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
                throw new InvalidOperationException("Editor events must be used on the Unity editor main thread.");
        }

        /// <inheritdoc />
        public bool IsEditableRoot(object root)
        {
            var obj = root as GameObject;
            return obj != null && obj.scene.IsValid() && obj.scene.isLoaded
                && !EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.scene)
                && PrefabStageUtility.GetPrefabStage(obj) == null;
        }

        /// <inheritdoc />
        public bool RootExists(object root) => root is GameObject obj && obj != null;
        /// <inheritdoc />
        public EditorSceneId GetSceneId(object root) => EditorSceneId.FromScene(((GameObject)root).scene);
        /// <inheritdoc />
        public HandlerContext<T> CreateHandlerContext<T>(T notification, object root, EventSubscription subscription)
            => new HandlerContext<T>(notification, (GameObject)root, subscription);
        /// <inheritdoc />
        public void LogWarning(string message, object root = null) => Debug.LogWarning(message, root as UnityEngine.Object);
        /// <inheritdoc />
        public void LogError(string message, object root = null) => Debug.LogError(message, root as UnityEngine.Object);
        /// <inheritdoc />
        public void LogException(Exception error, object root = null) => Debug.LogException(error, root as UnityEngine.Object);
        /// <inheritdoc />
        public int ReadPreferenceInt(string key, int defaultValue) => EditorPrefs.GetInt(key, defaultValue);
        /// <inheritdoc />
        public bool ReadPreferenceBool(string key, bool defaultValue) => EditorPrefs.GetBool(key, defaultValue);
        /// <inheritdoc />
        public string ReadPreferenceString(string key, string defaultValue)
            => EditorPrefs.HasKey(key) ? EditorPrefs.GetString(key) : defaultValue;
        /// <inheritdoc />
        public void WritePreferenceInt(string key, int value) => EditorPrefs.SetInt(key, value);
        /// <inheritdoc />
        public void WritePreferenceBool(string key, bool value) => EditorPrefs.SetBool(key, value);
        /// <inheritdoc />
        public void WritePreferenceString(string key, string value) => EditorPrefs.SetString(key, value);
        /// <inheritdoc />
        public void ErasePreferenceString(string key) => EditorPrefs.DeleteKey(key);
        /// <inheritdoc />
        public string ReadSessionString(string key) => SessionState.GetString(key, string.Empty);
        /// <inheritdoc />
        public void EraseSessionString(string key) => SessionState.EraseString(key);
    }

    // Unity がメインスレッド上でこの境界を初期化し、その後に登録実装が公開 API を使います。
    /// <summary>Unity のメインスレッドで実行環境と通知元を初期化する内部の入口です。</summary>
    [InitializeOnLoad]
    internal static class EditorRuntime
    {
        /// <summary>配送処理と管理画面が共有する Editor ホストです。</summary>
        internal static readonly IEditorHost Host;
        /// <summary>配送処理が共有する入力通知元です。</summary>
        internal static readonly IEditorChangeSource Source;

        /// <summary>実行環境を先に作成し、ディスパッチャーの初期化を遅らせる代理関数を通知元へ渡します。</summary>
        static EditorRuntime()
        {
            Host = new UnityEditorHost();
            // 代理関数により、通知元の構築中にディスパッチャーのフィールドへ触れることを避けます。
            Source = new UnityChangeSource(Dispatch);
        }

        /// <summary>Unity から届いた入力バッチをディスパッチャーへ渡します。</summary>
        /// <param name="changes">この更新で処理する入力変更の一覧です。</param>
        private static void Dispatch(IReadOnlyList<EditorChange> changes) => EventDispatcher.ProcessChanges(changes);
    }
}
