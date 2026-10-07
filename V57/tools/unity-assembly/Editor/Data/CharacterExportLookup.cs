using System;
using System.IO;
using UnityEngine;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Fallback loop source for <c>ANIM_&lt;Asset&gt;_&lt;Clip&gt;</c> when the asset manifest has no entry: the provider's
    /// Blender export sidecar <c>Characters/&lt;Asset&gt;/&lt;Asset&gt;_export.json</c> (<c>clips[].file</c> / <c>clips[].loop</c>).
    /// </summary>
    public static class CharacterExportLookup
    {
        #region Public Methods

        /// <summary>Loop flag for an animation file, or null when no sidecar lists it.</summary>
        public static bool? FindLoop(string assetPath)
        {
            string stem = Path.GetFileNameWithoutExtension(assetPath);
            if (!AssetNaming.TrySplitAnimation(stem, out string assetName, out _))
            {
                return null;
            }

            string animationsFolder = Path.GetDirectoryName(assetPath);
            string characterFolder = animationsFolder != null ? Path.GetDirectoryName(animationsFolder) : null;
            string sidecar = characterFolder != null ? Path.Combine(characterFolder, assetName + "_export.json") : null;
            if (sidecar == null || !File.Exists(sidecar))
            {
                return null;
            }

            CharacterExportDto export = JsonUtility.FromJson<CharacterExportDto>(File.ReadAllText(sidecar));
            string fileName = Path.GetFileName(assetPath);
            foreach (CharacterExportClipDto clip in export?.clips ?? new CharacterExportClipDto[0])
            {
                if (clip != null && string.Equals(clip.file, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip.loop;
                }
            }

            return null;
        }

        #endregion
    }

    /// <summary>JSON DTO for the export sidecar (snake_case/JsonUtility exception, see README "Standards exceptions").</summary>
    [Serializable]
    public sealed class CharacterExportDto
    {
        public string character;
        public CharacterExportClipDto[] clips;
    }

    /// <summary><c>clips[]</c> entry of the export sidecar.</summary>
    [Serializable]
    public sealed class CharacterExportClipDto
    {
        public string name;
        public string file;
        public bool loop;
    }
}
