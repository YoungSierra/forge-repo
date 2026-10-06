using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Import;

namespace V57.Assembly.Report
{
    /// <summary>Writes <c>Docs/V57/reports/assembly-report.json</c> from <see cref="AssemblyContext"/>.</summary>
    public static class ReportWriter
    {
        public const string FileName = "assembly-report.json";

        public static string Write()
        {
            List<MissingRefEntry> missing = MissingReferenceScanner.Scan(AssemblyContext.CreatedAssets);
            AssemblyContext.Counts.missing_refs = missing.Count;
            List<string> errors = new List<string>(AssemblyContext.Errors);
            AssemblyReport report = new AssemblyReport
            {
                generated_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                unity_version = Application.unityVersion,
                data_problems = GeneratedData.LastLoadProblems,
                counts = AssemblyContext.Counts,
                steps = new List<AssemblyStepResult>(AssemblyContext.Steps).ToArray(),
                errors = errors.ToArray(),
                warnings = new List<string>(AssemblyContext.Warnings).ToArray(),
                created_assets = new List<string>(AssemblyContext.CreatedAssets).ToArray(),
                missing_refs = missing.ToArray(),
                import_log = ImportLog.ReadAll().ToArray(),
                pass = errors.Count == 0 && missing.Count == 0
            };
            Directory.CreateDirectory(AssemblyPaths.ReportsDirectory);
            string path = Path.Combine(AssemblyPaths.ReportsDirectory, FileName);
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            AssemblyContext.LastReportPass = report.pass;
            Debug.Log($"V57.Assembly: report pass={report.pass} errors={errors.Count} missing_refs={missing.Count} → {path}");
            return path;
        }
    }
}
