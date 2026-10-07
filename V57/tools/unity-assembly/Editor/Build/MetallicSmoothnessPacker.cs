using System.IO;
using UnityEditor;
using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Packs separate metallic + roughness maps (DCC exports) into a URP metallic/smoothness mask
    /// <c>Materials/T_&lt;Asset&gt;_MS.png</c>: R = metallic, A = 1 − roughness. Either input may be null.
    /// </summary>
    public static class MetallicSmoothnessPacker
    {
        #region Public Methods

        public static Texture2D Pack(Texture2D metallic, Texture2D roughness, string materialsFolder, string assetName)
        {
            Texture2D size = metallic != null ? metallic : roughness;
            if (size == null)
            {
                return null;
            }

            Color32[] metal = ReadPixels(metallic, size.width, size.height);
            Color32[] rough = ReadPixels(roughness, size.width, size.height);
            Texture2D packed = new Texture2D(size.width, size.height, TextureFormat.RGBA32, false, true);
            try
            {
                Color32[] pixels = new Color32[size.width * size.height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte m = metal != null ? metal[i].r : (byte)0;
                    byte smooth = rough != null ? (byte)(255 - rough[i].r) : (byte)128;
                    pixels[i] = new Color32(m, m, m, smooth);
                }

                packed.SetPixels32(pixels);
                string assetPath = $"{materialsFolder}/T_{assetName}_MS.png";
                File.WriteAllBytes(AssemblyPaths.ToFullPath(assetPath), packed.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"V57 MetallicSmoothnessPacker: {assetName}: {exception.Message}");
                return null;
            }
            finally
            {
                Object.DestroyImmediate(packed);
            }
        }

        #endregion

        #region Private Methods

        private static Color32[] ReadPixels(Texture2D source, int width, int height)
        {
            if (source == null)
            {
                return null;
            }

            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply();
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(readable);
            }
        }

        #endregion
    }
}
