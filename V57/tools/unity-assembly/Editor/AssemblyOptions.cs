namespace V57.Assembly
{
    /// <summary>Run options shared by the assembly steps.</summary>
    public static class AssemblyOptions
    {
        /// <summary>
        /// When true, existing real Visual prefabs and level scenes are regenerated (same path/GUID).
        /// Default false: scenes/prefabs that already exist are kept to protect later gameplay edits.
        /// Placeholders are always refreshed and always replaced once real art exists.
        /// </summary>
        public static bool Force { get; set; }
    }
}
