using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Builds <c>Assets/_Game/Scenes/SCN_&lt;id&gt;.unity</c> for slice scenes in <c>scenes.json</c>: six root containers,
    /// blockout instance under <c>_Environment</c>, typed markers, static dressing at spawn markers (<see cref="MarkerDresser"/>), cameras from markers + camera.json, one directional
    /// light. Existing scenes are never overwritten unless <see cref="AssemblyOptions.Force"/> (protects gameplay work).
    /// Refuses to run in Play Mode or with unsaved open scenes (would lose user changes).
    /// </summary>
    public static class LevelSceneBuilder
    {
        #region Fields

        private const string Step = "BuildLevelScenes";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            ScenesDto scenes = GeneratedData.Scenes;
            if (scenes?.scenes == null || scenes.scenes.Length == 0)
            {
                AssemblyContext.Error(Step, "scenes.json missing or empty");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || HasDirtyScene())
            {
                AssemblyContext.Error(Step, "Editor is in Play Mode or has unsaved scenes; save/stop first (nothing was changed)");
                return;
            }

            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (SceneEntryDto entry in SelectSliceScenes(scenes.scenes))
                {
                    BuildScene(entry);
                }
            }
            finally
            {
                if (previous.Length > 0 && System.Array.TrueForAll(previous, setup => !string.IsNullOrEmpty(setup.path)))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
                }
            }
        }

        #endregion

        #region Private Methods

        private static List<SceneEntryDto> SelectSliceScenes(SceneEntryDto[] all)
        {
            List<SceneEntryDto> selected = new List<SceneEntryDto>();
            string[] packageSlice = GeneratedData.Package?.slice?.scenes ?? new string[0];
            foreach (SceneEntryDto entry in all)
            {
                if (entry != null && (entry.slice || System.Array.IndexOf(packageSlice, entry.id) >= 0))
                {
                    selected.Add(entry);
                }
            }

            if (selected.Count == 0)
            {
                AssemblyContext.Warn(Step, "no scene flagged slice; building every scene that declares a blockout");
                foreach (SceneEntryDto entry in all)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.blockout))
                    {
                        selected.Add(entry);
                    }
                }
            }

            return selected;
        }

        private static void BuildScene(SceneEntryDto entry)
        {
            string path = V57SceneNaming.ToScenePath(entry.id);
            if (VisualPrefabPaths.Exists(path) && !AssemblyOptions.Force)
            {
                AssemblyContext.Counts.skipped_existing++;
                AssemblyContext.TrackAsset(path);
                AssemblyContext.Warn(Step, $"{path} exists; not regenerated (use RunAllForce to rebuild)");
                return;
            }

            AssemblyPaths.EnsureFolder(V57SceneNaming.ScenesFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneContainers containers = SceneContainers.Create();
            CreateSun(containers.Lighting);
            GameObject source = BlockoutResolver.Resolve(entry.blockout, out string blockoutName);
            if (string.IsNullOrWhiteSpace(entry.blockout))
            {
                PlaceholderGround.Create(containers.Environment, entry.id);
            }
            else if (source == null)
            {
                AssemblyContext.Error(Step, $"{entry.id}: blockout '{entry.blockout}' not found (no model, prefab or placeholder)");
            }
            else
            {
                GameObject blockout = PrefabUtility.InstantiatePrefab(source, scene) as GameObject;
                blockout.transform.SetParent(containers.Environment, false);
                blockout.name = blockoutName;
                int markers = MarkerConverter.Convert(blockout, containers, entry.id);
                AssemblyContext.Counts.markers += markers;
                CheckDeclaredMarkers(entry, containers.Markers);
                AssemblyContext.Counts.dressed += MarkerDresser.Dress(containers, entry.id);
            }

            CameraPlacement.AssignMain(containers.Cameras, entry);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                AssemblyContext.Error(Step, $"{path}: SaveScene failed");
                return;
            }

            AssemblyContext.Counts.scenes++;
            AssemblyContext.TrackAsset(path);
            AddToBuildSettings(path);
        }

        private static void CreateSun(Transform lighting)
        {
            GameObject sun = new GameObject("Directional Light");
            sun.transform.SetParent(lighting, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
        }

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

        private static bool HasDirtyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        #endregion
    }
}
