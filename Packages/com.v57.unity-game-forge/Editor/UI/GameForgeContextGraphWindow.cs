using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>Opens the Context Intelligence HTML graph in the default browser.</summary>
    public static class GameForgeContextGraphWindow
    {
        public const string GraphHtmlRel =
            "Docs/V57/reports/context-graph-viewer/index.html";

        public static string GraphHtmlAbsolutePath =>
            Path.GetFullPath(Path.Combine(
                GameForgeBootstrap.ProjectRoot,
                GraphHtmlRel.Replace('/', Path.DirectorySeparatorChar)));

        public static bool GraphExists() => File.Exists(GraphHtmlAbsolutePath);

        [MenuItem("V57/GameForge/Open Graph", false, 50)]
        public static void OpenDemo()
        {
            var html = GraphHtmlAbsolutePath;
            if (!File.Exists(html))
            {
                EditorUtility.DisplayDialog(
                    "Context Graph",
                    "No graph HTML yet.\n\nRun Workbench → CONTEXT → Run Post-Mortem first.",
                    "OK");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = html,
                    UseShellExecute = true,
                });
            }
            catch
            {
                Application.OpenURL("file:///" + html.Replace("\\", "/"));
            }
        }

        [MenuItem("V57/GameForge/Open Graph", true)]
        static bool OpenDemoValidate() => GraphExists();
    }
}
