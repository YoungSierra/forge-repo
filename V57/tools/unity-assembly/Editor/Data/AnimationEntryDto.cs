using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>asset.animations[]</c>.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class AnimationEntryDto
    {
        public string clip;
        public string file;
        public bool loop;
        public AnimationEventDto[] events;
    }
}
