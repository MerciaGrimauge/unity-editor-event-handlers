using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EditorEventHandlers.Editor
{
    // Assembly-private base: protected operations are available only to local UI adapters.
    internal abstract class HandlerManagementController
    {
        protected const int MinimumMilliseconds = EventDispatcher.MinimumUserMilliseconds;
        protected const int MaximumMilliseconds = EventDispatcher.MaximumUserMilliseconds;
        protected const int DefaultMilliseconds = EventDispatcher.DefaultUserMilliseconds;
        protected const int MaximumRegistrations = EventDispatcher.MaximumRegistrations;
        protected bool CanManage => EventDispatcher.CanManage;
        protected bool IsHalted => EventDispatcher.Halted;
        protected EventSubscription[] ReadHandlers() => EventDispatcher.ReadHandlers();
        protected ConditionRegistration[] ReadConditions() => EventDispatcher.ReadConditions();
        protected void SetEnabled(EventSubscription token, bool enabled) => EventDispatcher.Manage(token, enabled, null);
        protected void SetEnabled(ConditionRegistration token, bool enabled) => EventDispatcher.Manage(token, enabled, null);
        protected void SetTimeout(EventSubscription token, int milliseconds) => EventDispatcher.Manage(token, null, milliseconds);
        protected void SetTimeout(ConditionRegistration token, int milliseconds) => EventDispatcher.Manage(token, null, milliseconds);
    }

    internal static partial class EventDispatcher
    {
        internal const int MinimumUserMilliseconds = 1;
        internal const int MaximumUserMilliseconds = 100;
        // Applied to new registrations and to saved values outside the user range, so it must lie within that range.
        internal const int DefaultUserMilliseconds = 100;
        private sealed class Policy
        {
            internal bool Enabled;
            internal int Milliseconds;
            internal string Fault;
            internal string PreferenceKey;
            internal string DisabledReason => !Enabled ? "Disabled in handler settings." : Fault;
        }
        private static readonly Dictionary<string, Policy> Policies = new Dictionary<string, Policy>();
        internal static bool CanManage => !_processing && CanEdit();

        private static Policy GetPolicy(RegistrationKind kind, Type type, string id)
        {
            var key = Key(kind, type, id);
            if (Policies.TryGetValue(key, out var cached)) return cached;
            string preference;
            using (var hash = SHA256.Create())
                preference = "EditorEventHandlers.User." + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(Host.SettingsScope + "\n" + key))).Replace("-", string.Empty);
            var milliseconds = Host.ReadPreferenceInt(preference + ".Timeout", DefaultUserMilliseconds);
            var fault = Host.ReadPreferenceString(preference + ".Fault", null);
            if (fault == null)
            {
                // Upgrade faults recorded before they persisted beyond the current Editor session.
                fault = Host.ReadSessionString(key);
                if (fault.Length != 0) Host.WritePreferenceString(preference + ".Fault", fault);
            }
            var policy = new Policy
            {
                Enabled = Host.ReadPreferenceBool(preference + ".Enabled", true),
                Milliseconds = milliseconds >= MinimumUserMilliseconds && milliseconds <= MaximumUserMilliseconds
                    ? milliseconds : DefaultUserMilliseconds,
                Fault = fault,
                PreferenceKey = preference
            };
            Policies.Add(key, policy);
            return policy;
        }
        private static void RecordFault(RegistrationKind kind, Type type, string id, string reason)
        {
            var policy = GetPolicy(kind, type, id);
            policy.Fault = reason;
            Host.WritePreferenceString(policy.PreferenceKey + ".Fault", reason);
        }
        internal static EventSubscription[] ReadHandlers()
        { RequireMainThread(); return Subscriptions.ToArray(); }
        internal static ConditionRegistration[] ReadConditions()
        { RequireMainThread(); return ConditionOrder.ToArray(); }

        private static void RequireManagement(int? milliseconds)
        {
            RequireMainThread();
            if (!CanManage) throw new InvalidOperationException("Settings cannot change while editor processing is active.");
            if (milliseconds.HasValue && (milliseconds.Value < MinimumUserMilliseconds || milliseconds.Value > MaximumUserMilliseconds))
                throw new ArgumentOutOfRangeException(nameof(milliseconds),
                    "Timeout must be between " + MinimumUserMilliseconds + " and " + MaximumUserMilliseconds + " ms.");
        }
        private static void ChangePolicy(RegistrationKind kind, Type type, string id, bool? enabled, int? milliseconds)
        {
            var policy = GetPolicy(kind, type, id);
            if (enabled.HasValue)
            {
                policy.Enabled = enabled.Value;
                if (enabled.Value)
                {
                    policy.Fault = string.Empty;
                    Host.ErasePreferenceString(policy.PreferenceKey + ".Fault");
                    Host.EraseSessionString(Key(kind, type, id));
                }
                Host.WritePreferenceBool(policy.PreferenceKey + ".Enabled", policy.Enabled);
            }
            if (milliseconds.HasValue)
            {
                policy.Milliseconds = milliseconds.Value;
                Host.WritePreferenceInt(policy.PreferenceKey + ".Timeout", policy.Milliseconds);
            }
        }
        internal static void Manage(EventSubscription token, bool? enabled, int? milliseconds)
        {
            RequireManagement(milliseconds);
            if (token == null || token.IsDisposed || !Subscriptions.Contains(token))
                throw new InvalidOperationException("The handler is no longer registered.");
            ChangePolicy(RegistrationKind.Subscription, token.EventType, token.Id, enabled, milliseconds);
            var policy = GetPolicy(RegistrationKind.Subscription, token.EventType, token.Id);
            token.IsEnabled = policy.DisabledReason.Length == 0; token.DisabledReason = policy.DisabledReason;
            token.ExecutionLimit = TimeSpan.FromMilliseconds(policy.Milliseconds);
            UpdateRoutes();
        }
        internal static void Manage(ConditionRegistration token, bool? enabled, int? milliseconds)
        {
            RequireManagement(milliseconds);
            if (token == null || token.IsDisposed || !ConditionOrder.Contains(token))
                throw new InvalidOperationException("The condition is no longer registered.");
            ChangePolicy(RegistrationKind.Condition, token.EventType, token.Id, enabled, milliseconds);
            var policy = GetPolicy(RegistrationKind.Condition, token.EventType, token.Id);
            token.IsEnabled = policy.DisabledReason.Length == 0; token.DisabledReason = policy.DisabledReason;
            token.ExecutionLimit = TimeSpan.FromMilliseconds(policy.Milliseconds);
            UpdateRoutes();
        }
    }
}
