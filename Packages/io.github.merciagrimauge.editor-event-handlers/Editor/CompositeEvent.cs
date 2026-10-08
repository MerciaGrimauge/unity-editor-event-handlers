using System;
using System.Collections.Generic;

namespace EditorEventHandlers.Editor
{
    /// <summary>Saved condition notifications selected for one flat All or Any subscription.</summary>
    /// <remarks>The collection is immutable; contained notification objects retain their original references. Any exposes only its first matching result.</remarks>
    public sealed class CompositeEvent
    {
        private readonly Dictionary<Type, object> _results;
        /// <summary>The input change shared by every evaluated condition in this combination.</summary>
        public EditorChange Change { get; }
        /// <summary>Immutable required notification types in the subscription's declaration order.</summary>
        public IReadOnlyList<Type> RequiredTypes { get; }

        internal CompositeEvent(EditorChange change, IReadOnlyList<Type> requiredTypes, Dictionary<Type, object> results)
        { Change = change; RequiredTypes = requiredTypes; _results = results; }

        /// <summary>Reads a selected notification by its exact declared type.</summary>
        /// <typeparam name="TEvent">Exact condition notification type; derived or assignable types are not substituted.</typeparam>
        /// <param name="notification">Saved result when present, otherwise the type's default value.</param>
        /// <returns>Whether the combination exposes a notification of this exact type.</returns>
        public bool TryGet<TEvent>(out TEvent notification)
        {
            if (_results.TryGetValue(typeof(TEvent), out var value))
            { notification = (TEvent)value; return true; }
            notification = default;
            return false;
        }
    }
}
