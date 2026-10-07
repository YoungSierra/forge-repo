using System.IO;
using UnityEditor;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Copies each delivered level data file (<c>level_data.json</c> index) to <c>Assets/_Game/Data/Levels/&lt;LevelId&gt;.json</c>
    /// so gameplay code loads it as a TextAsset. Content is copied verbatim and only rewritten when it changed; the
    /// game-specific payload is validated by the game's own tests against its TDD §6 level contract.
    /// </summary>
    public static class LevelDataBuilder
    {
        #region Fields

        private const string Step = "BuildLevelData";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            foreach (LevelDataEntryDto entry in GeneratedData.LevelData?.levels ?? new LevelDataEntryDto[0])
            {
                if (entry == null || string.IsNullOrEmpty(entry.source) || string.IsNullOrEmpty(entry.target))
                {
                    continue;
                }

                string source = Path.Combine(AssemblyPaths.ProjectRoot, entry.source);
                if (!File.Exists(source))
                {
                    AssemblyContext.Error(Step, $"{entry.level_id}: source '{entry.source}' not found (re-run intake)");
                    continue;
                }

                string target = Path.Combine(AssemblyPaths.ProjectRoot, entry.target);
                string content = File.ReadAllText(source);
                if (!File.Exists(target) || File.ReadAllText(target) != content)
                {
                    AssemblyPaths.EnsureFolder(AssemblyPaths.ParentFolder(entry.target));
                    File.WriteAllText(target, content);
                    AssetDatabase.ImportAsset(entry.target, ImportAssetOptions.ForceUpdate);
                }

                AssemblyContext.Counts.level_data++;
            }
        }

        #endregion
    }
}
