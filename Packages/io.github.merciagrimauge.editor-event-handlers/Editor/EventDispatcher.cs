using System;
using System.Collections.Generic;


namespace EditorEventHandlers.Editor
{
    /// <summary>通知型に依存しない形でハンドラーの実装型を取得する内部契約です。</summary>
    internal interface IHandlerEntry
    {
        /// <summary>登録されたハンドラーの実装型です。</summary>
        Type ImplementationType { get; }
    }
    /// <summary>型付きハンドラーの実装と、その実装型を保持します。</summary>
    /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
    internal sealed class HandlerEntry<T> : IHandlerEntry
    {
        /// <summary>このエントリーが呼び出す型付きハンドラーです。</summary>
        internal readonly IEventHandler<T> Handler;
        /// <summary>登録されたハンドラーの実装型です。</summary>
        public Type ImplementationType => Handler.GetType();
        /// <summary>型付きハンドラーの実装を保持します。</summary>
        /// <param name="handler">登録する型付きハンドラーの実装です。</param>
        internal HandlerEntry(IEventHandler<T> handler) { Handler = handler; }
    }
    /// <summary>通知型に依存しない形で条件を評価する内部契約です。</summary>
    internal interface IConditionEntry
    {
        /// <summary>登録された条件の実装型です。</summary>
        Type ImplementationType { get; }
        /// <summary>入力変更を個別の期限内で評価し、一致した結果を保存します。</summary>
        /// <param name="change">通知時点の種類と識別子を保持する入力変更です。</param>
        /// <param name="registration">評価する条件の状態と期限を保持する登録です。</param>
        /// <returns>一致した保存済み結果です。不一致なら null です。</returns>
        IConditionResult Evaluate(EditorChange change, ConditionRegistration registration);
    }
    /// <summary>評価後に配送する通知の内部契約です。</summary>
    internal interface INotification
    {
        /// <summary>配送前の状態を確認し、保存した通知を購読者へ渡します。</summary>
        void Execute();
    }
    /// <summary>条件の一致結果と評価時のシーンを保持し、通知型に対応するハンドラーを実行します。</summary>
    internal interface IConditionResult
    {
        /// <summary>条件が選んだ編集範囲のルートです。型を隠して保持しますが、存続は配送時にも確認します。</summary>
        object Root { get; }
        /// <summary>条件が生成した通知値です。購読者間で同じ結果を共有します。</summary>
        object Event { get; }
        /// <summary>条件評価時のルートのシーン識別子です。</summary>
        EditorSceneId Scene { get; }
        /// <summary>保存した一致結果を指定された購読へ配送します。</summary>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        void Execute(EventSubscription subscription);
    }
    /// <summary>型付き条件の評価と、期限・一致結果の確認を担当します。</summary>
    /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
    internal sealed class ConditionEntry<T> : IConditionEntry
    {
        /// <summary>通知型が値型で、参照の null 検証を必要としない場合は true です。</summary>
        private static readonly bool NotificationIsValueType = typeof(T).IsValueType;
        /// <summary>このエントリーが呼び出す型付き条件です。</summary>
        private readonly IEventCondition<T> _condition;
        /// <summary>登録された条件の実装型です。</summary>
        public Type ImplementationType => _condition.GetType();
        /// <summary>型付き条件の実装を保持します。</summary>
        /// <param name="condition">登録する型付き条件の実装です。</param>
        internal ConditionEntry(IEventCondition<T> condition) { _condition = condition; }
        /// <summary>条件を評価して期限と一致結果を確認し、評価時間を記録してコンテキストを閉じます。</summary>
        /// <param name="change">通知時点の種類と識別子を保持する入力変更です。</param>
        /// <param name="registration">評価する条件の状態と期限を保持する登録です。</param>
        /// <returns>一致した保存済み結果です。不一致なら null です。</returns>
        public IConditionResult Evaluate(EditorChange change, ConditionRegistration registration)
        {
            var context = new ConditionContext(change, registration.Id, registration.TimeLimit);
            try
            {
                var matched = _condition.TryMatch(context, out var match);
                context.CheckDeadline();
                if (!matched) return null;
                if ((!NotificationIsValueType && match.Event == null) || !EventDispatcher.IsEditableRoot(match.Root))
                    throw new InvalidOperationException("A match must provide a notification and a valid editable scene root.");
                return new ConditionResult<T>(match);
            }
            finally { registration.LastDuration = context.Elapsed; context.Close(); }
        }
    }
    /// <summary>型付き一致結果と評価時のシーンを保存し、単一または複合購読へ配送します。</summary>
    /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
    internal sealed class ConditionResult<T> : IConditionResult
    {
        /// <summary>評価時に得た通知値と編集範囲のルートです。</summary>
        private readonly ConditionMatch<T> _match;
        /// <summary>保存した一致結果の編集範囲のルートです。</summary>
        public object Root => _match.Root;
        /// <summary>保存した一致結果の通知値です。</summary>
        public object Event => _match.Event;
        /// <summary>一致結果を保存した時点のシーン識別子です。</summary>
        public EditorSceneId Scene { get; }
        /// <summary>一致結果を保持し、確認済みルートから現在のシーン識別子を取得して保存します。</summary>
        /// <param name="match">保存または配送する通知値と編集範囲のルートです。</param>
        internal ConditionResult(ConditionMatch<T> match)
            : this(match, EventDispatcher.Host.GetSceneId(match.Root)) { }
        /// <summary>一致結果と、評価時に確定したシーン識別子を保持します。</summary>
        /// <param name="match">保存または配送する通知値と編集範囲のルートです。</param>
        /// <param name="scene">一致結果の評価時に確定したシーン識別子です。</param>
        internal ConditionResult(ConditionMatch<T> match, EditorSceneId scene)
        { _match = match; Scene = scene; }
        /// <summary>保存した型付き一致結果で指定された購読を実行します。</summary>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        public void Execute(EventSubscription subscription) => EventDispatcher.Execute(subscription, _match);
    }
    /// <summary>1件の配送に必要な購読、依存条件、一致結果を保持します。</summary>
    internal sealed class Notification : INotification
    {
        /// <summary>この通知を配送する購読です。</summary>
        private readonly EventSubscription _subscription;
        /// <summary>この配送で有効状態を確認する依存条件の登録です。</summary>
        private readonly ConditionRegistration[] _conditions;
        /// <summary>通知値と編集範囲を保持する一致結果です。</summary>
        private readonly IConditionResult _result;
        /// <summary>配送先の購読、評価に使った依存条件、一致結果を保存します。</summary>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <param name="conditions">購読が依存する、通知型に対応した条件登録の配列です。</param>
        /// <param name="result">評価後に保存した条件の一致結果です。</param>
        internal Notification(EventSubscription subscription, ConditionRegistration[] conditions, IConditionResult result)
        { _subscription = subscription; _conditions = conditions; _result = result; }
        /// <summary>登録と依存条件の存続、編集可否、ルートとシーンを再確認して配送します。</summary>
        public void Execute()
        {
            if (!EventDispatcher.IsLive(_subscription, _conditions) || !EventDispatcher.CanEdit()) return;
            if (!EventDispatcher.IsEditableRoot(_result.Root) || EventDispatcher.Host.GetSceneId(_result.Root) != _result.Scene) return;
            _result.Execute(_subscription);
        }
    }

