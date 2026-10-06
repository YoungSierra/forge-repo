using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Creates/updates <c>&lt;AssetFolder&gt;/Materials/MAT_&lt;Asset&gt;.mat</c> (URP Lit) from <c>T_&lt;Asset&gt;_*</c>
    /// textures and remaps the models' embedded material to it. ORM is repacked (see <see cref="OrmPacker"/>)
    /// because URP Lit reads metallic from R / smoothness from A of _MetallicGlossMap and occlusion from G.
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
                    AssemblyContext.Warn(Step, $"{set.AssetFolder}: T_{set.AssetName}_BC missing; material not built");
                    continue;
                }

                Material material = BuildMaterial(set, lit);
                ModelMaterialRemapper.Remap(set, material);
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
                if (path.Contains("/UI/") || path.Contains("/Materials/")
                    || !AssetNaming.TrySplitTexture(Path.GetFileNameWithoutExtension(path), out string assetName, out string suffix))
                {
                    continue;
                }

                string textureFolder = AssemblyPaths.ParentFolder(path);
                string assetFolder = textureFolder.EndsWith("/Textures") ? AssemblyPaths.ParentFolder(textureFolder) : textureFolder;
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
            material.SetColor("_BaseColor", Color.white);
            if (set.Normal != null)
            {
                material.SetTexture("_BumpMap", set.Normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }

            ApplyOrm(set, material);
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

        private static void ApplyOrm(MaterialTextureSet set, Material material)
        {
            if (set.Orm == null)
            {
                return;
            }

            Texture2D packed = OrmPacker.Pack(set.Orm, set.AssetFolder + "/Materials", set.AssetName);
            if (packed == null)
            {
                AssemblyContext.Warn(Step, $"T_{set.AssetName}_ORM could not be repacked (GPU readback failed?) — metallic/occlusion left at defaults");
                return;
            }

            material.SetTexture("_MetallicGlossMap", packed);
            material.SetTexture("_OcclusionMap", packed);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_OcclusionStrength", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            AssemblyContext.Counts.orm_packed_textures++;
        }

        #endregion
    }
}
