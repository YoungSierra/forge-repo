using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// One <c>ui.layouts[]</c> entry.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class UiLayoutDto
    {
        public string id;
        public string mockup;
        public string[] contains;
    }
}
