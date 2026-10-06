using UnityEditor;
using V57.Assembly.Report;

namespace V57.Assembly.Import
{
    /// <summary>Force-reimports provider art/audio so <see cref="V57AssetPostprocessor"/> rules apply, then links ANIM_ avatars.</summary>
    public static class ImportRuleApplier
    {
        public static void Apply()
        {
            ImportLog.Clear();
            string[] roots = { AssemblyPaths.ArtRoot, AssemblyPaths.AudioRoot };
            foreach (string root in roots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                {
                    AssemblyContext.Warn("ApplyImportRules", $"{root} does not exist");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!AssetDatabase.IsValidFolder(path))
                        {
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            AnimationAvatarLinker.LinkAll();
            AssemblyContext.Counts.import_log_entries = ImportLog.ReadAll().Count;
        }
    }
}
