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
    /// <c>layout_missing</c>). Non-animated Visual instances are marked static unless the layout marks them dynamic. <c>Marker_*</c> objects (kind marker) become typed V57Markers under
    /// <c>_Environment/_Markers</c> via <see cref="MarkerConverter"/> (volume = 1 m cube scaled by the exported scale).
    /// unity_scene/1.1 extras: marker actors (a named gameplay prefab, <c>prefab</c>) under <c>_Gameplay/Level/&lt;Layer&gt;</c>,
    /// DCC <c>props</c> kept on the object (<c>V57LayoutProps</c>), and paths under <c>_Environment/_Paths</c> (<c>V57Path</c>).
    /// </summary>
    public static class LayoutSceneBuilder
    {
        #region Fields

        private const string AttachmentsGroup = "_Attachments";

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
            Dictionary<string, Transform> attachGroups = new Dictionary<string, Transform>();
            Dictionary<string, AssetAttachDto[]> attachByModel = AttachmentsByModel();
            int placed = 0;
            foreach (LayoutObjectDto item in layout.objects ?? new LayoutObjectDto[0])
            {
                if (item != null && item.kind == "marker")
                {
                    if (MarkerConverter.Create(containers, item.name, ToVector3(item.position, Vector3.zero), ToQuaternion(item.rotation),
                        ToVector3(item.scale, Vector3.one), MarkerConverter.UnitVolume, sceneId, item.shape == "box"))
                    {
                        markers++;
                        AttachProps(containers.Markers.Find(item.name), item.props);
                    }

                    continue;
                }

                // Marker actors (layout_aliases marker_actors): a named gameplay prefab, no model lookup.
                if (item != null && !string.IsNullOrEmpty(item.prefab))
                {
                    GameObject actorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefab);
                    if (actorPrefab == null)
                    {
                        AssemblyContext.Counts.layout_missing++;
                        AssemblyContext.Warn(Step, $"{layout.level_id}: '{item.name}' ({item.asset_id}) prefab '{item.prefab}' not found; slot left empty");
                        continue;
                    }

                    GameObject actor = (GameObject)PrefabUtility.InstantiatePrefab(actorPrefab, scene);
                    actor.name = item.name;
                    actor.transform.SetParent(Group(containers.LevelActors, actorGroups, item.layer), false);
                    actor.transform.localPosition = ToVector3(item.position, Vector3.zero);
                    actor.transform.localRotation = ToQuaternion(item.rotation);
                    actor.transform.localScale = ToVector3(item.scale, Vector3.one);
                    AttachProps(actor.transform, item.props);
                    AssemblyContext.Counts.layout_gameplay++;
                    placed++;
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
                // A gameplay prefab with no scripts (collision only: walls, floors, crates) is scenery: environment, static.
                bool solid = gameplay != null && !HasGameScripts(gameplay);
                if (solid)
                {
                    gameplay = null;
                }

                Transform parent = gameplay != null ? Group(containers.LevelActors, actorGroups, item.layer) : Group(environment, groups, item.layer);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = ToVector3(item.position, Vector3.zero);
                instance.transform.localRotation = ToQuaternion(item.rotation);
                instance.transform.localScale = ToVector3(item.scale, Vector3.one);
                AttachProps(instance.transform, item.props);
                if (gameplay != null)
                {
                    AssemblyContext.Counts.layout_gameplay++;
                }
                else if (!item.dynamic && instance.GetComponentInChildren<Animator>(true) == null)
                {
                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.isStatic = true;
                    }
                }

                PlaceAttachments(environment, attachGroups, attachByModel, item.model, instance.transform);
                placed++;
            }

            PlacePaths(environment, layout);
            return placed;
        }

        #endregion

        #region Private Methods

        /// <summary>Docs/V57/asset_rules.json <c>attach</c> lists by model path.</summary>
        private static Dictionary<string, AssetAttachDto[]> AttachmentsByModel()
        {
            Dictionary<string, AssetAttachDto[]> byModel = new Dictionary<string, AssetAttachDto[]>();
            foreach (AssetEntryDto entry in GeneratedData.AssetManifest?.assets ?? new AssetEntryDto[0])
            {
                string mesh = entry?.files?.mesh;
                if (!string.IsNullOrEmpty(mesh) && entry.attach != null && entry.attach.Length > 0)
                {
                    byModel[mesh] = entry.attach;
                }
            }

            return byModel;
        }

        /// <summary>
        /// Companion prefabs of the instance's asset (effects, lights, sounds), placed under
        /// <c>_Environment/_Attachments/&lt;Prefab&gt;</c> at the instance position + offset: unscaled, not static, and rebuilt with
        /// the layout, so a new map carries them without manual work.
        /// </summary>
        private static void PlaceAttachments(Transform environment, Dictionary<string, Transform> groups, Dictionary<string, AssetAttachDto[]> byModel, string model, Transform instance)
        {
            if (string.IsNullOrEmpty(model) || !byModel.TryGetValue(model, out AssetAttachDto[] attachments))
            {
                return;
            }

            foreach (AssetAttachDto attach in attachments)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(attach.prefab);
                if (prefab == null)
                {
                    AssemblyContext.Warn(Step, $"{instance.name}: attach prefab '{attach.prefab}' not found; nothing attached");
                    continue;
                }

                if (!groups.TryGetValue(prefab.name, out Transform group))
                {
                    Transform root = environment.Find(AttachmentsGroup);
                    if (root == null)
                    {
                        root = SceneContainers.CreateChild(environment, AttachmentsGroup);
                    }

                    group = SceneContainers.CreateChild(root, prefab.name);
                    groups.Add(prefab.name, group);
                }

                bool aligned = attach.align == "instance";
                Vector3 offset = ToVector3(attach.offset, Vector3.zero);
                GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.gameObject.scene);
                child.name = $"{prefab.name}_{instance.name}";
                child.transform.SetParent(group, false);
                child.transform.SetPositionAndRotation(instance.position + (aligned ? instance.rotation * offset : offset), aligned ? instance.rotation : Quaternion.identity);
                child.transform.localScale = Vector3.one;
            }
        }

        /// <summary>Scripts other than Unity's own (e.g. URP light/camera data on a lamp in the art).</summary>
        private static bool HasGameScripts(GameObject prefab)
        {
            foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string space = behaviour != null ? behaviour.GetType().Namespace ?? string.Empty : string.Empty;
                if (behaviour != null && !space.StartsWith("UnityEngine", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

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

        /// <summary>DCC custom props on the scene object (<see cref="V57.GoldPath.V57LayoutProps"/>); nothing when there are none.</summary>
        private static void AttachProps(Transform target, LayoutPropDto[] props)
        {
            if (target == null || props == null || props.Length == 0)
            {
                return;
            }

            V57.GoldPath.V57LayoutProps component = target.GetComponent<V57.GoldPath.V57LayoutProps>();
            if (component == null)
            {
                component = target.gameObject.AddComponent<V57.GoldPath.V57LayoutProps>();
            }

            List<V57.GoldPath.V57LayoutProps.Entry> entries = new List<V57.GoldPath.V57LayoutProps.Entry>();
            foreach (LayoutPropDto prop in props)
            {
                if (prop != null && !string.IsNullOrEmpty(prop.key))
                {
                    entries.Add(new V57.GoldPath.V57LayoutProps.Entry { key = prop.key, value = prop.value ?? string.Empty });
                }
            }

            component.Configure(entries);
        }

        /// <summary>unity_scene/1.1 paths → <c>_Environment/_Paths/&lt;Name&gt;</c> with a <see cref="V57.GoldPath.V57Path"/> (world-space points).</summary>
        private static void PlacePaths(Transform environment, LayoutDto layout)
        {
            if (layout.paths == null || layout.paths.Length == 0)
            {
                return;
            }

            Transform root = SceneContainers.CreateChild(environment, "_Paths");
            foreach (LayoutPathDto path in layout.paths)
            {
                if (path == null || path.points == null || path.points.Length < 3)
                {
                    continue;
                }

                Vector3[] points = new Vector3[path.points.Length / 3];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new Vector3(path.points[i * 3], path.points[i * 3 + 1], path.points[i * 3 + 2]);
                }

                Transform child = SceneContainers.CreateChild(root, path.name);
                child.position = points[0];
                V57.GoldPath.V57Path component = child.gameObject.AddComponent<V57.GoldPath.V57Path>();
                component.Configure(path.kind, path.actor, path.loop, path.mode, path.speed, path.zone, points, path.instances);
                AttachProps(child, path.props);
            }
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
