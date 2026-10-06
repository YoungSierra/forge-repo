namespace V57.GoldPath
{
    /// <summary>Immutable, validated gold path step produced by <see cref="GoldPathStepParser"/>.</summary>
    public sealed class GoldPathStep
    {
        #region Public Methods

        public GoldPathStep(
            GoldPathStepType type,
            string id,
            string target,
            string value,
            float seconds,
            int frames,
            GoldPathCondition condition,
            float timeoutSeconds,
            bool captureAfter)
        {
            Type = type;
            Id = id ?? string.Empty;
            Target = target ?? string.Empty;
            Value = value ?? string.Empty;
            Seconds = seconds < 0f ? 0f : seconds;
            Frames = frames < 0 ? 0 : frames;
            Condition = condition;
            TimeoutSeconds = timeoutSeconds < 0f ? 0f : timeoutSeconds;
            CaptureAfter = captureAfter;
        }

        public GoldPathStepType Type { get; }

        /// <summary>Step id from JSON (<c>S01</c>) or generated from the index.</summary>
        public string Id { get; }

        /// <summary><c>Map/Action</c> of an enabled/project-wide InputAction, or a control path (<c>&lt;Keyboard&gt;/space</c>).</summary>
        public string Target { get; }

        /// <summary>Raw value: <c>"x,y"</c> for Move/touch position, capture name for Capture.</summary>
        public string Value { get; }

        public float Seconds { get; }

        public int Frames { get; }

        public GoldPathCondition Condition { get; }

        public float TimeoutSeconds { get; }

        public bool CaptureAfter { get; }

        public string Describe()
        {
            switch (Type)
            {
                case GoldPathStepType.Wait:
                    return $"{Seconds}s";
                case GoldPathStepType.WaitUntil:
                    return $"{Condition} (timeout {TimeoutSeconds}s)";
                case GoldPathStepType.Expect:
                    return Condition?.ToString() ?? string.Empty;
                case GoldPathStepType.Capture:
                    return Value;
                default:
                    string suffix = Value.Length > 0 ? $" = {Value}" : string.Empty;
                    string duration = Seconds > 0f ? $" for {Seconds}s" : string.Empty;
                    return Target + suffix + duration;
            }
        }

        #endregion
    }
}
