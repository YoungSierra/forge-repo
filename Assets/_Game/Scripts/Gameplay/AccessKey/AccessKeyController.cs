using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>
    /// TDD §B KeyMaterialisation (gameplay prefab of the AccessKey asset). The key has no world presence until its zone
    /// completes: then the visual grows in spinning with a 1.2 s amber ceremony, hovers, and is collected at 1.5 m.
    /// Carried, it orbits behind the Professor's head; when its door unlocks it flies into the door and shrinks away
    /// (owner request, D-287). Only the nested visual moves; the gameplay transform stays where the map put it.
    /// </summary>
    public sealed class AccessKeyController : MonoBehaviour
    {
        #region Fields

        private const float BobAmplitude = 0.08f;
        private const float BobPeriod = 1.6f;
        private const float IdleSpin = 90f;
        private const float GrowOvershoot = 1.25f;
        private const float SettleTime = 0.35f;
        private const float CarriedScale = 0.7f;
        private const float CarriedSpin = 145f;
        private static readonly Vector3 CarriedOffset = new Vector3(0f, 1.35f, -0.45f);
        private const float SocketDuration = 0.9f;

        [SerializeField] private AccessKeyConfig _config;
        [Tooltip("Nested Visual prefab instance (hidden until the key materialises).")]
        [SerializeField] private GameObject _visual;
        [SerializeField] private Light _amberLight;

        private readonly AccessKeyModel _model = new AccessKeyModel();
        private ZoneDoorController _door;
        private Transform _carrier;
        private Vector3 _visualBase;
        private Vector3 _visualScale = Vector3.one;
        private float _settle;
        private float _socketElapsed = -1f;
        private Vector3 _socketFrom;
        private Vector3 _lightBase;

        #endregion

        #region Public Methods

        public KeyState State => _model.State;

        public string ZoneId => _model.ZoneId;

        public float LightIntensity => _amberLight != null ? _amberLight.intensity : 0f;

        public bool IsKeyCarried => _model.State == KeyState.Carried;

        /// <summary>World position of the key's visual (where Sprat looks and the camera frames).</summary>
        public Vector3 VisualPosition => _visual != null ? _visual.transform.position : transform.position;

        public void Arm(string zoneId, ZoneDoorController door)
        {
            _model.Arm(zoneId);
            _door = door;
            if (_door != null)
            {
                _door.Arm(zoneId);
            }
            SetPresent(false);
        }

        /// <summary>The character the key follows once collected.</summary>
        public void SetCarrier(Transform carrier) => _carrier = carrier;

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

            if (_visual != null)
            {
                _visualBase = _visual.transform.localPosition;
                _visualScale = _visual.transform.localScale;
            }

            _lightBase = _amberLight != null ? _amberLight.transform.localPosition : Vector3.zero;

            SetPresent(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            EventBus.Subscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneCompletedEvent>(OnZoneCompleted);
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            EventBus.Unsubscribe<DoorUnlockedEvent>(OnDoorUnlocked);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (_model.State)
            {
                case KeyState.Spawning:
                    TickCeremony(dt);
                    break;
                case KeyState.Waiting:
                    Hover(dt);
                    break;
                case KeyState.Carried:
                    if (_socketElapsed >= 0f)
                    {
                        TickSocket(dt);
                    }
                    else
                    {
                        Carry(dt);
                    }
                    break;
            }
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
            if (_visual != null)
            {
                _visual.transform.localScale = _visualScale * 0.001f;
            }
            EventBus.Publish(new KeySpawnedEvent(_model.ZoneId, transform.position, transform));
        }

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (!_model.TryCollect(Vector3.Distance(moved.Center, VisualPosition), _config.PickupRadius))
            {
                return;
            }

            if (_carrier == null)
            {
                SetPresent(false);
            }
            else if (_amberLight != null)
            {
                _amberLight.intensity = _config.AmberIntensity * 0.5f;
            }

            if (_door != null)
            {
                _door.SetKeyCarried(true);
            }
            EventBus.Publish(new KeyCollectedEvent(_model.ZoneId));
        }

        private void OnDoorUnlocked(DoorUnlockedEvent unlocked)
        {
            if (unlocked.ZoneId != _model.ZoneId || _model.State != KeyState.Carried || _visual == null || !_visual.activeSelf)
            {
                return;
            }

            _socketElapsed = 0f;
            _socketFrom = _visual.transform.position;
        }

        private void TickCeremony(float dt)
        {
            bool ended = _model.TickCeremony(dt, _config.CeremonyDuration);
            float k = _config.CeremonyDuration > 0f ? Mathf.Clamp01(_model.CeremonyElapsed / _config.CeremonyDuration) : 1f;
            float ramp = ended || _config.AmberRampDuration <= 0f ? 1f : Mathf.Clamp01(_model.CeremonyElapsed / _config.AmberRampDuration);
            if (_amberLight != null)
            {
                _amberLight.intensity = _config.AmberIntensity * ramp;
            }

            if (_visual == null)
            {
                return;
            }

            // Ease-out cubic growth to an overshoot, spinning fast then slowing (reference build's ceremony).
            float e = 1f - Mathf.Pow(1f - k, 3f);
            _visual.transform.localScale = _visualScale * Mathf.Max(0.001f, e * GrowOvershoot);
            _visual.transform.Rotate(0f, Mathf.Lerp(460f, 120f, e) * dt, 0f, Space.World);
            _visual.transform.localPosition = _visualBase;
            if (ended)
            {
                _settle = SettleTime;
            }
        }

        private void Hover(float dt)
        {
            if (_visual == null)
            {
                return;
            }

            if (_settle > 0f)
            {
                _settle = Mathf.Max(0f, _settle - dt);
                float s = Mathf.Lerp(1f, GrowOvershoot, _settle / SettleTime);
                _visual.transform.localScale = _visualScale * s;
            }

            float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / BobPeriod) * BobAmplitude;
            _visual.transform.localPosition = _visualBase + Vector3.up * bob;
            _visual.transform.Rotate(0f, IdleSpin * dt, 0f, Space.World);
            if (_amberLight != null)
            {
                _amberLight.intensity = _config.AmberIntensity * (1f + 0.12f * Mathf.Sin(Time.time * 3f));
            }
        }

        private void Carry(float dt)
        {
            if (_visual == null || _carrier == null || !_visual.activeSelf)
            {
                return;
            }

            Vector3 behind = _carrier.TransformPoint(CarriedOffset) + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.05f);
            Transform key = _visual.transform;
            key.position = Vector3.Lerp(key.position, behind, 1f - Mathf.Exp(-10f * dt));
            key.Rotate(0f, CarriedSpin * dt, 0f, Space.World);
            float scale = Mathf.Lerp(key.localScale.x / Mathf.Max(0.0001f, _visualScale.x), CarriedScale, 1f - Mathf.Exp(-6f * dt));
            key.localScale = _visualScale * scale;
            if (_amberLight != null)
            {
                _amberLight.transform.position = key.position;
            }
        }

        private void TickSocket(float dt)
        {
            Transform key = _visual.transform;
            Vector3 socket = _door != null ? _door.transform.position + Vector3.up * 1.4f : _socketFrom;
            _socketElapsed += dt;
            float k = Mathf.Clamp01(_socketElapsed / SocketDuration);
            float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.6f));
            key.position = Vector3.Lerp(_socketFrom, socket, move);
            key.Rotate(0f, 300f * dt, 0f, Space.World);
            float shrink = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
            key.localScale = _visualScale * CarriedScale * Mathf.Max(0.001f, shrink);
            if (_amberLight != null)
            {
                _amberLight.transform.position = key.position;
                _amberLight.intensity = _config.AmberIntensity * 0.5f * shrink;
            }

            if (k >= 1f)
            {
                _socketElapsed = -1f;
                SetPresent(false);
                EventBus.Publish(new KeySocketedEvent(_model.ZoneId));
            }
        }

        private void SetPresent(bool present)
        {
            if (_visual != null)
            {
                _visual.SetActive(present);
                _visual.transform.localPosition = _visualBase;
                _visual.transform.localScale = _visualScale;
            }

            if (_amberLight != null)
            {
                _amberLight.range = _config != null ? _config.AmberRange : 6f;
                _amberLight.intensity = 0f;
                _amberLight.enabled = present;
                _amberLight.transform.localPosition = _lightBase;
            }
        }

        #endregion
    }
}
