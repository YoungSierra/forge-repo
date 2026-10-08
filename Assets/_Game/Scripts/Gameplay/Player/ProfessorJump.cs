using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// TDD §B Jump: single jump (impulse 6.6 m/s), coyote 0.12 s, buffer 0.10 s, falling gravity 1.8x; stomp gravity is
    /// applied here while <see cref="ProfessorStomp"/> is stomping. Called by <see cref="ProfessorLocomotion"/> once per step.
    /// </summary>
    public sealed class ProfessorJump : MonoBehaviour
    {
        #region Fields

        private const float GroundStickVelocity = -2f;

        [Header("Config")]
        [SerializeField] private JumpConfig _config;
        [Header("Same body")]
        [SerializeField] private ProfessorLocomotion _locomotion;
        [SerializeField] private ProfessorStomp _stomp;

        private readonly JumpModel _model = new JumpModel();

        #endregion

        #region Public Methods

        public JumpConfig Config => _config;

        public bool IsAirborne => _locomotion != null && !_locomotion.IsGrounded;

        public float VerticalVelocity => _model.VerticalVelocity;

        public float TimeSinceJumpStart => _model.TimeSinceJumpStart;

        public bool JumpedThisAir => _model.JumpedThisAir;

        /// <summary>Jump button press (InputHandler). Buffered for 0.10 s.</summary>
        public void PressJump()
        {
            _model.Press(_config.JumpBuffer);
        }

        /// <summary>Vertical velocity for this physics step (m/s).</summary>
        public float StepVertical(float deltaTime, bool grounded)
        {
            _model.Tick(deltaTime);
            if (_model.TryStart(grounded, _locomotion.IsStunned, _config.JumpImpulse))
            {
                EventBus.Publish(new JumpStartedEvent(transform.position));
                return _model.VerticalVelocity;
            }

            if (grounded && _model.VerticalVelocity <= 0f)
            {
                _model.VerticalVelocity = GroundStickVelocity;
                return _model.VerticalVelocity;
            }

            float scale = _model.VerticalVelocity > 0f ? 1f : _config.FallingGravityScale;
            if (_stomp != null && _stomp.IsStomping)
            {
                scale = _config.FallingGravityScale * _stomp.Config.StompGravityMultiplier;
            }

            _model.VerticalVelocity -= JumpConfig.Gravity * scale * deltaTime;
            _model.ApexY = Mathf.Max(_model.ApexY, transform.position.y);
            return _model.VerticalVelocity;
        }

        public void NotifyLeftGround(Vector3 position)
        {
            _model.LeftGround(_config.CoyoteTime, position.y);
        }

        public void NotifyLanded(Vector3 position)
        {
            float fall = _model.Landed(position.y);
            EventBus.Publish(new LandedEvent(position, fall));
        }

        public void SetVerticalVelocity(float velocity)
        {
            _model.VerticalVelocity = velocity;
        }

        public void ApplyBounce(float speed)
        {
            _model.Bounce(speed);
        }

        public void ResetAir(Vector3 position)
        {
            _model.Reset(position.y);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _locomotion == null)
            {
                Debug.LogError($"{nameof(ProfessorJump)} on {name}: config or locomotion reference missing; disabled.", this);
                enabled = false;
            }
        }

        #endregion
    }
}
