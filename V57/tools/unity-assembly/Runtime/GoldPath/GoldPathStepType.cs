namespace V57.GoldPath
{
    /// <summary>Kinds of gold path steps executed by <see cref="GoldPathStepExecutor"/>.</summary>
    public enum GoldPathStepType
    {
        Press,
        Hold,
        Release,
        Move,
        Wait,
        WaitUntil,
        Expect,
        Capture
    }
}
