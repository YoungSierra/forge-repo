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
        public LayoutPathDto[] paths;
        public LayoutMaterialDto[] materials;
    }

    /// <summary>A DCC custom property flattened to strings (arrays comma-joined, objects as JSON).</summary>
    [Serializable]
    public sealed class LayoutPropDto
    {
        public string key;
        public string value;
    }

    /// <summary>A sampled curve from unity_scene/1.1 <c>trayectorias</c> (Unity axes); <c>points</c> = x,y,z,x,y,z,…</summary>
    [Serializable]
    public sealed class LayoutPathDto
    {
        public string name;
        public string kind;
        public string actor;
        public bool loop;
        public string mode;
        public float speed;
        public string zone;
        public float[] points;
        public string[] instances;
        public LayoutPropDto[] props;
    }

    /// <summary>One placed instance (<c>kind</c> model) or level marker (<c>kind</c> marker, <c>Marker_&lt;Type&gt;_&lt;Id&gt;</c>, no model);
    /// position/rotation(x,y,z,w)/scale already in Unity Y-up metres. A marker volume is a 1 m cube scaled by <c>scale</c>.</summary>
    [Serializable]
    public sealed class LayoutObjectDto
    {
        public string name;
        public string kind;
        public string shape;
        public string source_name;
        public string asset_id;
        public string model;
        public string layer;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        /// <summary>Gameplay prefab instanced instead of a model (marker actors); null for models and markers.</summary>
        public string prefab;
        /// <summary>True = never marked static (layout_aliases <c>static: false</c>, marker actors).</summary>
        public bool dynamic;
        public LayoutPropDto[] props;
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
