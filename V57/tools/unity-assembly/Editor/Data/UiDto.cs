using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Root of <c>ui.json</c>.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class UiDto
    {
        public UiScreenDto[] screens;
        public UiLayoutDto[] layouts;
    }
}
