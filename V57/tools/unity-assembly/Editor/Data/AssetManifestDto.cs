using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Root of <c>asset_manifest.json</c>.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class AssetManifestDto
    {
        public AssetEntryDto[] assets;
        public string[] orphans;
    }
}
