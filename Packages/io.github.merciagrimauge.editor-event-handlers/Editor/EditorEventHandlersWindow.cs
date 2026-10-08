using System;
using UnityEditor;
using UnityEngine;

namespace EditorEventHandlers.Editor
{
    internal sealed class EditorEventHandlersWindow : EditorWindow
    {
        private sealed class Controller : HandlerManagementController
        {
            internal bool Ready => CanManage;
            internal bool Halted => IsHalted;
            internal int Minimum => MinimumMilliseconds;
            internal int Maximum => MaximumMilliseconds;
            internal int Default => DefaultMilliseconds;
            internal int Capacity => MaximumRegistrations;
            internal EventSubscription[] Handlers() => ReadHandlers();
            internal ConditionRegistration[] Conditions() => ReadConditions();
            internal void Enable(EventSubscription token, bool value) => SetEnabled(token, value);
            internal void Enable(ConditionRegistration token, bool value) => SetEnabled(token, value);
            internal void Timeout(EventSubscription token, int value) => SetTimeout(token, value);
            internal void Timeout(ConditionRegistration token, int value) => SetTimeout(token, value);
        }
        private const float SourceMargin = 45;
        private readonly Controller _controller = new Controller();
        private Vector2 _scroll, _sourceScroll;
        private int _tab;
        private Type _sourceType;
        private string _source, _sourceLocation, _error;
        private GUIStyle _sourceStyle;
        // The style copied into _sourceStyle and the skin state it was copied under.
        private GUIStyle _styleBase;
        private Font _styleFont;
        private int _styleFontSize;
        private bool _styleProSkin;
        // Measuring the whole source is repeated only when the text, width, display scale or style changes.
        private GUIContent _sourceContent;
        private float _measuredWidth, _measuredPixelsPerPoint, _sourceWidth, _sourceHeight;

