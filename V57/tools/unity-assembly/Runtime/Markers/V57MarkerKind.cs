namespace V57.GoldPath
{
    /// <summary>Marker type parsed from provider FBX node names <c>Marker_&lt;Type&gt;_&lt;Id&gt;</c>.</summary>
    public enum V57MarkerKind
    {
        Spawn,
        Zone,
        Bounds,
        Camera,
        Checkpoint,
        Patrol,
        Other,

        // Appended (serialized values of the kinds above stay stable).
        Exit,
        Kill,
        CameraZone
    }
}
