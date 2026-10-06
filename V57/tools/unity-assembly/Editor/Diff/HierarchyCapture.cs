using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.Assembly.Diff
{
    /// <summary>Serializes a scene hierarchy (paths + component types). In Play Mode, DontDestroyOnLoad objects are
    /// included under the <c>[DontDestroyOnLoad]/</c> prefix.</summary>
    public static class HierarchyCapture
    {
        #region Fields

        public const string DontDestroyOnLoadPrefix = "[DontDestroyOnLoad]/";

        #endregion

        #region Public Methods

        public static HierarchySnapshot Capture(Scene scene, bool playMode)
        {
            List<HierarchyNode> nodes = new List<HierarchyNode>();
            AddRoots(scene.GetRootGameObjects(), string.Empty, nodes);
            if (playMode)
            {
                AddRoots(DontDestroyOnLoadRoots(), DontDestroyOnLoadPrefix, nodes);
            }

            return new HierarchySnapshot
            {
                scene = scene.path,
                mode = playMode ? "play" : "edit",
                captured_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                nodes = nodes.ToArray()
            };
        }

        #endregion

        #region Private Methods

        private static GameObject[] DontDestroyOnLoadRoots()
        {
            GameObject probe = new GameObject("V57HierarchyProbe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            Scene ddolScene = probe.scene;
            UnityEngine.Object.DestroyImmediate(probe);
            List<GameObject> roots = new List<GameObject>(ddolScene.GetRootGameObjects());
            return roots.ToArray();
        }

        private static void AddRoots(GameObject[] roots, string prefix, List<HierarchyNode> nodes)
        {
            Dictionary<string, int> seen = new Dictionary<string, int>();
            foreach (GameObject root in roots)
            {
                AddNode(root.transform, prefix + UniqueName(root.name, seen), nodes);
            }
        }

        private static void AddNode(Transform node, string path, List<HierarchyNode> nodes)
        {
            List<string> components = new List<string>();
            int rendererState = -1;
            foreach (Component component in node.GetComponents<Component>())
            {
                components.Add(component == null ? "<missing script>" : component.GetType().Name);
                if (component is Renderer renderer)
                {
                    rendererState = renderer.enabled ? 1 : 0;
                }
            }

            components.Sort(StringComparer.Ordinal);
            nodes.Add(new HierarchyNode { path = path, components = components.ToArray(), active_self = node.gameObject.activeSelf, renderer_enabled = rendererState });
            Dictionary<string, int> seen = new Dictionary<string, int>();
            foreach (Transform child in node)
            {
                AddNode(child, path + "/" + UniqueName(child.name, seen), nodes);
            }
        }

        private static string UniqueName(string name, Dictionary<string, int> seen)
        {
            seen.TryGetValue(name, out int count);
            seen[name] = count + 1;
            return count == 0 ? name : $"{name}#{count}";
        }

        #endregion
    }
}
