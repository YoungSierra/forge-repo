using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Import;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Points the FBX in an asset folder at <c>MAT_&lt;Asset&gt;</c> via <see cref="AssetImporter.AddRemap"/>. The embedded
    /// material named like the asset wins; otherwise a single material; otherwise the material covering the most vertices
    /// (secondary materials such as an eye lens keep their FBX-authored values).
    /// </summary>
    public static class ModelMaterialRemapper
    {
        #region Fields

        private const string Step = "BuildMaterials";

        #endregion

        #region Public Methods

        public static void Remap(string assetFolder, string assetName, Material material)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { assetFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || path.Contains("/Animations/") || IsAlreadyRemapped(importer, material))
                {
                    continue;
                }

                DropStaleRemaps(importer, path);
                List<string> names = EmbeddedMaterialNames(path);
                if (names.Count == 0)
                {
                    continue;
                }

                string chosen = names.Contains(assetName) ? assetName : names.Count == 1 ? names[0] : MainMaterial(path, names);
                if (names.Count > 1 && chosen != assetName)
                {
                    AssemblyContext.Warn(Step, $"{path}: materials [{string.Join(", ", names)}]; '{chosen}' (most vertices) → MAT_{assetName}, others keep FBX values");
                }

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), chosen), material);
                importer.SaveAndReimport();
                ImportLog.Record(path, "material-remap", $"'{chosen}' → {AssetDatabase.GetAssetPath(material)}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Source material names of the FBX: embedded sub-assets plus already-remapped sources (a remap hides the embedded
        /// sub-asset, and its target may have been deleted or rebuilt with a new GUID — it must be re-pointed, not skipped).
        /// </summary>
        private static List<string> EmbeddedMaterialNames(string path)
        {
            List<string> names = new List<string>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Material embedded && !names.Contains(embedded.name))
                {
                    names.Add(embedded.name);
                }
            }

            if (AssetImporter.GetAtPath(path) is ModelImporter importer)
            {
                foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> pair in importer.GetExternalObjectMap())
                {
                    if (pair.Key.type == typeof(Material) && !names.Contains(pair.Key.name))
                    {
                        names.Add(pair.Key.name);
                    }
                }
            }

            return names;
        }

        private static string MainMaterial(string path, List<string> names)
        {
            Dictionary<string, int> vertices = new Dictionary<string, int>();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (Renderer renderer in model != null ? model.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : filter != null ? filter.sharedMesh : null;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; mesh != null && i < materials.Length && i < mesh.subMeshCount; i++)
                {
                    string name = materials[i] != null ? materials[i].name : null;
                    if (name != null)
                    {
                        vertices[name] = (vertices.TryGetValue(name, out int count) ? count : 0) + mesh.GetSubMesh(i).vertexCount;
                    }
                }
            }

            string best = names[0];
            foreach (KeyValuePair<string, int> pair in vertices)
            {
                if (names.Contains(pair.Key) && (!vertices.ContainsKey(best) || pair.Value > vertices[best]))
                {
                    best = pair.Key;
                }
            }

            return best;
        }

        /// <summary>Remaps whose target material no longer exists are removed so the embedded materials come back.</summary>
        private static void DropStaleRemaps(ModelImporter importer, string path)
        {
            List<AssetImporter.SourceAssetIdentifier> stale = new List<AssetImporter.SourceAssetIdentifier>();
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> pair in importer.GetExternalObjectMap())
            {
                if (pair.Key.type == typeof(Material) && pair.Value == null)
                {
                    stale.Add(pair.Key);
                }
            }

            if (stale.Count == 0)
            {
                return;
            }

            stale.ForEach(key => importer.RemoveRemap(key));
            importer.SaveAndReimport();
            ImportLog.Record(path, "material-remap", $"removed {stale.Count} remap(s) to deleted materials");
        }

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
