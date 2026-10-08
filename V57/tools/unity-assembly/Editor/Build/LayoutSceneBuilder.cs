using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Places a normalized level layout (<c>layouts.json</c>) in a level scene, named <c>&lt;Name&gt;_NN</c>, with the exported
    /// Unity transform as-is: the asset's gameplay prefab <c>PRF_&lt;Asset&gt;</c> under <c>_Gameplay/Level/&lt;Layer&gt;</c> when
    /// it exists (collectibles, enemies, doors become functional with no manual work), else its Visual prefab under
    /// <c>_Environment/&lt;Layer&gt;</c>. Objects whose model or Visual prefab is missing are skipped (slot empty, counted in
    /// <c>layout_missing</c>). Non-animated Visual instances are marked static. <c>Marker_*</c> objects (kind marker) become typed V57Markers under
    /// <c>_Environment/_Markers</c> via <see cref="MarkerConverter"/> (volume = 1 m cube scaled by the exported scale).
    /// </summary>
    public static class LayoutSceneBuilder
    {
        #region Fields

        private const string Step = "BuildLevelScenes";

        #endregion

        #region Public Methods

        /// <summary>Returns the number of placed instances; <paramref name="markers"/> receives the number of markers.</summary>
        public static int Populate(SceneContainers containers, LayoutDto layout, Scene scene, string sceneId, out int markers)
        {
            Transform environment = containers.Environment;
            markers = 0;
            Dictionary<string, GameObject> prefabsByModel = VisualPrefabsByModel();
            Dictionary<string, string> gameplayByName = GameplayPrefabLinker.GameplayPrefabsByName();
            Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
            Dictionary<string, Transform> actorGroups = new Dictionary<string, Transform>();
            int placed = 0;
            foreach (LayoutObjectDto item in layout.objects ?? new LayoutObjectDto[0])
            {
                if (item != null && item.kind == "marker")
                {
                    if (MarkerConverter.Create(containers, item.name, ToVector3(item.position, Vector3.zero), ToQuaternion(item.rotation),
                        ToVector3(item.scale, Vector3.one), MarkerConverter.UnitVolume, sceneId, item.shape == "box"))
                    {
                        markers++;
                    }

                    continue;
                }

                if (item == null || string.IsNullOrEmpty(item.model) || !prefabsByModel.TryGetValue(item.model, out GameObject prefab))
                {
                    AssemblyContext.Counts.layout_missing++;
                    AssemblyContext.Warn(Step, $"{layout.level_id}: '{item?.name}' ({item?.asset_id}) has no model/Visual prefab; slot left empty");
                    continue;
                }

                // One prefab per thing: a gameplay prefab PRF_<Asset> (code + nested Visual) wins over the Visual prefab.
                string gameplayPath = GameplayPrefabLinker.FindFor(AssetDatabase.GetAssetPath(prefab), gameplayByName);
                GameObject gameplay = gameplayPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPath) : null;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gameplay != null ? gameplay : prefab, scene);
                instance.name = item.name;
                Transform parent = gameplay != null ? Group(containers.LevelActors, actorGroups, item.layer) : Group(environment, groups, item.layer);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = ToVector3(item.position, Vector3.zero);
                instance.transform.localRotation = ToQuaternion(item.rotation);
                instance.transform.localScale = ToVector3(item.scale, Vector3.one);
                if (gameplay != null)
                {
                    AssemblyContext.Counts.layout_gameplay++;
                }
                else if (instance.GetComponentInChildren<Animator>(true) == null)
                {
                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.isStatic = true;
                    }
                }

                placed++;
            }

            return placed;
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, GameObject> VisualPrefabsByModel()
        {
            // Same deterministic model → Visual prefab map the prefab builder uses (no ambiguity between naming generations).
            Dictionary<string, GameObject> byModel = new Dictionary<string, GameObject>();
            foreach (KeyValuePair<string, string> pair in VisualPrefabPaths.ExistingByModel())
            {
                byModel.Add(pair.Key, AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value));
            }

            return byModel;
        }

        private static Transform Group(Transform environment, Dictionary<string, Transform> groups, string layer)
        {
            string name = string.IsNullOrWhiteSpace(layer) ? "Ungrouped" : layer;
            if (!groups.TryGetValue(name, out Transform group))
            {
                group = SceneContainers.CreateChild(environment, name);
                groups.Add(name, group);
            }

            return group;
        }

        private static Vector3 ToVector3(float[] values, Vector3 fallback)
        {
            return values != null && values.Length >= 3 ? new Vector3(values[0], values[1], values[2]) : fallback;
        }

        private static Quaternion ToQuaternion(float[] values)
        {
            return values != null && values.Length >= 4 ? new Quaternion(values[0], values[1], values[2], values[3]).normalized : Quaternion.identity;
        }

        #endregion
    }
}
