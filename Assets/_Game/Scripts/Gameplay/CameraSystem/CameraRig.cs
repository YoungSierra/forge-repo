using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace ProfessorSprat.Gameplay.CameraSystem
{
    /// <summary>
    /// TDD §11.5 in-play camera, as a Cinemachine FreeLook (owner request, D-254/D-255/D-257, overrides "Look: none"):
    /// a <see cref="CinemachineOrbitalFollow"/> sphere orbit around the Professor's body, driven by
    /// <see cref="FreeLookInput"/> (mouse / right stick orbit, wheel / d-pad zoom). The orbit's horizontal center is the
    /// camera-zone yaw (<c>Marker_CameraZone_*</c>, blend 0.8 s) and both orbit axes recenter there after
    /// <see cref="CameraConfig.RecenterDelay"/> without input. Distance and elevation come from the authored follow offset.
    /// On <see cref="KeySpawnedEvent"/> a key-shot camera frames Professor + key for 1.2 s; a respawn cuts.
    /// <see cref="ActiveYaw"/> + <see cref="LookYaw"/> is the movement basis used by InputHandler.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        #region Fields

        private const int KeyShotPriority = 20;
        private static readonly Vector3 KeyShotOffset = new Vector3(0f, 3.4f, -7.0f);

        [SerializeField] private CameraConfig _config;
        [SerializeField] private CinemachineCamera _followCamera;
        [SerializeField] private CinemachineOrbitalFollow _orbit;
        [SerializeField] private FreeLookInput _freeLook;
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
        private float _elevation;
        private float _keyShotRemaining;

        #endregion

        #region Public Methods

        /// <summary>Yaw (degrees) of the camera zone the Professor is in — the stable movement basis.</summary>
        public float ActiveYaw { get; private set; }

        public float CurrentYaw => _yaw;

        public bool KeyShotActive => _keyShotRemaining > 0f;

        public CameraConfig Config => _config;

        /// <summary>Player orbit (degrees) relative to the camera-zone yaw.</summary>
        public float LookYaw => _orbit != null ? Mathf.DeltaAngle(_yaw, _orbit.HorizontalAxis.Value) : 0f;

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
            ResetOrbit();
            _followCamera.PreviousStateIsValid = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _followCamera == null || _orbit == null)
            {
                Debug.LogError($"{nameof(CameraRig)} on {name}: config or follow orbit missing; disabled.", this);
                enabled = false;
                return;
            }

            ConfigureOrbit();
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
            float previous = _yaw;
            _yaw = Mathf.SmoothDampAngle(_yaw, ActiveYaw, ref _yawVelocity, _config.ZoneBlendDuration * 0.3f);
            // A camera-zone change turns the whole orbit (center and current value) so the player's offset is kept.
            // Only write when the zone yaw actually moves: any write counts as input and restarts the recenter clock.
            float turn = Mathf.DeltaAngle(previous, _yaw);
            if (Mathf.Abs(turn) > 0.0001f)
            {
                _orbit.HorizontalAxis.Center = _yaw;
                _orbit.HorizontalAxis.Value = Mathf.Repeat(_orbit.HorizontalAxis.Value + turn + 180f, 360f) - 180f;
            }
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

        /// <summary>Orbit radius/elevation from the authored offset; pitch, zoom and recentering from the config.</summary>
        private void ConfigureOrbit()
        {
            Vector3 offset = _config.FollowOffset;
            _elevation = Mathf.Asin(Mathf.Clamp(offset.y / Mathf.Max(offset.magnitude, 0.01f), -1f, 1f)) * Mathf.Rad2Deg;
            var recenter = new InputAxis.RecenteringSettings { Enabled = true, Wait = _config.RecenterDelay, Time = _config.RecenterTime };
            _orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            _orbit.Radius = offset.magnitude;
            _orbit.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            _orbit.RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.AxisCenter;
            _orbit.HorizontalAxis.Range = new Vector2(-180f, 180f);
            _orbit.HorizontalAxis.Wrap = true;
            _orbit.HorizontalAxis.Recentering = recenter;
            _orbit.VerticalAxis.Range = new Vector2(_elevation + _config.PitchRange.x, _elevation + _config.PitchRange.y);
            _orbit.VerticalAxis.Wrap = false;
            _orbit.VerticalAxis.Recentering = recenter;
            _orbit.RadialAxis.Range = _config.ZoomRange;
            _orbit.RadialAxis.Recentering = new InputAxis.RecenteringSettings { Enabled = false };
            if (_composer != null)
            {
                _composer.TargetOffset = Vector3.up * _config.LookHeight;
            }

            if (_freeLook != null)
            {
                _freeLook.ApplyConfig();
            }

            ResetOrbit();
        }

        private void ResetOrbit()
        {
            _orbit.HorizontalAxis.Center = _yaw;
            _orbit.HorizontalAxis.Value = _yaw;
            _orbit.VerticalAxis.Center = _elevation;
            _orbit.VerticalAxis.Value = _elevation;
            _orbit.RadialAxis.Center = 1f;
            _orbit.RadialAxis.Value = 1f;
        }

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
                _keyShotFollow.FollowOffset = Quaternion.Euler(0f, ActiveYaw + LookYaw, 0f) * KeyShotOffset;
            }

            _keyShotCamera.Priority.Value = KeyShotPriority;
            _keyShotRemaining = _config.KeyShotHold;
        }

        private void OnRespawned(PlayerRespawnedEvent respawned)
        {
            _yaw = ActiveYaw;
            _orbit.HorizontalAxis.Center = _yaw;
            _followCamera.PreviousStateIsValid = false;
        }

        #endregion
    }
}
