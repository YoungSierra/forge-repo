using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// One prefab per thing: scenes use the gameplay prefab <c>Prefabs/Gameplay/**/PRF_&lt;Asset&gt;.prefab</c> (code) on top of
    /// the generated <c>PRF_&lt;Asset&gt;_Visual</c> (art) — normally a prefab variant of it. This class finds the gameplay prefab
    /// of a Visual prefab and, when the gameplay prefab was authored before its art existed (plain prefab, no base), nests
    /// the Visual once it is delivered. Variants already inherit the art and are left untouched; gameplay components are
    /// never edited.
    /// </summary>
    public static class GameplayPrefabLinker
    {
        #region Fields

        private const string Step = "BuildVisualPrefabs";
        private const string VisualSuffix = "_Visual";

        #endregion

        #region Public Methods

        /// <summary>Gameplay prefab paths by prefab name (<c>PRF_&lt;Asset&gt;</c>).</summary>
        public static Dictionary<string, string> GameplayPrefabsByName()
        {
            Dictionary<string, string> byName = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!AssetDatabase.IsValidFolder(AssemblyPaths.GameplayPrefabRoot))
            {
                return byName;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssemblyPaths.GameplayPrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!byName.ContainsKey(name))
                {
                    byName.Add(name, path);
                }
            }

            return byName;
        }

        /// <summary>Gameplay prefab path for a <c>PRF_&lt;Asset&gt;_Visual</c> prefab, or null.</summary>
        public static string FindFor(string visualPrefabPath, Dictionary<string, string> gameplayByName)
        {
            string name = Path.GetFileNameWithoutExtension(visualPrefabPath ?? string.Empty);
            if (!name.EndsWith(VisualSuffix, StringComparison.Ordinal))
            {
                return null;
            }

            return gameplayByName.TryGetValue(name.Substring(0, name.Length - VisualSuffix.Length), out string path) ? path : null;
        }

        /// <summary>Nests every Visual prefab into its gameplay prefab when the gameplay prefab does not contain it yet.</summary>
        public static void LinkAll()
        {
            Dictionary<string, string> gameplayByName = GameplayPrefabsByName();
            if (gameplayByName.Count == 0 || !AssetDatabase.IsValidFolder(AssemblyPaths.VisualPrefabRoot))
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssemblyPaths.VisualPrefabRoot }))
            {
                string visualPath = AssetDatabase.GUIDToAssetPath(guid);
                string gameplayPath = FindFor(visualPath, gameplayByName);
                if (gameplayPath != null)
                {
                    Link(gameplayPath, visualPath);
                }
            }
        }

        #endregion

        #region Private Methods

        private static void Link(string gameplayPath, string visualPath)
        {
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            GameObject root = PrefabUtility.LoadPrefabContents(gameplayPath);
            try
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == visual)
                    {
                        return; // already nested
                    }
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(visual, root.transform);
                instance.name = visual.name;
                PrefabUtility.SaveAsPrefabAsset(root, gameplayPath, out bool success);
                if (!success)
                {
                    AssemblyContext.Error(Step, $"{gameplayPath}: could not nest {visualPath}");
                    return;
                }

                AssemblyContext.Counts.gameplay_linked++;
                AssemblyContext.TrackAsset(gameplayPath);
                Debug.Log($"V57.Assembly: {gameplayPath} ← nested {visualPath} (art arrived after the gameplay prefab)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #endregion
    }
}
