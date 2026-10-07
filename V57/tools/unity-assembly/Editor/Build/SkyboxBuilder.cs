using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Each sky backdrop under <c>Art/Environment/Sky/</c> (lat-long PNG/JPG/EXR/HDR) → <c>Sky/Materials/MAT_&lt;Name&gt;.mat</c>
    /// with <c>Skybox/Panoramic</c> (360°, exposure 1, rotation 0). Level scenes use <see cref="DefaultSkybox"/> (first by name).
    /// Exposure, rotation and fog are art-direction values (M3 craft), not decided here.
    /// </summary>
    public static class SkyboxBuilder
    {
        #region Fields

        private const string Step = "BuildSkybox";
        private const string ShaderName = "Skybox/Panoramic";
        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".exr", ".hdr", ".tga", ".tif", ".tiff" };

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            List<string> skies = SkyTextures();
            if (skies.Count == 0)
            {
                return;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                AssemblyContext.Error(Step, $"shader '{ShaderName}' not found");
                return;
            }

            AssemblyPaths.EnsureFolder(AssemblyPaths.SkyRoot + "/Materials");
            foreach (string texturePath in skies)
            {
                string materialPath = MaterialPath(texturePath);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = Path.GetFileNameWithoutExtension(materialPath) };
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                material.shader = shader;
                material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetFloat("_Mapping", 1f);
                material.SetFloat("_ImageType", 0f);
                material.SetFloat("_MirrorOnBack", 0f);
                EditorUtility.SetDirty(material);
                AssemblyContext.Counts.skyboxes++;
                AssemblyContext.TrackAsset(materialPath);
            }

            if (skies.Count > 1)
            {
                AssemblyContext.Warn(Step, $"{skies.Count} sky textures; level scenes use {Path.GetFileName(skies[0])} (per-scene skies belong to the ADD/M3 craft)");
            }
        }

        /// <summary>Skybox material for level scenes, or null when the delivery has no sky.</summary>
        public static Material DefaultSkybox()
        {
            List<string> skies = SkyTextures();
            return skies.Count > 0 ? AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(skies[0])) : null;
        }

        #endregion

        #region Private Methods

        private static List<string> SkyTextures()
        {
            List<string> paths = new List<string>();
            if (!AssetDatabase.IsValidFolder(AssemblyPaths.SkyRoot))
            {
                return paths;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { AssemblyPaths.SkyRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Materials/") && System.Array.IndexOf(Extensions, Path.GetExtension(path).ToLowerInvariant()) >= 0)
                {
                    paths.Add(path);
                }
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths;
        }

        private static string MaterialPath(string texturePath)
        {
            return $"{AssemblyPaths.SkyRoot}/Materials/MAT_{Path.GetFileNameWithoutExtension(texturePath)}.mat";
        }

        #endregion
    }
}
