using System;

namespace V57.GoldPath
{
    /// <summary>One entry of <c>checks.json → steps</c>. JSON DTO (public snake_case fields, see README).</summary>
    [Serializable]
    public sealed class GoldPathStepResult
    {
        public int index;
        public string id;
        public string type;
        public string arg;
        public bool pass;
        public float elapsed;
        public string detail;
        public string screenshot;
    }
}
