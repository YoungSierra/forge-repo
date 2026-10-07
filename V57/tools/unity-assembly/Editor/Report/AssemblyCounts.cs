using System;

namespace V57.Assembly.Report
{
    /// <summary><c>assembly-report.json → counts</c>. JSON DTO (public snake_case fields, see README).</summary>
    [Serializable]
    public sealed class AssemblyCounts
    {
        public int import_log_entries;
        public int materials;
        public int orm_packed_textures;
        public int metallic_smoothness_packed;
        public int skyboxes;
        public int atlases;
        public int animator_controllers;
        public int visual_prefabs;
        public int scenes;
        public int layout_instances;
        public int layout_missing;
        public int level_data;
        public int markers;
        public int dressed;
        public int skipped_existing;
        public int missing_refs;
    }
}
