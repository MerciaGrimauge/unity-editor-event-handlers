using System;
using System.IO;
using System.Security;
using UnityEditor;
using UnityEditor.Compilation;
using Assembly = UnityEditor.Compilation.Assembly;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace EditorEventHandlers.Editor
{
    /// <summary>登録された条件の実装型から元の C# ソースを探し、表示上限内で読み取ります。</summary>
    internal static class ConditionSourceReader
    {
        /// <summary>ソース表示のサイズ上限を MiB 単位で表した値です。</summary>
        private const int MaximumMebibytes = 1;
        /// <summary>ソース読み込みに適用するバイト数の上限です。</summary>
        private const long MaximumBytes = MaximumMebibytes * 1024L * 1024L;
        /// <summary>表示上限を超えたソースの代わりに返すメッセージです。</summary>
        private static readonly string TooLargeMessage = "Source file exceeds the " + MaximumMebibytes + " MiB display limit.";
        /// <summary>条件型に対応する元のソースと表示用の場所を取得します。取得不能時は理由を返します。</summary>
        /// <param name="implementation">元ソースを探す条件の実装型です。</param>
        /// <param name="location">見つかったソースの表示用アセットパスです。取得できなければ空文字列です。</param>
        /// <returns>元の C# ソース本文、または取得できない理由を示すメッセージです。</returns>
        internal static string Read(Type implementation, out string location)
        {
            location = string.Empty;
            if (implementation == null) return "No condition is registered for this event type.";
            try
            {
                var assembly = FindEditorAssembly(implementation);
                if (assembly == null) return "Original C# source is unavailable (for example, a DLL-only condition).";
                if (!TryFindSourceFile(assembly, implementation, out var fullPath, out var assetPath))
                    return "The registered type could not be mapped to a MonoScript. Its original source is unavailable here.";
                location = assetPath;
                return TryReadBounded(fullPath, out var source) ? source : TooLargeMessage;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException
                || error is NotSupportedException || error is SecurityException)
            { return "Unable to read the original source: " + error.GetType().Name; }
        }
        /// <summary>実装型を含むアセンブリ名と一致する Editor コンパイルアセンブリを探します。</summary>
        /// <param name="implementation">元ソースを探す条件の実装型です。</param>
        /// <returns>一致するアセンブリです。見つからなければ null です。</returns>
        private static Assembly FindEditorAssembly(Type implementation)
        {
            var name = implementation.Assembly.GetName().Name;
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                if (assembly.name == name) return assembly;
            return null;
        }
        /// <summary>アセンブリの C# ソースから、実装型を宣言する MonoScript を探します。</summary>
        /// <param name="assembly">対象の実装型を含む Editor コンパイルアセンブリです。</param>
        /// <param name="implementation">元ソースを探す条件の実装型です。</param>
        /// <param name="fullPath">見つかったソースの絶対パスです。見つからなければ null です。</param>
        /// <param name="assetPath">見つかったソースの Assets または Packages からのパスです。見つからなければ null です。</param>
        /// <returns>対応するソースが見つかった場合は true、それ以外は false です。</returns>
        private static bool TryFindSourceFile(Assembly assembly, Type implementation, out string fullPath, out string assetPath)
        {
            foreach (var file in assembly.sourceFiles)
            {
                if (!string.Equals(Path.GetExtension(file), ".cs", StringComparison.OrdinalIgnoreCase)) continue;
                var full = Path.GetFullPath(file);
                var path = AssetPath(full);
                if (path == null || !Declares(AssetDatabase.LoadAssetAtPath<MonoScript>(path), implementation)) continue;
                fullPath = full; assetPath = path;
                return true;
            }
            fullPath = null; assetPath = null;
            return false;
        }
        // 入れ子の型は、その外側の型に対応する MonoScript から探します。
        /// <summary>MonoScript の型が実装型自身、またはその外側の型に一致するかを調べます。</summary>
        /// <param name="script">宣言する型を確認する MonoScript です。</param>
        /// <param name="implementation">元ソースを探す条件の実装型です。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        private static bool Declares(MonoScript script, Type implementation)
        {
            var scriptType = script == null ? null : script.GetClass();
            for (var type = implementation; type != null; type = type.DeclaringType)
                if (type == scriptType) return true;
            return false;
        }
        // 読み取り中に増えた場合も含め、MaximumBytes を超えるファイルは拒否します。
        /// <summary>ファイルを表示上限まで読み取ります。読み取り中に上限を超えて増えた場合も拒否します。</summary>
        /// <param name="fullPath">上限付きで読み取るソースファイルの絶対パスです。</param>
        /// <param name="source">読み取ったソース本文です。上限を超える場合は null です。</param>
        /// <returns>上限内で読み取れた場合は true、上限を超えた場合は false です。</returns>
        private static bool TryReadBounded(string fullPath, out string source)
        {
            source = null;
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length > MaximumBytes) return false;
                var buffer = new byte[MaximumBytes + 1];
                var count = 0;
                while (count < buffer.Length)
                {
                    var read = stream.Read(buffer, count, buffer.Length - count);
                    if (read == 0) break;
                    count += read;
                }
                if (count > MaximumBytes) return false;
                using (var reader = new StreamReader(new MemoryStream(buffer, 0, count), true))
                    source = reader.ReadToEnd();
                return true;
            }
        }
        /// <summary>絶対パスを Assets または登録済みパッケージのアセットパスに変換します。</summary>
        /// <param name="full">変換するファイルの絶対パスです。</param>
        /// <returns>プロジェクトまたはパッケージからのアセットパスです。対応範囲外なら null です。</returns>
        private static string AssetPath(string full)
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (Within(full, Application.dataPath)) return Relative(full, project);
            foreach (var package in PackageInfo.GetAllRegisteredPackages())
                if (!string.IsNullOrEmpty(package.resolvedPath) && Within(full, package.resolvedPath))
                    return "Packages/" + package.name + "/" + Relative(full, package.resolvedPath);
            return null;
        }
        /// <summary>区切り文字まで含めた比較で、ファイルが指定ディレクトリの内部にあるかを確認します。</summary>
        /// <param name="file">確認または変換するファイルのパスです。</param>
        /// <param name="directory">包含関係や相対パスの基準となるディレクトリです。</param>
        /// <returns>指定された条件を満たす場合は true、それ以外は false です。</returns>
        private static bool Within(string file, string directory) => file.StartsWith(
            Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        /// <summary>指定した基準ディレクトリからのファイルの相対パスを、スラッシュ区切りで返します。</summary>
        /// <param name="file">確認または変換するファイルのパスです。</param>
        /// <param name="directory">包含関係や相対パスの基準となるディレクトリです。</param>
        /// <returns>基準ディレクトリからの相対パスです。</returns>
        private static string Relative(string file, string directory) => file.Substring(
            Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
    }
}