    // ディスパッチャーはアバターの Descriptor、衣装、AAO、通知の個別フィールドの意味を扱いません。
    // Unity との接続は内部の実行環境が担当し、登録実装には委ねません。
    /// <summary>条件と購読を管理し、判定結果を共有して逐次配送する内部ディスパッチャーです。</summary>
    internal static partial class EventDispatcher
    {
        /// <summary>条件登録と購読の合計に適用する上限です。</summary>
        internal const int MaximumRegistrations = 100;
        /// <summary>登録処理の再入時にも上限へ算入する、確定前の予約数です。</summary>
        private static int _registrationReservations;
        /// <summary>Editor の状態確認、設定保存、編集を提供するホストです。</summary>
        internal static readonly IEditorHost Host = EditorRuntime.Host;
        /// <summary>完全一致する通知型をキーとした現在の条件登録です。</summary>
        private static readonly Dictionary<Type, ConditionRegistration> Conditions = new Dictionary<Type, ConditionRegistration>();
        /// <summary>条件を登録順に読み出すための一覧です。</summary>
        private static readonly List<ConditionRegistration> ConditionOrder = new List<ConditionRegistration>();
        /// <summary>現在登録されている購読の一覧です。</summary>
        private static readonly List<EventSubscription> Subscriptions = new List<EventSubscription>();
        /// <summary>入力種類ごとに再構築した条件評価と配送の構成です。</summary>
        private static Dictionary<EditorChangeKind, Route> Routes = new Dictionary<EditorChangeKind, Route>();
        // 対応する変更の種類の一覧です。各種類を UnityChangeSource で変換する必要があります。
        /// <summary>配送構成を組み立てる対象となる公開入力の種類です。</summary>
        private static readonly EditorChangeKind[] Kinds = { EditorChangeKind.Created, EditorChangeKind.ParentChanged,
            EditorChangeKind.PropertiesChanged, EditorChangeKind.StructureChanged, EditorChangeKind.HierarchyChanged,
            EditorChangeKind.ChildrenReordered, EditorChangeKind.Destroyed, EditorChangeKind.PrefabUpdated };
        /// <summary>現在の配送構成に応じて購読する Editor の入力通知元です。</summary>
        internal static readonly IEditorChangeSource Source = EditorRuntime.Source;
        /// <summary>確定・復元の失敗またはルート喪失により、全配送が停止しているかどうかです。</summary>
        internal static bool Halted { get; private set; }
        /// <summary>現在のバッチを区別する一時的な番号です。永続的な識別子ではありません。</summary>
        internal static long BatchId { get; private set; }
        /// <summary>入力バッチを処理中で、再入する配送を抑止する場合は true です。</summary>
        private static bool _processing;
        /// <summary>1種類の入力に必要な条件と配送先を保持する、バッチ用の配送構成です。</summary>
        private sealed class Route
        {
            /// <summary>この入力種類について評価する条件登録です。</summary>
            internal readonly ConditionRegistration[] Conditions;
            /// <summary>この入力種類について配送を試みる購読構成です。</summary>
            internal readonly SubscriptionRoute[] Subscribers;
            /// <summary>必要条件と配送先の配列を保持します。</summary>
            /// <param name="conditions">購読が依存する、通知型に対応した条件登録の配列です。</param>
            /// <param name="subscribers">この種類の入力の配送先となる購読構成です。</param>
            internal Route(ConditionRegistration[] conditions, SubscriptionRoute[] subscribers)
            { Conditions = conditions; Subscribers = subscribers; }
        }
        /// <summary>1件の購読と、その購読が依存する条件の登録を保持します。</summary>
        private sealed class SubscriptionRoute
        {
            /// <summary>この配送構成に対応する購読です。</summary>
            internal readonly EventSubscription Subscription;
            /// <summary>この購読の通知型に対応する依存条件です。</summary>
            internal readonly ConditionRegistration[] Conditions;
            /// <summary>購読と依存条件の配列を保持します。</summary>
            /// <param name="subscription">一致結果の配送先となる購読です。</param>
            /// <param name="conditions">購読が依存する、通知型に対応した条件登録の配列です。</param>
            internal SubscriptionRoute(EventSubscription subscription, ConditionRegistration[] conditions)
            { Subscription = subscription; Conditions = conditions; }
        }
        // 登録の設定と異常記録の種別を選びます。名前は保存キーの一部です。
        /// <summary>保存設定と異常記録のキーに使う登録種別です。</summary>
        private enum RegistrationKind
        {
            /// <summary>条件登録を表します。</summary>
            Condition,
            /// <summary>購読登録を表します。</summary>
            Subscription
        }
        /// <summary>登録種別、通知型、識別子から設定と異常記録の共通キーを作ります。</summary>
        /// <param name="kind">条件登録と購読を区別する種別です。</param>
        /// <param name="type">完全一致で登録や設定を区別する通知型です。</param>
        /// <param name="id">設定と異常記録の区別に使う安定した登録識別子です。</param>
        /// <returns>登録種別・型・識別子を含む共通のキーです。</returns>
        private static string Key(RegistrationKind kind, Type type, string id) => "EditorEventHandlers."
            + (kind == RegistrationKind.Condition ? "Condition." : "Subscription.") + type.AssemblyQualifiedName + ":" + id;

