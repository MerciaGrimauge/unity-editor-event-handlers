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
    internal static class ConditionSourceReader
    {
        private const int MaximumMebibytes = 1;
        private const long MaximumBytes = MaximumMebibytes * 1024L * 1024L;
        private static readonly string TooLargeMessage = "Source file exceeds the " + MaximumMebibytes + " MiB display limit.";
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
        private static Assembly FindEditorAssembly(Type implementation)
        {
            var name = implementation.Assembly.GetName().Name;
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                if (assembly.name == name) return assembly;
            return null;
        }
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
        // A nested type is found through the MonoScript of any type that encloses it.
        private static bool Declares(MonoScript script, Type implementation)
        {
            var scriptType = script == null ? null : script.GetClass();
            for (var type = implementation; type != null; type = type.DeclaringType)
                if (type == scriptType) return true;
            return false;
        }
        // Fails when the file is larger than MaximumBytes, including growth while it is open.
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
        private static string AssetPath(string full)
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (Within(full, Application.dataPath)) return Relative(full, project);
            foreach (var package in PackageInfo.GetAllRegisteredPackages())
                if (!string.IsNullOrEmpty(package.resolvedPath) && Within(full, package.resolvedPath))
                    return "Packages/" + package.name + "/" + Relative(full, package.resolvedPath);
            return null;
        }
        private static bool Within(string file, string directory) => file.StartsWith(
            Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        private static string Relative(string file, string directory) => file.Substring(
            Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
    }
}
