using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B SpratCompanion tuning.</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Sprat", fileName = "SpratConfig")]
    public sealed class SpratConfig : ScriptableObject
    {
        [SerializeField] private Vector3 _shoulderOffset = new Vector3(0.55f, 1.35f, -0.25f);
        [SerializeField] private float _followSmoothTime = 0.18f;
        [SerializeField] private float _maxLag = 1.5f;
        [SerializeField] private float _snapDistance = 4.0f;
        [SerializeField] private float _excitedDuration = 1.0f;
        [SerializeField] private float _spinDuration = 1.0f;
        [SerializeField] private float _yawRate = 360f;

        public Vector3 ShoulderOffset => _shoulderOffset;
        public float FollowSmoothTime => _followSmoothTime;
        public float MaxLag => _maxLag;
        public float SnapDistance => _snapDistance;
        public float ExcitedDuration => _excitedDuration;
        public float SpinDuration => _spinDuration;
        public float YawRate => _yawRate;
    }
}
