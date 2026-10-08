using ProfessorSprat.Gameplay.Config;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// TDD §B Jump drop shadow: a URP decal projected straight down under the Professor (radius 0.35 m, max 20 m), placed
    /// on the first surface below every frame. The decal material is a delivered asset; while missing the projector stays
    /// empty and one warning is logged (no generated texture).
    /// </summary>
    public sealed class DropShadow : MonoBehaviour
    {
        #region Fields

        private const float MaxDistance = 20f;

        [SerializeField] private JumpConfig _config;
        [SerializeField] private Transform _professor;
        [SerializeField] private DecalProjector _projector;
        [SerializeField] private LayerMask _surfaces = ~0;

        private bool _warned;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_projector == null || _professor == null)
            {
                enabled = false;
                return;
            }

            float size = (_config != null ? _config.DropShadowRadius : 0.35f) * 2f;
            _projector.size = new Vector3(size, size, MaxDistance);
            if (_projector.material == null && !_warned)
            {
                Debug.LogWarning($"{nameof(DropShadow)}: drop shadow decal material not delivered (Docs/V57/MISSING_ASSETS.md).", this);
                _warned = true;
            }
        }

        private void LateUpdate()
        {
            Vector3 origin = _professor.position + Vector3.up * 0.2f;
            bool hit = Physics.Raycast(origin, Vector3.down, out RaycastHit ground, MaxDistance, _surfaces, QueryTriggerInteraction.Ignore);
            _projector.enabled = hit && _projector.material != null;
            if (hit)
            {
                transform.SetPositionAndRotation(ground.point + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.down, _professor.forward));
            }
        }

        #endregion
    }
}
