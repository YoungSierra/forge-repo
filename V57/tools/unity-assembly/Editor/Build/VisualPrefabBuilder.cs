using System;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// For each manifest asset of type model/character/blockout with a model on disk, builds
    /// <c>Prefabs/Visual/&lt;Category&gt;/PRF_&lt;Asset&gt;_Visual.prefab</c>: model instance (kept as nested prefab so
    /// reimports flow through, sockets/markers kept), LODGroup, colliders, size/pivot validation. No gameplay scripts.
    /// Existing real prefabs are kept unless <see cref="AssemblyOptions.Force"/>; placeholders are always replaced.
    /// </summary>
    public static class VisualPrefabBuilder
    {
        #region Fields

        private const string Step = "BuildVisualPrefabs";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            AssetManifestDto manifest = GeneratedData.AssetManifest;
            if (manifest?.assets == null)
            {
                AssemblyContext.Error(Step, "asset_manifest.json missing or empty");
                return;
            }

            foreach (AssetEntryDto entry in manifest.assets)
            {
                if (entry == null || !VisualPrefabPaths.IsVisualType(entry))
                {
                    continue;
                }

                string modelPath = VisualPrefabPaths.ResolveModelPath(entry);
                if (modelPath == null)
                {
                    continue; // BuildPlaceholders covers assets without a model.
                }

                string prefabPath = VisualPrefabPaths.PrefabPath(entry, modelPath);
                if (VisualPrefabPaths.Exists(prefabPath) && !AssemblyOptions.Force && !VisualPrefabPaths.IsPlaceholder(prefabPath))
                {
                    AssemblyContext.Counts.skipped_existing++;
                    AssemblyContext.TrackAsset(prefabPath);
                    continue;
                }

                Build(entry, modelPath, prefabPath);
            }
        }

        #endregion

        #region Private Methods

        private static void Build(AssetEntryDto entry, string modelPath, string prefabPath)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            try
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    AssemblyContext.Error(Step, $"{modelPath}: could not instantiate model");
                    return;
                }

                instance.transform.SetParent(root.transform, false);
                int lodLevels = LodGroupBuilder.Apply(root, instance);
                Bounds bounds = RendererBounds.Compute(root);
                string colliders = PrefabColliderBuilder.Apply(entry, root, bounds);
                PrefabValidator.Validate(entry, bounds);
                AssemblyPaths.EnsureFolder(AssemblyPaths.ParentFolder(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success)
                {
                    AssemblyContext.Error(Step, $"{prefabPath}: SaveAsPrefabAsset failed");
                    return;
                }

                AssemblyContext.Counts.visual_prefabs++;
                AssemblyContext.TrackAsset(prefabPath);
                Debug.Log($"V57.Assembly: {prefabPath} ← {modelPath} (lod={lodLevels}, colliders={colliders})");
            }
            catch (Exception exception)
            {
                AssemblyContext.Error(Step, $"{prefabPath}: {exception.Message}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        #endregion
    }
}
