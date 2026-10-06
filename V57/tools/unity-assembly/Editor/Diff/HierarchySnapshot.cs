using System;

namespace V57.Assembly.Diff
{
    /// <summary>Hierarchy of one scene in Edit or Play mode. JSON DTO (public snake_case fields, see README).</summary>
    [Serializable]
    public sealed class HierarchySnapshot
    {
        public string scene;
        public string mode;
        public string captured_utc;
        public HierarchyNode[] nodes;
    }
}
