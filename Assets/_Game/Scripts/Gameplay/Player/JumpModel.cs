namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// Plain runtime state of the Professor's vertical motion: velocity, jump buffer, coyote window and airtime.
    /// Owned by <see cref="ProfessorJump"/>; engine-free for EditMode tests.
    /// </summary>
    public sealed class JumpModel
    {
        #region Public Methods

        public float VerticalVelocity { get; set; }

        public float BufferRemaining { get; private set; }

        public float CoyoteRemaining { get; private set; }

        public float TimeSinceJumpStart { get; private set; } = float.MaxValue;

        public bool JumpedThisAir { get; private set; }

        public float ApexY { get; set; }

        public void Press(float buffer)
        {
            BufferRemaining = buffer;
        }

        public void Tick(float deltaTime)
        {
            BufferRemaining = BufferRemaining > 0f ? BufferRemaining - deltaTime : 0f;
            CoyoteRemaining = CoyoteRemaining > 0f ? CoyoteRemaining - deltaTime : 0f;
            if (TimeSinceJumpStart < float.MaxValue)
            {
                TimeSinceJumpStart += deltaTime;
            }
        }

        /// <summary>Starts a jump when a buffered press meets ground or the coyote window; never a second jump in the air.</summary>
        public bool TryStart(bool grounded, bool stunned, float impulse)
        {
            if (BufferRemaining <= 0f || stunned)
            {
                return false;
            }

            if (!grounded && (CoyoteRemaining <= 0f || JumpedThisAir))
            {
                return false;
            }

            VerticalVelocity = impulse;
            JumpedThisAir = true;
            BufferRemaining = 0f;
            CoyoteRemaining = 0f;
            TimeSinceJumpStart = 0f;
            return true;
        }

        public void LeftGround(float coyoteTime, float y)
        {
            ApexY = y;
            CoyoteRemaining = JumpedThisAir ? 0f : coyoteTime;
        }

        /// <summary>Returns the fall distance from the airtime apex.</summary>
        public float Landed(float y)
        {
            float fall = ApexY > y ? ApexY - y : 0f;
            JumpedThisAir = false;
            CoyoteRemaining = 0f;
            ApexY = y;
            return fall;
        }

        public void Bounce(float speed)
        {
            VerticalVelocity = speed;
            JumpedThisAir = true;
            TimeSinceJumpStart = 0f;
        }

        public void Reset(float y)
        {
            VerticalVelocity = 0f;
            BufferRemaining = 0f;
            CoyoteRemaining = 0f;
            JumpedThisAir = false;
            TimeSinceJumpStart = float.MaxValue;
            ApexY = y;
        }

        #endregion
    }
}