        /// <summary>実行環境に Editor メインスレッド上での呼び出しを確認させます。</summary>
        internal static void RequireMainThread() => Host.RequireMainThread();
        /// <summary>登録識別子が null・空文字列・空白だけではないことを確認します。</summary>
        /// <param name="id">設定と異常記録の区別に使う安定した登録識別子です。</param>
        private static void Validate(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable, nonempty ID is required.");
        }
        // 登録実装のゲッターを読む前に枠を予約します。上限到達時は識別子を取得せず、
        // ゲッター内の再登録でも外側の予約を数えます。呼び出し元の finally で予約を解放します。
        /// <summary>登録実装のゲッターを呼ぶ前に枠を予約し、再入した登録も含めて上限を守ります。</summary>
        /// <remarks>呼び出し元は finally で予約を解放します。ゲッターが再入して登録した場合も外側の予約を数えます。</remarks>
        /// <exception cref="InvalidOperationException">条件登録と購読の合計が上限に達している場合です。</exception>
        private static void ReserveRegistration()
        {
            if (ConditionOrder.Count + Subscriptions.Count + _registrationReservations >= MaximumRegistrations)
                throw new InvalidOperationException("At most " + MaximumRegistrations + " conditions and handlers may be registered in total.");
            _registrationReservations++;
        }
        /// <summary>変更フラグが空でなく、対応する種類だけで構成されているかを確認します。</summary>
        /// <param name="changes">評価対象とする公開入力の変更フラグです。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        private static bool IsSupported(EditorChangeKind changes)
        {
            var supported = EditorChangeKind.None;
            foreach (var kind in Kinds) supported |= kind;
            return changes != EditorChangeKind.None && (changes & ~supported) == 0;
        }
        /// <summary>全体停止中でなく、指定型の条件が登録済みかつ有効かを確認します。</summary>
        /// <param name="type">完全一致で登録や設定を区別する通知型です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal static bool HasCondition(Type type)
        {
            RequireMainThread();
            return !Halted && Conditions.TryGetValue(type, out var value) && !value.IsDisposed && value.IsEnabled;
        }
        /// <summary>指定された全通知型の条件が有効かを確認します。</summary>
        /// <param name="types">有効な条件登録があることを確認する通知型の一覧です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal static bool HasConditions(IReadOnlyList<Type> types)
        {
            RequireMainThread();
            foreach (var type in types) if (!HasCondition(type)) return false;
            return true;
        }
        /// <summary>購読と全依存条件が有効で、評価時の条件登録が現在も同じ登録であるかを確認します。</summary>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <param name="dependencies">配送構成を作成した時点の依存条件の登録です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal static bool IsLive(EventSubscription subscription, ConditionRegistration[] dependencies)
        {
            if (Halted || subscription.IsDisposed || !subscription.IsEnabled) return false;
            foreach (var condition in dependencies)
                if (condition.IsDisposed || !condition.IsEnabled || !Conditions.TryGetValue(condition.EventType, out var current)
                    || !ReferenceEquals(current, condition)) return false;
            return true;
        }
        /// <summary>枠を予約し、識別子・変更フラグ・通知型の重複を確認して条件を登録します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="condition">登録する型付き条件の実装です。</param>
        /// <returns>条件の状態と明示的な解除に使う登録トークンです。</returns>
        internal static ConditionRegistration Register<T>(IEventCondition<T> condition)
        {
            RequireMainThread();
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            ReserveRegistration();
            try
            {
                var id = condition.Id; var changes = condition.Changes;
                Validate(id);
                if (!IsSupported(changes))
                    throw new ArgumentException("At least one supported change kind is required.", nameof(condition));
                if (Conditions.ContainsKey(typeof(T))) throw new ArgumentException("A condition is already registered for " + typeof(T).FullName);
                var policy = GetPolicy(RegistrationKind.Condition, typeof(T), id);
                var reason = policy.DisabledReason;
                var token = new ConditionRegistration(new ConditionEntry<T>(condition), typeof(T), id, changes, TimeSpan.FromMilliseconds(policy.Milliseconds))
                { IsEnabled = reason.Length == 0, DisabledReason = reason };
                Conditions.Add(typeof(T), token); ConditionOrder.Add(token); UpdateRoutes(); return token;
            }
            finally { _registrationReservations--; }
        }
        /// <summary>枠を予約し、指定通知型の単一購読を登録します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="handler">登録する型付きハンドラーの実装です。</param>
        /// <returns>購読の状態と明示的な解除に使うトークンです。</returns>
        internal static EventSubscription Subscribe<T>(IEventHandler<T> handler)
        {
            RequireMainThread();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            ReserveRegistration();
            try
            {
                return AddSubscription(handler, SubscriptionMode.Single, new[] { typeof(T) });
            }
            finally { _registrationReservations--; }
        }
        /// <summary>指定型一覧を検証してコピーし、全条件一致またはいずれか一致の購読を登録します。</summary>
        /// <param name="handler">登録する型付きハンドラーの実装です。</param>
        /// <param name="types">複合購読が要求する通知型の配列です。検証してコピーし、指定順を保持します。</param>
        /// <param name="mode">単一・全条件一致・いずれか一致の購読モードです。</param>
        /// <returns>複合購読の状態と明示的な解除に使うトークンです。</returns>
        internal static EventSubscription SubscribeComposite(IEventHandler<CompositeEvent> handler, Type[] types,
            SubscriptionMode mode)
        {
            RequireMainThread();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (types == null) throw new ArgumentNullException(nameof(types));
            ReserveRegistration();
            try
            {
                if (types.Length == 0 || types.Length > MaximumRegistrations)
                    throw new ArgumentException("Declare between one and 100 condition notification types.", nameof(types));
                var copy = (Type[])types.Clone();
                var seen = new HashSet<Type>();
                foreach (var type in copy)
                    if (type == null || type == typeof(void) || type == typeof(CompositeEvent) || type.IsByRef
                        || type.IsPointer || type.ContainsGenericParameters || !seen.Add(type))
                        throw new ArgumentException("Condition notification types must be distinct, closed types; nested CompositeEvent is not supported.", nameof(types));
                return AddSubscription(handler, mode, copy);
            }
            finally { _registrationReservations--; }
        }
        /// <summary>ハンドラーの識別子と保存設定を取得し、重複を拒否して購読を配送構成に追加します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="handler">登録する型付きハンドラーの実装です。</param>
        /// <param name="mode">単一・全条件一致・いずれか一致の購読モードです。</param>
        /// <param name="conditionTypes">購読が必要とする通知型の一覧です。</param>
        /// <returns>追加した購読のトークンです。</returns>
        private static EventSubscription AddSubscription<T>(IEventHandler<T> handler,
            SubscriptionMode mode, Type[] conditionTypes)
        {
            var id = handler.Id;
            Validate(id);
            foreach (var existing in Subscriptions)
                if (existing.EventType == typeof(T) && existing.Id == id)
                    throw new ArgumentException("Subscription ID already registered for this notification type: " + id);
            var policy = GetPolicy(RegistrationKind.Subscription, typeof(T), id);
            var reason = policy.DisabledReason;
            var token = new EventSubscription(new HandlerEntry<T>(handler), typeof(T), id,
                TimeSpan.FromMilliseconds(policy.Milliseconds), mode, conditionTypes)
            { IsEnabled = reason.Length == 0, DisabledReason = reason };
            Subscriptions.Add(token);
            UpdateRoutes(); return token;
        }
        /// <summary>購読を解除し、以後の配送から除外します。解除済みなら何もしません。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        internal static void Unsubscribe(EventSubscription token)
        {
            RequireMainThread(); if (token.IsDisposed) return;
            token.IsDisposed = true; token.IsEnabled = false; Subscriptions.Remove(token); UpdateRoutes();
        }
        /// <summary>条件を解除し、その条件に依存する購読を待機状態にします。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        internal static void Unregister(ConditionRegistration token)
        {
            RequireMainThread(); if (token.IsDisposed) return;
            token.IsDisposed = true; token.IsEnabled = false;
            Conditions.Remove(token.EventType); ConditionOrder.Remove(token); UpdateRoutes();
        }
        /// <summary>登録の異常理由を保存して無効化し、同じ通知型と識別子の現在の登録にも適用します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="reason">無効化または全体停止の理由です。</param>
        private static void Disable(EventSubscription token, string reason)
        {
            token.IsEnabled = false; token.DisabledReason = reason;
            foreach (var registered in Subscriptions)
                if (registered.EventType == token.EventType && registered.Id == token.Id)
                { registered.IsEnabled = false; registered.DisabledReason = reason; }
            RecordFault(RegistrationKind.Subscription, token.EventType, token.Id, reason); UpdateRoutes();
        }
        /// <summary>登録の異常理由を保存して無効化し、同じ通知型と識別子の現在の登録にも適用します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="reason">無効化または全体停止の理由です。</param>
        private static void Disable(ConditionRegistration token, string reason)
        {
            token.IsEnabled = false; token.DisabledReason = reason;
            foreach (var registered in ConditionOrder)
                if (registered.EventType == token.EventType && registered.Id == token.Id)
                { registered.IsEnabled = false; registered.DisabledReason = reason; }
            RecordFault(RegistrationKind.Condition, token.EventType, token.Id, reason); UpdateRoutes();
        }
        /// <summary>有効な購読から必要条件と配送先を組み直し、Unity 通知の購読種類を更新します。</summary>
        private static void UpdateRoutes()
        {
            // 構成を直接書き換えず置換し、処理中のバッチは開始時の配送構成を維持します。
            var updated = new Dictionary<EditorChangeKind, Route>();
            var mask = EditorChangeKind.None;
            if (!Halted)
                foreach (var kind in Kinds)
                {
                    var subscribers = new List<SubscriptionRoute>();
                    var required = new HashSet<ConditionRegistration>();
                    foreach (var subscription in Subscriptions)
                    {
                        if (!subscription.IsEnabled || subscription.IsDisposed) continue;
                        var dependencies = new ConditionRegistration[subscription.ConditionTypes.Count];
                        var active = true;
                        var relevant = false;
                        for (var i = 0; i < dependencies.Length; i++)
                        {
                            if (!Conditions.TryGetValue(subscription.ConditionTypes[i], out var condition)
                                || !condition.IsEnabled || condition.IsDisposed) { active = false; break; }
                            dependencies[i] = condition;
                            relevant |= (condition.Changes & kind) != 0;
                        }
                        if (!active || !relevant) continue;
                        subscribers.Add(new SubscriptionRoute(subscription, dependencies));
                        foreach (var condition in dependencies)
                            if ((condition.Changes & kind) != 0) required.Add(condition);
                    }
                    if (subscribers.Count == 0) continue;
                    // 条件の登録順を維持し、各必要条件の1回の評価結果を共有します。
                    var conditions = ConditionOrder.FindAll(required.Contains).ToArray();
                    updated.Add(kind, new Route(conditions, subscribers.ToArray())); mask |= kind;
                }
            Routes = updated;
            Source.SetKinds(mask);
        }
        /// <summary>現在の実行環境でシーン編集を行えるかを取得します。</summary>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal static bool CanEdit() => Host.CanEdit;
        /// <summary>実行環境の基準で、ルートが編集可能な通常シーンの階層かを確認します。</summary>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        internal static bool IsEditableRoot(object root) => Host.IsEditableRoot(root);

