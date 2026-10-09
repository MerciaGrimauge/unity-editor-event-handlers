using System;
using UnityEditor;
using UnityEngine;

namespace EditorEventHandlers.Editor
{
    /// <summary>条件と購読の状態、ユーザー設定、条件の元ソースを表示する管理ウィンドウです。</summary>
    internal sealed class EditorEventHandlersWindow : EditorWindow
    {
        /// <summary>管理処理をウィンドウから使うためのアダプターです。登録側には操作を公開しません。</summary>
        private sealed class Controller : HandlerManagementController
        {
            /// <summary>現在、管理画面から設定を変更できるかどうかです。</summary>
            internal bool Ready => CanManage;
            /// <summary>ディスパッチャーが全体停止しているかどうかです。</summary>
            internal bool Halted => IsHalted;
            /// <summary>ユーザーが設定できる期限の最小値です。単位はミリ秒です。</summary>
            internal int Minimum => MinimumMilliseconds;
            /// <summary>ユーザーが設定できる期限の最大値です。単位はミリ秒です。</summary>
            internal int Maximum => MaximumMilliseconds;
            /// <summary>期限の既定値です。単位はミリ秒です。</summary>
            internal int Default => DefaultMilliseconds;
            /// <summary>条件登録と購読の合計上限です。</summary>
            internal int Capacity => MaximumRegistrations;
            /// <summary>表示用に現在の購読一覧を取得します。</summary>
            /// <returns>現在の購読一覧のコピーです。</returns>
            internal EventSubscription[] Handlers() => ReadHandlers();
            /// <summary>表示用に現在の条件登録一覧を取得します。</summary>
            /// <returns>現在の条件登録一覧のコピーです。</returns>
            internal ConditionRegistration[] Conditions() => ReadConditions();
            /// <summary>指定した登録の有効状態をユーザー設定として変更します。</summary>
            /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
            /// <param name="value">登録を有効にする場合は true、無効にする場合は false です。</param>
            internal void Enable(EventSubscription token, bool value) => SetEnabled(token, value);
            /// <summary>指定した登録の有効状態をユーザー設定として変更します。</summary>
            /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
            /// <param name="value">登録を有効にする場合は true、無効にする場合は false です。</param>
            internal void Enable(ConditionRegistration token, bool value) => SetEnabled(token, value);
            /// <summary>指定した登録の期限をミリ秒単位で変更します。</summary>
            /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
            /// <param name="value">期限のミリ秒値です。許容範囲は1〜100です。</param>
            internal void Timeout(EventSubscription token, int value) => SetTimeout(token, value);
            /// <summary>指定した登録の期限をミリ秒単位で変更します。</summary>
            /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
            /// <param name="value">期限のミリ秒値です。許容範囲は1〜100です。</param>
            internal void Timeout(ConditionRegistration token, int value) => SetTimeout(token, value);
        }
        /// <summary>ソース表示領域の幅を調整するための余白です。</summary>
        private const float SourceMargin = 45;
        /// <summary>登録状態の読み取りと管理操作を仲介するコントローラーです。</summary>
        private readonly Controller _controller = new Controller();
        /// <summary>登録一覧とソース表示、それぞれのスクロール位置です。</summary>
        private Vector2 _scroll, _sourceScroll;
        /// <summary>現在選択している管理画面のタブです。</summary>
        private int _tab;
        /// <summary>現在ソースを表示している条件の実装型です。</summary>
        private Type _sourceType;
        /// <summary>読み取ったソース本文、表示用パス、管理操作のエラーです。</summary>
        private string _source, _sourceLocation, _error;
        /// <summary>ソース本文の表示とサイズ計測に使うスタイルです。</summary>
        private GUIStyle _sourceStyle;
        // _sourceStyle の複製元と、複製時のスキン状態です。
        /// <summary>現在のソース表示スタイルの作成に使った元のスタイルです。</summary>
        private GUIStyle _styleBase;
        /// <summary>スタイル変更を検出するために保存したフォントです。</summary>
        private Font _styleFont;
        /// <summary>スタイル変更を検出するために保存したフォントサイズです。</summary>
        private int _styleFontSize;
        /// <summary>スタイル作成時にダークスキンだった場合は true です。</summary>
        private bool _styleProSkin;
        // 全文の計測は内容・幅・表示倍率・スタイルが変わった場合だけやり直します。
        /// <summary>ソース本文の表示と計測に再利用する GUI コンテンツです。</summary>
        private GUIContent _sourceContent;
        /// <summary>前回の計測に使った表示幅と画面倍率、および計測結果の幅と高さです。</summary>
        private float _measuredWidth, _measuredPixelsPerPoint, _sourceWidth, _sourceHeight;

