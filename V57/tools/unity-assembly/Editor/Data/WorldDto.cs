using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>package.world</c> (<c>play_area_m</c> may be null → empty).
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class WorldDto
    {
        public string units;
        public string up;
        public string forward;
        public float[] play_area_m;
    }
}
