namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>Access Key states (TDD §B KeyMaterialisation).</summary>
    public enum KeyState
    {
        Dormant,
        Spawning,
        Waiting,
        Carried
    }

    /// <summary>Plain runtime state of one zone's Access Key; owned by <see cref="AccessKeyController"/>.</summary>
    public sealed class AccessKeyModel
    {
        #region Public Methods

        public string ZoneId { get; private set; }

        public KeyState State { get; private set; } = KeyState.Dormant;

        public float CeremonyElapsed { get; private set; }

        public void Arm(string zoneId)
        {
            ZoneId = zoneId;
            State = KeyState.Dormant;
            CeremonyElapsed = 0f;
        }

        /// <summary>Only the arming zone's completion spawns the key, once (idempotent).</summary>
        public bool TrySpawn(string zoneId)
        {
            if (State != KeyState.Dormant || zoneId != ZoneId)
            {
                return false;
            }

            State = KeyState.Spawning;
            CeremonyElapsed = 0f;
            return true;
        }

        /// <summary>Advances the ceremony; returns true on the step it ends.</summary>
        public bool TickCeremony(float deltaTime, float duration)
        {
            if (State != KeyState.Spawning)
            {
                return false;
            }

            CeremonyElapsed += deltaTime;
            if (CeremonyElapsed < duration)
            {
                return false;
            }

            State = KeyState.Waiting;
            return true;
        }

        /// <summary>Pickup only after the ceremony (Waiting) and within the radius.</summary>
        public bool TryCollect(float distance, float pickupRadius)
        {
            if (State != KeyState.Waiting || distance > pickupRadius)
            {
                return false;
            }

            State = KeyState.Carried;
            return true;
        }

        #endregion
    }
}
