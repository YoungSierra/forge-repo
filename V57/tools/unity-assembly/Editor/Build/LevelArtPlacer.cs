using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Puts a scene's level art under <c>_Environment</c>: the LevelMaps layout when the scene has one, else the
    /// <c>BLK_</c> blockout with its markers and marker dressing. Without either the scene keeps no level art (no placeholder).
    /// </summary>
    public static class LevelArtPlacer
    {
        #region Fields

        private const string Step = "BuildLevelScenes";

        #endregion

        #region Public Methods

        public static void Place(SceneEntryDto entry, SceneContainers containers, Scene scene)
        {
            if (!string.IsNullOrEmpty(entry.layout))
            {
                LayoutDto layout = GeneratedData.FindLayout(entry.layout);
                if (layout == null)
                {
                    AssemblyContext.Error(Step, $"{entry.id}: layout '{entry.layout}' not found in layouts.json");
                    return;
                }

                AssemblyContext.Counts.layout_instances += LayoutSceneBuilder.Populate(containers.Environment, layout, scene);
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.blockout))
            {
                AssemblyContext.Warn(Step, $"{entry.id}: no blockout or layout; scene has no level art (listed as missing by intake)");
                return;
            }

            GameObject source = BlockoutResolver.Resolve(entry.blockout, out string blockoutName);
            if (source == null)
            {
                AssemblyContext.Error(Step, $"{entry.id}: blockout '{entry.blockout}' not found (no model or prefab)");
                return;
            }

            GameObject blockout = PrefabUtility.InstantiatePrefab(source, scene) as GameObject;
            blockout.transform.SetParent(containers.Environment, false);
            blockout.name = blockoutName;
            AssemblyContext.Counts.markers += MarkerConverter.Convert(blockout, containers, entry.id);
            CheckDeclaredMarkers(entry, containers.Markers);
            AssemblyContext.Counts.dressed += MarkerDresser.Dress(containers, entry.id);
        }

        #endregion

        #region Private Methods

        private static void CheckDeclaredMarkers(SceneEntryDto entry, Transform markersRoot)
        {
            foreach (string declared in entry.markers ?? new string[0])
            {
                string wanted = V57MarkerNaming.IsMarker(declared) ? declared : V57MarkerNaming.Prefix + declared;
                if (markersRoot.Find(wanted) == null)
                {
                    AssemblyContext.Warn(Step, $"{entry.id}: declared marker '{wanted}' not found in blockout");
                }
            }
        }

        #endregion
    }
}
