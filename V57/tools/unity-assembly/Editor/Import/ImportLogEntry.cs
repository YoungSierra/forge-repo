using System;

namespace V57.Assembly.Import
{
    /// <summary>One import decision (JSON line in <c>Library/V57/import-log.jsonl</c>). JSON DTO, see README.</summary>
    [Serializable]
    public sealed class ImportLogEntry
    {
        public string path;
        public string rule;
        public string decision;
        public string utc;
    }
}
