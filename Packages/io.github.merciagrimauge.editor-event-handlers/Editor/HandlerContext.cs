using System;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    /// <summary>One synchronous handler invocation's typed notification and scoped editing context.</summary>
    /// <typeparam name="TEvent">Exact notification type selected by the condition.</typeparam>
    public sealed class HandlerContext<TEvent> : HandlerContext
    {
        /// <summary>Condition result shared with subscribers; Unity references are for inspection.</summary>
        public TEvent Event { get; }
        internal HandlerContext(TEvent notification, GameObject root, EventSubscription subscription)
            : base(root, subscription) { Event = notification; }
    }

    // Synchronous scoped context. Unity references are for inspection; edits use the transaction methods.
    /// <summary>Scoped editing methods and cooperative deadline for one synchronous main-thread handler invocation.</summary>
    /// <remarks>Do not reuse after execution, perform direct Unity writes, or use delayed edits. Tracked Undo is not a full hierarchy snapshot. Unity and edit-action exceptions can propagate.</remarks>
    public abstract class HandlerContext
    {
        private readonly EventSubscription _registration;
        private readonly long _start;
        private UnityEditTransaction _transaction;
        private bool _open = true;
        /// <summary>Condition-selected root of the editable scene hierarchy; inspect it and use context methods for edits.</summary>
        public GameObject Root { get; }
        /// <summary>Independent execution limit captured at invocation start.</summary>
        public TimeSpan TimeLimit { get; }
        /// <summary>Time elapsed since context creation; reading it does not extend the deadline.</summary>
        public TimeSpan Elapsed => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - _start)
            * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));
        internal bool HasChanges => _transaction != null && _transaction.HasChanges;

        // Read-only and skipped handlers do not allocate undo tracking collections.
        private UnityEditTransaction Transaction
        {
            get
            {
                if (_transaction != null) return _transaction;
                CheckDeadline();
                return _transaction = new UnityEditTransaction(Root, _registration.Id, this);
            }
        }

        internal HandlerContext(GameObject root, EventSubscription registration)
        {
            Root = root; _registration = registration; TimeLimit = registration.TimeLimit;
            _start = Stopwatch.GetTimestamp();
        }

        /// <summary>Checks the editor thread, invocation lifetime, and cooperative deadline.</summary>
        /// <exception cref="InvalidOperationException">Called outside the editor main thread or after the invocation ended.</exception>
        /// <exception cref="HandlerDeadlineExceededException">Elapsed time has reached the execution limit.</exception>
        public void CheckDeadline()
        {
            EventDispatcher.RequireMainThread();
            if (!_open) throw new InvalidOperationException("This handler context has already ended.");
            if (Elapsed >= TimeLimit) throw new HandlerDeadlineExceededException(_registration.Id, TimeLimit);
        }

        /// <summary>Adds an Undo-tracked Component to a GameObject in the editable hierarchy.</summary>
        /// <param name="target">Root or a descendant in the same scene.</param>
        /// <param name="componentType">Type derived from Component.</param>
        /// <returns>The added Component.</returns>
        /// <exception cref="ArgumentException">The target is outside the scope or the type is not a Component type.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, lost Root, or Unity could not add the Component.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public Component AddComponent(GameObject target, Type componentType) => Transaction.AddComponent(target, componentType);
        /// <summary>Adds an Undo-tracked Component of the specified type to the editable hierarchy.</summary>
        /// <typeparam name="T">Component type to add.</typeparam>
        /// <param name="target">Root or a descendant in the same scene.</param>
        /// <returns>The added typed Component.</returns>
        /// <exception cref="ArgumentException">The target is outside the editable scope.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, lost Root, or Unity could not add the Component.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public T AddComponent<T>(GameObject target) where T : Component => (T)AddComponent(target, typeof(T));
        /// <summary>Records a scoped GameObject or Component, then synchronously edits its serializable properties.</summary>
        /// <param name="target">Object whose properties are recorded and edited.</param>
        /// <param name="edit">Action that edits only this target's properties.</param>
        /// <remarks>Use the dedicated context methods for creation, destruction, and parenting. Other objects require separate Modify calls.</remarks>
        /// <exception cref="ArgumentException">The target is not a scene GameObject or Component within the scope.</exception>
        /// <exception cref="ArgumentNullException">The edit action is null.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, or lost Root.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public void Modify(Object target, Action edit) => Transaction.Modify(target, edit);
        /// <summary>Creates an Undo-tracked GameObject as a child in the editable scene hierarchy.</summary>
        /// <param name="name">Name passed to the new GameObject.</param>
        /// <param name="parent">Root or a descendant; null uses Root.</param>
        /// <returns>The created child.</returns>
        /// <exception cref="ArgumentException">The parent is outside the editable scope.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, or lost Root.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public GameObject CreateChild(string name, GameObject parent = null) => Transaction.CreateChild(name, parent);
        /// <summary>Instantiates a Prefab asset as an Undo-tracked child in Root's scene.</summary>
        /// <param name="prefab">Prefab asset to instantiate.</param>
        /// <param name="parent">Root or a descendant; null uses Root.</param>
        /// <returns>The created Prefab instance.</returns>
        /// <exception cref="ArgumentException">The input is not a Prefab asset or the parent is outside the editable scope.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, or lost Root.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null) => Transaction.InstantiatePrefab(prefab, parent);
        /// <summary>Reparents an Undo-tracked descendant within the editable hierarchy.</summary>
        /// <param name="child">Descendant to move; cannot be Root.</param>
        /// <param name="parent">Root or another descendant; cannot create a parenting cycle.</param>
        /// <exception cref="ArgumentException">An object is outside the scope, child is Root, or parenting would create a cycle.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, or lost Root.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public void SetParent(GameObject child, GameObject parent) => Transaction.SetParent(child, parent);
        /// <summary>Destroys an Undo-tracked child GameObject or allowed Component within the editable hierarchy.</summary>
        /// <param name="target">Descendant GameObject or Component; Root and Transform components cannot be destroyed.</param>
        /// <exception cref="ArgumentException">The target is outside the scope, Root, or a Transform.</exception>
        /// <exception cref="InvalidOperationException">Wrong thread, ended invocation, or lost Root.</exception>
        /// <exception cref="HandlerDeadlineExceededException">The invocation deadline is reached before or after editing.</exception>
        public void Destroy(Object target) => Transaction.Destroy(target);
        internal void Finish(bool commit)
        { _open = false; _transaction?.Finish(commit); }
    }
}
