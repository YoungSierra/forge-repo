using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Root of <c>layouts.json</c>: level layouts from <c>Docs/Design/LevelMaps/&lt;LevelId&gt;/unity_scene.json</c>,
    /// normalized by intake (English layer groups, <c>&lt;Name&gt;_NN</c> instance names, delivered paths).
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement).
    /// </summary>
    [Serializable]
    public sealed class LayoutsDto
    {
        public LayoutDto[] layouts;
    }

    /// <summary>One level layout.</summary>
    [Serializable]
    public sealed class LayoutDto
    {
        public string level_id;
        public string source;
        public string manifest;
        public LayoutObjectDto[] objects;
        public LayoutMaterialDto[] materials;
    }

    /// <summary>One placed instance; position/rotation(x,y,z,w)/scale already in Unity Y-up metres.</summary>
    [Serializable]
    public sealed class LayoutObjectDto
    {
        public string name;
        public string source_name;
        public string asset_id;
        public string model;
        public string layer;
        public float[] position;
        public float[] rotation;
        public float[] scale;
    }

    /// <summary>Material values authored in the DCC export manifest.</summary>
    [Serializable]
    public sealed class LayoutMaterialDto
    {
        public string asset_id;
        public string name;
        public string albedo;
        public string normal;
        public string metallic_smoothness;
        public float[] base_color;
        public float metallic;
        public float smoothness;
        public string alpha_mode;
        public float alpha_cutoff;
        public bool double_sided;
    }
}
