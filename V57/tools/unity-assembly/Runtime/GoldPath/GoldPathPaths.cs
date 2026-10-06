using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// File locations used by the gold path. Editor: relative to the project root (repo root).
    /// Development players: inputs from <c>StreamingAssets/V57/</c>, evidence to <c>persistentDataPath</c>.
    /// </summary>
    public static class GoldPathPaths
    {
        #region Fields

        public const string AcceptanceRelative = "Docs/Generated/json/acceptance.json";
        public const string FallbackRelative = "Docs/V57/gold_path.json";
        public const string EvidenceRelative = "Docs/V57/evidence/goldpath";

        /// <summary>When set, the run writes evidence exactly into this directory (lets the CLI pick the UTC folder).</summary>
        public const string EvidenceDirEnvironmentVariable = "V57_GOLDPATH_EVIDENCE_DIR";

        #endregion

        #region Public Methods

        public static string ProjectRoot => Application.isEditor
            ? Directory.GetParent(Application.dataPath).FullName
            : Application.persistentDataPath;

        public static string ResolveInput(string configuredPath, string defaultRelative)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(ProjectRoot, configuredPath);
            }

            string root = Application.isEditor ? ProjectRoot : Path.Combine(Application.streamingAssetsPath, "V57");
            string relative = Application.isEditor ? defaultRelative : Path.GetFileName(defaultRelative);
            return Path.Combine(root, relative);
        }

        public static string CreateRunDirectory(string configuredEvidenceRoot)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(EvidenceDirEnvironmentVariable);
            string directory;
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                directory = fromEnvironment;
            }
            else
            {
                string root = string.IsNullOrWhiteSpace(configuredEvidenceRoot)
                    ? Path.Combine(ProjectRoot, EvidenceRelative)
                    : ResolveInput(configuredEvidenceRoot, EvidenceRelative);
                string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
                directory = Path.Combine(root, stamp);
            }

            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string SanitizeFileName(string name)
        {
            string source = string.IsNullOrWhiteSpace(name) ? "capture" : name;
            char[] chars = source.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        #endregion
    }
}
