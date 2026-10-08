using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B Stomp tuning (read-only at runtime).</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Stomp", fileName = "StompConfig")]
    public sealed class StompConfig : ScriptableObject
    {
        [Tooltip("Multiplier on the falling gravity while stomping (3.0 x 17.66 = 52.97 m/s2).")]
        [SerializeField] private float _stompGravityMultiplier = 3.0f;
        [SerializeField] private float _impactRadius = 1.0f;
        [SerializeField] private float _bounceHeight = 0.8f;
        [SerializeField] private float _landRecovery = 0.25f;
        [SerializeField] private float _minTimeAfterJump = 0.15f;

        public float StompGravityMultiplier => _stompGravityMultiplier;
        public float ImpactRadius => _impactRadius;
        public float BounceHeight => _bounceHeight;
        public float LandRecovery => _landRecovery;
        public float MinTimeAfterJump => _minTimeAfterJump;
    }
}
