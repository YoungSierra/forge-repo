using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Resolves <c>scenes[].blockout</c> (asset_id, asset_name or path) to the asset to instantiate:
    /// the Visual prefab (real or placeholder) first, then the raw model.
    /// </summary>
    public static class BlockoutResolver
    {
        #region Public Methods

        public static GameObject Resolve(string blockout, out string displayName)
        {
            displayName = string.Empty;
            if (string.IsNullOrWhiteSpace(blockout))
            {
                return null;
            }

            string key = blockout.Trim();
            AssetEntryDto entry = FindEntry(key);
            displayName = entry != null ? VisualPrefabPaths.SafeName(entry) : Path.GetFileNameWithoutExtension(key);
            if (entry != null)
            {
                string modelPath = VisualPrefabPaths.ResolveModelPath(entry);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPaths.PrefabPath(entry, modelPath));
                if (prefab != null)
                {
                    return prefab;
                }

                if (modelPath != null)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                }
            }

            string direct = AssemblyPaths.ToAssetPath(key);
            return direct != null ? AssetDatabase.LoadAssetAtPath<GameObject>(direct) : null;
        }

        #endregion

        #region Private Methods

        private static AssetEntryDto FindEntry(string key)
        {
            AssetEntryDto[] assets = GeneratedData.AssetManifest?.assets ?? new AssetEntryDto[0];
            string fileName = Path.GetFileName(key.Replace('\\', '/'));
            foreach (AssetEntryDto entry in assets)
            {
                if (entry == null)
                {
                    continue;
                }

                bool byId = string.Equals(entry.asset_id, key, StringComparison.OrdinalIgnoreCase);
                bool byName = string.Equals(entry.asset_name, key, StringComparison.OrdinalIgnoreCase);
                string mesh = entry.files?.mesh ?? string.Empty;
                bool byFile = mesh.Length > 0 && mesh.Replace('\\', '/').EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase);
                if (byId || byName || byFile)
                {
                    return entry;
                }
            }

            return null;
        }

        #endregion
    }
}
