using System;

namespace EditorEventHandlers.Editor
{
    // Internal infrastructure only. Registrants cannot replace host behavior or its policy.
    internal interface IEditorChangeSource : IDisposable
    {
        void SetKinds(EditorChangeKind kinds);
    }

    internal interface IEditorHost
    {
        bool CanEdit { get; }
        string SettingsScope { get; }
        void RequireMainThread();
        bool IsEditableRoot(object root);
        bool RootExists(object root);
        EditorSceneId GetSceneId(object root);
        HandlerContext<T> CreateHandlerContext<T>(T notification, object root, EventSubscription subscription);
        void LogWarning(string message, object root = null);
        void LogError(string message, object root = null);
        void LogException(Exception error, object root = null);
        int ReadPreferenceInt(string key, int defaultValue);
        bool ReadPreferenceBool(string key, bool defaultValue);
        string ReadPreferenceString(string key, string defaultValue);
        void WritePreferenceInt(string key, int value);
        void WritePreferenceBool(string key, bool value);
        void WritePreferenceString(string key, string value);
        void ErasePreferenceString(string key);
        string ReadSessionString(string key);
        void EraseSessionString(string key);
    }
}
