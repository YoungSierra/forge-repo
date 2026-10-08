using System.Collections;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Animation;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Crab
{
    /// <summary>
    /// TDD §B CrabEncounter (gameplay prefab of MechanicalCrab). Patrols along its local X, knocks the Professor back on
    /// body contact (not while stomping or stunned; re-entry needed), is defeated by a stomp landing within the impact
    /// radius (stomp wins on the same step), and reports proximity for Sprat/audio. Drops nothing.
    /// </summary>
    public sealed class CrabController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private CrabConfig _config;
        [SerializeField] private Animator _animator;

        private CrabPatrolModel _patrol;
        private AnimatorStatePlayer _player;
        private Vector3 _origin;
        private Vector3 _axis;
        private float _reactionRemaining;
        private bool _requireExit;
        private bool _near;

        #endregion

        #region Public Methods

        public string CrabId => name;

        public bool IsDefeated { get; private set; }

        public CrabPatrolModel Patrol => _patrol;

        /// <summary>Patrol from the current pose along local X up to <paramref name="range"/> m (clamped to the cap).</summary>
        public void Init(float range)
        {
            if (range > _config.MaxRange)
            {
                Debug.LogWarning($"{name}: patrol range {range:F2} m above the {_config.MaxRange} m cap (level rule LR-04); clamped.", this);
                range = _config.MaxRange;
            }

            _origin = transform.position;
            _axis = transform.right;
            _patrol = new CrabPatrolModel(range, _config.PatrolSpeed, _config.EndpointPause);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(CrabController)} on {name}: config missing; disabled.", this);
                enabled = false;
                return;
            }

            _player = _animator != null ? new AnimatorStatePlayer(_animator, 0.1f) : null;
            Init(0f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            EventBus.Subscribe<StompLandedEvent>(OnStompLanded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            EventBus.Unsubscribe<StompLandedEvent>(OnStompLanded);
        }

        private void FixedUpdate()
        {
            if (IsDefeated)
            {
                return;
            }

            if (_reactionRemaining > 0f)
            {
                _reactionRemaining -= Time.fixedDeltaTime;
                _player?.Play("Charge");
                return;
            }

            _patrol.Step(Time.fixedDeltaTime);
            transform.position = _origin + _axis * _patrol.Offset;
            _player?.Play(_patrol.IsMoving ? (_patrol.Direction > 0 ? "ScuttleR" : "ScuttleL") : "Idle");
        }

        #endregion

        #region Private Methods

        private void OnStompLanded(StompLandedEvent landed)
        {
            if (IsDefeated || HorizontalDistance(landed.Position) > landed.ImpactRadius)
            {
                return;
            }

            IsDefeated = true;
            foreach (Collider body in GetComponentsInChildren<Collider>())
            {
                body.enabled = false;
            }

            EventBus.Publish(new CrabDefeatedEvent(CrabId, transform.position));
            StartCoroutine(CoDefeat());
        }

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (IsDefeated)
            {
                return;
            }

            float distance = HorizontalDistance(moved.Position);
            UpdateProximity(distance);
            bool overlapping = distance <= _config.BodyRadius + _config.PlayerRadius && Mathf.Abs(moved.Position.y - transform.position.y) < 0.9f;
            if (!overlapping)
            {
                _requireExit = false;
                return;
            }

            if (_requireExit || moved.IsStomping || moved.IsStunned)
            {
                return;
            }

            Vector3 away = moved.Position - transform.position;
            away.y = 0f;
            _requireExit = true;
            _reactionRemaining = _config.ContactReactionDuration;
            EventBus.Publish(new CrabContactPlayerEvent(CrabId, away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward));
        }

        private void UpdateProximity(float distance)
        {
            bool near = _near ? distance <= _config.WarnReleaseRadius : distance <= _config.WarnRadius;
            if (near == _near)
            {
                return;
            }

            _near = near;
            EventBus.Publish(new CrabProximityEvent(CrabId, near, transform.position));
        }

        private float HorizontalDistance(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private IEnumerator CoDefeat()
        {
            if (_near)
            {
                _near = false;
                EventBus.Publish(new CrabProximityEvent(CrabId, false, transform.position));
            }

            float length = _player != null ? _player.ClipLength("Death") : 0f;
            _player?.Play("Death", true, length > 0f ? length / _config.DefeatDuration : 1f);
            yield return new WaitForSeconds(_config.DefeatDuration);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
