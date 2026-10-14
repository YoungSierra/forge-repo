using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// LOD levels delivered as separate files next to the model: <c>SK_X.fbx</c> + <c>SK_XLOD01.fbx</c>, <c>SK_X_LOD2.fbx</c>…
    /// (one rig exported per level, same bone names). The level renderers move into the main model instance and skinned
    /// ones are rebound to its skeleton by bone name, so one Animator drives every level. A LODGroup is built with the
    /// main model as LOD0 and the files in level order. The level files get no prefab of their own.
    /// </summary>
    public static class SiblingLodFiles
    {
        #region Fields

        private const string Step = "BuildVisualPrefabs";
        private static readonly Regex LevelSuffix = new Regex(@"^(?<stem>.+?)_?LOD(?<level>\d+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        #endregion

        #region Public Methods

        /// <summary>Level files of <paramref name="modelPath"/> in level order (empty when none).</summary>
        public static List<string> Find(string modelPath)
        {
            string folder = Path.GetDirectoryName(modelPath)?.Replace('\\', '/');
            string stem = Path.GetFileNameWithoutExtension(modelPath);
            SortedDictionary<int, string> levels = new SortedDictionary<int, string>();
            if (folder == null || LevelSuffix.IsMatch(stem))
            {
                return new List<string>(levels.Values);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != folder)
                {
                    continue;
                }

                Match match = LevelSuffix.Match(Path.GetFileNameWithoutExtension(path));
                if (match.Success && string.Equals(match.Groups["stem"].Value, stem, System.StringComparison.OrdinalIgnoreCase))
                {
                    int level = int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture);
                    if (level > 0 && !levels.ContainsKey(level))
                    {
                        levels.Add(level, path);
                    }
                }
            }

            return new List<string>(levels.Values);
        }

        /// <summary>
        /// Moves the level files' renderers into <paramref name="modelInstance"/> and builds the LODGroup on
        /// <paramref name="root"/>. Returns the number of LOD levels (0 when the model has no level files).
        /// </summary>
        public static int Attach(GameObject root, GameObject modelInstance, string modelPath)
        {
            List<string> files = Find(modelPath);
            if (files.Count == 0)
            {
                return 0;
            }

            Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
            foreach (Transform bone in modelInstance.GetComponentsInChildren<Transform>(true))
            {
                if (!bones.ContainsKey(bone.name))
                {
                    bones.Add(bone.name, bone);
                }
            }

            List<Renderer[]> levels = new List<Renderer[]> { modelInstance.GetComponentsInChildren<Renderer>(true) };
            foreach (string file in files)
            {
                GameObject level = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file));
                PrefabUtility.UnpackPrefabInstance(level, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                level.transform.SetParent(root.transform, false);
                List<Renderer> moved = new List<Renderer>();
                foreach (Renderer renderer in level.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is SkinnedMeshRenderer skinned && !Rebind(skinned, bones, file))
                    {
                        continue;
                    }

                    renderer.transform.SetParent(modelInstance.transform, true);
                    moved.Add(renderer);
                }

                Object.DestroyImmediate(level);
                levels.Add(moved.ToArray());
            }

            LodGroupBuilder.ApplyLevels(root, levels);
            AssemblyContext.Warn(Step, $"{modelPath}: {files.Count} LOD level(s) delivered as separate files, merged into one prefab and rig (provider fix: export the levels as _LOD# nodes of one file)");
            return levels.Count;
        }

        #endregion

        #region Private Methods

        private static bool Rebind(SkinnedMeshRenderer skinned, Dictionary<string, Transform> bones, string file)
        {
            Transform[] mapped = new Transform[skinned.bones.Length];
            for (int i = 0; i < mapped.Length; i++)
            {
                Transform source = skinned.bones[i];
                if (source == null || !bones.TryGetValue(source.name, out mapped[i]))
                {
                    AssemblyContext.Warn(Step, $"{file}: bone '{source?.name}' is not in the main rig; level renderer '{skinned.name}' skipped");
                    return false;
                }
            }

            skinned.bones = mapped;
            if (skinned.rootBone != null && bones.TryGetValue(skinned.rootBone.name, out Transform rootBone))
            {
                skinned.rootBone = rootBone;
            }

            return true;
        }

        #endregion
    }
}
