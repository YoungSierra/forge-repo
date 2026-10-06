using System;

namespace V57.Assembly.Diff
{
    /// <summary>
    /// <c>Docs/V57/reports/hierarchy-diff.json</c> (M2 gate). Violations: objects created outside spawn roots,
    /// destroyed objects, added/removed components, renderer toggles outside <c>_UI</c>. Activation changes are info.
    /// JSON DTO (public snake_case fields, see README).
    /// </summary>
    [Serializable]
    public sealed class HierarchyDiffReport
    {
        public string scene;
        public string edit_captured_utc;
        public string play_captured_utc;
        public int edit_nodes;
        public int play_nodes;
        public string[] spawn_roots;
        public string[] created_allowed;
        public string[] created_violations;
        public string[] destroyed;
        public string[] components_added;
        public string[] components_removed;
        public string[] renderer_toggled;
        public string[] activation_changed;
        public string[] ignored;
        public bool pass;
    }
}
