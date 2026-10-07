namespace V57.Assembly
{
    /// <summary>Run options shared by the assembly steps.</summary>
    public static class AssemblyOptions
    {
        /// <summary>
        /// When true, existing Visual prefabs, animator controllers and level scenes are regenerated (same path/GUID).
        /// Default false: assets that already exist are kept to protect later gameplay edits.
        /// </summary>
        public static bool Force { get; set; }

        /// <summary>
        /// True while <c>RebuildLevelContent</c> replaces only the level content of an existing scene: marker cameras are
        /// not created again (<c>_Cameras</c> belongs to the scene's gameplay setup).
        /// </summary>
        public static bool LevelContentOnly { get; set; }
    }
}
