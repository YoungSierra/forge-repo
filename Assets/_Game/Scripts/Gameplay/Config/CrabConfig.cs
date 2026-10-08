using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B CrabEncounter tuning.</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Crab", fileName = "CrabConfig")]
    public sealed class CrabConfig : ScriptableObject
    {
        [SerializeField] private float _patrolSpeed = 1.2f;
        [SerializeField] private float _endpointPause = 0.4f;
        [SerializeField] private float _maxRange = 4.0f;
        [SerializeField] private float _bodyRadius = 0.5f;
        [Tooltip("Professor capsule radius added to the body radius for the contact test.")]
        [SerializeField] private float _playerRadius = 0.35f;
        [SerializeField] private float _contactReactionDuration = 0.5f;
        [SerializeField] private float _warnRadius = 3.0f;
        [SerializeField] private float _warnReleaseRadius = 3.5f;
        [SerializeField] private float _defeatDuration = 0.4f;

        public float PatrolSpeed => _patrolSpeed;
        public float EndpointPause => _endpointPause;
        public float MaxRange => _maxRange;
        public float BodyRadius => _bodyRadius;
        public float PlayerRadius => _playerRadius;
        public float ContactReactionDuration => _contactReactionDuration;
        public float WarnRadius => _warnRadius;
        public float WarnReleaseRadius => _warnReleaseRadius;
        public float DefeatDuration => _defeatDuration;
    }
}
