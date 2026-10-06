using System;

namespace V57.Assembly.Data
{
    /// <summary>Provider naming convention helpers (design brief §1: SM_, SK_, ANIM_, BLK_, T_, SPR_, VFX_).</summary>
    public static class AssetNaming
    {
        #region Fields

        private static readonly string[] MeshPrefixes = { "SM_", "SK_", "BLK_" };

        #endregion

        #region Public Methods

        public static string StripPrefix(string stem)
        {
            foreach (string prefix in MeshPrefixes)
            {
                if (stem.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return stem.Substring(prefix.Length);
                }
            }

            return stem;
        }

        /// <summary><c>ANIM_&lt;Asset&gt;_&lt;Clip&gt;</c>: asset is the first token (PascalCase, no underscores).</summary>
        public static bool TrySplitAnimation(string stem, out string assetName, out string clip)
        {
            assetName = string.Empty;
            clip = string.Empty;
            if (!stem.StartsWith("ANIM_", StringComparison.Ordinal))
            {
                return false;
            }

            string rest = stem.Substring(5);
            int separator = rest.IndexOf('_');
            if (separator <= 0)
            {
                return false;
            }

            assetName = rest.Substring(0, separator);
            clip = rest.Substring(separator + 1);
            return true;
        }

        /// <summary><c>T_&lt;Asset&gt;_&lt;Suffix&gt;</c> → asset and suffix (BC, N, ORM, E, Mask, MSO…).</summary>
        public static bool TrySplitTexture(string stem, out string assetName, out string suffix)
        {
            assetName = string.Empty;
            suffix = string.Empty;
            int last = stem.LastIndexOf('_');
            if (!stem.StartsWith("T_", StringComparison.Ordinal) || last <= 2)
            {
                return false;
            }

            assetName = stem.Substring(2, last - 2);
            suffix = stem.Substring(last + 1);
            return assetName.Length > 0 && suffix.Length > 0;
        }

        #endregion
    }
}
