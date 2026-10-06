using System;

namespace V57.Assembly.Report
{
    /// <summary>One executed AssemblyRunner step. JSON DTO (public snake_case fields, see README).</summary>
    [Serializable]
    public sealed class AssemblyStepResult
    {
        public string name;
        public bool ok;
        public float duration_s;
        public string detail;
    }
}
