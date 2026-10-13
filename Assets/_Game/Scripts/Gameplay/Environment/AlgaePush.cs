using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// Lets the Professor push algae aside (D-263): sends his centre and a push radius to the Algae Sway shader
    /// (global <c>_AlgaePusher</c>). The position arrives with <see cref="PlayerMovedEvent"/> at the physics rate and is
    /// smoothed per frame, so the plants do not step. Visual only.
    /// </summary>
    public sealed class AlgaePush : MonoBehaviour
    {
        #region Fields

        private static readonly int PusherId = Shader.PropertyToID("_AlgaePusher");

        [Tooltip("Distance (metres) around the Professor within which algae bend away.")]
        [SerializeField] private float _radius = 0.9f;
        [Tooltip("How quickly the push follows the Professor (1/s).")]
        [SerializeField] private float _follow = 12f;

        private Vector3 _target;
        private Vector3 _current;
        private bool _hasTarget;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            Shader.SetGlobalVector(PusherId, Vector4.zero);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            Shader.SetGlobalVector(PusherId, Vector4.zero);
            _hasTarget = false;
        }

        private void Update()
        {
            if (!_hasTarget)
            {
                return;
            }

            _current = Vector3.Lerp(_current, _target, 1f - Mathf.Exp(-_follow * Time.deltaTime));
            Shader.SetGlobalVector(PusherId, new Vector4(_current.x, _current.y, _current.z, _radius));
        }

        #endregion

        #region Private Methods

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (!_hasTarget || (moved.Center - _target).sqrMagnitude > 4f)
            {
                _current = moved.Center;
            }

            _target = moved.Center;
            _hasTarget = true;
        }

        #endregion
    }
}
