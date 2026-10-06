using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// One Sprite Atlas per <c>Assets/_Game/Art/UI/Sprites/&lt;Id&gt;/</c> folder →
    /// <c>Assets/_Game/Art/UI/Atlases/ATL_&lt;Id&gt;</c>. Uses the V1 <c>.spriteatlas</c> API unless the project's
    /// Sprite Packer mode is a V2 mode, in which case a <c>.spriteatlasv2</c> is written through SpriteAtlasAsset.
    /// </summary>
    public static class AtlasBuilder
    {
        #region Fields

        private const string Step = "BuildUiAtlases";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(AssemblyPaths.UiSpritesRoot))
            {
                AssemblyContext.Warn(Step, $"{AssemblyPaths.UiSpritesRoot} does not exist; no atlases");
                return;
            }

            AssemblyPaths.EnsureFolder(AssemblyPaths.AtlasRoot);
            // Enum member names differ across versions (SpriteAtlasV2, SpriteAtlasV2Build…); match by name to stay compile-safe.
            bool useV2 = EditorSettings.spritePackerMode.ToString().Contains("V2");
            foreach (string folder in AssetDatabase.GetSubFolders(AssemblyPaths.UiSpritesRoot))
            {
                string id = Path.GetFileName(folder);
                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
                string atlasPath = useV2 ? BuildV2(id, folderAsset) : BuildV1(id, folderAsset);
                if (atlasPath != null)
                {
                    AssemblyContext.Counts.atlases++;
                }
            }

            CheckUiScreens();
        }

        #endregion

        #region Private Methods

        private static string BuildV1(string id, DefaultAsset folderAsset)
        {
            string path = $"{AssemblyPaths.AtlasRoot}/ATL_{id}.spriteatlas";
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            atlas.SetIncludeInBuild(true);
            atlas.SetPackingSettings(PackingSettings());
            atlas.SetTextureSettings(TextureSettings());
            bool alreadyPacked = false;
            foreach (Object packable in atlas.GetPackables())
            {
                alreadyPacked |= packable == folderAsset;
            }

            if (!alreadyPacked)
            {
                atlas.Add(new Object[] { folderAsset });
            }

            EditorUtility.SetDirty(atlas);
            return path;
        }

        private static string BuildV2(string id, DefaultAsset folderAsset)
        {
            // Unverified in 6000.6: SpriteAtlasAsset.Save/Load and SpriteAtlasImporter settings (2022.1+ API).
            string path = $"{AssemblyPaths.AtlasRoot}/ATL_{id}.spriteatlasv2";
            if (!File.Exists(AssemblyPaths.ToFullPath(path)))
            {
                SpriteAtlasAsset asset = new SpriteAtlasAsset();
                asset.Add(new Object[] { folderAsset });
                SpriteAtlasAsset.Save(asset, path);
                AssetDatabase.ImportAsset(path);
            }

            SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
            if (importer == null)
            {
                AssemblyContext.Error(Step, $"{path}: SpriteAtlasImporter not found after save");
                return null;
            }

            importer.includeInBuild = true;
            importer.packingSettings = PackingSettings();
            importer.textureSettings = TextureSettings();
            importer.SaveAndReimport();
            return path;
        }

        private static SpriteAtlasPackingSettings PackingSettings()
        {
            return new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            };
        }

        private static SpriteAtlasTextureSettings TextureSettings()
        {
            return new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
        }

        private static void CheckUiScreens()
        {
            UiDto ui = GeneratedData.Ui;
            if (ui?.screens == null)
            {
                return;
            }

            foreach (UiScreenDto screen in ui.screens)
            {
                if (screen == null || screen.presentation == "world")
                {
                    continue;
                }

                string folderName = string.IsNullOrEmpty(screen.sprites) ? screen.id : Path.GetFileName(screen.sprites.TrimEnd('/'));
                if (!string.IsNullOrEmpty(folderName) && !AssetDatabase.IsValidFolder($"{AssemblyPaths.UiSpritesRoot}/{folderName}"))
                {
                    AssemblyContext.Warn(Step, $"ui screen '{screen.id}': sprite folder '{folderName}' not found (UI will need placeholders)");
                }
            }
        }

        #endregion
    }
}
