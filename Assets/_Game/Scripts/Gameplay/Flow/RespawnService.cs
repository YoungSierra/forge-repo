using System.Collections;
using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using ProfessorSprat.Gameplay.Input;
using ProfessorSprat.Gameplay.Player;
using ProfessorSprat.Gameplay.UI;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// TDD §B-S RespawnService: the safe point is the Professor's position after 0.5 s continuously grounded (refreshed
    /// every 0.5 s). Entering a <c>Marker_Kill_*</c> volume or falling below the floor fades out 0.25 s, teleports to the
    /// safe point, fades in 0.25 s and publishes <see cref="PlayerRespawnedEvent"/>. Flies, key and crabs keep their state.
    /// </summary>
    public sealed class RespawnService : MonoBehaviour
    {
        #region Fields

        [SerializeField] private RespawnConfig _config;
        [SerializeField] private ProfessorLocomotion _professor;
        [SerializeField] private InputHandler _input;
        [SerializeField] private ScreenFade _fade;

        private readonly List<Collider> _kills = new List<Collider>();
        private Vector3 _safePosition;
        private Quaternion _safeRotation;
        private float _groundedTime;
        private bool _grounded;

        #endregion

        #region Public Methods

        public bool IsRespawning { get; private set; }

        public Vector3 SafePoint => _safePosition;

        public int RespawnCount { get; private set; }

        public void Bind(IReadOnlyList<Collider> killVolumes, Transform spawn)
        {
            _kills.Clear();
            _kills.AddRange(killVolumes);
            _safePosition = spawn != null ? spawn.position : _professor.transform.position;
            _safeRotation = spawn != null ? spawn.rotation : _professor.transform.rotation;
        }

        public void TriggerRespawn()
        {
            if (!IsRespawning)
            {
                StartCoroutine(CoRespawn());
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _professor == null)
            {
                Debug.LogError($"{nameof(RespawnService)} on {name}: config or professor missing; disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GroundedChangedEvent>(OnGroundedChanged);
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GroundedChangedEvent>(OnGroundedChanged);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        #endregion

        #region Private Methods

        private void OnGroundedChanged(GroundedChangedEvent changed)
        {
            _grounded = changed.IsGrounded;
            _groundedTime = 0f;
        }

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (IsRespawning)
            {
                return;
            }

            if (_grounded && !moved.IsStunned)
            {
                _groundedTime += Time.fixedDeltaTime;
                if (_groundedTime >= _config.SafeGroundTime)
                {
                    _safePosition = moved.Position;
                    _safeRotation = _professor.transform.rotation;
                    _groundedTime = 0f;
                }
            }

            if (moved.Position.y < _config.FallFloorY || InsideKillVolume(moved.Center))
            {
                TriggerRespawn();
            }
        }

        private bool InsideKillVolume(Vector3 point)
        {
            foreach (Collider kill in _kills)
            {
                if (ZoneRuntime.Contains(kill, point))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator CoRespawn()
        {
            IsRespawning = true;
            if (_input != null)
            {
                _input.SetGameplayEnabled(false);
            }
            if (_fade != null)
            {
                yield return _fade.CoFade(1f, _config.FadeOut);
            }

            _professor.Teleport(_safePosition, _safeRotation);
            RespawnCount++;
            EventBus.Publish(new PlayerRespawnedEvent(_safePosition));
            if (_fade != null)
            {
                yield return _fade.CoFade(0f, _config.FadeIn);
            }

            if (_input != null)
            {
                _input.SetGameplayEnabled(true);
            }
            IsRespawning = false;
        }

        #endregion
    }
}
