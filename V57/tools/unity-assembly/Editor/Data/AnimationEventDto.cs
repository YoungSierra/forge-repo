using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>asset.animations[].events[]</c> (frame at the clip sample rate).
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class AnimationEventDto
    {
        public string name;
        public int frame;
    }
}
