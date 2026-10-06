#if UNITY_6000_5_OR_NEWER
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Unity 6.5 treats InstanceID APIs as compile errors (CS0619). Official AI packages
    /// (assistant / generators / toolkit) are not fully migrated yet, which blocks the whole
    /// project including GameForge. Inject a per-assembly csc.rsp that suppresses CS0619
    /// until Unity ships EntityId-compatible package versions.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameForgeUnity65PackageCompat
    {
        private const string MarkerFlag = "-nowarn:0619";
        private const string RspBody = "-nowarn:0619\n";

        static GameForgeUnity65PackageCompat()
        {
            EditorApplication.delayCall += EnsureShims;
        }

        private static void EnsureShims()
        {
            try
            {
                // PackageCache is immutable — writing csc.rsp there spams
                // "has no meta file, but it's in an immutable folder" errors.
                // Only patch embedded copies under Packages/ when present.
                var wrote = false;
                var packages = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages"));
                if (Directory.Exists(packages))
                {
                    foreach (var dir in Directory.GetDirectories(packages))
                    {
                        var name = Path.GetFileName(dir);
                        if (name is "com.unity.ai.assistant" or "com.unity.ai.generators" or "com.unity.ai.toolkit")
                            wrote |= InjectRspBesideAsmdefs(dir);
                    }
                }

                if (wrote)
                {
                    Debug.Log(
                        "[GameForge] Applied Unity 6.5 CS0619 shims to AI packages. Recompiling…");
                    CompilationPipeline.RequestScriptCompilation();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameForge] Unity 6.5 package shim failed: " + ex.Message);
            }
        }

        private static bool InjectRspBesideAsmdefs(string packageRoot)
        {
            var wrote = false;
            foreach (var asmdef in Directory.GetFiles(packageRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                // Skip samples / tests noise where possible
                var rel = asmdef.Replace('\\', '/');
                if (rel.Contains("/Samples~/", StringComparison.OrdinalIgnoreCase) ||
                    rel.Contains("/Tests/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rsp = Path.Combine(Path.GetDirectoryName(asmdef)!, "csc.rsp");
                if (File.Exists(rsp))
                {
                    var existing = File.ReadAllText(rsp);
                    // Old shim used a '# …' comment; Bee/csc treated those words as source paths (CS2001).
                    if (existing.Contains('#') || existing.IndexOf("GameForge", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        File.WriteAllText(rsp, RspBody);
                        wrote = true;
                        continue;
                    }

                    if (existing.Contains(MarkerFlag, StringComparison.Ordinal))
                        continue;
                    File.WriteAllText(rsp, existing.TrimEnd() + "\n" + RspBody);
                }
                else
                {
                    File.WriteAllText(rsp, RspBody);
                }

                wrote = true;
            }

            return wrote;
        }
    }
}
#endif
