using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace V57.Assembly
{
    /// <summary>Folder conventions from the V57 design brief §1–§2 and helpers for asset paths.</summary>
    public static class AssemblyPaths
    {
        #region Fields

        public const string ArtRoot = "Assets/_Game/Art";
        public const string AudioRoot = "Assets/_Game/Audio";
        public const string UiSpritesRoot = "Assets/_Game/Art/UI/Sprites";
        public const string AtlasRoot = "Assets/_Game/Art/UI/Atlases";
        public const string VisualPrefabRoot = "Assets/_Game/Prefabs/Visual";
        public const string GameplayPrefabRoot = "Assets/_Game/Prefabs/Gameplay";
        public const string AnimationRoot = "Assets/_Game/Data/Animation";
        public const string SkyRoot = "Assets/_Game/Art/Environment/Sky";
        public const string LevelDataRoot = "Assets/_Game/Data/Levels";
        public const string GeneratedJsonRelative = "Docs/Generated/json";
        public const string ReportsRelative = "Docs/V57/reports";
        public const string LitShaderName = "Universal Render Pipeline/Lit";

        #endregion

        #region Public Methods

        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public static string GeneratedJsonDirectory => Path.Combine(ProjectRoot, GeneratedJsonRelative);

        public static string ReportsDirectory => Path.Combine(ProjectRoot, ReportsRelative);

        public static bool IsArtPath(string assetPath)
        {
            return assetPath != null && assetPath.StartsWith(ArtRoot + "/", StringComparison.Ordinal);
        }

        public static bool IsAudioPath(string assetPath)
        {
            return assetPath != null && assetPath.StartsWith(AudioRoot + "/", StringComparison.Ordinal);
        }

        /// <summary>Normalizes a repo-relative or absolute path from JSON to an <c>Assets/...</c> path, or null.</summary>
        public static string ToAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string normalized = path.Trim().Replace('\\', '/');
            string root = ProjectRoot.Replace('\\', '/') + "/";
            if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(root.Length);
            }

            normalized = normalized.TrimStart('.', '/');
            return normalized.StartsWith("Assets/", StringComparison.Ordinal) ? normalized : null;
        }

        public static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath);
        }

        /// <summary>Creates every missing folder of an <c>Assets/...</c> folder path through the AssetDatabase.</summary>
        public static void EnsureFolder(string folderAssetPath)
        {
            string[] parts = folderAssetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        public static string ParentFolder(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            return slash > 0 ? assetPath.Substring(0, slash) : assetPath;
        }

        #endregion
    }
}
