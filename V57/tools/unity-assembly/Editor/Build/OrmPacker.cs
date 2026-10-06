using System.IO;
using UnityEditor;
using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Repacks a provider ORM texture (R=occlusion, G=roughness, B=metallic) into <c>T_&lt;Asset&gt;_MSO.png</c>
    /// for URP Lit: R=metallic, G=occlusion, B=0, A=smoothness(1-roughness). The same texture is assigned to
    /// _MetallicGlossMap (reads R/A) and _OcclusionMap (reads G). The source is read through a GPU blit, so it
    /// does not need Read/Write; requires a graphics device (fails with -nographics).
    /// </summary>
    public static class OrmPacker
    {
        #region Public Methods

        public static Texture2D Pack(Texture2D orm, string materialsFolder, string assetName)
        {
            int width = orm.width;
            int height = orm.height;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                Graphics.Blit(orm, target);
                RenderTexture.active = target;
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply();
                Color32[] pixels = readable.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 source = pixels[i];
                    pixels[i] = new Color32(source.b, source.r, 0, (byte)(255 - source.g));
                }

                readable.SetPixels32(pixels);
                string assetPath = $"{materialsFolder}/T_{assetName}_MSO.png";
                File.WriteAllBytes(AssemblyPaths.ToFullPath(assetPath), readable.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"V57 OrmPacker: {assetName}: {exception.Message}");
                return null;
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
