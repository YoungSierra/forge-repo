using UnityEditor;
using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>Magenta-ish URP Lit material <c>MAT_V57_Placeholder</c> shared by all placeholders.</summary>
    public static class PlaceholderMaterial
    {
        private static readonly Color PlaceholderColor = new Color(1f, 0.15f, 0.8f, 1f);

        public static Material GetOrCreate()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssemblyPaths.PlaceholderMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(AssemblyPaths.LitShaderName);
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader) { name = "MAT_V57_Placeholder" };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", PlaceholderColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", PlaceholderColor);
            }

            AssemblyPaths.EnsureFolder(AssemblyPaths.ParentFolder(AssemblyPaths.PlaceholderMaterialPath));
            AssetDatabase.CreateAsset(material, AssemblyPaths.PlaceholderMaterialPath);
            return material;
        }
    }
}
