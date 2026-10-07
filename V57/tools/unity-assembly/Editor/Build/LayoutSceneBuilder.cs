using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Places a normalized level layout (<c>layouts.json</c>) in a level scene: one Visual prefab instance per object under
    /// <c>_Environment/&lt;Layer&gt;</c>, named <c>&lt;Name&gt;_NN</c>, with the exported Unity transform as-is. Objects whose
    /// model or Visual prefab is missing are skipped (slot empty, counted in <c>layout_missing</c>). Non-animated
    /// instances are marked static.
    /// </summary>
    public static class LayoutSceneBuilder
    {
        #region Fields

        private const string Step = "BuildLevelScenes";

        #endregion

        #region Public Methods

        /// <summary>Returns the number of placed instances.</summary>
        public static int Populate(Transform environment, LayoutDto layout, Scene scene)
        {
            Dictionary<string, GameObject> prefabsByModel = VisualPrefabsByModel();
            Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
            int placed = 0;
            foreach (LayoutObjectDto item in layout.objects ?? new LayoutObjectDto[0])
            {
                if (item == null || string.IsNullOrEmpty(item.model) || !prefabsByModel.TryGetValue(item.model, out GameObject prefab))
                {
                    AssemblyContext.Counts.layout_missing++;
                    AssemblyContext.Warn(Step, $"{layout.level_id}: '{item?.name}' ({item?.asset_id}) has no model/Visual prefab; slot left empty");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = item.name;
                instance.transform.SetParent(Group(environment, groups, item.layer), false);
                instance.transform.localPosition = ToVector3(item.position, Vector3.zero);
                instance.transform.localRotation = ToQuaternion(item.rotation);
                instance.transform.localScale = ToVector3(item.scale, Vector3.one);
                if (instance.GetComponentInChildren<Animator>(true) == null)
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
            Dictionary<string, GameObject> byModel = new Dictionary<string, GameObject>();
            if (!AssetDatabase.IsValidFolder(AssemblyPaths.VisualPrefabRoot))
            {
                return byModel;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AssemblyPaths.VisualPrefabRoot }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (Transform child in prefab.transform)
                {
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject);
                    string modelPath = source != null ? AssetDatabase.GetAssetPath(source) : null;
                    if (!string.IsNullOrEmpty(modelPath) && !byModel.ContainsKey(modelPath))
                    {
                        byModel.Add(modelPath, prefab);
                    }
                }
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