        /// <summary>メニューから管理ウィンドウを開きます。</summary>
        [MenuItem("Tools/MerciaGrimauge/Editor Event Handlers")]
        private static void Open() => GetWindow<EditorEventHandlersWindow>("Event Handlers");
        /// <summary>最小表示サイズを設定し、以前の GUI 状態に属するソース表示のキャッシュを破棄します。</summary>
        private void OnEnable()
        {
            minSize = new Vector2(620, 420);
            // リロード前のスタイルと計測値は以前の GUI 状態に属するため、再利用しません。
            _sourceStyle = null;
            _sourceContent = null;
        }
        /// <summary>状態表示を更新するためにウィンドウの再描画を要求します。</summary>
        private void OnInspectorUpdate() => Repaint();
        /// <summary>購読・条件の一覧、設定操作、異常理由、条件ソースを描画します。</summary>
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
        /// <summary>指定された通知型と完全一致する条件登録を一覧から探します。</summary>
        /// <param name="conditions">表示対象から条件を探すための現在の登録一覧です。</param>
        /// <param name="eventType">完全一致で扱う通知型です。</param>
        /// <returns>一致する条件登録です。見つからなければ null です。</returns>
        private static ConditionRegistration FindCondition(ConditionRegistration[] conditions, Type eventType)
        {
            foreach (var candidate in conditions)
                if (candidate.EventType == eventType) return candidate;
            return null;
        }
        /// <summary>1件の購読の実装、依存条件、状態、実行結果、設定を描画します。</summary>
        /// <param name="handler">表示するハンドラーの購読です。</param>
        /// <param name="conditions">表示対象から条件を探すための現在の登録一覧です。</param>
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
        /// <summary>1件の条件の実装、通知型、入力種類、評価時間、設定を描画します。</summary>
        /// <param name="condition">表示する条件登録です。</param>
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
        /// <summary>元ソースを表示し、必要な場合だけ表示サイズを再計測します。</summary>
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
        // スキン・スタイルのインスタンス・フォントが変わった場合に表示スタイルを複製し直します。
        // 複製を作り直し、以前の計測値が使えなくなった場合に true を返します。
        /// <summary>スキン・基準スタイル・フォントが変わった場合にソース表示用スタイルを作り直します。</summary>
        /// <returns>スタイルを作り直して以前の計測が無効になった場合は true、それ以外は false です。</returns>
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
        /// <summary>操作可能な状態で有効・無効と期限の入力を受け付け、変更された値だけを適用します。</summary>
        /// <param name="enabled">手動で有効にする場合は true、無効にする場合は false です。</param>
        /// <param name="milliseconds">期限のミリ秒値です。許容範囲は1〜100です。</param>
        /// <param name="setEnabled">有効状態の変更を適用する処理です。</param>
        /// <param name="setTimeout">期限の変更を適用する処理です。</param>
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
        /// <summary>無効化理由がある場合に警告として表示します。</summary>
        /// <param name="reason">無効化または全体停止の理由です。</param>
        private static void DrawReason(string reason)
        { if (!string.IsNullOrEmpty(reason)) EditorGUILayout.HelpBox(reason, MessageType.Warning); }
        /// <summary>設定変更を実行し、管理状態や入力範囲の例外を画面のエラー表示に変換します。</summary>
        /// <param name="operation">管理画面から実行する設定変更です。</param>
        private void Apply(Action operation)
        {
            try { operation(); _error = null; }
            catch (InvalidOperationException error) { _error = error.Message; }
            catch (ArgumentOutOfRangeException) { _error = "Timeout must be between " + _controller.Minimum + " and " + _controller.Maximum + " ms."; }
        }
        /// <summary>条件型の元ソースを取得し、表示内容とスクロール位置を更新します。</summary>
        /// <param name="type">元ソースを表示する条件の実装型です。</param>
        private void LoadSource(Type type)
        {
            _sourceType = type;
            _source = ConditionSourceReader.Read(type, out _sourceLocation);
            _sourceContent = null;
            _sourceScroll = Vector2.zero;
        }
    }
}
