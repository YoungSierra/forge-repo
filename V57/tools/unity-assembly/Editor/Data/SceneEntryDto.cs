using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// One <c>scenes.scenes[]</c> entry.
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class SceneEntryDto
    {
        public string id;
        public string purpose;
        public string world_owner;
        public string[] systems;
        public string[] acs;
        public bool slice;
        public string blockout;
        /// <summary>LevelMaps level id whose layout (layouts.json) places the level art; wins over <see cref="blockout"/>.</summary>
        public string layout;
        public string[] markers;
        public string camera_ref;
    }
}
