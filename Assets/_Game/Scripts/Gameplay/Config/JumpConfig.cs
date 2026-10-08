using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B Jump tuning (read-only at runtime).</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Jump", fileName = "JumpConfig")]
    public sealed class JumpConfig : ScriptableObject
    {
        public const float Gravity = 9.81f;

        [SerializeField] private float _jumpImpulse = 6.6f;
        [SerializeField] private float _fallingGravityScale = 1.8f;
        [SerializeField] private float _coyoteTime = 0.12f;
        [SerializeField] private float _jumpBuffer = 0.10f;
        [SerializeField] private float _dropShadowRadius = 0.35f;

        public float JumpImpulse => _jumpImpulse;
        public float FallingGravityScale => _fallingGravityScale;
        public float CoyoteTime => _coyoteTime;
        public float JumpBuffer => _jumpBuffer;
        public float DropShadowRadius => _dropShadowRadius;
    }
}
