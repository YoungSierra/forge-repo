using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B FlyCollection tuning.</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Fly", fileName = "FlyConfig")]
    public sealed class FlyConfig : ScriptableObject
    {
        [SerializeField] private float _triggerRadius = 0.6f;
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _glowIntensity = 2.5f;

        public float TriggerRadius => _triggerRadius;
        public float PopDuration => _popDuration;
        public float GlowIntensity => _glowIntensity;
    }
}
