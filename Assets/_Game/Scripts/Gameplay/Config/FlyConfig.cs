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
        [Tooltip("Emissive tint (multiplied by the fly's own colours and by the glow intensity). TDD: cyan glow.")]
        [SerializeField] private Color _glowColor = new Color(0.12f, 0.42f, 0.5f, 1f);

        public float TriggerRadius => _triggerRadius;
        public float PopDuration => _popDuration;
        public float GlowIntensity => _glowIntensity;
        public Color GlowColor => _glowColor;
    }
}
