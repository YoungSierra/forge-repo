using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B Locomotion tuning (read-only at runtime).</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Locomotion", fileName = "LocomotionConfig")]
    public sealed class LocomotionConfig : ScriptableObject
    {
        [Header("Ground")]
        [SerializeField] private float _runSpeed = 5.5f;
        [SerializeField] private float _acceleration = 18f;
        [SerializeField] private float _deceleration = 22f;
        [SerializeField] private float _turnRate = 720f;
        [Header("Air and slopes")]
        [Tooltip("Fraction of ground acceleration available in the air.")]
        [SerializeField] private float _airControl = 0.4f;
        [SerializeField] private float _slopeLimit = 45f;
        [SerializeField] private float _slideSpeed = 2.0f;
        [SerializeField] private float _stepHeight = 0.25f;
        [Header("Crab contact")]
        [SerializeField] private float _knockbackDistance = 2.5f;
        [SerializeField] private float _knockbackDuration = 0.5f;
        [SerializeField] private float _stunDuration = 0.5f;

        public float RunSpeed => _runSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float TurnRate => _turnRate;
        public float AirControl => _airControl;
        public float SlopeLimit => _slopeLimit;
        public float SlideSpeed => _slideSpeed;
        public float StepHeight => _stepHeight;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;
        public float StunDuration => _stunDuration;
    }
}
