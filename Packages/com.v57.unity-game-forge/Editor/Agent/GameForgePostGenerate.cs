using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Runs inside the Editor after Generate/Chat writes files.
    /// Because GameForge Chat already lives in Unity, we refresh/open scenes
    /// directly — no Unity MCP relay required for this step.
    /// </summary>
    public static class GameForgePostGenerate
    {
        public static string Run(string slug, string forgeMode, IReadOnlyList<string> writtenFiles)
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                var modeRoot = ModeRoot(forgeMode);
                var scenePath = ResolveScenePath(slug, modeRoot, writtenFiles);
                if (string.IsNullOrEmpty(scenePath))
                {
                    return $"Editor: refreshed assets under {modeRoot}. No .unity scene found — add one under {modeRoot}/Scenes/ then Play.";
                }

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return "Editor: refresh OK · scene open cancelled (unsaved changes).";

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                EnsureBootstrapNote(slug, scene);

                return $"Editor: refreshed · opened {scenePath.Replace('\\', '/')} · press Play to run graybox.";
            }
            catch (Exception ex)
            {
                return "Editor post-generate failed: " + Trunc(ex.Message, 160);
            }
        }

        private static string ModeRoot(string forgeMode)
        {
            if (string.Equals(forgeMode, "VerticalSlice", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(forgeMode, nameof(GameForgeMode.VerticalSlice), StringComparison.OrdinalIgnoreCase))
                return "Assets/VerticalSlice";
            if (string.Equals(forgeMode, "Production", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(forgeMode, nameof(GameForgeMode.Production), StringComparison.OrdinalIgnoreCase))
                return "Assets";
            return "Assets/Prototypes";
        }

        private static string ResolveScenePath(string slug, string modeRoot, IReadOnlyList<string> writtenFiles)
        {
            if (writtenFiles != null)
            {
                foreach (var f in writtenFiles)
                {
                    if (string.IsNullOrEmpty(f)) continue;
                    var n = f.Replace('\\', '/');
                    if (n.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && File.Exists(Abs(n)))
                        return n.StartsWith("Assets/", StringComparison.Ordinal) ? n : null;
                }
            }

            // Prefer project folder: Assets/Prototypes/<slug>/Scenes (prototype-full layout).
            if (!string.IsNullOrEmpty(slug) &&
                string.Equals(modeRoot, "Assets/Prototypes", StringComparison.OrdinalIgnoreCase))
            {
                var projectScenes = Path.Combine(ProjectRoot(), modeRoot, slug, "Scenes").Replace('\\', '/');
                var fromProject = PickSceneInDir(projectScenes, slug);
                if (!string.IsNullOrEmpty(fromProject)) return fromProject;
            }

            var scenesDir = Path.Combine(ProjectRoot(), modeRoot, "Scenes").Replace('\\', '/');
            return PickSceneInDir(scenesDir, slug);
        }

        private static string PickSceneInDir(string scenesDir, string slug)
        {
            if (!Directory.Exists(scenesDir)) return null;

            var candidates = Directory.GetFiles(scenesDir, "*.unity", SearchOption.TopDirectoryOnly)
                .Select(p => ToAssetPath(p))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (candidates.Count == 0) return null;

            if (!string.IsNullOrEmpty(slug))
            {
                var bySlug = candidates.FirstOrDefault(c =>
                    Path.GetFileNameWithoutExtension(c).Equals(slug, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(c).IndexOf(slug, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(bySlug)) return bySlug;

                var byMain = candidates.FirstOrDefault(c =>
                    Path.GetFileNameWithoutExtension(c).Equals("Main", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(c).EndsWith("_Main", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(byMain)) return byMain;
            }

            return candidates[0];
        }

        private static void EnsureBootstrapNote(string slug, Scene scene)
        {
            if (string.IsNullOrEmpty(slug)) return;
            var found = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.IndexOf("Bootstrap", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                Debug.Log($"[GameForge] Opened {scene.path}. Prefer [{slug}Bootstrap] with RuntimeInitializeOnLoad (CoinRush pattern) so Play builds the graybox.");
        }

        private static string ProjectRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string Abs(string assetOrRel)
        {
            var n = assetOrRel.Replace('\\', '/');
            if (n.StartsWith("Assets/", StringComparison.Ordinal))
                return Path.GetFullPath(Path.Combine(ProjectRoot(), n));
            return Path.GetFullPath(Path.Combine(ProjectRoot(), n));
        }

        private static string ToAssetPath(string fullPath)
        {
            var full = Path.GetFullPath(fullPath).Replace('\\', '/');
            var root = ProjectRoot().Replace('\\', '/') + "/";
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
            return full.Substring(root.Length);
        }

        private static string Trunc(string s, int n) =>
            string.IsNullOrEmpty(s) || s.Length <= n ? s ?? "" : s.Substring(0, n) + "…";
    }
}
