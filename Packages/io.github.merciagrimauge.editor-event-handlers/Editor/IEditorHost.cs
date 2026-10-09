using System;

namespace EditorEventHandlers.Editor
{
    // 内部基盤の契約です。登録実装は実行環境の動作や方針を差し替えられません。
    /// <summary>ディスパッチャーが必要とする種類の Unity 変更通知だけを購読する内部契約です。</summary>
    internal interface IEditorChangeSource : IDisposable
    {
        /// <summary>必要な入力種類を更新します。None は通知の購読と保留入力を解除します。</summary>
        /// <param name="kinds">購読が必要とする公開入力の種類です。None なら購読を終了します。</param>
        void SetKinds(EditorChangeKind kinds);
    }

    /// <summary>メインスレッド、編集可否、コンテキスト生成、設定保存、ログを扱う内部の実行環境です。</summary>
    internal interface IEditorHost
    {
        /// <summary>現在、シーン編集と通知配送を行える状態かどうかです。</summary>
        bool CanEdit { get; }
        /// <summary>保存設定をプロジェクトごとに区別するための識別情報です。</summary>
        string SettingsScope { get; }
        /// <summary>現在の呼び出しが Editor のメインスレッド上にあることを確認します。</summary>
        void RequireMainThread();
        /// <summary>対象が存続し、編集可能な通常シーンにあるルートかを確認します。</summary>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        bool IsEditableRoot(object root);
        /// <summary>ルートが現在も存続する GameObject かを確認します。</summary>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        /// <remarks>Unity オブジェクトの破棄済み状態も確認します。C# の参照だけを調べる判定で代用しません。</remarks>
        bool RootExists(object root);
        /// <summary>確認済みルートの現在のシーン識別子を取得します。</summary>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <returns>ルートが現在属するシーンの一時的な識別子です。</returns>
        EditorSceneId GetSceneId(object root);
        /// <summary>型付き通知、編集範囲のルート、購読設定から呼び出し用コンテキストを作成します。</summary>
        /// <typeparam name="T">条件と購読が共有する通知の型です。</typeparam>
        /// <param name="notification">同じ入力に一致した購読者へ渡す通知値です。</param>
        /// <param name="root">現在の編集範囲のルートです。Unity オブジェクトの存続は使用時に確認します。</param>
        /// <param name="subscription">一致結果の配送先となる購読です。</param>
        /// <returns>このハンドラー呼び出し専用のコンテキストです。</returns>
        HandlerContext<T> CreateHandlerContext<T>(T notification, object root, EventSubscription subscription);
        /// <summary>警告を記録し、指定された場合は Unity オブジェクトをログの対象に関連付けます。</summary>
        /// <param name="message">記録する警告またはエラーの内容です。</param>
        /// <param name="root">ログに関連付ける Unity オブジェクトです。関連付ける対象がなければ null です。</param>
        void LogWarning(string message, object root = null);
        /// <summary>エラーを記録し、指定された場合は Unity オブジェクトをログの対象に関連付けます。</summary>
        /// <param name="message">記録する警告またはエラーの内容です。</param>
        /// <param name="root">ログに関連付ける Unity オブジェクトです。関連付ける対象がなければ null です。</param>
        void LogError(string message, object root = null);
        /// <summary>例外を記録し、指定された場合は Unity オブジェクトをログの対象に関連付けます。</summary>
        /// <param name="error">記録する例外です。</param>
        /// <param name="root">ログに関連付ける Unity オブジェクトです。関連付ける対象がなければ null です。</param>
        void LogException(Exception error, object root = null);
        /// <summary>永続設定から整数を取得します。値がない場合は指定された既定値を返します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="defaultValue">設定がない場合に返す既定値です。</param>
        /// <returns>保存された整数、または指定された既定値です。</returns>
        int ReadPreferenceInt(string key, int defaultValue);
        /// <summary>永続設定から真偽値を取得します。値がない場合は指定された既定値を返します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="defaultValue">設定がない場合に返す既定値です。</param>
        /// <returns>保存された真偽値、または指定された既定値です。</returns>
        bool ReadPreferenceBool(string key, bool defaultValue);
        /// <summary>永続設定から文字列を取得します。値がない場合は null も含めて指定された既定値を返します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="defaultValue">設定がない場合に返す既定値です。</param>
        /// <returns>保存された文字列、または null も含む指定された既定値です。</returns>
        string ReadPreferenceString(string key, string defaultValue);
        /// <summary>整数の永続設定を保存します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="value">永続設定に保存する値です。</param>
        void WritePreferenceInt(string key, int value);
        /// <summary>真偽値の永続設定を保存します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="value">永続設定に保存する値です。</param>
        void WritePreferenceBool(string key, bool value);
        /// <summary>文字列の永続設定を保存します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <param name="value">永続設定に保存する値です。</param>
        void WritePreferenceString(string key, string value);
        /// <summary>文字列の永続設定を削除します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        void ErasePreferenceString(string key);
        /// <summary>旧 Editor セッション内の文字列設定を取得します。値がない場合は空文字列を返します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        /// <returns>セッション内の保存値です。値がなければ空文字列です。</returns>
        string ReadSessionString(string key);
        /// <summary>旧 Editor セッション内の文字列設定を削除します。</summary>
        /// <param name="key">読み取り・保存・削除の対象となる設定キーです。</param>
        void EraseSessionString(string key);
    }
}
