using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// TDD §B Stomp: air-only straight-down slam (horizontal speed 0, gravity 3x falling). Landing publishes
    /// <see cref="StompLandedEvent"/>; a crab defeated by that landing (synchronous <see cref="CrabDefeatedEvent"/>) bounces
    /// the Professor 0.8 m, otherwise a 0.25 s recovery locks movement.
    /// </summary>
    public sealed class ProfessorStomp : MonoBehaviour
    {
        #region Fields

        [Header("Config")]
        [SerializeField] private StompConfig _config;
        [Header("Same body")]
        [SerializeField] private ProfessorJump _jump;
        [SerializeField] private ProfessorLocomotion _locomotion;

        private readonly StompModel _model = new StompModel();

        #endregion

        #region Public Methods

        public StompConfig Config => _config;

        public bool IsStomping => _model.State == StompState.Stomping;

        public StompState State => _model.State;

        public float ImpactRadius => _config.ImpactRadius;

        /// <summary>Stomp button press (InputHandler). Ignored when not eligible (grounded press → no state change).</summary>
        public void PressStomp()
        {
            if (!_model.CanStart(_jump.IsAirborne, _locomotion.IsStunned, _jump.JumpedThisAir, _jump.TimeSinceJumpStart, _config.MinTimeAfterJump))
            {
                return;
            }

            _model.Start();
            _jump.SetVerticalVelocity(0f);
            _locomotion.SetHorizontalLock(true);
            EventBus.Publish(new StompStartedEvent(transform.position));
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _jump == null || _locomotion == null)
            {
                Debug.LogError($"{nameof(ProfessorStomp)} on {name}: config or body references missing; disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LandedEvent>(OnLanded);
            EventBus.Subscribe<CrabDefeatedEvent>(OnCrabDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LandedEvent>(OnLanded);
            EventBus.Unsubscribe<CrabDefeatedEvent>(OnCrabDefeated);
        }

        private void FixedUpdate()
        {
            if (_model.Tick(Time.fixedDeltaTime))
            {
                _locomotion.SetHorizontalLock(false);
            }
        }

        #endregion

        #region Private Methods

        private void OnLanded(LandedEvent landed)
        {
            if (!IsStomping)
            {
                _model.ResetAirUse();
                return;
            }

            EventBus.Publish(new StompLandedEvent(landed.Position, _config.ImpactRadius));
            _model.Land(_config.LandRecovery);
            if (_model.State == StompState.Idle)
            {
                _locomotion.SetHorizontalLock(false);
                _jump.ApplyBounce(Mathf.Sqrt(2f * JumpConfig.Gravity * _config.BounceHeight));
            }
        }

        private void OnCrabDefeated(CrabDefeatedEvent defeated)
        {
            if (IsStomping)
            {
                _model.DefeatOnLanding = true;
            }
        }

        #endregion
    }
}
