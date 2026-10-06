using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Surfaces Unity MCP connection failures in the Editor (legacy relay approval or CLI setup).
    /// Only real failure signals — never benign status like "Unity CLI MCP ready".
    /// </summary>
    public static class GameForgeMcpApprovalUi
    {
        private static double _lastPromptTime;
        private const double PromptCooldownSec = 8.0;

        public static bool LooksLikeApprovalNeeded(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // Happy-path / informational — never open the setup dialog.
            if (text.IndexOf("MCP ready", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (text.IndexOf("MCP attached", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (text.IndexOf("CLI live", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (text.IndexOf("tool(s)", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("unavailable", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("not configured", System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            // Legacy relay approval / capacity.
            if (text.IndexOf("Connection revoked", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Capacity limit", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("0 direct connection", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Pending Connection", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("mcp-approval", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Allow «GameForge", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Allow \"GameForge", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("needs your approval", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // Real CLI / MCP failures (not "please install" mentions inside a PASS report).
            if (text.IndexOf("CLI MCP not configured", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("connected but no tools", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Unity MCP unavailable", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("Unity MCP relay not configured", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (text.IndexOf("spawn", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("ENOENT", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false; // shell tool failure — not MCP setup

            return false;
        }

        /// <summary>Show modal (rate-limited) with CLI or legacy setup steps.</summary>
        public static void Prompt(string detail = null, bool force = false)
        {
            var now = EditorApplication.timeSinceStartup;
            if (!force && now - _lastPromptTime < PromptCooldownSec) return;
            _lastPromptTime = now;

            EditorApplication.delayCall += () =>
            {
                var useCli = GameForgeSession.IsPipelinePackagePresent();
                string body;
                string title;

                if (useCli)
                {
                    title = "Unity CLI MCP — setup";
                    body =
                        "GameForge uses Unity CLI MCP (`unity mcp`).\n\n" +
                        "1) Install Unity CLI (`unity --version`)\n" +
                        "2) In a terminal at the project root: `unity pipeline install`\n" +
                        "3) `unity mcp configure cursor`\n" +
                        "4) Keep this Editor open on the project\n" +
                        "5) Workbench → Test MCP\n\n" +
                        "Docs: V57/docs/mcp/UNITY_CLI_MIGRATION.md\n\n" +
                        (string.IsNullOrEmpty(detail) ? "" : "Detail: " + Trunc(detail, 240));
                }
                else
                {
                    OpenLegacyMcpSettingsPage();
                    title = "Unity MCP — approval required (legacy)";
                    body =
                        "Legacy in-Editor MCP (deprecated end 2026).\n\n" +
                        "Preferred: install Unity CLI + `unity pipeline install`.\n\n" +
                        "Legacy steps:\n" +
                        "1) Project Settings → AI → Unity MCP → Bridge Running\n" +
                        "2) Allow «GameForge Chat»\n" +
                        "3) Workbench → Test MCP\n\n" +
                        (string.IsNullOrEmpty(detail) ? "" : "Detail: " + Trunc(detail, 240));
                }

                EditorUtility.DisplayDialog(title, body, "OK", "Dismiss");
            };
        }

        public static void OpenMcpSettingsPage()
        {
            if (GameForgeSession.IsPipelinePackagePresent())
            {
                var doc = System.IO.Path.Combine(
                    GameForgeBootstrap.ProjectRoot, "V57", "docs", "mcp", "UNITY_CLI_MIGRATION.md");
                if (System.IO.File.Exists(doc))
                    EditorUtility.RevealInFinder(doc);
                return;
            }
            OpenLegacyMcpSettingsPage();
        }

        private static void OpenLegacyMcpSettingsPage()
        {
            try { SettingsService.OpenProjectSettings("Project/AI/Unity MCP"); }
            catch
            {
                try { SettingsService.OpenProjectSettings("Project/AI/Unity MCP Server"); }
                catch
                {
                    try { SettingsService.OpenProjectSettings("Project/AI"); }
                    catch { EditorApplication.ExecuteMenuItem("Edit/Project Settings..."); }
                }
            }
        }

        private static string Trunc(string s, int n) =>
            string.IsNullOrEmpty(s) || s.Length <= n ? s ?? "" : s.Substring(0, n) + "…";
    }
}
