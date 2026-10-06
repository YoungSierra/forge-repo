using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// One <c>asset_manifest.assets[]</c> entry. <c>rig</c> (humanoid|generic) is optional; absent = generic.
    /// Fields with uncertain JSON types (bones, tris_lod0, texture_size, issues) are intentionally not mapped.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class AssetEntryDto
    {
        public string asset_id;
        public string asset_name;
        public string category;
        public string type;
        public string[] serves;
        public AssetFilesDto files;
        public float[] size_m;
        public string pivot;
        public string side;
        public string collision;
        public string rig;
        public string status;
        public string source;
        public AnimationEntryDto[] animations;
    }
}
