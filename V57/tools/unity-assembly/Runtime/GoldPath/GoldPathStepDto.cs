using System;

namespace V57.GoldPath
{
    /// <summary>
    /// One gold path step as authored in <c>acceptance.json → gold_path.steps</c> or <c>Docs/V57/gold_path.json</c>.
    /// JSON DTO: public snake_case fields mirror the JSON contract (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class GoldPathStepDto
    {
        public string id;
        public string @do;
        public string action;
        public string control;
        public string value;
        public float seconds;
        public int frames;
        public string expect;
        public string op;
        public float timeout_s;
        public bool screenshot;
        public string condition;
        public string name;
    }
}
