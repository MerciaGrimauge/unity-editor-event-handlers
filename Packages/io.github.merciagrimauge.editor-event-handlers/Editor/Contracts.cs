using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    // A new member must also be listed in EventDispatcher.Kinds and translated by UnityChangeSource.
    /// <summary>対応する Unity 変更の種類です。条件はフラグを組み合わせて指定します。各入力変更は1種類です。</summary>
    [Flags]
    public enum EditorChangeKind
    {
        /// <summary>変更の種類を指定しない値です。条件の変更フラグとしては登録できません。</summary>
        None = 0,
        /// <summary>GameObject の階層が作成されました。</summary>
        Created = 1,
        /// <summary>GameObject の親または所属シーンが変更されました。</summary>
        ParentChanged = 2,
        /// <summary>GameObject または Component のプロパティが変更されました。</summary>
        PropertiesChanged = 4,
        /// <summary>GameObject のコンポーネント構成が変更されました。</summary>
        StructureChanged = 8,
        /// <summary>GameObject の階層構造が変更されました。</summary>
        HierarchyChanged = 16,
        /// <summary>子オブジェクトの順序が変更されました。</summary>
        ChildrenReordered = 32,
        /// <summary>GameObject の階層が破棄されました。</summary>
        Destroyed = 64,
        /// <summary>Prefab インスタンスが更新されました。</summary>
        PrefabUpdated = 128
    }

    // Snapshot of the native notification. References resolve at evaluation time and may be null.
    // For Destroyed, PreviousSceneId repeats SceneId and PreviousParentId is the last parent.
    /// <summary>Unity の変更通知から保存した、変更の種類と一時的な識別子です。</summary>
    /// <remarks>識別子から取得する Unity 参照は現在の状態の確認用で、null の場合があります。変更を起こしたユーザーを識別する情報ではありません。</remarks>
    public readonly struct EditorChange : IEquatable<EditorChange>
    {
        /// <summary>入力通知の変更の種類です。</summary>
        public EditorChangeKind Kind { get; }
        /// <summary>通知対象の一時的な識別子です。</summary>
        public EditorObjectId ObjectId { get; }
        /// <summary>通知時点のシーン識別子です。親変更の場合は新しい所属シーンです。</summary>
        public EditorSceneId SceneId { get; }
        /// <summary>親変更前の所属シーンです。破棄の場合は SceneId と同じ値、それ以外は default です。</summary>
        public EditorSceneId PreviousSceneId { get; }
        /// <summary>親変更前の親、または破棄直前の親です。それ以外は default です。</summary>
        public EditorObjectId PreviousParentId { get; }
        /// <summary>親変更時の新しい親です。それ以外は default です。</summary>
        public EditorObjectId NewParentId { get; }
        /// <summary>Editor のメインスレッドで現在の対象への参照を取得します。取得できない場合は null です。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public Object Target => ObjectId.Resolve();
        /// <summary>Editor のメインスレッドで対象の GameObject、または対象 Component が属する GameObject を取得します。取得できない場合は null です。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public GameObject GameObject
        {
            get { var target = Target; return target is Component component ? component.gameObject : target as GameObject; }
        }
        /// <summary>Editor のメインスレッドで変更前の親の現在の GameObject を取得します。取得できない場合は null です。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public GameObject PreviousParent => PreviousParentId.Resolve() as GameObject;
        /// <summary>Editor のメインスレッドで新しい親の現在の GameObject を取得します。取得できない場合は null です。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public GameObject NewParent => NewParentId.Resolve() as GameObject;

        internal EditorChange(EditorChangeKind kind, EditorObjectId id, EditorSceneId scene, EditorSceneId previousScene = default,
            EditorObjectId previousParent = default, EditorObjectId newParent = default)
        {
            Kind = kind; ObjectId = id; SceneId = scene; PreviousSceneId = previousScene;
            PreviousParentId = previousParent; NewParentId = newParent;
        }
        /// <summary>Unity オブジェクトへの参照を取得せず、変更の種類と保存した全識別子を比較します。</summary>
        /// <param name="other">比較対象の変更です。</param>
        /// <returns>保存した全フィールドが等しいかどうかです。</returns>
        public bool Equals(EditorChange other) => Kind == other.Kind && ObjectId == other.ObjectId
            && SceneId == other.SceneId && PreviousSceneId == other.PreviousSceneId
            && PreviousParentId == other.PreviousParentId && NewParentId == other.NewParentId;
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EditorChange other && Equals(other);
        /// <summary>一時的なコレクションに使う、変更の種類と全識別子のハッシュ値です。</summary>
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
    /// <summary>指定した通知型に対する、同期的で読み取り専用の条件を定義します。</summary>
    /// <typeparam name="TEvent">購読者と共有する通知の型です。</typeparam>
    /// <remarks>評価中は Unity オブジェクトの編集、非同期処理、Editor のイベントループへの再入を行わないでください。</remarks>
    public interface IEventCondition<TEvent>
    {
        /// <summary>空白でない安定した登録識別子です。ゲッターは軽量な処理にしてください。</summary>
        string Id { get; }
        /// <summary>評価対象の変更フラグです。None や未対応のフラグは登録時に拒否します。</summary>
        EditorChangeKind Changes { get; }
        /// <summary>Editor のメインスレッドで、対象を編集せずに変更を評価します。</summary>
        /// <param name="context">この呼び出しの入力と、個別の期限です。</param>
        /// <param name="match">戻り値が true のときの通知と、編集可能な階層のルートです。</param>
        /// <returns>一致した場合は true です。false の場合、出力値は使いません。</returns>
        bool TryMatch(ConditionContext context, out ConditionMatch<TEvent> match);
    }

    /// <summary>条件が選んだ通知と、編集可能な階層です。</summary>
    /// <typeparam name="TEvent">通知の型です。型は完全一致で扱います。</typeparam>
    public readonly struct ConditionMatch<TEvent>
    {
        /// <summary>一致した購読者間で共有する通知です。参照型の通知には null を指定できません。</summary>
        public TEvent Event { get; }
        /// <summary>購読者がコンテキストを通じて編集できる、読み込み済みの通常シーンにある既存の階層です。</summary>
        public GameObject Root { get; }
        /// <summary>一致結果を作成します。一致した結果の妥当性は、評価後にディスパッチャーが確認します。</summary>
        /// <param name="notification">配送する通知です。</param>
        /// <param name="transactionRoot">編集可能な階層のルートです。階層全体の状態を複製するものではありません。</param>
        public ConditionMatch(TEvent notification, GameObject transactionRoot)
        { Event = notification; Root = transactionRoot; }
    }

    /// <summary>同期的な条件の1回の呼び出しで使う、読み取り専用の入力と協調的な期限です。</summary>
    /// <remarks>評価中の Editor メインスレッドでだけ使ってください。後の処理のために保持しないでください。</remarks>
    public sealed class ConditionContext
    {
        private readonly string _id;
        private readonly long _start = Stopwatch.GetTimestamp();
        private bool _open = true;
        /// <summary>評価対象の入力変更です。</summary>
        public EditorChange Change { get; }
        /// <summary>重複排除に使う一時的なバッチ番号です。永続的な識別子ではありません。</summary>
        public long BatchId { get; }
        /// <summary>この呼び出しの開始時に保持した、個別の期限です。</summary>
        public TimeSpan TimeLimit { get; }
        /// <summary>このコンテキストの作成からの経過時間です。</summary>
        public TimeSpan Elapsed => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - _start)
            * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));
        internal ConditionContext(EditorChange change, string id, TimeSpan limit)
        { Change = change; BatchId = EventDispatcher.BatchId; _id = id; TimeLimit = limit; }
        /// <summary>Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。</summary>
        /// <exception cref="InvalidOperationException">別スレッド、または条件評価の終了後に呼び出した場合です。</exception>
        /// <exception cref="ConditionDeadlineExceededException">経過時間が呼び出しの期限に達した場合です。</exception>
        public void CheckDeadline()
        {
            EventDispatcher.RequireMainThread();
            if (!_open) throw new InvalidOperationException("This condition context has already ended.");
            if (Elapsed >= TimeLimit) throw new ConditionDeadlineExceededException(_id, TimeLimit);
        }
        internal void Close() => _open = false;
    }

    // Event handler: synchronous execution, with explicit completion and scoped edits.
    /// <summary>同期的にハンドラーを実行し、終了結果を明示して返します。</summary>
    /// <typeparam name="TEvent">条件から渡される通知の型です。</typeparam>
    /// <remarks>コンテキストの編集メソッドを使ってください。Unity の遅延編集の予約や、Editor のイベントループへの再入は行わないでください。</remarks>
    public interface IEventHandler<TEvent>
    {
        /// <summary>空白でない安定した識別子です。同じ通知型の購読内で一意にしてください。</summary>
        string Id { get; }
        /// <summary>Editor のメインスレッドで、個別の期限内に通知を処理します。</summary>
        /// <param name="context">通知、編集範囲を限定した編集メソッド、呼び出しの期限です。</param>
        /// <returns>成功・スキップ・失敗・キャンセルのいずれかを明示した結果です。</returns>
        HandlerResult Execute(HandlerContext<TEvent> context);
    }

    /// <summary>ハンドラーの明示的な終了状態です。Unspecified は実行契約違反として扱います。</summary>
    public enum HandlerStatus
    {
        /// <summary>終了状態が明示されていません。</summary>
        Unspecified,
        /// <summary>ハンドラーが正常終了しました。</summary>
        Succeeded,
        /// <summary>記録対象の編集を行わずに、ハンドラーが通知の処理をスキップしました。</summary>
        Skipped,
        /// <summary>ハンドラーが失敗しました。記録した変更は復元の対象です。</summary>
        Failed,
        /// <summary>ハンドラーがキャンセルしました。記録した変更は復元の対象です。</summary>
        Cancelled
    }
    /// <summary>ハンドラーが返す終了状態と、省略可能な説明です。</summary>
    public readonly struct HandlerResult
    {
        /// <summary>明示的な終了状態です。既定値は Unspecified です。</summary>
        public HandlerStatus Status { get; }
        /// <summary>省略可能な説明です。生成メソッドは null を空文字列に変換します。</summary>
        public string Message { get; }
        private HandlerResult(HandlerStatus status, string message)
        { Status = status; Message = message ?? string.Empty; }
        /// <summary>正常終了を報告します。実行後の確認をすべて通過した場合に、記録した変更を確定します。</summary>
        /// <returns>成功を表す結果です。</returns>
        public static HandlerResult Success() => new HandlerResult(HandlerStatus.Succeeded, null);
        /// <summary>処理を行わなかったことを報告します。記録対象の編集後にスキップすると失敗として扱います。</summary>
        /// <param name="reason">省略可能な説明です。</param>
        /// <returns>スキップを表す結果です。</returns>
        public static HandlerResult Skip(string reason = null) => new HandlerResult(HandlerStatus.Skipped, reason);
        /// <summary>失敗を報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。</summary>
        /// <param name="reason">失敗理由です。null は空文字列に変換します。</param>
        /// <returns>失敗を表す結果です。</returns>
        public static HandlerResult Failure(string reason) => new HandlerResult(HandlerStatus.Failed, reason);
        /// <summary>キャンセルを報告します。ディスパッチャーは記録した変更の復元を試み、購読を無効化します。</summary>
        /// <param name="reason">省略可能な説明です。</param>
        /// <returns>キャンセルを表す結果です。</returns>
        public static HandlerResult Cancel(string reason = null) => new HandlerResult(HandlerStatus.Cancelled, reason);
    }

    /// <summary>ハンドラーの協調的な期限を超過したことを示します。同期的な Unity コードを強制中断するものではありません。</summary>
    public sealed class HandlerDeadlineExceededException : TimeoutException
    {
        internal HandlerDeadlineExceededException(string id, TimeSpan limit)
            : base("Handler '" + id + "' exceeded its " + limit.TotalMilliseconds + " ms deadline.") { }
    }
    /// <summary>条件の協調的な期限を超過したことを示します。同期的な Unity コードを強制中断するものではありません。</summary>
    public sealed class ConditionDeadlineExceededException : TimeoutException
    {
        internal ConditionDeadlineExceededException(string id, TimeSpan limit)
            : base("Condition '" + id + "' exceeded its " + limit.TotalMilliseconds + " ms deadline.") { }
    }

    /// <summary>同じ入力変更に対する条件結果を、1件の購読でどのように組み合わせるかを指定します。</summary>
    public enum SubscriptionMode
    {
        /// <summary>完全一致で扱う1つの通知型です。</summary>
        Single,
        /// <summary>指定した全条件が一致し、同じ編集範囲のルートを返す必要があります。</summary>
        All,
        /// <summary>指定順で最初に一致した条件の通知と編集範囲のルートを使います。</summary>
        Any
    }

    /// <summary>読み取り専用の購読状態と、明示的な解除トークンです。解除するまで登録への強参照が維持されます。</summary>
    public sealed class EventSubscription : IDisposable
    {
        internal readonly IHandlerEntry Entry;
        /// <summary>登録時に保存した識別子です。</summary>
        public string Id { get; }
        /// <summary>ハンドラーへ渡す通知の型です。All または Any 購読では CompositeEvent です。</summary>
        public Type EventType { get; }
        /// <summary>登録時に保存した条件の組み合わせ方です。</summary>
        public SubscriptionMode Mode { get; }
        /// <summary>指定順に並んだ、変更できない条件の通知型一覧です。</summary>
        public IReadOnlyList<Type> ConditionTypes { get; }
        internal TimeSpan ExecutionLimit;
        /// <summary>ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。</summary>
        public TimeSpan TimeLimit => ExecutionLimit;
        /// <summary>購読が明示的に解除されているかどうかです。</summary>
        public bool IsDisposed { get; internal set; }
        /// <summary>ユーザー設定と異常時の方針に基づき、現在この購読が有効かどうかです。</summary>
        public bool IsEnabled { get; internal set; }
        /// <summary>有効かつ未解除で、必要な全条件が有効かどうかです。Editor のメインスレッドで取得してください。</summary>
        public bool IsActive => !IsDisposed && IsEnabled && EventDispatcher.HasConditions(ConditionTypes);
        /// <summary>無効化の理由です。有効な購読の初期値は空文字列です。</summary>
        public string DisabledReason { get; internal set; }
        /// <summary>直近の終了結果です。初回呼び出し前は Unspecified です。</summary>
        public HandlerResult LastResult { get; internal set; }
        /// <summary>直近の呼び出し時間です。変更の確定・復元にかかった時間は含みません。初回使用前はゼロです。</summary>
        public TimeSpan LastDuration { get; internal set; }
        internal EventSubscription(IHandlerEntry entry, Type eventType, string id, TimeSpan limit,
            SubscriptionMode mode, Type[] conditionTypes)
        {
            Entry = entry; EventType = eventType; Id = id; ExecutionLimit = limit;
            Mode = mode; ConditionTypes = Array.AsReadOnly(conditionTypes);
        }
        /// <summary>この購読を解除して登録枠を解放します。繰り返し呼び出しても何もしません。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public void Dispose() => EventDispatcher.Unsubscribe(this);
    }

    /// <summary>読み取り専用の条件登録状態と、明示的な解除トークンです。条件を解除すると、その購読は待機状態になります。</summary>
    public sealed class ConditionRegistration : IDisposable
    {
        internal readonly IConditionEntry Entry;
        /// <summary>登録時に保存した識別子です。</summary>
        public string Id { get; }
        /// <summary>この条件が生成する通知の型です。</summary>
        public Type EventType { get; }
        /// <summary>登録時に保存した変更の種類です。</summary>
        public EditorChangeKind Changes { get; }
        internal TimeSpan ExecutionLimit;
        /// <summary>ユーザーが設定した現在の期限です。各呼び出しは開始時の値を保持します。</summary>
        public TimeSpan TimeLimit => ExecutionLimit;
        /// <summary>条件登録が明示的に解除されているかどうかです。</summary>
        public bool IsDisposed { get; internal set; }
        /// <summary>ユーザー設定と異常時の方針に基づき、現在この条件が有効かどうかです。</summary>
        public bool IsEnabled { get; internal set; }
        /// <summary>無効化の理由です。有効な条件の初期値は空文字列です。</summary>
        public string DisabledReason { get; internal set; }
        /// <summary>直近の評価時間です。初回評価前はゼロです。</summary>
        public TimeSpan LastDuration { get; internal set; }
        internal ConditionRegistration(IConditionEntry entry, Type eventType, string id, EditorChangeKind changes, TimeSpan limit)
        { Entry = entry; EventType = eventType; Id = id; Changes = changes; ExecutionLimit = limit; }
        /// <summary>この条件を解除して登録枠を解放します。購読者は解除しません。繰り返し呼び出しても何もしません。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合です。</exception>
        public void Dispose() => EventDispatcher.Unregister(this);
    }

    // The entire public dispatcher facade: registration only; no public publish or dispatch method.
    /// <summary>メインスレッドで使う登録 API の入口です。任意の通知の投入や実行ポリシーの変更を行う公開 API はありません。</summary>
    public static class EditorEvents
    {
        /// <summary>条件登録と購読の合計上限内で、指定した通知型に条件を1件登録します。</summary>
        /// <typeparam name="TEvent">通知の型です。型は完全一致で扱います。</typeparam>
        /// <param name="condition">読み取り専用の条件です。登録時に使うゲッターは軽量な処理にしてください。</param>
        /// <returns>登録を解除するトークンと、読み取り専用の条件登録状態です。</returns>
        /// <exception cref="ArgumentNullException">condition が null の場合です。</exception>
        /// <exception cref="ArgumentException">識別子や変更フラグが無効、またはこの通知型にすでに条件が登録されている場合です。</exception>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合、または登録数が上限に達した場合です。</exception>
        /// <remarks>ゲッターの例外は呼び出し元へ伝播します。トークンを手放しても条件登録は解除されません。</remarks>
        public static ConditionRegistration RegisterCondition<TEvent>(IEventCondition<TEvent> condition)
            => EventDispatcher.Register(condition);
        /// <summary>同期的なハンドラーを購読登録します。同じ通知型の有効な条件がない間は待機します。</summary>
        /// <typeparam name="TEvent">通知の型です。型は完全一致で扱います。</typeparam>
        /// <param name="handler">識別子のゲッターが軽量なハンドラーです。</param>
        /// <returns>購読を解除するトークンと、読み取り専用の購読状態です。</returns>
        /// <exception cref="ArgumentNullException">ハンドラーが null の場合です。</exception>
        /// <exception cref="ArgumentException">識別子が空白、または同じ通知型ですでに購読登録されている場合です。</exception>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合、または登録数が上限に達した場合です。</exception>
        /// <remarks>識別子のゲッターの例外は呼び出し元へ伝播します。トークンを手放しても購読は解除されません。ハンドラー間の実行順は規定しません。</remarks>
        public static EventSubscription Subscribe<TEvent>(IEventHandler<TEvent> handler)
            => EventDispatcher.Subscribe(handler);

        /// <summary>指定した全条件が同じ入力変更に一致し、同じ編集範囲のルートを返す場合に処理する購読を登録します。</summary>
        /// <param name="handler">安定した識別子を持つ、同期的な複合通知ハンドラーです。</param>
        /// <param name="eventTypes">型引数が確定した、重複のない通知型を指定順に1〜100件渡します。登録時にコピーします。</param>
        /// <returns>購読を解除するトークンと、読み取り専用の購読状態です。</returns>
        /// <exception cref="ArgumentNullException">ハンドラーまたは型の配列が null の場合です。</exception>
        /// <exception cref="ArgumentException">型一覧や識別子が無効、または複合購読の識別子がすでに登録されている場合です。</exception>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合、または条件登録と購読の合計が上限に達した場合です。</exception>
        /// <remarks>必要な全条件が登録済みで有効である必要があります。バッチ内の全条件評価が終わってからハンドラーを実行します。ハンドラー間の実行順は規定しません。</remarks>
        public static EventSubscription SubscribeAll(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
            => EventDispatcher.SubscribeComposite(handler, eventTypes, SubscriptionMode.All);

        /// <summary>指定したいずれかの条件が同じ入力変更に一致した場合に処理する購読を登録します。</summary>
        /// <param name="handler">安定した識別子を持つ、同期的な複合通知ハンドラーです。</param>
        /// <param name="eventTypes">型引数が確定した、重複のない通知型を指定順に1〜100件渡します。登録時にコピーします。</param>
        /// <returns>購読を解除するトークンと、読み取り専用の購読状態です。</returns>
        /// <exception cref="ArgumentNullException">ハンドラーまたは型の配列が null の場合です。</exception>
        /// <exception cref="ArgumentException">型一覧や識別子が無効、または複合購読の識別子がすでに登録されている場合です。</exception>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外で呼び出した場合、または条件登録と購読の合計が上限に達した場合です。</exception>
        /// <remarks>必要な全条件が登録済みで有効である必要があります。評価は途中で省略しません。結果の選択は最初の一致で止まり、その結果だけを公開します。ハンドラー間の実行順は規定しません。</remarks>
        public static EventSubscription SubscribeAny(IEventHandler<CompositeEvent> handler, Type[] eventTypes)
            => EventDispatcher.SubscribeComposite(handler, eventTypes, SubscriptionMode.Any);
    }
}
