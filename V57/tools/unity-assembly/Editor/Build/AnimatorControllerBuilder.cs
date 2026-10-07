using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// One <c>Data/Animation/AC_&lt;Asset&gt;.controller</c> per <c>SK_&lt;Asset&gt;</c> that has <c>ANIM_&lt;Asset&gt;_*</c> clips:
    /// every clip is a state, the default state is a looping idle (else the first looping clip, else the first clip).
    /// No transitions or parameters — gameplay specs own them. Existing controllers are kept unless Force.
    /// </summary>
    public static class AnimatorControllerBuilder
    {
        #region Fields

        private const string Step = "BuildAnimators";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            foreach (KeyValuePair<string, List<AnimationClip>> pair in CollectClips())
            {
                string path = ControllerPath(pair.Key);
                if (pair.Value.Count == 0)
                {
                    AssemblyContext.Warn(Step, $"ANIM_{pair.Key}_*: no animation clips imported; {path} not built");
                    continue;
                }

                if (File.Exists(AssemblyPaths.ToFullPath(path)) && !AssemblyOptions.Force)
                {
                    AssemblyContext.Counts.skipped_existing++;
                    AssemblyContext.TrackAsset(path);
                    continue;
                }

                AssemblyPaths.EnsureFolder(AssemblyPaths.AnimationRoot);
                AssetDatabase.DeleteAsset(path);
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                List<AnimationClip> clips = pair.Value;
                clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                AnimationClip preferred = PickDefault(clips);
                for (int i = 0; i < clips.Count; i++)
                {
                    AnimatorState state = machine.AddState(clips[i].name, new Vector3(300f, 60f * i, 0f));
                    state.motion = clips[i];
                    if (clips[i] == preferred)
                    {
                        machine.defaultState = state;
                    }
                }

                EditorUtility.SetDirty(controller);
                AssemblyContext.Counts.animator_controllers++;
                AssemblyContext.TrackAsset(path);
                Debug.Log($"V57.Assembly: {path} ← {clips.Count} clips, default '{preferred.name}'");
            }
        }

        public static string ControllerPath(string assetName)
        {
            return $"{AssemblyPaths.AnimationRoot}/AC_{assetName}.controller";
        }

        /// <summary>Looping clip named *idle* → first looping clip → first clip.</summary>
        public static AnimationClip PickDefault(IList<AnimationClip> clips)
        {
            int index = PickDefaultIndex(Names(clips), Loops(clips));
            return index >= 0 ? clips[index] : null;
        }

        /// <summary>Pure selection rule (unit-tested): looping idle → first looping → first.</summary>
        public static int PickDefaultIndex(IList<string> names, IList<bool> loops)
        {
            int firstLoop = -1;
            for (int i = 0; i < names.Count; i++)
            {
                if (loops[i] && names[i].IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }

                firstLoop = firstLoop < 0 && loops[i] ? i : firstLoop;
            }

            return firstLoop >= 0 ? firstLoop : names.Count > 0 ? 0 : -1;
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, List<AnimationClip>> CollectClips()
        {
            Dictionary<string, List<AnimationClip>> byAsset = new Dictionary<string, List<AnimationClip>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AssemblyPaths.ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetNaming.TrySplitAnimation(Path.GetFileNameWithoutExtension(path), out string assetName, out _))
                {
                    continue;
                }

                if (!byAsset.TryGetValue(assetName, out List<AnimationClip> clips))
                {
                    clips = new List<AnimationClip>();
                    byAsset.Add(assetName, clips);
                }

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                    {
                        clips.Add(clip);
                    }
                }
            }

            return byAsset;
        }

        private static List<string> Names(IList<AnimationClip> clips)
        {
            List<string> names = new List<string>();
            foreach (AnimationClip clip in clips)
            {
                names.Add(clip.name);
            }

            return names;
        }

        private static List<bool> Loops(IList<AnimationClip> clips)
        {
            List<bool> loops = new List<bool>();
            foreach (AnimationClip clip in clips)
            {
                loops.Add(clip.isLooping);
            }

            return loops;
        }

        #endregion
    }
}
