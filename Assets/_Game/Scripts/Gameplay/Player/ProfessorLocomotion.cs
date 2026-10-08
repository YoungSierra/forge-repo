using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// TDD §B Locomotion: owns the Professor's CharacterController. Each FixedUpdate combines the horizontal velocity
    /// (<see cref="LocomotionModel"/>) with the vertical velocity of <see cref="ProfessorJump"/>, moves once, resolves
    /// landing (which may chain stomp → crab defeat → bounce in the same step) and publishes the movement events.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-100)]
    public sealed class ProfessorLocomotion : MonoBehaviour
    {
        #region Fields

        [Header("Config")]
        [SerializeField] private LocomotionConfig _config;
        [Header("Same body")]
        [SerializeField] private ProfessorJump _jump;
        [SerializeField] private ProfessorStomp _stomp;
        [SerializeField] private BodyInterpolation _interpolation;

        private readonly LocomotionModel _model = new LocomotionModel();
        private CharacterController _controller;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _hitNormal = Vector3.up;

        #endregion

        #region Public Methods

        public LocomotionConfig Config => _config;

        /// <summary>The rendered body (interpolated between physics steps): what camera, Sprat and the shadow follow.</summary>
        public Transform Body => _interpolation != null ? _interpolation.Body : transform;

        public bool IsGrounded => _model.Grounded;

        public bool IsStunned => _model.IsStunned;

        public LocomotionState State => _model.State;

        public Vector3 HorizontalVelocity => _model.HorizontalVelocity;

        public Vector3 Velocity => _model.HorizontalVelocity + Vector3.up * (_jump != null ? _jump.VerticalVelocity : 0f);

        public Vector3 Center => transform.position + Vector3.up * (_controller != null ? _controller.center.y : 0.5f);

        public void SetMoveInput(Vector3 worldDirection)
        {
            _model.SetMoveInput(worldDirection);
        }

        public void SetHorizontalLock(bool locked)
        {
            _model.HorizontalLocked = locked;
        }

        /// <summary>Crab contact: horizontal knockback + stun (ignored while already stunned).</summary>
        public void ApplyKnockback(Vector3 direction)
        {
            if (_model.BeginStun(_config.StunDuration, direction, _config.KnockbackDistance, _config.KnockbackDuration))
            {
                EventBus.Publish(new StunBeganEvent(_config.StunDuration));
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            bool wasStunned = _model.IsStunned;
            _model.ResetMotion();
            _jump.ResetAir(position);
            if (wasStunned)
            {
                EventBus.Publish(new StunEndedEvent());
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_config == null || _jump == null)
            {
                Debug.LogError($"{nameof(ProfessorLocomotion)} on {name}: config or jump reference missing; disabled.", this);
                enabled = false;
                return;
            }

            _controller.slopeLimit = _config.SlopeLimit;
            _controller.stepOffset = _config.StepHeight;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CrabContactPlayerEvent>(OnCrabContact);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CrabContactPlayerEvent>(OnCrabContact);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            if (_model.Tick(deltaTime))
            {
                EventBus.Publish(new StunEndedEvent());
            }

            bool steep = _model.Grounded && Vector3.Angle(_groundNormal, Vector3.up) > _config.SlopeLimit + 0.5f;
            _model.Sliding = steep;
            Vector3 downhill = steep ? Vector3.ProjectOnPlane(Vector3.down, _groundNormal) : Vector3.zero;
            Vector3 horizontal = _model.StepHorizontal(_config, deltaTime, downhill);
            float vertical = _jump.StepVertical(deltaTime, _model.Grounded);
            _hitNormal = Vector3.up;
            _controller.Move((horizontal + Vector3.up * vertical) * deltaTime);
            _groundNormal = _hitNormal;
            ResolveGrounded(_controller.isGrounded);
            Face(horizontal, deltaTime);
            bool stomping = _stomp != null && _stomp.IsStomping;
            EventBus.Publish(new PlayerMovedEvent(transform.position, Center, Velocity, stomping, _model.IsStunned));
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            bool underFeet = hit.point.y < transform.position.y + _controller.radius;
            if (underFeet && hit.normal.y > 0.05f && hit.normal.y < _hitNormal.y + 0.0001f)
            {
                _hitNormal = hit.normal;
            }
        }

        #endregion

        #region Private Methods

        private void ResolveGrounded(bool grounded)
        {
            if (grounded == _model.Grounded)
            {
                return;
            }

            _model.Grounded = grounded;
            if (grounded)
            {
                _jump.NotifyLanded(transform.position);
            }
            else
            {
                _jump.NotifyLeftGround(transform.position);
            }

            EventBus.Publish(new GroundedChangedEvent(grounded, transform.position));
        }

        private void Face(Vector3 horizontal, float deltaTime)
        {
            if (_model.IsStunned || _model.HorizontalLocked || horizontal.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(new Vector3(horizontal.x, 0f, horizontal.z), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _config.TurnRate * deltaTime);
        }

        private void OnCrabContact(CrabContactPlayerEvent contact)
        {
            ApplyKnockback(contact.KnockbackDirection);
        }

        #endregion
    }
}
