using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>Adds a LODGroup on the prefab root when the model has <c>*_LOD0</c>, <c>*_LOD1</c>… nodes (≥2 levels).</summary>
    public static class LodGroupBuilder
    {
        #region Fields

        private static readonly Regex LodSuffix = new Regex(@"_LOD(\d+)$", RegexOptions.CultureInvariant);

        #endregion

        #region Public Methods

        /// <summary>Returns the number of LOD levels configured (0 when none).</summary>
        public static int Apply(GameObject root, GameObject modelInstance)
        {
            SortedDictionary<int, List<Renderer>> levels = new SortedDictionary<int, List<Renderer>>();
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

            if (levels.Count < 2)
            {
                return 0;
            }

            LOD[] lods = new LOD[levels.Count];
            int index = 0;
            foreach (KeyValuePair<int, List<Renderer>> level in levels)
            {
                float height = 0.5f / (1 << index);
                if (index == levels.Count - 1)
                {
                    height = Mathf.Min(0.02f, height);
                }

                lods[index] = new LOD(height, level.Value.ToArray());
                index++;
            }

            LODGroup group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
            return levels.Count;
        }

        #endregion
    }
}
