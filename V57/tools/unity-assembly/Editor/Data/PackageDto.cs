using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Subset of <c>package.json</c> used by the assembly tools.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class PackageDto
    {
        public string slug;
        public string title;
        public string version;
        public string perspective;
        public string physics;
        public PlatformDto platform;
        public WorldDto world;
        public SliceDto slice;
    }
}
