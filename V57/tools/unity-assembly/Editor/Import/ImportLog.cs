using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace V57.Assembly.Import
{
    /// <summary>
    /// Records every import-rule decision as a JSON line in <c>Library/V57/import-log.jsonl</c>.
    /// A file (not a static list) because postprocessors may run in import worker processes and
    /// statics are lost on domain reload. The assembly report embeds the entries.
    /// </summary>
    public static class ImportLog
    {
        #region Public Methods

        public static string LogPath => Path.Combine(AssemblyPaths.ProjectRoot, "Library", "V57", "import-log.jsonl");

        public static void Record(string assetPath, string rule, string decision)
        {
            ImportLogEntry entry = new ImportLogEntry
            {
                path = assetPath,
                rule = rule,
                decision = decision,
                utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, JsonUtility.ToJson(entry) + "\n");
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"V57 ImportLog: could not record {assetPath}: {exception.Message}");
            }
        }

        public static void Clear()
        {
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }

        /// <summary>Returns the latest decision per (path, rule), in file order.</summary>
        public static List<ImportLogEntry> ReadAll()
        {
            List<ImportLogEntry> entries = new List<ImportLogEntry>();
            if (!File.Exists(LogPath))
            {
                return entries;
            }

            Dictionary<string, int> indexByKey = new Dictionary<string, int>();
            foreach (string line in File.ReadAllLines(LogPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                ImportLogEntry entry;
                try
                {
                    entry = JsonUtility.FromJson<ImportLogEntry>(line);
                }
                catch (ArgumentException)
                {
                    continue; // partially written line (concurrent import worker)
                }

                string key = entry.path + "|" + entry.rule;
                if (indexByKey.TryGetValue(key, out int existing))
                {
                    entries[existing] = entry;
                }
                else
                {
                    indexByKey[key] = entries.Count;
                    entries.Add(entry);
                }
            }

            return entries;
        }

        #endregion
    }
}
