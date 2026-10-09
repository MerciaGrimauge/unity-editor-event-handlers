using System;
using System.Collections.Generic;

namespace EditorEventHandlers.Editor
{
    /// <summary>入れ子のない1件の All または Any 購読で選ばれた、保存済みの条件通知です。</summary>
    /// <remarks>コレクションは変更できません。含まれる通知オブジェクトは元の参照を保持します。Any は最初に一致した結果だけを公開します。</remarks>
    public sealed class CompositeEvent
    {
        private readonly Dictionary<Type, object> _results;
        /// <summary>この組み合わせで評価した全条件が共有する入力変更です。</summary>
        public EditorChange Change { get; }
        /// <summary>購読時の指定順に並んだ、変更できない必須通知型の一覧です。</summary>
        public IReadOnlyList<Type> RequiredTypes { get; }

        internal CompositeEvent(EditorChange change, IReadOnlyList<Type> requiredTypes, Dictionary<Type, object> results)
        { Change = change; RequiredTypes = requiredTypes; _results = results; }

        /// <summary>指定した型と完全一致する、選択済みの通知を取得します。</summary>
        /// <typeparam name="TEvent">条件に指定した通知型です。派生型や代入可能な別型で代用しません。</typeparam>
        /// <param name="notification">保存済みの結果です。結果がなければ、その型の既定値です。</param>
        /// <returns>この組み合わせに、指定した型と完全一致する通知が含まれるかどうかです。</returns>
        public bool TryGet<TEvent>(out TEvent notification)
        {
            if (_results.TryGetValue(typeof(TEvent), out var value))
            { notification = (TEvent)value; return true; }
            notification = default;
            return false;
        }
    }
}
