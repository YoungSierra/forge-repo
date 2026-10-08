using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>
    /// TDD §B KeyMaterialisation (gameplay prefab of the AccessKey asset). The key has no world presence until its zone
    /// completes: then the visual appears with a 1.2 s amber ceremony, can be collected at 1.5 m, and tells the zone door
    /// it is carried. Hover bob/spin moves the nested visual only.
    /// </summary>
    public sealed class AccessKeyController : MonoBehaviour
    {
        #region Fields

        private const float BobAmplitude = 0.1f;
        private const float BobPeriod = 0.5f;
        private const float SpinSpeed = 90f;

        [SerializeField] private AccessKeyConfig _config;
        [Tooltip("Nested Visual prefab instance (hidden until the key materialises).")]
        [SerializeField] private GameObject _visual;
        [SerializeField] private Light _amberLight;

        private readonly AccessKeyModel _model = new AccessKeyModel();
        private ZoneDoorController _door;
        private Vector3 _visualBase;

        #endregion

        #region Public Methods

        public KeyState State => _model.State;

        public string ZoneId => _model.ZoneId;

        public float LightIntensity => _amberLight != null ? _amberLight.intensity : 0f;

        public bool IsKeyCarried => _model.State == KeyState.Carried;

        public void Arm(string zoneId, ZoneDoorController door)
        {
            _model.Arm(zoneId);
            _door = door;
            _door?.Arm(zoneId);
            SetPresent(false);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(AccessKeyController)} on {name}: config missing; disabled.", this);
                enabled = false;
                return;
            }

            _visualBase = _visual != null ? _visual.transform.localPosition : Vector3.zero;
            SetPresent(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void Update()
        {
            if (_model.State == KeyState.Spawning || _model.State == KeyState.Waiting)
            {
                Hover();
            }

            if (_model.State != KeyState.Spawning)
            {
                return;
            }

            float ramp = _config.AmberRampDuration > 0f ? Mathf.Clamp01(_model.CeremonyElapsed / _config.AmberRampDuration) : 1f;
            if (_amberLight != null)
            {
                _amberLight.intensity = _config.AmberIntensity * ramp;
            }

            _model.TickCeremony(Time.deltaTime, _config.CeremonyDuration);
        }

        #endregion

        #region Private Methods

        private void OnZoneCompleted(ZoneCompletedEvent completed)
        {
            if (!_model.TrySpawn(completed.ZoneId))
            {
                return;
            }

            SetPresent(true);
            EventBus.Publish(new KeySpawnedEvent(_model.ZoneId, transform.position, transform));
        }

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (!_model.TryCollect(Vector3.Distance(moved.Center, transform.position), _config.PickupRadius))
            {
                return;
            }

            SetPresent(false);
            _door?.SetKeyCarried(true);
            EventBus.Publish(new KeyCollectedEvent(_model.ZoneId));
        }

        private void SetPresent(bool present)
        {
            if (_visual != null)
            {
                _visual.SetActive(present);
                _visual.transform.localPosition = _visualBase;
            }

            if (_amberLight != null)
            {
                _amberLight.range = _config != null ? _config.AmberRange : 6f;
                _amberLight.intensity = 0f;
                _amberLight.enabled = present;
            }
        }

        private void Hover()
        {
            if (_visual == null)
            {
                return;
            }

            float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / BobPeriod) * BobAmplitude;
            _visual.transform.localPosition = _visualBase + Vector3.up * bob;
            _visual.transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        #endregion
    }
}
