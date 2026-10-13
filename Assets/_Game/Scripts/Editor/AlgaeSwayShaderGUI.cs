using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProfessorSprat.Editor
{
    /// <summary>
    /// Inspector for "ProfessorSprat/Environment/Algae Sway" (D-263). URP's Lit inspector is internal, so this one draws
    /// the shader's properties and keeps the Lit keywords and render state in sync with them (normal map, metallic map,
    /// occlusion, emission, alpha clip, receive shadows), as the Lit inspector would. The material is always opaque.
    /// </summary>
    public sealed class AlgaeSwayShaderGUI : ShaderGUI
    {
        #region Public Methods

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI(materialEditor, properties);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Object target in materialEditor.targets)
                {
                    if (target is Material material)
                    {
                        Sync(material);
                    }
                }
            }
        }

        public override void ValidateMaterial(Material material)
        {
            Sync(material);
        }

        #endregion

        #region Private Methods

        private static void Sync(Material material)
        {
            SetKeyword(material, "_NORMALMAP", HasTexture(material, "_BumpMap"));
            SetKeyword(material, "_METALLICSPECGLOSSMAP", HasTexture(material, "_MetallicGlossMap"));
            SetKeyword(material, "_OCCLUSIONMAP", HasTexture(material, "_OcclusionMap"));
            SetKeyword(material, "_SPECULAR_SETUP", false);

            bool emission = material.HasProperty("_EmissionColor") && material.GetColor("_EmissionColor").maxColorComponent > 0.0001f;
            SetKeyword(material, "_EMISSION", emission);
            material.globalIlluminationFlags = emission ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            bool alphaClip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
            SetKeyword(material, "_ALPHATEST_ON", alphaClip);
            bool receiveShadows = !material.HasProperty("_ReceiveShadows") || material.GetFloat("_ReceiveShadows") > 0.5f;
            SetKeyword(material, "_RECEIVE_SHADOWS_OFF", !receiveShadows);

            // Opaque only (sway is for solid plants).
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_AlphaToMask", 0f);
            material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
            material.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;
            material.SetShaderPassEnabled("ShadowCaster", true);
        }

        private static bool HasTexture(Material material, string property)
        {
            return material.HasProperty(property) && material.GetTexture(property) != null;
        }

        private static void SetKeyword(Material material, string keyword, bool on)
        {
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        #endregion
    }
}
