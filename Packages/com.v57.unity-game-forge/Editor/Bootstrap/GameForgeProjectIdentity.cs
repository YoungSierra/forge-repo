using UnityEditor;
using UnityEngine;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Keeps Unity PlayerSettings aligned with the active TDD (game-setup / Production).
    /// </summary>
    public static class GameForgeProjectIdentity
    {
        /// <summary>
        /// Set <see cref="PlayerSettings.productName"/> from TDD §A / Game title for the given slug.
        /// </summary>
        public static bool SyncPlayerSettingsFromSlug(string slug)
        {
            slug = (slug ?? "").Trim();
            if (string.IsNullOrEmpty(slug)) return false;

            TddInfo tdd;
            try
            {
                tdd = TddParser.ReadTdd(slug);
            }
            catch
            {
                return false;
            }

            var displayName = (tdd.projectName ?? slug).Trim();
            if (string.IsNullOrEmpty(displayName)) return false;

            var changed = false;
            if (PlayerSettings.productName != displayName)
            {
                PlayerSettings.productName = displayName;
                changed = true;
            }

            if (PlayerSettings.companyName is "DefaultCompany" or "")
            {
                PlayerSettings.companyName = "V57";
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[GameForge] PlayerSettings.productName → \"{displayName}\" (TDD {slug})");
            }

            return changed;
        }
    }
}
