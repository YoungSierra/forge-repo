using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>asset.files</c>: repo-relative paths.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class AssetFilesDto
    {
        public string mesh;
        public string[] textures;
        public string reference;
    }
}
