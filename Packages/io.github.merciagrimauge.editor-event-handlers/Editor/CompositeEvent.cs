using System;
using System.Collections.Generic;

namespace EditorEventHandlers.Editor
{
    /// <summary>入れ子のない1件の All または Any 購読で選ばれた、保存済みの条件通知です。</summary>
    /// <remarks>コレクションは変更できません。含まれる通知オブジェクトは元の参照を保持します。Any は最初に一致した結果だけを公開します。</remarks>
    public sealed class CompositeEvent
    {
        /// <summary>完全一致する通知型をキーとした、複合購読の一致結果です。</summary>
        private readonly Dictionary<Type, object> _results;
        /// <summary>この組み合わせで評価した全条件が共有する入力変更です。</summary>
        public EditorChange Change { get; }
        /// <summary>購読時の指定順に並んだ、変更できない必須通知型の一覧です。</summary>
        public IReadOnlyList<Type> RequiredTypes { get; }

        /// <summary>入力変更、必要な型一覧、選択済みの通知結果を保持します。</summary>
        /// <param name="change">通知時点の種類と識別子を保持する入力変更です。</param>
        /// <param name="requiredTypes">購読時の指定順に並んだ必須通知型の一覧です。</param>
        /// <param name="results">同じ入力変更に対する、評価済みまたは選択済みの条件結果です。</param>
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
