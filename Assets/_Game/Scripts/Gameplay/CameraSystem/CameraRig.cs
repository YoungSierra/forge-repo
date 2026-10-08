using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace ProfessorSprat.Gameplay.CameraSystem
{
    /// <summary>
    /// TDD §11.5 in-play camera (Crash Bandicoot 3 style, authored — the player never rotates it). A Cinemachine follow
    /// camera keeps a world-space offset rotated by the active camera-zone yaw (blend 0.8 s); <c>Marker_CameraZone_*</c>
    /// volumes set the yaw. On <see cref="KeySpawnedEvent"/> a key-shot camera frames Professor + key for 1.2 s; a respawn
    /// cuts. <see cref="ActiveYaw"/> is the movement basis used by InputHandler.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        #region Fields

        private const int KeyShotPriority = 20;
        private static readonly Vector3 KeyShotOffset = new Vector3(0f, 4.2f, -8.5f);

        [SerializeField] private CameraConfig _config;
        [SerializeField] private CinemachineCamera _followCamera;
        [SerializeField] private CinemachineFollow _follow;
        [SerializeField] private CinemachineRotationComposer _composer;
        [SerializeField] private CinemachineCamera _keyShotCamera;
        [SerializeField] private CinemachineFollow _keyShotFollow;
        [SerializeField] private CinemachineTargetGroup _keyGroup;

        private readonly List<Collider> _zones = new List<Collider>();
        private readonly List<float> _zoneYaws = new List<float>();
        private Transform _target;
        private float _defaultYaw;
        private float _yaw;
        private float _yawVelocity;
        private float _keyShotRemaining;

        #endregion

        #region Public Methods

        /// <summary>Yaw (degrees) of the camera zone the Professor is in — the stable movement basis.</summary>
        public float ActiveYaw { get; private set; }

        public float CurrentYaw => _yaw;

        public bool KeyShotActive => _keyShotRemaining > 0f;

        public void Bind(Transform target, float defaultYaw, IReadOnlyList<Collider> zones, IReadOnlyList<float> zoneYaws)
        {
            _target = target;
            _defaultYaw = defaultYaw;
            _zones.Clear();
            _zoneYaws.Clear();
            _zones.AddRange(zones);
            _zoneYaws.AddRange(zoneYaws);
            ActiveYaw = ResolveYaw();
            _yaw = ActiveYaw;
            _followCamera.Follow = target;
            _followCamera.LookAt = target;
            Apply();
            _followCamera.PreviousStateIsValid = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _followCamera == null || _follow == null)
            {
                Debug.LogError($"{nameof(CameraRig)} on {name}: config or follow camera missing; disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Subscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<KeySpawnedEvent>(OnKeySpawned);
            EventBus.Unsubscribe<PlayerRespawnedEvent>(OnRespawned);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            ActiveYaw = ResolveYaw();
            _yaw = Mathf.SmoothDampAngle(_yaw, ActiveYaw, ref _yawVelocity, _config.ZoneBlendDuration * 0.3f);
            Apply();
            if (_keyShotRemaining > 0f)
            {
                _keyShotRemaining -= Time.deltaTime;
                if (_keyShotRemaining <= 0f && _keyShotCamera != null)
                {
                    _keyShotCamera.Priority.Value = 0;
                }
            }
        }

        #endregion

        #region Private Methods

        private float ResolveYaw()
        {
            Vector3 point = _target.position + Vector3.up * 0.5f;
            for (int i = 0; i < _zones.Count; i++)
            {
                Collider zone = _zones[i];
                if (zone != null && (zone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f)
                {
                    return _zoneYaws[i];
                }
            }

            return _defaultYaw;
        }

        private void Apply()
        {
            _follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            _follow.FollowOffset = Quaternion.Euler(0f, _yaw, 0f) * _config.FollowOffset;
            if (_composer != null)
            {
                _composer.TargetOffset = Vector3.up * _config.LookHeight;
            }
        }

        private void OnKeySpawned(KeySpawnedEvent spawned)
        {
            if (_keyShotCamera == null || _keyGroup == null || spawned.Key == null || _target == null)
            {
                return;
            }

            _keyGroup.Targets.Clear();
            _keyGroup.AddMember(_target, 1f, 0.5f);
            _keyGroup.AddMember(spawned.Key, 1f, 0.5f);
            if (_keyShotFollow != null)
            {
                // Same direction as the follow camera, higher and farther: frames Professor + key without crossing level walls.
                _keyShotFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
                _keyShotFollow.FollowOffset = Quaternion.Euler(0f, ActiveYaw, 0f) * KeyShotOffset;
            }

            _keyShotCamera.Priority.Value = KeyShotPriority;
            _keyShotRemaining = _config.KeyShotHold;
        }

        private void OnRespawned(PlayerRespawnedEvent respawned)
        {
            _yaw = ActiveYaw;
            Apply();
            _followCamera.PreviousStateIsValid = false;
        }

        #endregion
    }
}
