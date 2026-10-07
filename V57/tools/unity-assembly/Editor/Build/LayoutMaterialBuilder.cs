using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Applies the material values of the level-layout manifest (DCC export) to <c>MAT_&lt;Asset&gt;</c>: assets without a
    /// base-colour texture get a material from the authored flat colour; textured materials take the authored culling,
    /// alpha mode and (when no metallic/smoothness map exists) metallic + smoothness. These are provider values, not fillers.
    /// </summary>
    public static class LayoutMaterialBuilder
    {
        #region Fields

        private const string Step = "BuildMaterials";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            Shader lit = Shader.Find(AssemblyPaths.LitShaderName);
            foreach (LayoutDto layout in GeneratedData.Layouts?.layouts ?? new LayoutDto[0])
            {
                foreach (LayoutMaterialDto entry in layout?.materials ?? new LayoutMaterialDto[0])
                {
                    string model = ModelFor(layout, entry.asset_id);
                    if (model == null || lit == null)
                    {
                        continue;
                    }

                    string assetFolder = AssetFolderOf(model);
                    string assetName = Path.GetFileName(assetFolder);
                    string materialPath = $"{assetFolder}/Materials/MAT_{assetName}.mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    bool created = material == null;
                    if (created)
                    {
                        AssemblyPaths.EnsureFolder(assetFolder + "/Materials");
                        material = new Material(lit) { name = "MAT_" + assetName };
                        AssetDatabase.CreateAsset(material, materialPath);
                        AssemblyContext.Counts.materials++;
                    }

                    Apply(entry, material);
                    EditorUtility.SetDirty(material);
                    ModelMaterialRemapper.Remap(assetFolder, assetName, material);
                }
            }
        }

        #endregion

        #region Private Methods

        private static void Apply(LayoutMaterialDto entry, Material material)
        {
            if (material.GetTexture("_BaseMap") == null)
            {
                Texture2D albedo = string.IsNullOrEmpty(entry.albedo) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(entry.albedo);
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
                material.SetColor("_BaseColor", ToColor(entry.base_color));
            }

            if (material.GetTexture("_MetallicGlossMap") == null)
            {
                material.SetFloat("_Metallic", entry.metallic);
                material.SetFloat("_Smoothness", entry.smoothness);
            }

            material.SetFloat("_Cull", entry.double_sided ? 0f : 2f);
            material.doubleSidedGI = entry.double_sided;
            string mode = (entry.alpha_mode ?? "OPAQUE").ToUpperInvariant();
            bool clip = mode == "MASK" || mode == "CLIP";
            material.SetFloat("_AlphaClip", clip ? 1f : 0f);
            material.SetFloat("_Cutoff", entry.alpha_cutoff);
            if (clip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            if (mode == "BLEND")
            {
                AssemblyContext.Warn(Step, $"{material.name}: alpha_mode BLEND requested — transparent surface left to the material craft step (opaque kept)");
            }
        }

        private static string ModelFor(LayoutDto layout, string assetId)
        {
            foreach (LayoutObjectDto placed in layout.objects ?? new LayoutObjectDto[0])
            {
                if (placed != null && placed.asset_id == assetId && !string.IsNullOrEmpty(placed.model))
                {
                    return placed.model;
                }
            }

            return null;
        }

        private static string AssetFolderOf(string modelPath)
        {
            string folder = AssemblyPaths.ParentFolder(modelPath);
            return folder.EndsWith("/Meshes") ? AssemblyPaths.ParentFolder(folder) : folder;
        }

        private static Color ToColor(float[] rgba)
        {
            return rgba != null && rgba.Length >= 3 ? new Color(rgba[0], rgba[1], rgba[2], rgba.Length > 3 ? rgba[3] : 1f) : Color.white;
        }

        #endregion
    }
}