        [MenuItem("Tools/MerciaGrimauge/Editor Event Handlers")]
        private static void Open() => GetWindow<EditorEventHandlersWindow>("Event Handlers");
        private void OnEnable()
        {
            minSize = new Vector2(620, 420);
            // Styles and sizes from before a reload belong to the previous GUI state.
            _sourceStyle = null;
            _sourceContent = null;
        }
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, new[] { "Handlers", "Conditions" });
            EditorGUILayout.HelpBox("Timeout: " + _controller.Minimum + "–" + _controller.Maximum + " ms per call (default "
                + _controller.Default + " ms). Up to " + _controller.Capacity
                + " conditions and handlers in total. Fault disables persist until re-enabled here. "
                + "Non-returning code cannot be forcibly stopped.", MessageType.Info);
            if (_controller.Halted)
                EditorGUILayout.HelpBox("Dispatch is halted because a transaction could not be completed or its root no longer exists. "
                    + "See the Console for the reason and handler ID, and check the scene state. "
                    + "The halt is cleared by the next script reload, such as recompilation, or by restarting the Editor.", MessageType.Error);
            if (!_controller.Ready) EditorGUILayout.HelpBox("Settings are read-only while editor processing is active.", MessageType.Info);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120));
            var conditions = _controller.Conditions();
            Array.Sort(conditions, (a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
            if (_tab == 0)
            {
                var handlers = _controller.Handlers();
                Array.Sort(handlers, (a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
                if (handlers.Length == 0) EditorGUILayout.LabelField("No handlers are registered.");
                foreach (var handler in handlers) DrawHandler(handler, conditions);
            }
            else
            {
                if (conditions.Length == 0) EditorGUILayout.LabelField("No conditions are registered.");
                foreach (var condition in conditions) DrawCondition(condition);
            }
            EditorGUILayout.EndScrollView();
            if (_source != null) DrawSource();
        }
        private static ConditionRegistration FindCondition(ConditionRegistration[] conditions, Type eventType)
        {
            foreach (var candidate in conditions)
                if (candidate.EventType == eventType) return candidate;
            return null;
        }
        private void DrawHandler(EventSubscription handler, ConditionRegistration[] conditions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(handler.Id, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Handler", handler.Entry.ImplementationType.FullName);
            EditorGUILayout.LabelField("Event", handler.EventType.FullName);
            EditorGUILayout.LabelField("Combination", handler.Mode == SubscriptionMode.All ? "AND (all matches)"
                : handler.Mode == SubscriptionMode.Any ? "OR (first matching result)" : "Single condition");
            var state = handler.IsActive ? "Active" : handler.IsEnabled ? "Waiting" : "Disabled";
            EditorGUILayout.LabelField("State", state);
            EditorGUILayout.LabelField("Last call", handler.LastDuration.TotalMilliseconds.ToString("F3") + " ms, " + handler.LastResult.Status);
            DrawSettings(handler.IsEnabled, (int)handler.TimeLimit.TotalMilliseconds,
                value => _controller.Enable(handler, value), value => _controller.Timeout(handler, value));
            DrawReason(handler.DisabledReason);
            foreach (var eventType in handler.ConditionTypes)
            {
                var condition = FindCondition(conditions, eventType);
                EditorGUILayout.LabelField("Condition event", eventType.FullName);
                EditorGUILayout.LabelField("Condition", condition == null ? "Waiting for registration"
                    : condition.Id + (condition.IsEnabled ? " (enabled)" : " (disabled)"));
                using (new EditorGUI.DisabledScope(condition == null))
                    if (GUILayout.Button("View condition source") && condition != null) LoadSource(condition.Entry.ImplementationType);
            }
            EditorGUILayout.EndVertical();
        }
        private void DrawCondition(ConditionRegistration condition)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(condition.Id, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", condition.Entry.ImplementationType.FullName);
            EditorGUILayout.LabelField("Event / changes", condition.EventType.FullName + " / " + condition.Changes);
            EditorGUILayout.LabelField("Last call", condition.LastDuration.TotalMilliseconds.ToString("F3") + " ms");
            DrawSettings(condition.IsEnabled, (int)condition.TimeLimit.TotalMilliseconds,
                value => _controller.Enable(condition, value), value => _controller.Timeout(condition, value));
            DrawReason(condition.DisabledReason);
            if (GUILayout.Button("View condition source")) LoadSource(condition.Entry.ImplementationType);
            EditorGUILayout.EndVertical();
        }
        private void DrawSource()
        {
            EditorGUILayout.LabelField("Condition source", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_sourceType?.FullName ?? "No condition");
            EditorGUILayout.LabelField(_sourceLocation ?? string.Empty);
            if (GUILayout.Button("Reload source")) LoadSource(_sourceType);
            var styleChanged = RefreshSourceStyle();
            var width = position.width - SourceMargin;
            var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            if (styleChanged || _sourceContent == null || _measuredWidth != width || _measuredPixelsPerPoint != pixelsPerPoint)
            {
                if (_sourceContent == null) _sourceContent = new GUIContent(_source);
                _measuredWidth = width;
                _measuredPixelsPerPoint = pixelsPerPoint;
                _sourceWidth = _sourceStyle.CalcSize(_sourceContent).x;
                _sourceHeight = _sourceStyle.CalcHeight(_sourceContent, width);
            }
            _sourceScroll = EditorGUILayout.BeginScrollView(_sourceScroll, GUILayout.Height(200));
            EditorGUILayout.SelectableLabel(_source, _sourceStyle,
                GUILayout.Width(Mathf.Max(width, _sourceWidth)), GUILayout.Height(Mathf.Max(180, _sourceHeight)));
            EditorGUILayout.EndScrollView();
        }
        // Copies the text area style again when the skin, the style instance or its font changes.
        // Returns true when the copy was rebuilt and earlier measurements no longer apply.
        private bool RefreshSourceStyle()
        {
            var baseStyle = EditorStyles.textArea;
            var font = baseStyle.font != null ? baseStyle.font : GUI.skin.font;
            var proSkin = EditorGUIUtility.isProSkin;
            if (_sourceStyle != null && _styleBase == baseStyle && _styleFont == font
                && _styleFontSize == baseStyle.fontSize && _styleProSkin == proSkin) return false;
            _styleBase = baseStyle;
            _styleFont = font;
            _styleFontSize = baseStyle.fontSize;
            _styleProSkin = proSkin;
            _sourceStyle = new GUIStyle(baseStyle) { wordWrap = false };
            return true;
        }
        private void DrawSettings(bool enabled, int milliseconds, Action<bool> setEnabled, Action<int> setTimeout)
        {
            using (new EditorGUI.DisabledScope(!_controller.Ready))
            {
                var next = EditorGUILayout.Toggle("Enabled", enabled);
                if (next != enabled) Apply(() => setEnabled(next));
                var timeout = EditorGUILayout.DelayedIntField("Timeout (ms)", milliseconds);
                if (timeout != milliseconds)
                    Apply(() => setTimeout(timeout));
            }
        }
        private static void DrawReason(string reason)
        { if (!string.IsNullOrEmpty(reason)) EditorGUILayout.HelpBox(reason, MessageType.Warning); }
        private void Apply(Action operation)
        {
            try { operation(); _error = null; }
            catch (InvalidOperationException error) { _error = error.Message; }
            catch (ArgumentOutOfRangeException) { _error = "Timeout must be between " + _controller.Minimum + " and " + _controller.Maximum + " ms."; }
        }
        private void LoadSource(Type type)
        {
            _sourceType = type;
            _source = ConditionSourceReader.Read(type, out _sourceLocation);
            _sourceContent = null;
            _sourceScroll = Vector2.zero;
        }
    }
}
