using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §B-S RespawnService.</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Respawn", fileName = "RespawnConfig")]
    public sealed class RespawnConfig : ScriptableObject
    {
        [SerializeField] private float _safeGroundTime = 0.5f;
        [SerializeField] private float _fadeOut = 0.25f;
        [SerializeField] private float _fadeIn = 0.25f;
        [SerializeField] private float _fallFloorY = -50f;

        public float SafeGroundTime => _safeGroundTime;
        public float FadeOut => _fadeOut;
        public float FadeIn => _fadeIn;
        public float FallFloorY => _fallFloorY;
    }
}
