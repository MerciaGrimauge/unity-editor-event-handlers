using System;
using System.Collections.Generic;


namespace EditorEventHandlers.Editor
{
    internal interface IHandlerEntry { Type ImplementationType { get; } }
    internal sealed class HandlerEntry<T> : IHandlerEntry
    {
        internal readonly IEventHandler<T> Handler;
        public Type ImplementationType => Handler.GetType();
        internal HandlerEntry(IEventHandler<T> handler) { Handler = handler; }
    }
    internal interface IConditionEntry
    {
        Type ImplementationType { get; }
        IConditionResult Evaluate(EditorChange change, ConditionRegistration registration);
    }
    internal interface INotification { void Execute(); }
    internal interface IConditionResult
    {
        object Root { get; }
        object Event { get; }
        EditorSceneId Scene { get; }
        void Execute(EventSubscription subscription);
    }
    internal sealed class ConditionEntry<T> : IConditionEntry
    {
        private static readonly bool NotificationIsValueType = typeof(T).IsValueType;
        private readonly IEventCondition<T> _condition;
        public Type ImplementationType => _condition.GetType();
        internal ConditionEntry(IEventCondition<T> condition) { _condition = condition; }
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
    internal sealed class ConditionResult<T> : IConditionResult
    {
        private readonly ConditionMatch<T> _match;
        public object Root => _match.Root;
        public object Event => _match.Event;
        public EditorSceneId Scene { get; }
        internal ConditionResult(ConditionMatch<T> match)
            : this(match, EventDispatcher.Host.GetSceneId(match.Root)) { }
        internal ConditionResult(ConditionMatch<T> match, EditorSceneId scene)
        { _match = match; Scene = scene; }
        public void Execute(EventSubscription subscription) => EventDispatcher.Execute(subscription, _match);
    }
    internal sealed class Notification : INotification
    {
        private readonly EventSubscription _subscription;
        private readonly ConditionRegistration[] _conditions;
        private readonly IConditionResult _result;
        internal Notification(EventSubscription subscription, ConditionRegistration[] conditions, IConditionResult result)
        { _subscription = subscription; _conditions = conditions; _result = result; }
        public void Execute()
        {
            if (!EventDispatcher.IsLive(_subscription, _conditions) || !EventDispatcher.CanEdit()) return;
            if (!EventDispatcher.IsEditableRoot(_result.Root) || EventDispatcher.Host.GetSceneId(_result.Root) != _result.Scene) return;
            _result.Execute(_subscription);
        }
    }

    // The dispatcher has no knowledge of avatar descriptors, clothing, AAO, or the payload's fields.
    // Unity integration is owned by the internal runtime boundary, not registrants.
    internal static partial class EventDispatcher
    {
        internal const int MaximumRegistrations = 100;
        private static int _registrationReservations;
        internal static readonly IEditorHost Host = EditorRuntime.Host;
        private static readonly Dictionary<Type, ConditionRegistration> Conditions = new Dictionary<Type, ConditionRegistration>();
        private static readonly List<ConditionRegistration> ConditionOrder = new List<ConditionRegistration>();
        private static readonly List<EventSubscription> Subscriptions = new List<EventSubscription>();
        private static Dictionary<EditorChangeKind, Route> Routes = new Dictionary<EditorChangeKind, Route>();
        // The single list of supported change kinds. UnityChangeSource must translate each of them.
        private static readonly EditorChangeKind[] Kinds = { EditorChangeKind.Created, EditorChangeKind.ParentChanged,
            EditorChangeKind.PropertiesChanged, EditorChangeKind.StructureChanged, EditorChangeKind.HierarchyChanged,
            EditorChangeKind.ChildrenReordered, EditorChangeKind.Destroyed, EditorChangeKind.PrefabUpdated };
        internal static readonly IEditorChangeSource Source = EditorRuntime.Source;
        internal static bool Halted { get; private set; }
        internal static long BatchId { get; private set; }
        private static bool _processing;
        private sealed class Route
        {
            internal readonly ConditionRegistration[] Conditions;
            internal readonly SubscriptionRoute[] Subscribers;
            internal Route(ConditionRegistration[] conditions, SubscriptionRoute[] subscribers)
            { Conditions = conditions; Subscribers = subscribers; }
        }
        private sealed class SubscriptionRoute
        {
            internal readonly EventSubscription Subscription;
            internal readonly ConditionRegistration[] Conditions;
            internal SubscriptionRoute(EventSubscription subscription, ConditionRegistration[] conditions)
            { Subscription = subscription; Conditions = conditions; }
        }
        // Selects which settings and fault records a registration uses. The names are part of the stored keys.
        private enum RegistrationKind { Condition, Subscription }
        private static string Key(RegistrationKind kind, Type type, string id) => "EditorEventHandlers."
            + (kind == RegistrationKind.Condition ? "Condition." : "Subscription.") + type.AssemblyQualifiedName + ":" + id;

