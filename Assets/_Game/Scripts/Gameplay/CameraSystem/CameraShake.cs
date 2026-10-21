using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Options;
using Unity.Cinemachine;
using UnityEngine;

namespace ProfessorSprat.Gameplay.CameraSystem
{
    /// <summary>
    /// Camera shake (D-287; TDD §B stun / locked-door shakes): a Cinemachine impulse on <see cref="CameraShakeEvent"/>,
    /// heard by the impulse listeners on the gameplay cameras. Off when the player disables camera shake.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraShake : MonoBehaviour
    {
        #region Fields

        private CinemachineImpulseSource _source;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _source = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CameraShakeEvent>(OnShake);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CameraShakeEvent>(OnShake);
        }

        #endregion

        #region Private Methods

        private void OnShake(CameraShakeEvent shake)
        {
            if (!GameSettings.CameraShake || shake.Strength <= 0f)
            {
                return;
            }

            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) + 0.5f;
            _source.GenerateImpulseWithVelocity(direction.normalized * shake.Strength);
        }

        #endregion
    }
}
