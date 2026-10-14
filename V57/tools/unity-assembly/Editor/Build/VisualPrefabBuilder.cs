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
    /// Rigged models get an Animator bound to <c>AC_&lt;Asset&gt;</c> (<see cref="AnimatorBinder"/>).
    /// Existing prefabs are kept unless <see cref="AssemblyOptions.Force"/>. Assets without a model get no prefab (no placeholder).
    /// Finally nests each Visual prefab into its gameplay prefab <c>PRF_&lt;Asset&gt;</c> when that exists without it
    /// (<see cref="GameplayPrefabLinker"/>), so art delivered after the system appears without manual work.
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

            System.Collections.Generic.Dictionary<string, string> existingByModel = VisualPrefabPaths.ExistingByModel();
            foreach (AssetEntryDto entry in manifest.assets)
            {
                if (entry == null || !VisualPrefabPaths.IsVisualType(entry))
                {
                    continue;
                }

                string modelPath = VisualPrefabPaths.ResolveModelPath(entry);
                if (modelPath == null)
                {
                    continue; // missing model: slot stays empty, intake lists it in MISSING_ASSETS.md
                }

                string prefabPath = VisualPrefabPaths.PrefabPath(entry, modelPath);
                if (existingByModel.TryGetValue(modelPath, out string existing) && existing != prefabPath)
                {
                    // One Visual prefab per model: a prefab built under an earlier naming rule is reused, never duplicated.
                    prefabPath = existing;
                }
                if (VisualPrefabPaths.Exists(prefabPath) && !AssemblyOptions.Force)
                {
                    AssemblyContext.Counts.skipped_existing++;
                    AssemblyContext.TrackAsset(prefabPath);
                    RefreshKept(entry, prefabPath, modelPath);
                    continue;
                }

                Build(entry, modelPath, prefabPath);
            }

            GameplayPrefabLinker.LinkAll();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kept prefab, possibly re-delivered model: its LODGroup follows the model's current <c>_LOD#</c> nodes and, for a
        /// ruled asset (<c>Docs/V57/asset_rules.json</c>), its generated colliders and shadow casting follow the rules.
        /// </summary>
        private static void RefreshKept(AssetEntryDto entry, string prefabPath, string modelPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                GameObject instance = null;
                foreach (Transform child in root.transform)
                {
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject);
                    if (source != null && AssetDatabase.GetAssetPath(source) == modelPath)
                    {
                        instance = child.gameObject;
                        break;
                    }
                }

                // Level files merged at build (SiblingLodFiles) are not _LOD# nodes of the model: rebuild the prefab to refresh them.
                bool lods = instance != null && SiblingLodFiles.Find(modelPath).Count == 0 && LodGroupBuilder.Refresh(root, instance);
                bool colliders = entry.collision_ruled && AssetRulesApplier.RefreshColliders(entry, root);
                bool shadows = AssetRulesApplier.ApplyShadows(entry, root);
                bool bounds = instance != null && AnimatedBounds.Fit(instance);
                bool cull = AnimatedBounds.ApplyCullRule(entry, root);
                if (lods || colliders || shadows || bounds || cull)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log($"V57.Assembly: {prefabPath} refreshed (lods={lods}, colliders={colliders}, shadows={shadows}, animatedBounds={bounds}, cull={cull}) ← {modelPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

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
                bool animated = AnimatorBinder.Bind(instance, modelPath);
                // LOD levels inside the model (_LOD# nodes), or delivered as separate files next to it (SK_XLOD01…).
                int lodLevels = SiblingLodFiles.Find(modelPath).Count > 0 ? SiblingLodFiles.Attach(root, instance, modelPath) : LodGroupBuilder.Apply(root, instance);
                if (animated)
                {
                    AnimatedBounds.Fit(instance);
                }

                AnimatedBounds.ApplyCullRule(entry, root);
                Bounds bounds = RendererBounds.Compute(root);
                string colliders = PrefabColliderBuilder.Apply(entry, root, bounds);
                AssetRulesApplier.ApplyShadows(entry, root);
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
                Debug.Log($"V57.Assembly: {prefabPath} ← {modelPath} (lod={lodLevels}, colliders={colliders}, animator={animated})");
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
