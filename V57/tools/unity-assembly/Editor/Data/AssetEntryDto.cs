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

        /// <summary>A Docs/V57/asset_rules.json rule matched: kept Visual prefabs follow the rules on every build.</summary>
        public bool ruled;

        /// <summary>Docs/V57/asset_rules.json: on | off | two_sided (empty = leave the import default).</summary>
        public string shadows;

        /// <summary>Docs/V57/asset_rules.json: game shader the generated material keeps (Lit-compatible properties).</summary>
        public string shader;

        /// <summary>Docs/V57/asset_rules.json: prefabs placed with every layout instance of the asset.</summary>
        public AssetAttachDto[] attach;
    }

    /// <summary>One <c>attach</c> entry: prefab placed at the instance position + offset (metres, world or instance axes).</summary>
    [Serializable]
    public sealed class AssetAttachDto
    {
        public string prefab;
        public float[] offset;
        public string align;
    }
}
