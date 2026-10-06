using System;

namespace V57.GoldPath
{
    /// <summary>Gold path root: <c>{ "scene": "...", "status": "draft|final", "steps": [...] }</c>. JSON DTO (public fields, see README).</summary>
    [Serializable]
    public sealed class GoldPathDto
    {
        public string scene;
        public string status;
        public GoldPathStepDto[] steps;
    }
}
