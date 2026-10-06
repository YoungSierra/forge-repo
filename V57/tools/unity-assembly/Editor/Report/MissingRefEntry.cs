using System;

namespace V57.Assembly.Report
{
    /// <summary>A missing script or broken object reference found in a created prefab/scene. JSON DTO, see README.</summary>
    [Serializable]
    public sealed class MissingRefEntry
    {
        public string asset;
        public string object_path;
        public string component;
        public string property;
    }
}
