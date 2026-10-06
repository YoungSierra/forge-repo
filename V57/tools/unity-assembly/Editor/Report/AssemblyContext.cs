using System.Collections.Generic;
using UnityEngine;

namespace V57.Assembly.Report
{
    /// <summary>
    /// Collects counts, warnings, errors, executed steps and created assets across AssemblyRunner steps
    /// (one Editor domain; reset by <c>RunAll</c>). Individual step calls accumulate onto the current context.
    /// </summary>
    public static class AssemblyContext
    {
        #region Fields

        private static readonly List<string> WarningList = new List<string>();
        private static readonly List<string> ErrorList = new List<string>();
        private static readonly List<string> CreatedList = new List<string>();
        private static readonly List<AssemblyStepResult> StepList = new List<AssemblyStepResult>();
        private static AssemblyCounts _counts = new AssemblyCounts();

        #endregion

        #region Public Methods

        public static AssemblyCounts Counts => _counts;

        public static IReadOnlyList<string> Warnings => WarningList;

        public static IReadOnlyList<string> Errors => ErrorList;

        public static IReadOnlyList<string> CreatedAssets => CreatedList;

        public static IReadOnlyList<AssemblyStepResult> Steps => StepList;

        public static bool LastReportPass { get; set; }

        public static void Reset()
        {
            WarningList.Clear();
            ErrorList.Clear();
            CreatedList.Clear();
            StepList.Clear();
            _counts = new AssemblyCounts();
            LastReportPass = false;
        }

        public static void Warn(string step, string message)
        {
            string line = $"[{step}] {message}";
            WarningList.Add(line);
            Debug.LogWarning("V57.Assembly " + line);
        }

        /// <summary>Records an assembly error. Logged as a warning in the console on purpose: the report is the gate,
        /// and I1/I2 console-error checks must not double count it.</summary>
        public static void Error(string step, string message)
        {
            string line = $"[{step}] {message}";
            ErrorList.Add(line);
            Debug.LogWarning("V57.Assembly ERROR " + line);
        }

        /// <summary>Tracks a created/updated prefab or scene for the missing-reference scan.</summary>
        public static void TrackAsset(string assetPath)
        {
            if (!CreatedList.Contains(assetPath))
            {
                CreatedList.Add(assetPath);
            }
        }

        public static void AddStep(AssemblyStepResult step)
        {
            StepList.Add(step);
        }

        /// <summary>Drops earlier warnings, errors and results of one step so a re-run can flip the report back to pass.</summary>
        public static void ClearStep(string step)
        {
            string prefix = $"[{step}]";
            WarningList.RemoveAll(line => line.StartsWith(prefix, System.StringComparison.Ordinal));
            ErrorList.RemoveAll(line => line.StartsWith(prefix, System.StringComparison.Ordinal));
            StepList.RemoveAll(result => result.name == step);
        }

        #endregion
    }
}
