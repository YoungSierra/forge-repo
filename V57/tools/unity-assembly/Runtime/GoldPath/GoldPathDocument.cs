using System.Collections.Generic;

namespace V57.GoldPath
{
    /// <summary>A parsed gold path: where it came from, which scene it targets and its steps.</summary>
    public sealed class GoldPathDocument
    {
        public GoldPathDocument(string sourcePath, string scenePath, List<GoldPathStep> steps, string status = null)
        {
            SourcePath = sourcePath ?? string.Empty;
            ScenePath = scenePath ?? string.Empty;
            Steps = steps ?? new List<GoldPathStep>();
            Status = status ?? string.Empty;
        }

        /// <summary><c>status</c> from JSON (intake emits <c>draft</c>; V57-authored files may say <c>final</c>).</summary>
        public string Status { get; }

        public bool IsDraft => string.Equals(Status, "draft", System.StringComparison.OrdinalIgnoreCase);

        public string SourcePath { get; }

        /// <summary>
        /// Raw JSON <c>scene</c> value: a scene id (<c>SCN_Level01</c> / <c>Level01</c>) or an asset path; may be empty.
        /// Convert with <see cref="V57SceneNaming.ToScenePath"/> before loading.
        /// </summary>
        public string ScenePath { get; }

        public IReadOnlyList<GoldPathStep> Steps { get; }

        public bool HasSteps => Steps.Count > 0;
    }
}
