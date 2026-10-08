using UnityEngine;

namespace ProfessorSprat.Core
{
    /// <summary>Every FixedUpdate (TDD OnPlayerMoved). <c>Center</c> = capsule centre used by proximity rules.</summary>
    public readonly struct PlayerMovedEvent
    {
        public PlayerMovedEvent(Vector3 position, Vector3 center, Vector3 velocity, bool isStomping, bool isStunned)
        {
            Position = position;
            Center = center;
            Velocity = velocity;
            IsStomping = isStomping;
            IsStunned = isStunned;
        }

        public Vector3 Position { get; }

        public Vector3 Center { get; }

        public Vector3 Velocity { get; }

        public bool IsStomping { get; }

        public bool IsStunned { get; }
    }

    /// <summary>TDD OnGroundedChanged.</summary>
    public readonly struct GroundedChangedEvent
    {
        public GroundedChangedEvent(bool isGrounded, Vector3 position)
        {
            IsGrounded = isGrounded;
            Position = position;
        }

        public bool IsGrounded { get; }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnStunBegin.</summary>
    public readonly struct StunBeganEvent
    {
        public StunBeganEvent(float duration)
        {
            Duration = duration;
        }

        public float Duration { get; }
    }

    /// <summary>TDD OnStunEnd.</summary>
    public readonly struct StunEndedEvent
    {
    }

    /// <summary>TDD OnJumpStarted.</summary>
    public readonly struct JumpStartedEvent
    {
        public JumpStartedEvent(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnLanded.</summary>
    public readonly struct LandedEvent
    {
        public LandedEvent(Vector3 position, float fallDistance)
        {
            Position = position;
            FallDistance = fallDistance;
        }

        public Vector3 Position { get; }

        public float FallDistance { get; }
    }

    /// <summary>TDD OnStompStarted.</summary>
    public readonly struct StompStartedEvent
    {
        public StompStartedEvent(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnStompLanded.</summary>
    public readonly struct StompLandedEvent
    {
        public StompLandedEvent(Vector3 position, float impactRadius)
        {
            Position = position;
            ImpactRadius = impactRadius;
        }

        public Vector3 Position { get; }

        public float ImpactRadius { get; }
    }

    /// <summary>TDD OnPlayerRespawned.</summary>
    public readonly struct PlayerRespawnedEvent
    {
        public PlayerRespawnedEvent(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnPauseChanged.</summary>
    public readonly struct PauseChangedEvent
    {
        public PauseChangedEvent(bool paused)
        {
            Paused = paused;
        }

        public bool Paused { get; }
    }
}
