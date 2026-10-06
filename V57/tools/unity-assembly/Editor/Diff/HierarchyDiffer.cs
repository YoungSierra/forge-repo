using System;
using System.Collections.Generic;

namespace V57.Assembly.Diff
{
    /// <summary>
    /// Compares Edit vs Play snapshots against SCENE_PRODUCTION_STANDARDS: runtime additions only under spawn roots,
    /// no removals, no component additions/removals, no renderer toggles on art (outside <c>_UI</c>).
    /// Known engine/test-runner objects and auto-added URP components are ignored and listed.
    /// </summary>
    public static class HierarchyDiffer
    {
        #region Fields

        public static readonly string[] DefaultSpawnRoots = { "_Gameplay/Spawned" };

        private static readonly string[] IgnoredPathPrefixes =
        {
            HierarchyCapture.DontDestroyOnLoadPrefix + "[Debug Updater]",
            HierarchyCapture.DontDestroyOnLoadPrefix + "Code-based tests runner",
            "V57GoldPathDriver"
        };

        public const string AllowedPersistentRootPrefix = "_Systems";

        private static readonly string[] IgnoredComponents = { "UniversalAdditionalCameraData", "UniversalAdditionalLightData" };

        #endregion

        #region Public Methods

        public static HierarchyDiffReport Compare(HierarchySnapshot edit, HierarchySnapshot play, string[] spawnRoots)
        {
            Dictionary<string, HierarchyNode> editNodes = Index(edit);
            Dictionary<string, HierarchyNode> playNodes = Index(play);
            List<string> allowed = new List<string>();
            List<string> violations = new List<string>();
            List<string> destroyed = new List<string>();
            List<string> added = new List<string>();
            List<string> removed = new List<string>();
            List<string> toggled = new List<string>();
            List<string> activation = new List<string>();
            List<string> ignored = new List<string>();
            foreach (KeyValuePair<string, HierarchyNode> pair in playNodes)
            {
                if (IsIgnored(pair.Key))
                {
                    ignored.Add(pair.Key);
                }
                else if (!editNodes.TryGetValue(pair.Key, out HierarchyNode before))
                {
                    (UnderAny(pair.Key, spawnRoots) || IsAllowedPersistent(pair.Key) ? allowed : violations).Add(pair.Key);
                }
                else
                {
                    CompareComponents(pair.Key, before, pair.Value, added, removed);
                    if (before.renderer_enabled >= 0 && pair.Value.renderer_enabled >= 0 && before.renderer_enabled != pair.Value.renderer_enabled && !pair.Key.StartsWith("_UI", StringComparison.Ordinal))
                    {
                        toggled.Add($"{pair.Key}: {before.renderer_enabled}→{pair.Value.renderer_enabled}");
                    }

                    if (before.active_self != pair.Value.active_self)
                    {
                        activation.Add($"{pair.Key}: {before.active_self}→{pair.Value.active_self}");
                    }
                }
            }

            foreach (string path in editNodes.Keys)
            {
                if (!playNodes.ContainsKey(path) && !IsIgnored(path))
                {
                    destroyed.Add(path);
                }
            }

            return new HierarchyDiffReport
            {
                scene = play.scene,
                edit_captured_utc = edit.captured_utc,
                play_captured_utc = play.captured_utc,
                edit_nodes = editNodes.Count,
                play_nodes = playNodes.Count,
                spawn_roots = spawnRoots,
                created_allowed = allowed.ToArray(),
                created_violations = violations.ToArray(),
                destroyed = destroyed.ToArray(),
                components_added = added.ToArray(),
                components_removed = removed.ToArray(),
                renderer_toggled = toggled.ToArray(),
                activation_changed = activation.ToArray(),
                ignored = ignored.ToArray(),
                pass = violations.Count == 0 && destroyed.Count == 0 && added.Count == 0 && removed.Count == 0 && toggled.Count == 0
            };
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, HierarchyNode> Index(HierarchySnapshot snapshot)
        {
            Dictionary<string, HierarchyNode> index = new Dictionary<string, HierarchyNode>();
            foreach (HierarchyNode node in snapshot?.nodes ?? new HierarchyNode[0])
            {
                index[node.path] = node;
            }

            return index;
        }

        private static void CompareComponents(string path, HierarchyNode before, HierarchyNode after, List<string> added, List<string> removed)
        {
            List<string> remaining = new List<string>(before.components ?? new string[0]);
            foreach (string component in after.components ?? new string[0])
            {
                if (!remaining.Remove(component) && Array.IndexOf(IgnoredComponents, component) < 0)
                {
                    added.Add($"{path}: +{component}");
                }
            }

            foreach (string component in remaining)
            {
                removed.Add($"{path}: -{component}");
            }
        }

        private static bool UnderAny(string path, string[] roots)
        {
            foreach (string root in roots)
            {
                if (path.StartsWith(root + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>DontDestroyOnLoad roots named <c>_Systems*</c> (persistent services) and their children are allowed.</summary>
        private static bool IsAllowedPersistent(string path)
        {
            return path.StartsWith(HierarchyCapture.DontDestroyOnLoadPrefix + AllowedPersistentRootPrefix, StringComparison.Ordinal);
        }

        private static bool IsIgnored(string path)
        {
            foreach (string prefix in IgnoredPathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
