using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Makes every <c>ANIM_&lt;Asset&gt;_&lt;Clip&gt;</c> use the rig of <c>SK_&lt;Asset&gt;</c> (same animation type,
    /// avatar copied from the skinned model). Done after import instead of in OnPreprocessModel because the SK_
    /// avatar may not be imported yet at that point.
    /// </summary>
    public static class AnimationAvatarLinker
    {
        #region Public Methods

        public static void LinkAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AssemblyPaths.ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetNaming.TrySplitAnimation(Path.GetFileNameWithoutExtension(path), out string assetName, out string clip))
                {
                    continue;
                }

                ModelImporter animation = AssetImporter.GetAtPath(path) as ModelImporter;
                string skinnedPath = FindSkinnedModel(assetName);
                ModelImporter skinned = skinnedPath != null ? AssetImporter.GetAtPath(skinnedPath) as ModelImporter : null;
                Avatar avatar = skinnedPath != null ? FindAvatar(skinnedPath) : null;
                if (animation == null || skinned == null || avatar == null)
                {
                    AssemblyContext.Warn("ApplyImportRules", $"{path}: no SK_{assetName} model/avatar to copy the rig from");
                    continue;
                }

                if (animation.animationType == skinned.animationType && animation.sourceAvatar == avatar)
                {
                    continue;
                }

                animation.animationType = skinned.animationType;
                animation.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                animation.sourceAvatar = avatar;
                animation.SaveAndReimport();
                ImportLog.Record(path, "animation-rig", $"{skinned.animationType} avatar copied from {skinnedPath} (clip {clip})");
            }
        }

        #endregion

        #region Private Methods

        private static string FindSkinnedModel(string assetName)
        {
            string wanted = "SK_" + assetName;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AssemblyPaths.ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == wanted)
                {
                    return path;
                }
            }

            return null;
        }

        private static Avatar FindAvatar(string modelPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        #endregion
    }
}
