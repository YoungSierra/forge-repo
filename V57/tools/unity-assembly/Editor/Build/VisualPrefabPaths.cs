using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>Resolves model sources and <c>Prefabs/Visual/&lt;Category&gt;/PRF_&lt;Asset&gt;_Visual.prefab</c> paths.</summary>
    public static class VisualPrefabPaths
    {
        #region Public Methods

        public static bool IsVisualType(AssetEntryDto entry)
        {
            string type = (entry?.type ?? string.Empty).ToLowerInvariant();
            return type == "model" || type == "character" || type == "blockout";
        }

        public static string SafeName(AssetEntryDto entry)
        {
            string source = !string.IsNullOrWhiteSpace(entry.asset_name) ? entry.asset_name : entry.asset_id ?? "Unnamed";
            return GoldPathPaths.SanitizeFileName(source.Trim());
        }

        /// <summary>Category comes from the declared files.mesh first, so re-deliveries of the model keep one prefab path/GUID.</summary>
        public static string PrefabPath(AssetEntryDto entry, string modelPath)
        {
            string hint = AssemblyPaths.ToAssetPath(entry?.files?.mesh) ?? modelPath;
            return $"{AssemblyPaths.VisualPrefabRoot}/{Category(entry, hint)}/PRF_{SafeName(entry)}_Visual.prefab";
        }

        /// <summary>files.mesh when it loads, else a model under Art named SM_/SK_/BLK_/plain asset_name; null if none.</summary>
        public static string ResolveModelPath(AssetEntryDto entry)
        {
            string mesh = AssemblyPaths.ToAssetPath(entry?.files?.mesh);
            if (mesh != null && AssetDatabase.LoadAssetAtPath<GameObject>(mesh) != null)
            {
                return mesh;
            }

            string name = entry?.asset_name ?? string.Empty;
            if (name.Length == 0)
            {
                return null;
            }

            string[] candidates = { "SM_" + name, "SK_" + name, "BLK_" + name, name };
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AssemblyPaths.ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Array.IndexOf(candidates, Path.GetFileNameWithoutExtension(path)) >= 0)
                {
                    return path;
                }
            }

            return null;
        }

        public static bool Exists(string assetPath)
        {
            return File.Exists(AssemblyPaths.ToFullPath(assetPath));
        }

        /// <summary>Existing Visual prefab path per source model path (first by path order), from the nested model instances.</summary>
        public static System.Collections.Generic.Dictionary<string, string> ExistingByModel()
        {
            System.Collections.Generic.Dictionary<string, string> byModel = new System.Collections.Generic.Dictionary<string, string>();
            if (!AssetDatabase.IsValidFolder(AssemblyPaths.VisualPrefabRoot))
            {
                return byModel;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { AssemblyPaths.VisualPrefabRoot });
            string[] paths = Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath);
            Array.Sort(paths, StringComparer.Ordinal);
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform child in prefab.transform)
                {
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject);
                    string model = source != null ? AssetDatabase.GetAssetPath(source) : null;
                    if (!string.IsNullOrEmpty(model) && !byModel.ContainsKey(model))
                    {
                        byModel.Add(model, path);
                    }
                }
            }

            return byModel;
        }

        #endregion

        #region Private Methods

        private static string Category(AssetEntryDto entry, string modelPath)
        {
            string path = modelPath ?? string.Empty;
            string type = (entry.type ?? string.Empty).ToLowerInvariant();
            string category = (entry.category ?? string.Empty).ToLowerInvariant();
            if (type == "character" || path.Contains("/Characters/"))
            {
                return "Characters";
            }

            if (type == "blockout" || path.Contains("/Environment/") || category.Contains("env"))
            {
                return "Environment";
            }

            if (path.Contains("/VFX/") || category.Contains("vfx"))
            {
                return "VFX";
            }

            return category.Contains("character") ? "Characters" : "Props";
        }

        #endregion
    }
}
