using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>package.platform</c>.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class PlatformDto
    {
        public string[] targets;
        public string orientation;
        public float[] reference_resolution;
        public float target_fps;
    }
}
