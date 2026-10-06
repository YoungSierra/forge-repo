using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// <c>package.slice</c>; <c>gold_path</c> is read by the runtime driver from acceptance.json instead.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class SliceDto
    {
        public string[] scenes;
    }
}
