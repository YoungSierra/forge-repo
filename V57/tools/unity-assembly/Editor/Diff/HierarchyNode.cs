using System;

namespace V57.Assembly.Diff
{
    /// <summary>
    /// One GameObject in a hierarchy snapshot. <c>path</c> is root-relative (<c>_Gameplay/Player</c>), duplicate
    /// sibling names get <c>#n</c>. <c>renderer_enabled</c>: -1 no renderer, 0 disabled, 1 enabled.
    /// JSON DTO (public snake_case fields, see README).
    /// </summary>
    [Serializable]
    public sealed class HierarchyNode
    {
        public string path;
        public string[] components;
        public bool active_self;
        public int renderer_enabled;
    }
}
