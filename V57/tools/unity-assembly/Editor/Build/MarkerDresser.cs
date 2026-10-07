using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Places static set dressing: every <c>Marker_Spawn_&lt;AssetName&gt;_NN</c> whose asset has no <c>serves</c>
    /// (pure environment / decoration, not a gameplay entity) gets its Visual prefab instance under
    /// <c>_Environment/_Dressing</c> with the marker pose (position, rotation, scale). Markers of gameplay entities
    /// (asset serves at least one mechanic) are left for gameplay code at M1. Genre-agnostic: the provider decides
    /// placement in the blockout, V57 only instantiates.
    /// </summary>
    public static class MarkerDresser
    {
        #region Fields

        private const string Step = "BuildLevelScenes";
        private static readonly Regex InstanceSuffix = new Regex(@"_\d+$", RegexOptions.Compiled);

        #endregion

        #region Public Methods

        public static int Dress(SceneContainers containers, string sceneId)
        {
            Transform dressingRoot = null;
            int placed = 0;
            foreach (V57Marker marker in containers.Markers.GetComponentsInChildren<V57Marker>(true))
            {
                if (marker.Kind != V57MarkerKind.Spawn)
                {
                    continue;
                }

                string entity = InstanceSuffix.Replace(marker.Id ?? string.Empty, string.Empty);
                AssetEntryDto entry = FindAsset(entity);
                if (entry == null)
                {
                    continue;
                }

                if (entry.serves != null && entry.serves.Length > 0)
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VisualPrefabPaths.PrefabPath(entry, VisualPrefabPaths.ResolveModelPath(entry)));
                if (prefab == null)
                {
                    AssemblyContext.Warn(Step, $"{sceneId}: {marker.name} → no Visual prefab for '{entity}' (run BuildVisualPrefabs first; missing art stays empty)");
                    continue;
                }

                if (dressingRoot == null)
                {
                    dressingRoot = SceneContainers.CreateChild(containers.Environment, "_Dressing");
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, containers.Environment.gameObject.scene) as GameObject;
                if (instance == null)
                {
                    AssemblyContext.Error(Step, $"{sceneId}: InstantiatePrefab failed for {prefab.name}");
                    continue;
                }

                instance.transform.SetParent(dressingRoot, false);
                instance.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
                instance.transform.localScale = marker.transform.lossyScale;
                instance.name = $"{VisualPrefabPaths.SafeName(entry)}_{marker.Id}";
                instance.isStatic = true;
                placed++;
            }

            return placed;
        }

        #endregion

        #region Private Methods

        private static AssetEntryDto FindAsset(string entity)
        {
            if (entity.Length == 0)
            {
                return null;
            }

            foreach (AssetEntryDto entry in GeneratedData.AssetManifest?.assets ?? new AssetEntryDto[0])
            {
                if (entry == null || !VisualPrefabPaths.IsVisualType(entry))
                {
                    continue;
                }

                if (string.Equals(entry.asset_name, entity, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.asset_id, entity, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        #endregion
    }
}
