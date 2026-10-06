using System;
using System.IO;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Asset-manifest queries for import rules. Re-reads <c>asset_manifest.json</c> when its timestamp changes,
    /// so it also works inside import worker processes (no shared statics with the main Editor).
    /// </summary>
    public static class ManifestLookup
    {
        #region Fields

        private static AssetManifestDto _manifest;
        private static DateTime _stamp = DateTime.MinValue;

        #endregion

        #region Public Methods

        public static AssetEntryDto[] Assets
        {
            get
            {
                string path = GeneratedData.PathOf("asset_manifest");
                DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
                if (stamp != _stamp)
                {
                    _stamp = stamp;
                    _manifest = stamp == DateTime.MinValue ? null : GeneratedData.Load<AssetManifestDto>("asset_manifest");
                }

                return _manifest?.assets ?? new AssetEntryDto[0];
            }
        }

        public static AssetEntryDto FindByName(string assetName)
        {
            foreach (AssetEntryDto entry in Assets)
            {
                if (entry != null && string.Equals(entry.asset_name, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>Finds the entry whose <c>files.mesh</c> is this file, else by name without SM_/SK_ prefix.</summary>
        public static AssetEntryDto FindForModel(string assetPath)
        {
            string fileName = Path.GetFileName(assetPath);
            foreach (AssetEntryDto entry in Assets)
            {
                string mesh = entry?.files?.mesh;
                if (!string.IsNullOrEmpty(mesh) && mesh.Replace('\\', '/').EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            string stem = Path.GetFileNameWithoutExtension(assetPath);
            return FindByName(stem) ?? FindByName(AssetNaming.StripPrefix(stem));
        }

        /// <summary>Resolves <c>ANIM_&lt;Asset&gt;_&lt;Clip&gt;</c> to its manifest animation entry, if any.</summary>
        public static AnimationEntryDto FindAnimation(string assetPath)
        {
            string stem = Path.GetFileNameWithoutExtension(assetPath);
            if (!AssetNaming.TrySplitAnimation(stem, out string assetName, out string clip))
            {
                return null;
            }

            AssetEntryDto owner = FindByName(assetName);
            if (owner?.animations == null)
            {
                return null;
            }

            string fileName = Path.GetFileName(assetPath);
            foreach (AnimationEntryDto animation in owner.animations)
            {
                bool byFile = !string.IsNullOrEmpty(animation?.file) && animation.file.Replace('\\', '/').EndsWith(fileName, StringComparison.OrdinalIgnoreCase);
                bool byClip = animation != null && string.Equals(animation.clip, clip, StringComparison.OrdinalIgnoreCase);
                if (byFile || byClip)
                {
                    return animation;
                }
            }

            return null;
        }

        public static bool IsHumanoid(string assetName)
        {
            AssetEntryDto entry = FindByName(assetName);
            return entry != null && string.Equals(entry.rig, "humanoid", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
