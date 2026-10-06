using System;
using V57.Assembly.Import;

namespace V57.Assembly.Report
{
    /// <summary>
    /// Root of <c>Docs/V57/reports/assembly-report.json</c> (gate I2: <c>pass</c> = 0 errors and 0 missing refs).
    /// JSON DTO (public snake_case fields, see README).
    /// </summary>
    [Serializable]
    public sealed class AssemblyReport
    {
        public string generated_utc;
        public string unity_version;
        public string data_problems;
        public bool pass;
        public AssemblyCounts counts;
        public AssemblyStepResult[] steps;
        public string[] errors;
        public string[] warnings;
        public string[] created_assets;
        public MissingRefEntry[] missing_refs;
        public ImportLogEntry[] import_log;
    }
}