        /// <summary>バッチの配送構成を固定し、必要な全条件の評価が終わってから保存した配送を逐次実行します。</summary>
        /// <param name="changes">この更新で処理する入力変更の一覧です。</param>
        internal static void ProcessChanges(IReadOnlyList<EditorChange> changes)
        {
            RequireMainThread();
            if (Halted || _processing || !CanEdit()) return;
            _processing = true;
            unchecked { BatchId++; }
            try
            {
                // バッチ全体の配送構成を固定し、ハンドラーが編集する前に全一致結果を確定します。
                var routes = Routes;
                List<INotification> notifications = null;
                Dictionary<ConditionRegistration, IConditionResult> results = null;
                HashSet<ConditionRegistration> required = null;
                for (var i = 0; i < changes.Count; i++)
                {
                    var change = changes[i];
                    if (!routes.TryGetValue(change.Kind, out var interested)) continue;
                    // 一時コレクションはこのバッチ内だけで使います。配送側は個別の結果を保持するため、
                    // 一時コレクションのクリアで保留中の通知値が変わることはありません。
                    if (results == null)
                    {
                        results = new Dictionary<ConditionRegistration, IConditionResult>();
                        required = new HashSet<ConditionRegistration>();
                    }
                    else { results.Clear(); required.Clear(); }
                    foreach (var candidate in interested.Subscribers)
                        if (IsLive(candidate.Subscription, candidate.Conditions))
                            foreach (var condition in candidate.Conditions) required.Add(condition);
                    foreach (var condition in interested.Conditions)
                    {
                        if (!required.Contains(condition) || condition.IsDisposed || !condition.IsEnabled) continue;
                        try
                        {
                            results.Add(condition, condition.Entry.Evaluate(change, condition));
                        }
                        catch (Exception error)
                        {
                            var reason = error.GetType().Name + ": " + error.Message;
                            Disable(condition, reason);
                            Host.LogWarning("[Editor Events] Condition '" + condition.Id + "' disabled until re-enabled in handler settings: " + reason);
                        }
                    }
                    foreach (var candidate in interested.Subscribers)
                    {
                        if (!IsLive(candidate.Subscription, candidate.Conditions)) continue;
                        var result = Combine(change, candidate, results);
                        if (result == null) continue;
                        if (notifications == null) notifications = new List<INotification>();
                        notifications.Add(new Notification(candidate.Subscription, candidate.Conditions, result));
                    }
                }
                if (notifications != null)
                    foreach (var notification in notifications) notification.Execute();
            }
            finally { _processing = false; }
        }
        /// <summary>同じ入力に対する保存済み結果を購読モードで選び、ルートとシーンが一致する複合通知を作ります。</summary>
        /// <param name="change">通知時点の種類と識別子を保持する入力変更です。</param>
        /// <param name="route">この入力種類の購読と依存条件の構成です。</param>
        /// <param name="results">同じ入力変更に対する、評価済みまたは選択済みの条件結果です。</param>
        /// <returns>購読モードに適合した一致結果です。不成立、ルート不一致、シーン不一致なら null です。</returns>
        private static IConditionResult Combine(EditorChange change, SubscriptionRoute route,
            Dictionary<ConditionRegistration, IConditionResult> results)
        {
            var subscription = route.Subscription;
            if (subscription.Mode == SubscriptionMode.Single)
                return results.TryGetValue(route.Conditions[0], out var single) ? single : null;
            IConditionResult first = null;
            Type firstType = null;
            foreach (var condition in route.Conditions)
            {
                results.TryGetValue(condition, out var result);
                if (result == null)
                {
                    if (subscription.Mode == SubscriptionMode.All) return null;
                    continue;
                }
                if (first == null) { first = result; firstType = condition.EventType; }
                else if (!ReferenceEquals(first.Root, result.Root) || first.Scene != result.Scene) return null;
                if (subscription.Mode == SubscriptionMode.Any) break;
            }
            if (first == null) return null;
            // 組み合わせが不成立なら通知コレクションを作成しません。All は全結果の検証後に作成し、
            // Any は最初に選んだ一致結果だけを保存します。
            var selected = new Dictionary<Type, object>(subscription.Mode == SubscriptionMode.Any ? 1 : route.Conditions.Length);
            if (subscription.Mode == SubscriptionMode.Any) selected.Add(firstType, first.Event);
            else foreach (var condition in route.Conditions) selected.Add(condition.EventType, results[condition].Event);
            var notification = new CompositeEvent(change, subscription.ConditionTypes, selected);
            return new ConditionResult<CompositeEvent>(new ConditionMatch<CompositeEvent>(notification, (UnityEngine.GameObject)first.Root), first.Scene);
        }
        /// <summary>ハンドラーを実行し、結果に応じて変更を確定または復元します。確定・復元異常やルート喪失では全体停止します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <param name="match">保存または配送する通知値と編集範囲のルートです。</param>
        internal static void Execute<T>(EventSubscription subscription, ConditionMatch<T> match)
        {
            var context = Host.CreateHandlerContext(match.Event, match.Root, subscription);
            var result = InvokeHandler(subscription, context);
            subscription.LastDuration = context.Elapsed;
            subscription.LastResult = result;
            try { context.Finish(result.Status == HandlerStatus.Succeeded); }
            catch (Exception error)
            {
                // 確定または復元が失敗した後はシーン状態が不明なため、後続処理を止めます。
                var reason = "Transaction completion failed: " + error.Message;
                subscription.LastResult = HandlerResult.Failure(reason);
                Halt(subscription, reason, match.Root);
                Disable(subscription, reason);
                Host.LogException(error, match.Root);
                return;
            }
            if (result.Status == HandlerStatus.Failed || result.Status == HandlerStatus.Cancelled)
            {
                var reason = string.IsNullOrEmpty(result.Message) ? result.Status.ToString() : result.Message;
                Disable(subscription, reason);
                Host.LogWarning("[Editor Events] Handler '" + subscription.Id + "' disabled until re-enabled in handler settings: " + reason, match.Root);
            }
            // コンテキスト API はルートを破棄できないため、ルート喪失は記録対象外の編集を意味します。
            if (!Host.RootExists(match.Root))
            {
                var reason = "The transaction root no longer exists after the handler returned.";
                subscription.LastResult = HandlerResult.Failure(reason);
                Halt(subscription, reason, null);
                Disable(subscription, reason);
            }
        }

