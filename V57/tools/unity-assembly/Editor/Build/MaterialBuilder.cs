using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Creates/updates <c>&lt;AssetFolder&gt;/Materials/MAT_&lt;Asset&gt;.mat</c> (URP Lit) from the asset's textures and remaps
    /// the asset's FBX to it. Texture roles come from contract names (<c>T_&lt;Asset&gt;_BC|N|ORM|MS|E|Mask</c>) or DCC
    /// aliases (<c>*_albedo|_normal|_MetallicSmoothness|_metallic|_roughness|_ao|_emission</c>, asset = folder name).
    /// </summary>
    public static class MaterialBuilder
    {
        #region Fields

        private const string Step = "BuildMaterials";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            Shader lit = Shader.Find(AssemblyPaths.LitShaderName);
            if (lit == null)
            {
                AssemblyContext.Error(Step, $"shader '{AssemblyPaths.LitShaderName}' not found — is URP installed and assigned?");
                return;
            }

            foreach (MaterialTextureSet set in CollectTextureSets().Values)
            {
                if (set.BaseColor == null)
                {
                    AssemblyContext.Warn(Step, $"{set.AssetFolder}: no base colour texture for '{set.AssetName}'; material left to the layout manifest or FBX");
                    continue;
                }

                Material material = BuildMaterial(set, lit);
                ModelMaterialRemapper.Remap(set.AssetFolder, set.AssetName, material);
            }
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, MaterialTextureSet> CollectTextureSets()
        {
            Dictionary<string, MaterialTextureSet> sets = new Dictionary<string, MaterialTextureSet>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { AssemblyPaths.ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/UI/") || path.Contains("/Materials/") || path.Contains("/Environment/Sky/"))
                {
                    continue;
                }

                string stem = Path.GetFileNameWithoutExtension(path);
                string textureFolder = AssemblyPaths.ParentFolder(path);
                string assetFolder = textureFolder.EndsWith("/Textures") ? AssemblyPaths.ParentFolder(textureFolder) : textureFolder;
                if (!AssetNaming.TrySplitTexture(stem, out string assetName, out string suffix))
                {
                    if (!AssetNaming.TrySplitDccTexture(stem, out suffix))
                    {
                        continue;
                    }

                    assetName = Path.GetFileName(assetFolder);
                }

                string key = assetFolder + "|" + assetName;
                if (!sets.TryGetValue(key, out MaterialTextureSet set))
                {
                    set = new MaterialTextureSet(assetName, assetFolder);
                    sets.Add(key, set);
                }

                Assign(set, suffix.ToUpperInvariant(), AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }

            return sets;
        }

        private static void Assign(MaterialTextureSet set, string suffix, Texture2D texture)
        {
            switch (suffix)
            {
                case "BC": set.BaseColor = texture; break;
                case "N": set.Normal = texture; break;
                case "ORM": set.Orm = texture; break;
                case "MS": set.MetallicSmoothness = texture; break;
                case "M": set.Metallic = texture; break;
                case "R": set.Roughness = texture; break;
                case "AO": set.Occlusion = texture; break;
                case "E": set.Emission = texture; break;
                case "MASK": set.Mask = texture; break;
            }
        }

        private static Material BuildMaterial(MaterialTextureSet set, Shader lit)
        {
            AssemblyPaths.EnsureFolder(set.AssetFolder + "/Materials");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(set.MaterialPath);
            if (material == null)
            {
                material = new Material(lit) { name = "MAT_" + set.AssetName };
                AssetDatabase.CreateAsset(material, set.MaterialPath);
            }
            else if (material.shader != lit)
            {
                material.shader = lit;
            }

            material.SetTexture("_BaseMap", set.BaseColor);
            material.SetTexture("_MainTex", set.BaseColor);
            material.SetColor("_BaseColor", Color.white);
            if (set.Normal != null)
            {
                material.SetTexture("_BumpMap", set.Normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }

            ApplySurfaceMaps(set, material);
            if (set.Emission != null)
            {
                material.SetTexture("_EmissionMap", set.Emission);
                material.SetColor("_EmissionColor", Color.white);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            if (set.Mask != null)
            {
                AssemblyContext.Warn(Step, $"T_{set.AssetName}_Mask not wired (game-specific use; linear import only)");
            }

            EditorUtility.SetDirty(material);
            AssemblyContext.Counts.materials++;
            return material;
        }

        /// <summary>Metallic/smoothness and occlusion: MS mask as delivered → ORM repack → metallic + roughness pack.</summary>
        private static void ApplySurfaceMaps(MaterialTextureSet set, Material material)
        {
            Texture2D metallicGloss = set.MetallicSmoothness;
            Texture2D occlusion = set.Occlusion;
            if (metallicGloss == null && set.Orm != null)
            {
                metallicGloss = OrmPacker.Pack(set.Orm, set.AssetFolder + "/Materials", set.AssetName);
                occlusion = metallicGloss;
                AssemblyContext.Counts.orm_packed_textures += metallicGloss != null ? 1 : 0;
            }
            else if (metallicGloss == null && (set.Metallic != null || set.Roughness != null))
            {
                metallicGloss = MetallicSmoothnessPacker.Pack(set.Metallic, set.Roughness, set.AssetFolder + "/Materials", set.AssetName);
                AssemblyContext.Counts.metallic_smoothness_packed += metallicGloss != null ? 1 : 0;
            }

            if (metallicGloss != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGloss);
                material.SetFloat("_Smoothness", 1f);
                material.SetFloat("_SmoothnessTextureChannel", 0f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else if (set.Orm != null || set.Metallic != null || set.Roughness != null)
            {
                AssemblyContext.Warn(Step, $"{set.AssetName}: metallic/smoothness maps could not be packed (GPU readback failed?) — defaults kept");
            }

            if (occlusion != null)
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 1f);
                material.EnableKeyword("_OCCLUSIONMAP");
            }
        }

        #endregion
    }
}
