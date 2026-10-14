using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Adds a LODGroup on the prefab root when the model has <c>*_LOD0</c>, <c>*_LOD1</c>… nodes (≥2 levels) and the
    /// imported model carries no LODGroup of its own (Unity's importer creates one for some files; a second group would
    /// register every renderer twice).
    /// </summary>
    public static class LodGroupBuilder
    {
        #region Fields

        private static readonly Regex LodSuffix = new Regex(@"_LOD(\d+)$", RegexOptions.CultureInvariant);

        #endregion

        #region Public Methods

        /// <summary>Returns the number of LOD levels configured (0 when none).</summary>
        public static int Apply(GameObject root, GameObject modelInstance)
        {
            SortedDictionary<int, List<Renderer>> levels = Levels(modelInstance);
            List<Renderer[]> ordered = new List<Renderer[]>();
            foreach (KeyValuePair<int, List<Renderer>> level in levels)
            {
                ordered.Add(level.Value.ToArray());
            }

            return ApplyLevels(root, ordered);
        }

        /// <summary>LODGroup on <paramref name="root"/> from renderer sets in level order (≥2 levels; else nothing).</summary>
        public static int ApplyLevels(GameObject root, IList<Renderer[]> levels)
        {
            if (levels.Count < 2)
            {
                return 0;
            }

            LOD[] lods = new LOD[levels.Count];
            for (int index = 0; index < levels.Count; index++)
            {
                float height = 0.5f / (1 << index);
                if (index == levels.Count - 1)
                {
                    height = Mathf.Min(0.02f, height);
                }

                lods[index] = new LOD(height, levels[index]);
            }

            LODGroup group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
            return levels.Count;
        }

        /// <summary>
        /// Existing prefab after a model re-delivery: rebuilds the root LODGroup when it no longer matches the model's
        /// <c>_LOD#</c> nodes (levels added, removed or renamed; missing renderers). Returns true when it changed.
        /// Without a LODGroup every LOD node renders at once (overlapping meshes, shadows cast N times).
        /// </summary>
        public static bool Refresh(GameObject root, GameObject modelInstance)
        {
            SortedDictionary<int, List<Renderer>> levels = Levels(modelInstance);
            LODGroup current = root.GetComponent<LODGroup>();
            if (Matches(current, levels))
            {
                return false;
            }

            if (current != null)
            {
                Object.DestroyImmediate(current);
            }

            Apply(root, modelInstance);
            return true;
        }

        #endregion

        #region Private Methods

        private static bool Matches(LODGroup current, SortedDictionary<int, List<Renderer>> levels)
        {
            if (current == null)
            {
                return levels.Count < 2;
            }

            LOD[] lods = current.GetLODs();
            if (levels.Count < 2 || lods.Length != levels.Count)
            {
                return false;
            }

            int index = 0;
            foreach (KeyValuePair<int, List<Renderer>> level in levels)
            {
                HashSet<Renderer> assigned = new HashSet<Renderer>(lods[index].renderers);
                if (assigned.Contains(null) || !assigned.SetEquals(level.Value))
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        private static SortedDictionary<int, List<Renderer>> Levels(GameObject modelInstance)
        {
            SortedDictionary<int, List<Renderer>> levels = new SortedDictionary<int, List<Renderer>>();
            if (modelInstance.GetComponentInChildren<LODGroup>(true) != null)
            {
                return levels; // the imported model has its own LODGroup
            }

            foreach (Transform node in modelInstance.GetComponentsInChildren<Transform>(true))
            {
                Match match = LodSuffix.Match(node.name);
                if (!match.Success)
                {
                    continue;
                }

                int level = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!levels.TryGetValue(level, out List<Renderer> renderers))
                {
                    renderers = new List<Renderer>();
                    levels.Add(level, renderers);
                }

                foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.enabled && !renderers.Contains(renderer))
                    {
                        renderers.Add(renderer);
                    }
                }
            }

            return levels;
        }

        #endregion
    }
}