        internal static void RequireMainThread() => Host.RequireMainThread();
        private static void Validate(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable, nonempty ID is required.");
        }
        // Taken before any registrant getter runs, so a full registry never reads third-party IDs and
        // a getter that registers again counts the outer registration. Callers release it in finally.
        private static void ReserveRegistration()
        {
            if (ConditionOrder.Count + Subscriptions.Count + _registrationReservations >= MaximumRegistrations)
                throw new InvalidOperationException("At most " + MaximumRegistrations + " conditions and handlers may be registered in total.");
            _registrationReservations++;
        }
        private static bool IsSupported(EditorChangeKind changes)
        {
            var supported = EditorChangeKind.None;
            foreach (var kind in Kinds) supported |= kind;
            return changes != EditorChangeKind.None && (changes & ~supported) == 0;
        }
        internal static bool HasCondition(Type type)
        {
            RequireMainThread();
            return !Halted && Conditions.TryGetValue(type, out var value) && !value.IsDisposed && value.IsEnabled;
        }
        internal static bool HasConditions(IReadOnlyList<Type> types)
        {
            RequireMainThread();
            foreach (var type in types) if (!HasCondition(type)) return false;
            return true;
        }
        internal static bool IsLive(EventSubscription subscription, ConditionRegistration[] dependencies)
        {
            if (Halted || subscription.IsDisposed || !subscription.IsEnabled) return false;
            foreach (var condition in dependencies)
                if (condition.IsDisposed || !condition.IsEnabled || !Conditions.TryGetValue(condition.EventType, out var current)
                    || !ReferenceEquals(current, condition)) return false;
            return true;
        }
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
        internal static void Unsubscribe(EventSubscription token)
        {
            RequireMainThread(); if (token.IsDisposed) return;
            token.IsDisposed = true; token.IsEnabled = false; Subscriptions.Remove(token); UpdateRoutes();
        }
        internal static void Unregister(ConditionRegistration token)
        {
            RequireMainThread(); if (token.IsDisposed) return;
            token.IsDisposed = true; token.IsEnabled = false;
            Conditions.Remove(token.EventType); ConditionOrder.Remove(token); UpdateRoutes();
        }
        private static void Disable(EventSubscription token, string reason)
        {
            token.IsEnabled = false; token.DisabledReason = reason;
            foreach (var registered in Subscriptions)
                if (registered.EventType == token.EventType && registered.Id == token.Id)
                { registered.IsEnabled = false; registered.DisabledReason = reason; }
            RecordFault(RegistrationKind.Subscription, token.EventType, token.Id, reason); UpdateRoutes();
        }
        private static void Disable(ConditionRegistration token, string reason)
        {
            token.IsEnabled = false; token.DisabledReason = reason;
            foreach (var registered in ConditionOrder)
                if (registered.EventType == token.EventType && registered.Id == token.Id)
                { registered.IsEnabled = false; registered.DisabledReason = reason; }
            RecordFault(RegistrationKind.Condition, token.EventType, token.Id, reason); UpdateRoutes();
        }
        private static void UpdateRoutes()
        {
            // Replace rather than mutate: an active batch keeps an immutable routing snapshot.
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
                    // Preserve condition registration order while sharing each required evaluation once.
                    var conditions = ConditionOrder.FindAll(required.Contains).ToArray();
                    updated.Add(kind, new Route(conditions, subscribers.ToArray())); mask |= kind;
                }
            Routes = updated;
            Source.SetKinds(mask);
        }
        internal static bool CanEdit() => Host.CanEdit;
        internal static bool IsEditableRoot(object root) => Host.IsEditableRoot(root);

        internal static void ProcessChanges(IReadOnlyList<EditorChange> changes)
        {
            RequireMainThread();
            if (Halted || _processing || !CanEdit()) return;
            _processing = true;
            unchecked { BatchId++; }
            try
            {
                // Freeze routing for the entire batch. Determine every match before any handler edits.
                var routes = Routes;
                List<INotification> notifications = null;
                Dictionary<ConditionRegistration, IConditionResult> results = null;
                HashSet<ConditionRegistration> required = null;
                for (var i = 0; i < changes.Count; i++)
                {
                    var change = changes[i];
                    if (!routes.TryGetValue(change.Kind, out var interested)) continue;
                    // Scratch collections belong only to this batch. Deliveries retain individual
                    // result objects, so clearing these collections cannot alter pending payloads.
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
            // Failed combinations allocate no payload collection. An All result has already
            // validated every member; Any stores only the first selected match.
            var selected = new Dictionary<Type, object>(subscription.Mode == SubscriptionMode.Any ? 1 : route.Conditions.Length);
            if (subscription.Mode == SubscriptionMode.Any) selected.Add(firstType, first.Event);
            else foreach (var condition in route.Conditions) selected.Add(condition.EventType, results[condition].Event);
            var notification = new CompositeEvent(change, subscription.ConditionTypes, selected);
            return new ConditionResult<CompositeEvent>(new ConditionMatch<CompositeEvent>(notification, (UnityEngine.GameObject)first.Root), first.Scene);
        }
        internal static void Execute<T>(EventSubscription subscription, ConditionMatch<T> match)
        {
            var context = Host.CreateHandlerContext(match.Event, match.Root, subscription);
            var result = InvokeHandler(subscription, context);
            subscription.LastDuration = context.Elapsed;
            subscription.LastResult = result;
            try { context.Finish(result.Status == HandlerStatus.Succeeded); }
            catch (Exception error)
            {
                // The scene state is unknown after a failed commit or rollback, so nothing else may run.
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
            // Context APIs cannot destroy the root, so its loss means an edit outside the transaction.
            if (!Host.RootExists(match.Root))
            {
                var reason = "The transaction root no longer exists after the handler returned.";
                subscription.LastResult = HandlerResult.Failure(reason);
                Halt(subscription, reason, null);
                Disable(subscription, reason);
            }
        }

        // Runs the handler and turns exceptions, deadline overruns and contract violations into a result.
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

        // Stops all dispatch at once. The flag lives only in memory and is cleared by a script domain reload.
        private static void Halt(EventSubscription subscription, string reason, object root)
        {
            Halted = true;
            UpdateRoutes();
            Host.LogError("[Editor Events] Processing halted after handler '" + subscription.Id + "': " + reason, root);
        }
    }
}
