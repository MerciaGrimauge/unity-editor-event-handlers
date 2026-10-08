using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    // A new member must also be listed in EventDispatcher.Kinds and translated by UnityChangeSource.
    /// <summary>Supported Unity change categories. Conditions combine flags; each input change has one category.</summary>
    [Flags]
    public enum EditorChangeKind
    {
        /// <summary>No category; cannot be registered as a condition's change mask.</summary>
        None = 0,
        /// <summary>A GameObject hierarchy was created.</summary>
        Created = 1,
        /// <summary>A GameObject's parent or scene changed.</summary>
        ParentChanged = 2,
        /// <summary>A GameObject or Component's properties changed.</summary>
        PropertiesChanged = 4,
        /// <summary>A GameObject's Component structure changed.</summary>
        StructureChanged = 8,
        /// <summary>A GameObject hierarchy's structure changed.</summary>
        HierarchyChanged = 16,
        /// <summary>Children were reordered.</summary>
        ChildrenReordered = 32,
        /// <summary>A GameObject hierarchy was destroyed.</summary>
        Destroyed = 64,
        /// <summary>A prefab instance was updated.</summary>
        PrefabUpdated = 128
    }

    // Snapshot of the native notification. References resolve at evaluation time and may be null.
    // For Destroyed, PreviousSceneId repeats SceneId and PreviousParentId is the last parent.
    /// <summary>Snapshot of a native change's category and temporary identities.</summary>
    /// <remarks>Resolved Unity references expose current state for inspection; they may be null. This does not identify the user who caused a change.</remarks>
    public readonly struct EditorChange : IEquatable<EditorChange>
    {
        /// <summary>The single category of the input notification.</summary>
        public EditorChangeKind Kind { get; }
        /// <summary>Temporary identity of the notification's target.</summary>
        public EditorObjectId ObjectId { get; }
        /// <summary>Notification-time scene identity; for a parent change, the new scene.</summary>
        public EditorSceneId SceneId { get; }
        /// <summary>Previous scene for parent changes; the same as SceneId for destruction; otherwise default.</summary>
        public EditorSceneId PreviousSceneId { get; }
        /// <summary>Previous parent for parent changes or last parent for destruction; otherwise default.</summary>
        public EditorObjectId PreviousParentId { get; }
        /// <summary>New parent for parent changes; otherwise default.</summary>
        public EditorObjectId NewParentId { get; }
        /// <summary>Resolves the current target on the editor main thread, or null if unavailable.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public Object Target => ObjectId.Resolve();
        /// <summary>Resolves the target GameObject, or a Component's owning GameObject, on the editor main thread; otherwise null.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public GameObject GameObject
        {
            get { var target = Target; return target is Component component ? component.gameObject : target as GameObject; }
        }
        /// <summary>Resolves the previous parent's current GameObject on the editor main thread, or null.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public GameObject PreviousParent => PreviousParentId.Resolve() as GameObject;
        /// <summary>Resolves the new parent's current GameObject on the editor main thread, or null.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public GameObject NewParent => NewParentId.Resolve() as GameObject;

        internal EditorChange(EditorChangeKind kind, EditorObjectId id, EditorSceneId scene, EditorSceneId previousScene = default,
            EditorObjectId previousParent = default, EditorObjectId newParent = default)
        {
            Kind = kind; ObjectId = id; SceneId = scene; PreviousSceneId = previousScene;
            PreviousParentId = previousParent; NewParentId = newParent;
        }
        /// <summary>Compares the category and all stored identities without resolving Unity objects.</summary>
        /// <param name="other">Change to compare.</param>
        /// <returns>Whether every stored field is equal.</returns>
        public bool Equals(EditorChange other) => Kind == other.Kind && ObjectId == other.ObjectId
            && SceneId == other.SceneId && PreviousSceneId == other.PreviousSceneId
            && PreviousParentId == other.PreviousParentId && NewParentId == other.NewParentId;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorChange other && Equals(other);
        /// <summary>Hash of the category and all identities for temporary collections.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ((int)Kind * 397) ^ ObjectId.GetHashCode();
                hash = (hash * 397) ^ SceneId.GetHashCode();
                hash = (hash * 397) ^ PreviousSceneId.GetHashCode();
                hash = (hash * 397) ^ PreviousParentId.GetHashCode();
                return (hash * 397) ^ NewParentId.GetHashCode();
            }
        }
    }

    // Condition provider: read-only detection. One definition per notification type.
    /// <summary>Defines a synchronous, read-only condition for an exact notification type.</summary>
    /// <typeparam name="TEvent">Notification type shared with its subscribers.</typeparam>
    /// <remarks>Do not edit Unity objects, use asynchronous work, or reenter the editor event loop during evaluation.</remarks>
    public interface IEventCondition<TEvent>
    {
        /// <summary>Stable, nonblank registration identifier. Its getter must be lightweight.</summary>
        string Id { get; }
        /// <summary>Required supported change flags; None and unknown flags are rejected at registration.</summary>
        EditorChangeKind Changes { get; }
        /// <summary>Evaluates the change on the editor main thread without editing it.</summary>
        /// <param name="context">This invocation's input and independent deadline.</param>
        /// <param name="match">Notification and editable hierarchy root when the return value is true.</param>
        /// <returns>True for a match; false leaves the output unused.</returns>
        bool TryMatch(ConditionContext context, out ConditionMatch<TEvent> match);
    }

    /// <summary>Notification and editable hierarchy selected by a condition.</summary>
    /// <typeparam name="TEvent">Exact notification type.</typeparam>
    public readonly struct ConditionMatch<TEvent>
    {
        /// <summary>Notification shared with matching subscribers; reference-type notifications must not be null.</summary>
        public TEvent Event { get; }
        /// <summary>Existing, ordinary loaded scene hierarchy that subscribers may edit through their contexts.</summary>
        public GameObject Root { get; }
        /// <summary>Creates a match value. The dispatcher validates successful matches after evaluation.</summary>
        /// <param name="notification">Notification to deliver.</param>
        /// <param name="transactionRoot">Root of the editable hierarchy; this does not snapshot the entire hierarchy.</param>
        public ConditionMatch(TEvent notification, GameObject transactionRoot)
        { Event = notification; Root = transactionRoot; }
    }

    /// <summary>Read-only input and cooperative deadline for one synchronous condition invocation.</summary>
    /// <remarks>Use on the editor main thread during evaluation only; do not retain for later work.</remarks>
    public sealed class ConditionContext
    {
        private readonly string _id;
        private readonly long _start = Stopwatch.GetTimestamp();
        private bool _open = true;
        /// <summary>Input change being evaluated.</summary>
        public EditorChange Change { get; }
        /// <summary>Temporary batch number for deduplication; not a persistent identity.</summary>
        public long BatchId { get; }
        /// <summary>Independent deadline captured when this invocation starts.</summary>
        public TimeSpan TimeLimit { get; }
        /// <summary>Time elapsed since this context was created.</summary>
        public TimeSpan Elapsed => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - _start)
            * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));
        internal ConditionContext(EditorChange change, string id, TimeSpan limit)
        { Change = change; BatchId = EventDispatcher.BatchId; _id = id; TimeLimit = limit; }
        /// <summary>Checks the editor thread, invocation lifetime, and cooperative deadline.</summary>
        /// <exception cref="InvalidOperationException">Called on another thread or after evaluation ended.</exception>
        /// <exception cref="ConditionDeadlineExceededException">Elapsed time has reached the invocation limit.</exception>
        public void CheckDeadline()
        {
            EventDispatcher.RequireMainThread();
            if (!_open) throw new InvalidOperationException("This condition context has already ended.");
            if (Elapsed >= TimeLimit) throw new ConditionDeadlineExceededException(_id, TimeLimit);
        }
        internal void Close() => _open = false;
    }

    // Event handler: synchronous execution, with explicit completion and scoped edits.
    /// <summary>Executes a synchronous handler and reports an explicit completion result.</summary>
    /// <typeparam name="TEvent">Exact notification type supplied by its condition.</typeparam>
    /// <remarks>Use context edit methods, and do not schedule delayed Unity edits or reenter the editor event loop.</remarks>
    public interface IEventHandler<TEvent>
    {
        /// <summary>Stable, nonblank identifier, unique among subscriptions for this notification type.</summary>
        string Id { get; }
        /// <summary>Handles the notification on the editor main thread within its independent deadline.</summary>
        /// <param name="context">Notification, scoped editing methods, and invocation deadline.</param>
        /// <returns>An explicit success, skip, failure, or cancellation result.</returns>
        HandlerResult Execute(HandlerContext<TEvent> context);
    }

    /// <summary>Explicit handler completion states. Unspecified is treated as a contract failure.</summary>
    public enum HandlerStatus
    {
        /// <summary>No explicit completion was reported.</summary>
        Unspecified,
        /// <summary>The handler completed successfully.</summary>
        Succeeded,
        /// <summary>The handler declined the notification without tracked edits.</summary>
        Skipped,
        /// <summary>The handler failed; tracked edits are subject to rollback.</summary>
        Failed,
        /// <summary>The handler cancelled; tracked edits are subject to rollback.</summary>
        Cancelled
    }
    /// <summary>Completion state and optional explanation returned by a handler.</summary>
    public readonly struct HandlerResult
    {
        /// <summary>Explicit completion state; the default value is Unspecified.</summary>
        public HandlerStatus Status { get; }
        /// <summary>Optional explanation; factory methods normalize null to an empty string.</summary>
        public string Message { get; }
        private HandlerResult(HandlerStatus status, string message)
        { Status = status; Message = message ?? string.Empty; }
        /// <summary>Reports successful completion. Tracked edits are committed if all execution checks pass.</summary>
        /// <returns>A successful result.</returns>
        public static HandlerResult Success() => new HandlerResult(HandlerStatus.Succeeded, null);
        /// <summary>Reports no work. A skip after tracked edits is treated as failure.</summary>
        /// <param name="reason">Optional explanation.</param>
        /// <returns>A skipped result.</returns>
        public static HandlerResult Skip(string reason = null) => new HandlerResult(HandlerStatus.Skipped, reason);
        /// <summary>Reports failure; the dispatcher attempts tracked rollback and disables the subscription.</summary>
        /// <param name="reason">Failure explanation; null becomes an empty string.</param>
        /// <returns>A failed result.</returns>
        public static HandlerResult Failure(string reason) => new HandlerResult(HandlerStatus.Failed, reason);
        /// <summary>Reports cancellation; the dispatcher attempts tracked rollback and disables the subscription.</summary>
        /// <param name="reason">Optional explanation.</param>
        /// <returns>A cancelled result.</returns>
        public static HandlerResult Cancel(string reason = null) => new HandlerResult(HandlerStatus.Cancelled, reason);
    }

    /// <summary>Cooperative handler deadline exceeded; it does not forcibly interrupt synchronous Unity code.</summary>
    public sealed class HandlerDeadlineExceededException : TimeoutException
    {
        internal HandlerDeadlineExceededException(string id, TimeSpan limit)
            : base("Handler '" + id + "' exceeded its " + limit.TotalMilliseconds + " ms deadline.") { }
    }
    /// <summary>Cooperative condition deadline exceeded; it does not forcibly interrupt synchronous Unity code.</summary>
    public sealed class ConditionDeadlineExceededException : TimeoutException
    {
        internal ConditionDeadlineExceededException(string id, TimeSpan limit)
            : base("Condition '" + id + "' exceeded its " + limit.TotalMilliseconds + " ms deadline.") { }
    }

    /// <summary>How one subscription combines condition results from the same input change.</summary>
    public enum SubscriptionMode
    {
        /// <summary>One exact notification type.</summary>
        Single,
        /// <summary>All declared conditions must match the same editable root.</summary>
        All,
        /// <summary>The first matching declared condition supplies the notification and editable root.</summary>
        Any
    }

    /// <summary>Read-only subscription state and explicit removal token. Retained registrations remain strongly referenced.</summary>
    public sealed class EventSubscription : IDisposable
    {
        internal readonly IHandlerEntry Entry;
        /// <summary>Identifier captured at registration.</summary>
        public string Id { get; }
        /// <summary>Handler payload type; CompositeEvent for an All or Any subscription.</summary>
        public Type EventType { get; }
        /// <summary>Combination mode captured at registration.</summary>
        public SubscriptionMode Mode { get; }
        /// <summary>Immutable condition notification types, in declaration order.</summary>
        public IReadOnlyList<Type> ConditionTypes { get; }
        internal TimeSpan ExecutionLimit;
        /// <summary>Current user-configured limit; each invocation captures its starting value.</summary>
        public TimeSpan TimeLimit => ExecutionLimit;
        /// <summary>Whether the subscription has been explicitly removed.</summary>
        public bool IsDisposed { get; internal set; }
        /// <summary>Whether user settings and fault policy currently enable the subscription.</summary>
        public bool IsEnabled { get; internal set; }
        /// <summary>Whether enabled, not disposed, and backed by every required active condition. Read on the editor main thread.</summary>
        public bool IsActive => !IsDisposed && IsEnabled && EventDispatcher.HasConditions(ConditionTypes);
        /// <summary>Reason for disabling; initially empty for an enabled subscription.</summary>
        public string DisabledReason { get; internal set; }
        /// <summary>Most recent completion result; Unspecified before the first invocation.</summary>
        public HandlerResult LastResult { get; internal set; }
        /// <summary>Most recent invocation duration, excluding transaction finalization; zero before first use.</summary>
        public TimeSpan LastDuration { get; internal set; }
        internal EventSubscription(IHandlerEntry entry, Type eventType, string id, TimeSpan limit,
            SubscriptionMode mode, Type[] conditionTypes)
        {
            Entry = entry; EventType = eventType; Id = id; ExecutionLimit = limit;
            Mode = mode; ConditionTypes = Array.AsReadOnly(conditionTypes);
        }
        /// <summary>Removes this subscription and releases its registration slot; repeated calls do nothing.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public void Dispose() => EventDispatcher.Unsubscribe(this);
    }

    /// <summary>Read-only condition state and explicit removal token. Removing it leaves subscriptions waiting.</summary>
    public sealed class ConditionRegistration : IDisposable
    {
        internal readonly IConditionEntry Entry;
        /// <summary>Identifier captured at registration.</summary>
        public string Id { get; }
        /// <summary>Exact notification type produced by this condition.</summary>
        public Type EventType { get; }
        /// <summary>Change categories captured at registration.</summary>
        public EditorChangeKind Changes { get; }
        internal TimeSpan ExecutionLimit;
        /// <summary>Current user-configured limit; each invocation captures its starting value.</summary>
        public TimeSpan TimeLimit => ExecutionLimit;
        /// <summary>Whether the condition has been explicitly removed.</summary>
        public bool IsDisposed { get; internal set; }
        /// <summary>Whether user settings and fault policy currently enable the condition.</summary>
        public bool IsEnabled { get; internal set; }
        /// <summary>Reason for disabling; initially empty for an enabled condition.</summary>
        public string DisabledReason { get; internal set; }
        /// <summary>Most recent evaluation duration; zero before first evaluation.</summary>
        public TimeSpan LastDuration { get; internal set; }
        internal ConditionRegistration(IConditionEntry entry, Type eventType, string id, EditorChangeKind changes, TimeSpan limit)
        { Entry = entry; EventType = eventType; Id = id; Changes = changes; ExecutionLimit = limit; }
        /// <summary>Removes this condition and releases its slot without removing its subscribers; repeated calls do nothing.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread.</exception>
        public void Dispose() => EventDispatcher.Unregister(this);
    }

    // The entire public dispatcher facade: registration only; no public publish or dispatch method.
    /// <summary>Main-thread registration facade. No public notification injection or policy override is exposed.</summary>
    public static class EditorEvents
    {
        /// <summary>Registers one condition for the exact notification type, within the combined registration limit.</summary>
        /// <typeparam name="TEvent">Exact notification type.</typeparam>
        /// <param name="condition">Read-only condition; its registration getters must be lightweight.</param>
        /// <returns>A removal token and read-only condition state.</returns>
        /// <exception cref="ArgumentNullException">The condition is null.</exception>
        /// <exception cref="ArgumentException">The identifier or change flags are invalid, or this type already has a condition.</exception>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread or the registration limit has been reached.</exception>
        /// <remarks>Getter exceptions propagate to the caller. Dropping the token does not unregister the condition.</remarks>
        public static ConditionRegistration RegisterCondition<TEvent>(IEventCondition<TEvent> condition)
            => EventDispatcher.Register(condition);
        /// <summary>Subscribes a synchronous handler; it waits while no active condition exists for this exact type.</summary>
        /// <typeparam name="TEvent">Exact notification type.</typeparam>
        /// <param name="handler">Handler with a lightweight identifier getter.</param>
        /// <returns>A removal token and read-only subscription state.</returns>
        /// <exception cref="ArgumentNullException">The handler is null.</exception>
        /// <exception cref="ArgumentException">The identifier is blank or already subscribed for this notification type.</exception>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread or the registration limit has been reached.</exception>
        /// <remarks>Identifier getter exceptions propagate. Dropping the token does not remove the subscription. Handler execution order is unspecified.</remarks>
        public static EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler)
            => EventDispatcher.Subscribe(handler);

        /// <summary>Subscribes when all declared conditions match the same input change and editable root.</summary>
        /// <param name="handler">Synchronous composite handler with a stable identifier.</param>
        /// <param name="eventTypes">One to 100 distinct, closed notification types in declaration order; copied at registration.</param>
        /// <returns>A removal token and read-only subscription state.</returns>
        /// <exception cref="ArgumentNullException">The handler or type array is null.</exception>
        /// <exception cref="ArgumentException">Types or identifier are invalid, or the composite identifier is already registered.</exception>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread or the combined registration limit has been reached.</exception>
        /// <remarks>Every required condition must be present and enabled. All required evaluations precede all handlers in a batch. Handler execution order is unspecified.</remarks>
        public static EventSubscription SubscribeAll(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
            => EventDispatcher.SubscribeComposite(handler, eventTypes, SubscriptionMode.All);

        /// <summary>Subscribes when any declared condition matches the same input change.</summary>
        /// <param name="handler">Synchronous composite handler with a stable identifier.</param>
        /// <param name="eventTypes">One to 100 distinct, closed notification types in declaration order; copied at registration.</param>
        /// <returns>A removal token and read-only subscription state.</returns>
        /// <exception cref="ArgumentNullException">The handler or type array is null.</exception>
        /// <exception cref="ArgumentException">Types or identifier are invalid, or the composite identifier is already registered.</exception>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread or the combined registration limit has been reached.</exception>
        /// <remarks>Every required condition must be present and enabled. Evaluations do not short-circuit; result selection stops at the first match and exposes only that result. Handler execution order is unspecified.</remarks>
        public static EventSubscription SubscribeAny(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
            => EventDispatcher.SubscribeComposite(handler, eventTypes, SubscriptionMode.Any);
    }
}
