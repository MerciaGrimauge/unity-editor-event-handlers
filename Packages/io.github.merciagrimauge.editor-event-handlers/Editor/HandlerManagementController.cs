using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EditorEventHandlers.Editor
{
    // 同じアセンブリ内の基底型です。protected の管理操作は内部の UI アダプターにだけ提供します。
    /// <summary>同じアセンブリ内の管理 UI にだけ、設定変更の操作を提供する基底クラスです。</summary>
    internal abstract class HandlerManagementController
    {
        /// <summary>管理画面から指定できる期限の下限です。</summary>
        protected const int MinimumMilliseconds = EventDispatcher.MinimumUserMilliseconds;
        /// <summary>管理画面から指定できる期限の上限です。</summary>
        protected const int MaximumMilliseconds = EventDispatcher.MaximumUserMilliseconds;
        /// <summary>期限設定が保存されていない場合に使う既定値です。</summary>
        protected const int DefaultMilliseconds = EventDispatcher.DefaultUserMilliseconds;
        /// <summary>管理画面に表示する条件登録と購読の合計上限です。</summary>
        protected const int MaximumRegistrations = EventDispatcher.MaximumRegistrations;
        /// <summary>現在、管理画面から設定を変更できるかどうかです。</summary>
        protected bool CanManage => EventDispatcher.CanManage;
        /// <summary>ディスパッチャーが全体停止しているかどうかです。</summary>
        protected bool IsHalted => EventDispatcher.Halted;
        /// <summary>表示用に現在の購読一覧のコピーを取得します。</summary>
        /// <returns>現在の購読一覧のコピーです。</returns>
        protected EventSubscription[] ReadHandlers() => EventDispatcher.ReadHandlers();
        /// <summary>表示用に現在の条件登録一覧のコピーを取得します。</summary>
        /// <returns>現在の条件登録一覧のコピーです。</returns>
        protected ConditionRegistration[] ReadConditions() => EventDispatcher.ReadConditions();
        /// <summary>指定した登録の手動の有効・無効を保存します。有効化時は保存済みの異常理由も解除します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="enabled">手動で有効にする場合は true、無効にする場合は false です。</param>
        protected void SetEnabled(EventSubscription token, bool enabled) => EventDispatcher.Manage(token, enabled, null);
        /// <summary>指定した登録の手動の有効・無効を保存します。有効化時は保存済みの異常理由も解除します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="enabled">手動で有効にする場合は true、無効にする場合は false です。</param>
        protected void SetEnabled(ConditionRegistration token, bool enabled) => EventDispatcher.Manage(token, enabled, null);
        /// <summary>指定した登録の個別の期限をミリ秒単位で変更し、保存します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="milliseconds">期限のミリ秒値です。許容範囲は1〜100です。</param>
        protected void SetTimeout(EventSubscription token, int milliseconds) => EventDispatcher.Manage(token, null, milliseconds);
        /// <summary>指定した登録の個別の期限をミリ秒単位で変更し、保存します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="milliseconds">期限のミリ秒値です。許容範囲は1〜100です。</param>
        protected void SetTimeout(ConditionRegistration token, int milliseconds) => EventDispatcher.Manage(token, null, milliseconds);
    }

    /// <summary>条件と購読を管理し、判定結果を共有して逐次配送する内部ディスパッチャーです。</summary>
    internal static partial class EventDispatcher
    {
        /// <summary>利用者が指定できる期限のミリ秒値の下限です。</summary>
        internal const int MinimumUserMilliseconds = 1;
        /// <summary>利用者が指定できる期限のミリ秒値の上限です。</summary>
        internal const int MaximumUserMilliseconds = 100;
        // 新規登録と範囲外の保存値に適用する既定値なので、ユーザー設定の許容範囲内に置きます。
        /// <summary>期限設定が保存されていない場合に使うミリ秒値です。</summary>
        internal const int DefaultUserMilliseconds = 100;
        /// <summary>1件の登録に適用する手動設定、期限、異常理由、保存先キーを保持します。</summary>
        private sealed class Policy
        {
            /// <summary>利用者が設定した手動の有効状態です。</summary>
            internal bool Enabled;
            /// <summary>利用者が設定した期限のミリ秒値です。</summary>
            internal int Milliseconds;
            /// <summary>異常により登録を停止している理由です。停止していなければ空文字列です。</summary>
            internal string Fault;
            /// <summary>この登録の永続設定を読み書きするキーの接頭辞です。</summary>
            internal string PreferenceKey;
            /// <summary>手動無効化を優先し、それ以外の場合は保存済みの異常理由を返します。</summary>
            internal string DisabledReason => !Enabled ? "Disabled in handler settings." : Fault;
        }
        /// <summary>登録種別・通知型・識別子を組み合わせたキーごとの設定キャッシュです。</summary>
        private static readonly Dictionary<string, Policy> Policies = new Dictionary<string, Policy>();
        /// <summary>配送中でなく、実行環境が編集可能な状態であるかどうかです。</summary>
        internal static bool CanManage => !_processing && CanEdit();

        /// <summary>プロジェクトと登録を区別するキーで設定を読み込み、旧セッションの異常理由を必要な場合だけ移行します。</summary>
        /// <param name="kind">条件登録と購読を区別する種別です。</param>
        /// <param name="type">完全一致で登録や設定を区別する通知型です。</param>
        /// <param name="id">設定と異常記録の区別に使う安定した登録識別子です。</param>
        /// <returns>読み込み済みまたはキャッシュ済みの、この登録に適用する設定です。</returns>
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
                // Editor セッション終了後に永続化されなかった旧方式の異常記録を引き継ぎます。
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
        /// <summary>登録の異常理由を現在の方針と永続設定に保存します。</summary>
        /// <param name="kind">条件登録と購読を区別する種別です。</param>
        /// <param name="type">完全一致で登録や設定を区別する通知型です。</param>
        /// <param name="id">設定と異常記録の区別に使う安定した登録識別子です。</param>
        /// <param name="reason">無効化または全体停止の理由です。</param>
        private static void RecordFault(RegistrationKind kind, Type type, string id, string reason)
        {
            var policy = GetPolicy(kind, type, id);
            policy.Fault = reason;
            Host.WritePreferenceString(policy.PreferenceKey + ".Fault", reason);
        }
        /// <summary>メインスレッドで現在の購読一覧のコピーを返します。</summary>
        /// <returns>現在の購読一覧のコピーです。</returns>
        internal static EventSubscription[] ReadHandlers()
        { RequireMainThread(); return Subscriptions.ToArray(); }
        /// <summary>メインスレッドで現在の条件登録一覧のコピーを返します。</summary>
        /// <returns>現在の条件登録一覧のコピーです。</returns>
        internal static ConditionRegistration[] ReadConditions()
        { RequireMainThread(); return ConditionOrder.ToArray(); }

        /// <summary>メインスレッド・管理可能状態・指定期限の範囲を確認します。</summary>
        /// <param name="milliseconds">期限のミリ秒値です。null なら期限を変更しません。指定値の許容範囲は1〜100です。</param>
        private static void RequireManagement(int? milliseconds)
        {
            RequireMainThread();
            if (!CanManage) throw new InvalidOperationException("Settings cannot change while editor processing is active.");
            if (milliseconds.HasValue && (milliseconds.Value < MinimumUserMilliseconds || milliseconds.Value > MaximumUserMilliseconds))
                throw new ArgumentOutOfRangeException(nameof(milliseconds),
                    "Timeout must be between " + MinimumUserMilliseconds + " and " + MaximumUserMilliseconds + " ms.");
        }
        /// <summary>指定された手動有効状態と期限だけを変更・保存し、有効化時は新旧の異常記録を解除します。</summary>
        /// <param name="kind">条件登録と購読を区別する種別です。</param>
        /// <param name="type">完全一致で登録や設定を区別する通知型です。</param>
        /// <param name="id">設定と異常記録の区別に使う安定した登録識別子です。</param>
        /// <param name="enabled">手動の有効・無効です。null なら現在の設定を変更しません。</param>
        /// <param name="milliseconds">期限のミリ秒値です。null なら期限を変更しません。指定値の許容範囲は1〜100です。</param>
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
        /// <summary>現在も登録されているトークンの設定を変更し、状態・期限・配送構成へ反映します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="enabled">手動の有効・無効です。null なら現在の設定を変更しません。</param>
        /// <param name="milliseconds">期限のミリ秒値です。null なら期限を変更しません。指定値の許容範囲は1〜100です。</param>
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
        /// <summary>現在も登録されているトークンの設定を変更し、状態・期限・配送構成へ反映します。</summary>
        /// <param name="token">状態または設定を変更する現在の登録トークンです。</param>
        /// <param name="enabled">手動の有効・無効です。null なら現在の設定を変更しません。</param>
        /// <param name="milliseconds">期限のミリ秒値です。null なら期限を変更しません。指定値の許容範囲は1〜100です。</param>
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
