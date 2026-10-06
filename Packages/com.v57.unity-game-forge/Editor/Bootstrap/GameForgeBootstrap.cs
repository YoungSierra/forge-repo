using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Automatic first-run: materialize agent sidecar and npm install when needed.
    /// </summary>
    [InitializeOnLoad]
    public static class GameForgeBootstrap
    {
        private const string PrefKeyReady = "V57.GameForge.BootstrapReady";
        private static bool _running;

        static GameForgeBootstrap()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (_running) return;
            _running = true;
            try
            {
                EnsureAgentSidecar();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameForge] Bootstrap: {ex.Message}");
            }
            finally
            {
                _running = false;
            }
        }

        public static void BootstrapMenu() => EnsureAgentSidecar(forceNpm: true);

        public static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string AgentRoot =>
            Path.Combine(ProjectRoot, "Tools", "gameforge-agent");

        public static bool IsAgentInstalled =>
            File.Exists(Path.Combine(AgentRoot, "package.json")) &&
            Directory.Exists(Path.Combine(AgentRoot, "node_modules"));

        public static bool IsNodeAvailable(out string nodePath)
        {
            nodePath = FindOnPath("node.exe") ?? FindOnPath("node");
            return !string.IsNullOrEmpty(nodePath);
        }

        private static void EnsureAgentSidecar(bool forceNpm = false)
        {
            var agentRoot = AgentRoot;
            Directory.CreateDirectory(agentRoot);

            var pkg = Path.Combine(agentRoot, "package.json");
            if (!File.Exists(pkg))
            {
                var template = FindEmbeddedAgentTemplate();
                if (!string.IsNullOrEmpty(template) && Directory.Exists(template))
                {
                    CopyDirectory(template, agentRoot, overwrite: false);
                    Debug.Log("[GameForge] Materialized Tools/gameforge-agent from package template.");
                }
                else
                {
                    Debug.LogWarning("[GameForge] Agent template missing. Ensure Tools/gameforge-agent exists in the repo.");
                    return;
                }
            }

            if (!IsNodeAvailable(out _))
            {
                EditorUtility.DisplayDialog(
                    "V57 GameForge — Node.js required",
                    "Node.js 22+ is required for the in-Editor agent (Cursor SDK + LLM providers).\n\n" +
                    "Install Node.js, then open Workbench and click Bootstrap Agent.",
                    "OK");
                return;
            }

            var nodeModules = Path.Combine(agentRoot, "node_modules");
            if (forceNpm || !Directory.Exists(nodeModules))
            {
                EditorUtility.DisplayProgressBar("V57 GameForge", "npm install (agent sidecar)…", 0.4f);
                try
                {
                    RunNpmInstall(agentRoot);
                    SessionState.SetBool(PrefKeyReady, true);
                    Debug.Log("[GameForge] Agent sidecar ready.");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private static string FindEmbeddedAgentTemplate()
        {
            // Dev monorepo: Tools/gameforge-agent already is the agent.
            var local = AgentRoot;
            if (File.Exists(Path.Combine(local, "package.json")))
                return null;

            // Package embedded template (future): Packages/.../AgentTemplate~
            var packages = Path.Combine(ProjectRoot, "Packages", "com.v57.unity-game-forge", "AgentTemplate~");
            return Directory.Exists(packages) ? packages : null;
        }

        private static void RunNpmInstall(string cwd)
        {
            var npm = FindOnPath("npm.cmd") ?? FindOnPath("npm");
            if (string.IsNullOrEmpty(npm))
                throw new InvalidOperationException("npm not found on PATH.");

            var psi = new ProcessStartInfo
            {
                FileName = npm,
                Arguments = "install",
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException("Failed to start npm.");
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(300000);
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"npm install failed ({p.ExitCode}): {stderr}\n{stdout}");
        }

        private static string FindOnPath(string fileName)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(Path.PathSeparator))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim('"'), fileName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    /* skip */
                }
            }

            return null;
        }

        private static void CopyDirectory(string source, string dest, bool overwrite)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (rel.StartsWith("node_modules", StringComparison.OrdinalIgnoreCase)) continue;
                var target = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!overwrite && File.Exists(target)) continue;
                File.Copy(file, target, overwrite);
            }
        }
    }
}