        // ハンドラーを実行し、例外・期限超過・契約違反を終了結果へ変換します。
        /// <summary>ハンドラーの終了結果と期限を確認し、例外や実行契約違反を明示的な失敗・キャンセル結果へ変換します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <param name="context">このハンドラー呼び出しの編集範囲と期限を確認するコンテキストです。</param>
        /// <returns>期限と契約を確認した終了結果です。例外は失敗またはキャンセルに変換します。</returns>
        private static HandlerResult InvokeHandler<T>(EventSubscription subscription, HandlerContext<T> context)
        {
            try
            {
                var result = ((HandlerEntry<T>)subscription.Entry).Handler.Execute(context);
                context.CheckDeadline();
                if (result.Status == HandlerStatus.Unspecified || !Enum.IsDefined(typeof(HandlerStatus), result.Status))
                    throw new InvalidOperationException("The handler did not return a valid completion result.");
                if (result.Status == HandlerStatus.Skipped && context.HasChanges)
                    throw new InvalidOperationException("A skipped handler must not make changes.");
                return result;
            }
            catch (HandlerDeadlineExceededException error) { return HandlerResult.Failure(error.Message); }
            catch (OperationCanceledException error) { return HandlerResult.Cancel(error.Message); }
            catch (Exception error) { return HandlerResult.Failure(error.GetType().Name + ": " + error.Message); }
        }

        // 全配送を即時停止します。状態はメモリ上だけに保持し、ドメインリロードで解除します。
        /// <summary>全配送を停止して通知元の購読を更新し、原因となったハンドラーと理由を記録します。</summary>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <param name="reason">無効化または全体停止の理由です。</param>
        /// <param name="root">ログに関連付ける Unity オブジェクトです。関連付ける対象がなければ null です。</param>
        private static void Halt(EventSubscription subscription, string reason, object root)
        {
            Halted = true;
            UpdateRoutes();
            Host.LogError("[Editor Events] Processing halted after handler '" + subscription.Id + "': " + reason, root);
        }
    }
}
