using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// One <c>ui.screens[]</c> entry.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class UiScreenDto
    {
        public string id;
        public string purpose;
        public string[] states;
        public string[] consumed_by;
        public string presentation;
        public string sprites;
        public string asset_id;
        public string[] text_keys;
    }
}
