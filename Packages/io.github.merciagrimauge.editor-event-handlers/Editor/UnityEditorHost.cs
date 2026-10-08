using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorEventHandlers.Editor
{
    // This type owns the Unity API boundary; registration and dispatch policy stay in the dispatcher.
    internal sealed class UnityEditorHost : IEditorHost
    {
        private readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        public bool CanEdit => !EditorApplication.isPlayingOrWillChangePlaymode
            && !EditorApplication.isCompiling && !EditorApplication.isUpdating
            && !BuildPipeline.isBuildingPlayer && !Undo.isProcessing;
        public string SettingsScope => Application.dataPath;

        public void RequireMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
                throw new InvalidOperationException("Editor events must be used on the Unity editor main thread.");
        }

        public bool IsEditableRoot(object root)
        {
            var obj = root as GameObject;
            return obj != null && obj.scene.IsValid() && obj.scene.isLoaded
                && !EditorUtility.IsPersistent(obj) && !EditorSceneManager.IsPreviewScene(obj.scene)
                && PrefabStageUtility.GetPrefabStage(obj) == null;
        }

        public bool RootExists(object root) => root is GameObject obj && obj != null;
        public EditorSceneId GetSceneId(object root) => EditorSceneId.FromScene(((GameObject)root).scene);
        public HandlerContext<T> CreateHandlerContext<T>(T notification, object root, EventSubscription subscription)
            => new HandlerContext<T>(notification, (GameObject)root, subscription);
        public void LogWarning(string message, object root = null) => Debug.LogWarning(message, root as UnityEngine.Object);
        public void LogError(string message, object root = null) => Debug.LogError(message, root as UnityEngine.Object);
        public void LogException(Exception error, object root = null) => Debug.LogException(error, root as UnityEngine.Object);
        public int ReadPreferenceInt(string key, int defaultValue) => EditorPrefs.GetInt(key, defaultValue);
        public bool ReadPreferenceBool(string key, bool defaultValue) => EditorPrefs.GetBool(key, defaultValue);
        public string ReadPreferenceString(string key, string defaultValue)
            => EditorPrefs.HasKey(key) ? EditorPrefs.GetString(key) : defaultValue;
        public void WritePreferenceInt(string key, int value) => EditorPrefs.SetInt(key, value);
        public void WritePreferenceBool(string key, bool value) => EditorPrefs.SetBool(key, value);
        public void WritePreferenceString(string key, string value) => EditorPrefs.SetString(key, value);
        public void ErasePreferenceString(string key) => EditorPrefs.DeleteKey(key);
        public string ReadSessionString(string key) => SessionState.GetString(key, string.Empty);
        public void EraseSessionString(string key) => SessionState.EraseString(key);
    }

    // Unity eagerly initializes this boundary on its main thread, before registrants use the facade.
    [InitializeOnLoad]
    internal static class EditorRuntime
    {
        internal static readonly IEditorHost Host;
        internal static readonly IEditorChangeSource Source;

        static EditorRuntime()
        {
            Host = new UnityEditorHost();
            // The proxy avoids touching dispatcher fields while its source is being constructed.
            Source = new UnityChangeSource(Dispatch);
        }

        private static void Dispatch(IReadOnlyList<EditorChange> changes) => EventDispatcher.ProcessChanges(changes);
    }
}
