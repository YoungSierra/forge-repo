using System;

namespace V57.GoldPath
{
    /// <summary>
    /// Root of <c>Docs/V57/evidence/goldpath/&lt;UTC&gt;/checks.json</c>. <c>pass</c> is true only when there is at
    /// least one step, every step passed, there are no parse errors and no console errors/exceptions.
    /// JSON DTO (public snake_case fields, see README).
    /// </summary>
    [Serializable]
    public sealed class GoldPathChecks
    {
        public string started;
        public string finished;
        public string source;
        public string scene;
        public string evidence_dir;
        public GoldPathStepResult[] steps;
        public string[] parse_errors;
        public int console_errors;
        public string[] console_error_samples;
        public float fps_avg;
        public bool pass;
    }
}
