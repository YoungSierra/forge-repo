namespace V57.GoldPath
{
    /// <summary>
    /// Game-side observer that answers gold path conditions (e.g. <c>session.state</c>, <c>score.value</c>).
    /// Probes are read-only: they observe public state and never mutate gameplay.
    /// Return false for keys the probe does not know (the check then fails as "unknown", never true).
    /// Register in <c>OnEnable</c> and unregister in <c>OnDisable</c> via <see cref="GoldPathProbeRegistry"/>.
    /// </summary>
    public interface IGoldPathProbe
    {
        bool TryGetBool(string key, out bool value);

        bool TryGetFloat(string key, out float value);

        bool TryGetString(string key, out string value);
    }
}
