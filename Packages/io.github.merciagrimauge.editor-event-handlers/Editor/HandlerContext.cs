using System;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorEventHandlers.Editor
{
    /// <summary>同期的なハンドラーの1回の呼び出しで使う、型付き通知と編集範囲を限定したコンテキストです。</summary>
    /// <typeparam name="TEvent">条件が選んだ通知の型です。</typeparam>
    public sealed class HandlerContext<TEvent> : HandlerContext
    {
        /// <summary>購読者間で共有する条件の判定結果です。Unity 参照は状態の確認に使います。</summary>
        public TEvent Event { get; }
        internal HandlerContext(TEvent notification, GameObject root, EventSubscription subscription)
            : base(root, subscription) { Event = notification; }
    }

    // Synchronous scoped context. Unity references are for inspection; edits use the transaction methods.
    /// <summary>メインスレッドで同期的に行うハンドラーの1回の呼び出しに対して、編集範囲を限定したメソッドと協調的な期限を提供します。</summary>
    /// <remarks>実行後の再利用、Unity オブジェクトへの直接書き込み、遅延編集を行わないでください。Undo 記録は階層全体の複製ではありません。Unity や編集処理の例外は呼び出し元へ伝播する場合があります。</remarks>
    public abstract class HandlerContext
    {
        private readonly EventSubscription _registration;
        private readonly long _start;
        private UnityEditTransaction _transaction;
        private bool _open = true;
        /// <summary>条件が選んだ編集可能なシーン階層のルートです。状態の確認に使い、編集にはコンテキストのメソッドを使ってください。</summary>
        public GameObject Root { get; }
        /// <summary>呼び出し開始時に保持した、個別の実行期限です。</summary>
        public TimeSpan TimeLimit { get; }
        /// <summary>コンテキスト作成からの経過時間です。取得しても期限は延長されません。</summary>
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

        /// <summary>Editor のスレッド、呼び出しの有効期間、協調的な期限を確認します。</summary>
        /// <exception cref="InvalidOperationException">Editor のメインスレッド以外、または呼び出しの終了後に実行した場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">経過時間が実行期限に達した場合です。</exception>
        public void CheckDeadline()
        {
            EventDispatcher.RequireMainThread();
            if (!_open) throw new InvalidOperationException("This handler context has already ended.");
            if (Elapsed >= TimeLimit) throw new HandlerDeadlineExceededException(_registration.Id, TimeLimit);
        }

        /// <summary>編集可能な階層内の GameObject に Component を追加し、Undo に記録します。</summary>
        /// <param name="target">Root、または同じシーンに属する子孫です。</param>
        /// <param name="componentType">Component の派生型です。</param>
        /// <returns>追加した Component です。</returns>
        /// <exception cref="ArgumentException">対象が編集範囲外、または指定型が Component の派生型ではない場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、Root の喪失、または Unity が Component を追加できなかった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public Component AddComponent(GameObject target, Type componentType) => Transaction.AddComponent(target, componentType);
        /// <summary>編集可能な階層に指定型の Component を追加し、Undo に記録します。</summary>
        /// <typeparam name="T">追加する Component の型です。</typeparam>
        /// <param name="target">Root、または同じシーンに属する子孫です。</param>
        /// <returns>追加した指定型の Component です。</returns>
        /// <exception cref="ArgumentException">対象が編集範囲外の場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、Root の喪失、または Unity が Component を追加できなかった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public T AddComponent<T>(GameObject target) where T : Component => (T)AddComponent(target, typeof(T));
        /// <summary>編集範囲内の GameObject または Component を記録し、シリアライズ可能なプロパティを同期的に編集します。</summary>
        /// <param name="target">プロパティを記録して編集するオブジェクトです。</param>
        /// <param name="edit">この対象のプロパティだけを編集する処理です。</param>
        /// <remarks>作成・破棄・親子関係の変更には専用のコンテキストメソッドを使ってください。別のオブジェクトを編集する場合は、個別に Modify を呼び出してください。</remarks>
        /// <exception cref="ArgumentException">対象が編集範囲内のシーンにある GameObject または Component ではない場合です。</exception>
        /// <exception cref="ArgumentNullException">編集処理が null の場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public void Modify(Object target, Action edit) => Transaction.Modify(target, edit);
        /// <summary>編集可能なシーン階層に子の GameObject を作成し、Undo に記録します。</summary>
        /// <param name="name">新しい GameObject に付ける名前です。</param>
        /// <param name="parent">Root またはその子孫です。null を指定すると Root を使います。</param>
        /// <returns>作成した子オブジェクトです。</returns>
        /// <exception cref="ArgumentException">親が編集範囲外の場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public GameObject CreateChild(string name, GameObject parent = null) => Transaction.CreateChild(name, parent);
        /// <summary>Root のシーンに Prefab アセットを子としてインスタンス化し、Undo に記録します。</summary>
        /// <param name="prefab">インスタンス化する Prefab アセットです。</param>
        /// <param name="parent">Root またはその子孫です。null を指定すると Root を使います。</param>
        /// <returns>作成した Prefab インスタンスです。</returns>
        /// <exception cref="ArgumentException">入力が Prefab アセットではない場合、または親が編集範囲外の場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public GameObject InstantiatePrefab(GameObject prefab, GameObject parent = null) => Transaction.InstantiatePrefab(prefab, parent);
        /// <summary>編集可能な階層内で子孫の親を変更し、Undo に記録します。</summary>
        /// <param name="child">移動する子孫です。Root 自体は指定できません。</param>
        /// <param name="parent">Root または別の子孫です。親子関係が循環する指定はできません。</param>
        /// <exception cref="ArgumentException">対象が編集範囲外、子が Root 自体、または親子関係が循環する場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public void SetParent(GameObject child, GameObject parent) => Transaction.SetParent(child, parent);
        /// <summary>編集可能な階層内の子の GameObject または許可された Component を破棄し、Undo に記録します。</summary>
        /// <param name="target">子孫の GameObject または Component です。Root と Transform コンポーネントは破棄できません。</param>
        /// <exception cref="ArgumentException">対象が編集範囲外、Root 自体、または Transform の場合です。</exception>
        /// <exception cref="InvalidOperationException">別スレッドからの呼び出し、呼び出し終了後、または Root の喪失があった場合です。</exception>
        /// <exception cref="HandlerDeadlineExceededException">編集の前後で呼び出しの期限に達した場合です。</exception>
        public void Destroy(Object target) => Transaction.Destroy(target);
        internal void Finish(bool commit)
        { _open = false; _transaction?.Finish(commit); }
    }
}
