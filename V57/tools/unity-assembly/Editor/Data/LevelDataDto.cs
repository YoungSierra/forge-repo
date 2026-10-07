using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Root of <c>level_data.json</c>: index of non-spatial level content from <c>Docs/Design/LevelData/&lt;LevelId&gt;.json</c>
    /// (contract <c>level_data/1.x</c>). The game-specific payload stays in the source file.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement).
    /// </summary>
    [Serializable]
    public sealed class LevelDataIndexDto
    {
        public LevelDataEntryDto[] levels;
    }

    /// <summary>One level data file: copied from <c>source</c> (repo-relative) to <c>target</c> (asset path).</summary>
    [Serializable]
    public sealed class LevelDataEntryDto
    {
        public string level_id;
        public string source;
        public string target;
        public string contract;
        public string[] keys;
    }
}
