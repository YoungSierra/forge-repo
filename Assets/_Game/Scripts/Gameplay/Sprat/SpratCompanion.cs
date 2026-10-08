using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Animation;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Sprat
{
    /// <summary>Sprat reaction states (TDD §B SpratCompanion), highest priority first: Spin, Freeze, Hazard, Excited, Idle.</summary>
    public enum SpratState
    {
        Idle,
        Excited,
        Hazard,
        Freeze,
        Spin
    }

    /// <summary>
    /// TDD §B SpratCompanion: Aku-Aku-style companion floating at the Professor's shoulder. Follows with SmoothDamp, snaps
    /// on respawn, reacts with its delivered clips (<see cref="SpratReactions"/>). No collider and no influence on any
    /// gameplay value.
    /// </summary>
    public sealed class SpratCompanion : MonoBehaviour
    {
        #region Fields

        [SerializeField] private SpratConfig _config;
        [SerializeField] private Transform _professor;
        [SerializeField] private Animator _animator;

        private readonly SpratReactions _reactions = new SpratReactions();
        private AnimatorStatePlayer _player;
        private Vector3 _velocity;

        #endregion

        #region Public Methods

        public SpratState State { get; private set; } = SpratState.Idle;

        public Vector3 Target => _professor.TransformPoint(_config.ShoulderOffset);

        public void Bind(Transform professor)
        {
            _professor = professor;
            Snap();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _professor == null)
            {
                Debug.LogError($"{nameof(SpratCompanion)} on {name}: config or professor missing; disabled.", this);
                enabled = false;
                return;
            }

            _player = _animator != null ? new AnimatorStatePlayer(_animator, 0.1f) : null;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<CrabProximityEvent>(OnCrabProximity);
            EventBus.Subscribe<StunBeganEvent>(OnStunBegan);
            EventBus.Subscribe<StunEndedEvent>(OnStunEnded);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FlyCollectedEvent>(OnFlyCollected);
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<CrabProximityEvent>(OnCrabProximity);
            EventBus.Unsubscribe<StunBeganEvent>(OnStunBegan);
            EventBus.Unsubscribe<StunEndedEvent>(OnStunEnded);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            State = _reactions.Tick(deltaTime);
            Follow(deltaTime);
            _player?.Play(State.ToString());
        }

        #endregion

        #region Private Methods

        private void Follow(float deltaTime)
        {
            Vector3 target = Target;
            if ((target - transform.position).magnitude > _config.SnapDistance)
            {
                Snap();
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, _config.FollowSmoothTime);
            Vector3 lag = transform.position - target;
            if (lag.magnitude > _config.MaxLag)
            {
                transform.position = target + lag.normalized * _config.MaxLag;
            }

            Vector3 look = _professor.forward;
            if (State == SpratState.Hazard && _reactions.TryNearestCrab(transform.position, out Vector3 crab))
            {
                look = crab - transform.position;
            }

            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
            {
                Quaternion wanted = Quaternion.LookRotation(look, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, _config.YawRate * deltaTime);
            }
        }

        private void Snap()
        {
            if (_professor == null)
            {
                return;
            }

            transform.position = Target;
            _velocity = Vector3.zero;
        }

        private void OnFlyCollected(FlyCollectedEvent collected) => _reactions.Excite(_config.ExcitedDuration);

        private void OnZoneCompleted(ZoneCompletedEvent completed) => _reactions.Excite(_config.ExcitedDuration);

        private void OnKeySpawned(KeySpawnedEvent spawned) => _reactions.Spin(_config.SpinDuration);

        private void OnStunBegan(StunBeganEvent stun) => _reactions.Frozen = true;

        private void OnStunEnded(StunEndedEvent stun) => _reactions.Frozen = false;

        private void OnCrabProximity(CrabProximityEvent proximity) => _reactions.SetCrabNear(proximity.CrabId, proximity.Near, proximity.Position);

        private void OnRespawned(PlayerRespawnedEvent respawned)
        {
            _reactions.Frozen = false;
            Snap();
        }

        #endregion
    }
}
