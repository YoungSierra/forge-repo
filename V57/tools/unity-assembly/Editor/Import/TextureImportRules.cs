using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Texture rules by folder + suffix: <c>UI/Sprites</c> and <c>UI/Icons</c> → single Sprite, no mips,
    /// <c>_9s-&lt;px&gt;</c> → 9-slice border; <c>T_*_N</c> → NormalMap; <c>_ORM/_Mask/_MSO</c> → linear;
    /// <c>_BC/_E</c> → sRGB; <c>VFX_*</c> → sRGB + alpha (sheet grid logged for TextureSheetAnimation).
    /// </summary>
    public static class TextureImportRules
    {
        #region Fields

        private static readonly Regex NineSlice = new Regex(@"_9s-(\d+)$", RegexOptions.CultureInvariant);
        private static readonly Regex VfxSheet = new Regex(@"_Sheet_(\d+)x(\d+)$", RegexOptions.CultureInvariant);

        #endregion

        #region Public Methods

        public static void Apply(TextureImporter importer, string assetPath)
        {
            string stem = Path.GetFileNameWithoutExtension(assetPath);
            if (assetPath.Contains("/UI/Sprites/") || assetPath.Contains("/UI/Icons/"))
            {
                ApplySprite(importer, assetPath, stem);
                return;
            }

            if (assetPath.Contains("/Environment/Sky/"))
            {
                ApplySky(importer, assetPath);
                return;
            }

            if (AssetNaming.TrySplitTexture(stem, out _, out string suffix) || AssetNaming.TrySplitDccTexture(stem, out suffix))
            {
                ApplyMaterialTexture(importer, assetPath, suffix);
                return;
            }

            if (stem.StartsWith("VFX_", System.StringComparison.Ordinal))
            {
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                Match sheet = VfxSheet.Match(stem);
                string grid = sheet.Success ? $"{sheet.Groups[1].Value}x{sheet.Groups[2].Value}" : "none";
                ImportLog.Record(assetPath, "texture", $"VFX sRGB alpha sheet={grid} (set TextureSheetAnimation tiles to match)");
                return;
            }

            ImportLog.Record(assetPath, "texture", "no naming rule matched (importer defaults kept)");
        }

        #endregion

        #region Private Methods

        private static void ApplySprite(TextureImporter importer, string assetPath, string stem)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            Match nineSlice = NineSlice.Match(stem);
            string border = "none";
            if (nineSlice.Success && int.TryParse(nineSlice.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pixels))
            {
                // Vector4 order is (left, bottom, right, top) in pixels.
                importer.spriteBorder = new Vector4(pixels, pixels, pixels, pixels);
                border = pixels + "px";
            }

            ImportLog.Record(assetPath, "sprite", $"Sprite Single mips=off clamp border={border}");
        }

        private static void ApplyMaterialTexture(TextureImporter importer, string assetPath, string suffix)
        {
            string decision;
            switch (suffix.ToUpperInvariant())
            {
                case "N":
                    importer.textureType = TextureImporterType.NormalMap;
                    decision = "NormalMap";
                    break;
                case "ORM":
                case "MASK":
                case "MSO":
                case "MS":
                case "M":
                case "R":
                case "AO":
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;
                    decision = "Default linear (sRGB off)";
                    break;
                case "BC":
                case "E":
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    decision = "Default sRGB";
                    break;
                default:
                    ImportLog.Record(assetPath, "texture", $"unknown suffix _{suffix} (importer defaults kept)");
                    return;
            }

            importer.mipmapEnabled = true;
            ImportLog.Record(assetPath, "texture", decision + " mips=on");
        }

        /// <summary>
        /// Sky backdrops (equirectangular / lat-long): no mips (a mip seam shows at the wrap), wrap U repeat, V clamp,
        /// full source resolution up to 8192.
        /// </summary>
        private static void ApplySky(TextureImporter importer, string assetPath)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(width, height)), 32, 8192);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            ImportLog.Record(assetPath, "sky", $"lat-long sky sRGB mips=off wrapU=repeat wrapV=clamp max={importer.maxTextureSize}");
        }

        #endregion
    }
}
