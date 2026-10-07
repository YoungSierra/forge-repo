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
    /// Builds <c>Assets/_Game/Scenes/SCN_&lt;id&gt;.unity</c> for slice scenes and layout scenes in <c>scenes.json</c>: six root
    /// containers; level art from the LevelMaps layout (<see cref="LayoutSceneBuilder"/>) or the blockout instance with typed
    /// markers and static dressing (<see cref="MarkerDresser"/>); cameras from markers + camera.json; one directional light;
    /// the delivered sky (<see cref="SkyboxBuilder"/>). No placeholder geometry: a scene without level art stays empty.
    /// Existing scenes are never overwritten unless <see cref="AssemblyOptions.Force"/> (protects gameplay work).
    /// Refuses to run in Play Mode or with unsaved open scenes (would lose user changes).
    /// </summary>
    public static class LevelSceneBuilder
    {
        #region Fields

        private const string Step = "BuildLevelScenes";
        private const string RebuildStep = "RebuildLevelContent";

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

        /// <summary>
        /// New or changed level delivery: in each existing level scene, replaces only the level content (everything under
        /// <c>_Environment</c> and <c>_Gameplay/Level</c>) from the current layout or blockout. The player, systems, UI,
        /// cameras and lighting placed during implementation are kept. Scenes that do not exist yet are built normally.
        /// </summary>
        public static void RebuildContentAll()
        {
            ScenesDto scenes = GeneratedData.Scenes;
            if (scenes?.scenes == null || scenes.scenes.Length == 0)
            {
                AssemblyContext.Error(RebuildStep, "scenes.json missing or empty");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || HasDirtyScene())
            {
                AssemblyContext.Error(RebuildStep, "Editor is in Play Mode or has unsaved scenes; save/stop first (nothing was changed)");
                return;
            }

            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (SceneEntryDto entry in SelectSliceScenes(scenes.scenes))
                {
                    string path = V57SceneNaming.ToScenePath(entry.id);
                    if (!VisualPrefabPaths.Exists(path))
                    {
                        BuildScene(entry);
                        continue;
                    }

                    RebuildContent(entry, path);
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

        private static void RebuildContent(SceneEntryDto entry, string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SceneContainers containers = SceneContainers.FromScene(scene);
            int removed = containers.ClearLevelContent();
            AssemblyOptions.LevelContentOnly = true;
            try
            {
                LevelArtPlacer.Place(entry, containers, scene);
            }
            finally
            {
                AssemblyOptions.LevelContentOnly = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                AssemblyContext.Error(RebuildStep, $"{path}: SaveScene failed");
                return;
            }

            AssemblyContext.Counts.scenes++;
            AssemblyContext.TrackAsset(path);
            Debug.Log($"V57.Assembly: {path} level content rebuilt ({removed} old objects replaced; gameplay setup kept)");
        }

        private static List<SceneEntryDto> SelectSliceScenes(SceneEntryDto[] all)
        {
            List<SceneEntryDto> selected = new List<SceneEntryDto>();
            string[] packageSlice = GeneratedData.Package?.slice?.scenes ?? new string[0];
            foreach (SceneEntryDto entry in all)
            {
                if (entry != null && (entry.slice || !string.IsNullOrEmpty(entry.layout) || System.Array.IndexOf(packageSlice, entry.id) >= 0))
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
            LevelArtPlacer.Place(entry, containers, scene);
            CameraPlacement.AssignMain(containers.Cameras, entry);
            Material sky = SkyboxBuilder.DefaultSkybox();
            if (sky != null)
            {
                RenderSettings.skybox = sky;
            }

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
