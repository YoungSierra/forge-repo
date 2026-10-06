using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Import;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Points the embedded material named <c>&lt;Asset&gt;</c> (brief: FBX material name == asset_name) of every model in
    /// the asset folder to <c>MAT_&lt;Asset&gt;</c> via <see cref="AssetImporter.AddRemap"/>. A single differently named
    /// material is remapped with a warning; several non-matching materials are left alone.
    /// </summary>
    public static class ModelMaterialRemapper
    {
        #region Fields

        private const string Step = "BuildMaterials";

        #endregion

        #region Public Methods

        public static void Remap(MaterialTextureSet set, Material material)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { set.AssetFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || IsAlreadyRemapped(importer, material))
                {
                    continue;
                }

                List<string> names = new List<string>();
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Material embedded && !names.Contains(embedded.name))
                    {
                        names.Add(embedded.name);
                    }
                }

                string chosen = names.Contains(set.AssetName) ? set.AssetName : (names.Count == 1 ? names[0] : null);
                if (chosen == null)
                {
                    AssemblyContext.Warn(Step, $"{path}: materials [{string.Join(", ", names)}] — none named '{set.AssetName}'; not remapped");
                    continue;
                }

                if (chosen != set.AssetName)
                {
                    AssemblyContext.Warn(Step, $"{path}: material '{chosen}' != asset_name '{set.AssetName}'; remapped anyway (single material)");
                }

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), chosen), material);
                importer.SaveAndReimport();
                ImportLog.Record(path, "material-remap", $"'{chosen}' → {AssetDatabase.GetAssetPath(material)}");
            }
        }

        #endregion

        #region Private Methods

        private static bool IsAlreadyRemapped(ModelImporter importer, Material material)
        {
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> pair in importer.GetExternalObjectMap())
            {
                if (pair.Key.type == typeof(Material) && pair.Value == material)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
