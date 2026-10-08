namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>Stomp states (TDD §B Stomp state machine).</summary>
    public enum StompState
    {
        Idle,
        Stomping,
        LandRecovery
    }

    /// <summary>Plain runtime state of the stomp; owned by <see cref="ProfessorStomp"/>.</summary>
    public sealed class StompModel
    {
        #region Public Methods

        public StompState State { get; private set; } = StompState.Idle;

        public bool UsedThisAir { get; private set; }

        public float RecoveryRemaining { get; private set; }

        public bool DefeatOnLanding { get; set; }

        /// <summary>
        /// Eligibility: airborne, not stunned, not used this airtime, and either airborne without a jump (fell off a ledge)
        /// or at least <paramref name="minTimeAfterJump"/> after the jump started.
        /// </summary>
        public bool CanStart(bool airborne, bool stunned, bool jumpedThisAir, float timeSinceJump, float minTimeAfterJump)
        {
            if (State != StompState.Idle || !airborne || stunned || UsedThisAir)
            {
                return false;
            }

            return !jumpedThisAir || timeSinceJump >= minTimeAfterJump;
        }

        public void Start()
        {
            State = StompState.Stomping;
            UsedThisAir = true;
            DefeatOnLanding = false;
        }

        /// <summary>Landing while stomping: bounce (back to Idle, stomp available again) or recovery.</summary>
        public void Land(float recovery)
        {
            if (DefeatOnLanding)
            {
                State = StompState.Idle;
                UsedThisAir = false;
                return;
            }

            State = StompState.LandRecovery;
            RecoveryRemaining = recovery;
        }

        /// <summary>Returns true on the step recovery ends.</summary>
        public bool Tick(float deltaTime)
        {
            if (State != StompState.LandRecovery)
            {
                return false;
            }

            RecoveryRemaining -= deltaTime;
            if (RecoveryRemaining > 0f)
            {
                return false;
            }

            State = StompState.Idle;
            UsedThisAir = false;
            return true;
        }

        public void ResetAirUse()
        {
            if (State == StompState.Idle)
            {
                UsedThisAir = false;
            }
        }

        #endregion
    }
}
