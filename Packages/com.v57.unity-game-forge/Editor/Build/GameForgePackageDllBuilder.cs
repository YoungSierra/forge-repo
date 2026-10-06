using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Builds a consumer-ready UPM folder under <c>Dist/com.v57.unity-game-forge</c>
    /// with precompiled Editor/Runtime DLLs + UI assets (no C# sources).
    /// Dev iteration keeps <c>.cs</c> under <c>Packages/com.v57.unity-game-forge</c>.
    /// </summary>
    public static class GameForgePackageDllBuilder
    {
        public const string PackageName = "com.v57.unity-game-forge";
        /// <summary>Stable GUIDs so consumers keep references across rebuilds.</summary>
        public const string RuntimeDllGuid = "a7f31c2e4b5d6a7890abcdef12345601";
        public const string EditorDllGuid = "b8e42d3f5c6e7b8901bcdef234567802";

        [MenuItem("V57/GameForge/Build/Export UPM package (DLLs → Dist)", false, 100)]
        public static void ExportForReleaseMenu()
        {
            try
            {
                var dist = ExportForRelease();
                EditorUtility.DisplayDialog(
                    "GameForge UPM export",
                    "Release package written to:\n" + dist + "\n\nConsumers should install that folder (DLLs only, no .cs).",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError("[GameForge] DLL export failed: " + ex.Message);
                EditorUtility.DisplayDialog("GameForge UPM export failed", ex.Message, "OK");
                throw;
            }
        }

        /// <summary>Batchmode entry (must be void): -executeMethod …ExportForReleaseBatch</summary>
        public static void ExportForReleaseBatch()
        {
            try
            {
                ExportForRelease();
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception)
            {
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        /// <summary>Returns Dist path. Prefer <see cref="ExportForReleaseBatch"/> from CLI.</summary>
        public static string ExportForRelease()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var distRoot = Path.Combine(projectRoot, "Dist", PackageName);
            var assemblies = Path.Combine(projectRoot, "Library", "ScriptAssemblies");
            var runtimeSrc = Path.Combine(assemblies, "V57.GameForge.Runtime.dll");
            var editorSrc = Path.Combine(assemblies, "V57.GameForge.Editor.dll");
            var packageSrc = Path.Combine(projectRoot, "Packages", PackageName);

            if (!File.Exists(runtimeSrc) || !File.Exists(editorSrc))
                throw new FileNotFoundException(
                    "Compiled assemblies missing. Open the project in Unity and wait for compile, then retry.\n" +
                    runtimeSrc + "\n" + editorSrc);

            if (!Directory.Exists(packageSrc))
                throw new DirectoryNotFoundException("Package folder missing: " + packageSrc);

            if (Directory.Exists(distRoot))
                Directory.Delete(distRoot, true);
            Directory.CreateDirectory(distRoot);

            File.Copy(
                Path.Combine(packageSrc, "package.json"),
                Path.Combine(distRoot, "package.json"),
                true);

            CopyTreeFiltered(
                Path.Combine(packageSrc, "UI"),
                Path.Combine(distRoot, "UI"),
                path =>
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    return ext is ".uss" or ".uxml" or ".meta" or ".json";
                });

            foreach (var name in new[] { "LICENSE.md", "LICENSE", "CHANGELOG.md" })
            {
                var p = Path.Combine(packageSrc, name);
                if (File.Exists(p))
                    File.Copy(p, Path.Combine(distRoot, name), true);
            }

            var runtimePlugins = Path.Combine(distRoot, "Runtime", "Plugins");
            var editorPlugins = Path.Combine(distRoot, "Editor", "Plugins");
            Directory.CreateDirectory(runtimePlugins);
            Directory.CreateDirectory(editorPlugins);

            var runtimeDst = Path.Combine(runtimePlugins, "V57.GameForge.Runtime.dll");
            var editorDst = Path.Combine(editorPlugins, "V57.GameForge.Editor.dll");
            File.Copy(runtimeSrc, runtimeDst, true);
            File.Copy(editorSrc, editorDst, true);

            WriteDllMeta(runtimeDst + ".meta", RuntimeDllGuid, editorOnly: false);
            WriteDllMeta(editorDst + ".meta", EditorDllGuid, editorOnly: true);

            File.WriteAllText(Path.Combine(distRoot, "README.md"), DistReadme(), Encoding.UTF8);
            File.WriteAllText(
                Path.Combine(distRoot, ".gameforge-dll-package"),
                "V57.GameForge precompiled UPM package\n" + DateTime.UtcNow.ToString("o") + "\n",
                Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log("[GameForge] UPM DLL package exported → " + distRoot);
            return distRoot;
        }

        private static string DistReadme() =>
            "# V57 Unity GameForge (release package)\n\n" +
            "This folder is the **consumer UPM** layout: precompiled `Editor`/`Runtime` DLLs + UI Toolkit assets.\n\n" +
            "- No C# sources (`.cs`) are included.\n" +
            "- Install via Package Manager → Add package from disk, or git subtree that points at this Dist output.\n" +
            "- Development sources live in the monorepo under `Packages/com.v57.unity-game-forge/`.\n" +
            "- Rebuild: `Tools/build-package-dlls.ps1` or menu **V57 → GameForge → Build → Export UPM package**.\n";

        private static void CopyTreeFiltered(string src, string dst, Func<string, bool> includeFile)
        {
            if (!Directory.Exists(src)) return;
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                if (!includeFile(file)) continue;
                var rel = file.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var outPath = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                File.Copy(file, outPath, true);
            }
        }

        private static void WriteDllMeta(string metaPath, string guid, bool editorOnly)
        {
            var sb = new StringBuilder();
            sb.AppendLine("fileFormatVersion: 2");
            sb.AppendLine("guid: " + guid);
            sb.AppendLine("PluginImporter:");
            sb.AppendLine("  externalObjects: {}");
            sb.AppendLine("  serializedVersion: 2");
            sb.AppendLine("  iconMap: {}");
            sb.AppendLine("  executionOrder: {}");
            sb.AppendLine("  defineConstraints: []");
            sb.AppendLine("  isPreloaded: 0");
            sb.AppendLine("  isOverridable: 0");
            sb.AppendLine("  isExplicitlyReferenced: 0");
            sb.AppendLine("  validateReferences: 1");
            sb.AppendLine("  platformData:");
            if (editorOnly)
            {
                sb.AppendLine("  - first:");
                sb.AppendLine("      Any: ");
                sb.AppendLine("    second:");
                sb.AppendLine("      enabled: 0");
                sb.AppendLine("      settings: {}");
                sb.AppendLine("  - first:");
                sb.AppendLine("      Editor: Editor");
                sb.AppendLine("    second:");
                sb.AppendLine("      enabled: 1");
                sb.AppendLine("      settings:");
                sb.AppendLine("        DefaultValueInitialized: true");
            }
            else
            {
                sb.AppendLine("  - first:");
                sb.AppendLine("      Any: ");
                sb.AppendLine("    second:");
                sb.AppendLine("      enabled: 1");
                sb.AppendLine("      settings: {}");
                sb.AppendLine("  - first:");
                sb.AppendLine("      Editor: Editor");
                sb.AppendLine("    second:");
                sb.AppendLine("      enabled: 0");
                sb.AppendLine("      settings: {}");
            }
            sb.AppendLine("  userData: ");
            sb.AppendLine("  assetBundleName: ");
            sb.AppendLine("  assetBundleVariant: ");
            File.WriteAllText(metaPath, sb.ToString(), Encoding.UTF8);
        }
    }
}
