using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Visual prefabs of rigged models (<c>SK_&lt;Asset&gt;</c>) get an Animator with <c>AC_&lt;Asset&gt;</c> and the model's avatar.
    /// Root motion stays off; locomotion code owns movement.
    /// </summary>
    public static class AnimatorBinder
    {
        #region Public Methods

        /// <summary>Returns true when an Animator was bound on <paramref name="modelInstance"/>.</summary>
        public static bool Bind(GameObject modelInstance, string modelPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            string assetName = AssetNaming.StripPrefix(Path.GetFileNameWithoutExtension(modelPath));
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerBuilder.ControllerPath(assetName));
            if (importer == null || importer.animationType == ModelImporterAnimationType.None || controller == null)
            {
                return false;
            }

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = FindAvatar(modelPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return true;
        }

        #endregion

        #region Private Methods

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
