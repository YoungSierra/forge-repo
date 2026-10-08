using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>Locomotion states (TDD §B Locomotion state machine).</summary>
    public enum LocomotionState
    {
        GroundedIdle,
        GroundedRun,
        Airborne,
        Sliding,
        Stunned
    }

    /// <summary>
    /// Plain runtime state of the Professor's horizontal movement: velocity, input, stun and knockback timers. Owned by
    /// <see cref="ProfessorLocomotion"/>; deterministic and engine-free so EditMode tests can drive it.
    /// </summary>
    public sealed class LocomotionModel
    {
        #region Fields

        private const float InputDeadzone = 0.05f;
        private Vector3 _knockbackVelocity;

        #endregion

        #region Public Methods

        public Vector3 HorizontalVelocity { get; private set; }

        public Vector3 MoveInput { get; private set; }

        public bool Grounded { get; set; }

        public bool Sliding { get; set; }

        public bool HorizontalLocked { get; set; }

        public float StunRemaining { get; private set; }

        public float KnockbackRemaining { get; private set; }

        public bool IsStunned => StunRemaining > 0f;

        public LocomotionState State
        {
            get
            {
                if (IsStunned)
                {
                    return LocomotionState.Stunned;
                }

                if (!Grounded)
                {
                    return LocomotionState.Airborne;
                }

                if (Sliding)
                {
                    return LocomotionState.Sliding;
                }

                return HorizontalVelocity.sqrMagnitude > 0.01f ? LocomotionState.GroundedRun : LocomotionState.GroundedIdle;
            }
        }

        public void SetMoveInput(Vector3 worldDirection)
        {
            Vector3 flat = new Vector3(worldDirection.x, 0f, worldDirection.z);
            MoveInput = flat.magnitude < InputDeadzone ? Vector3.zero : Vector3.ClampMagnitude(flat, 1f);
        }

        /// <summary>Starts a stun with a horizontal knockback; false when already stunned (no stacking).</summary>
        public bool BeginStun(float stunDuration, Vector3 direction, float distance, float duration)
        {
            if (IsStunned)
            {
                return false;
            }

            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            StunRemaining = stunDuration;
            KnockbackRemaining = duration;
            _knockbackVelocity = flat.sqrMagnitude > 0f && duration > 0f ? flat.normalized * (distance / duration) : Vector3.zero;
            return true;
        }

        /// <summary>Advances timers; returns true on the step the stun ends.</summary>
        public bool Tick(float deltaTime)
        {
            KnockbackRemaining = Mathf.Max(0f, KnockbackRemaining - deltaTime);
            if (!IsStunned)
            {
                return false;
            }

            StunRemaining = Mathf.Max(0f, StunRemaining - deltaTime);
            return !IsStunned;
        }

        /// <summary>Horizontal velocity for this step (m/s).</summary>
        public Vector3 StepHorizontal(LocomotionConfig config, float deltaTime, Vector3 downhill)
        {
            if (KnockbackRemaining > 0f)
            {
                HorizontalVelocity = _knockbackVelocity;
                return HorizontalVelocity;
            }

            if (HorizontalLocked)
            {
                HorizontalVelocity = Vector3.zero;
                return HorizontalVelocity;
            }

            if (Sliding && Grounded)
            {
                Vector3 slide = new Vector3(downhill.x, 0f, downhill.z);
                HorizontalVelocity = slide.sqrMagnitude > 0f ? slide.normalized * config.SlideSpeed : Vector3.zero;
                return HorizontalVelocity;
            }

            Vector3 desired = IsStunned ? Vector3.zero : MoveInput * config.RunSpeed;
            if (Grounded)
            {
                float rate = desired.sqrMagnitude > 0f ? config.Acceleration : config.Deceleration;
                HorizontalVelocity = Vector3.MoveTowards(HorizontalVelocity, desired, rate * deltaTime);
            }
            else if (desired.sqrMagnitude > 0f)
            {
                HorizontalVelocity = Vector3.MoveTowards(HorizontalVelocity, desired, config.Acceleration * config.AirControl * deltaTime);
            }

            return HorizontalVelocity;
        }

        public void ResetMotion()
        {
            HorizontalVelocity = Vector3.zero;
            StunRemaining = 0f;
            KnockbackRemaining = 0f;
            Sliding = false;
            HorizontalLocked = false;
            Grounded = false;
        }

        #endregion
    }
}
