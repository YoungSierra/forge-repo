using System.Collections;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>
    /// TDD §B KeyMaterialisation door (gameplay prefab of the ZoneDoor asset; its blocking collider lives on this prefab
    /// with the mesh). With the zone key carried and the Professor within 1.5 m, the door slides up by its own height in
    /// 0.8 s (collider moves with it) and publishes <see cref="DoorUnlockedEvent"/>. Without the key it stays closed.
    /// </summary>
    public sealed class ZoneDoorController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private AccessKeyConfig _config;
        [SerializeField] private Collider _blocker;

        private string _zoneId;
        private bool _keyCarried;
        private bool _opening;
        private bool _lockedNoticeShown;

        #endregion

        #region Public Methods

        public bool IsOpen { get; private set; }

        public bool IsKeyCarried => _keyCarried;

        public string DoorId => name;

        public void Arm(string zoneId)
        {
            _zoneId = zoneId;
            _keyCarried = false;
        }

        public void SetKeyCarried(bool carried)
        {
            _keyCarried = carried;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null || _blocker == null)
            {
                Debug.LogError($"{nameof(ZoneDoorController)} on {name}: config or blocker collider missing; disabled.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        #endregion

        #region Private Methods

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (IsOpen || _opening || _zoneId == null)
            {
                return;
            }

            Vector3 closest = _blocker.ClosestPoint(moved.Center);
            Vector3 offset = closest - moved.Center;
            offset.y = 0f;
            if (offset.magnitude > _config.DoorUnlockRadius)
            {
                _lockedNoticeShown = false;
                return;
            }

            if (!_keyCarried)
            {
                if (!_lockedNoticeShown)
                {
                    Debug.Log($"{name}: locked — the zone key is required.", this);
                    _lockedNoticeShown = true;
                }

                return;
            }

            StartCoroutine(CoOpen());
        }

        private IEnumerator CoOpen()
        {
            _opening = true;
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * _blocker.bounds.size.y;
            float elapsed = 0f;
            while (elapsed < _config.DoorOpenDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / _config.DoorOpenDuration));
                yield return null;
            }

            transform.position = end;
            _opening = false;
            IsOpen = true;
            EventBus.Publish(new DoorUnlockedEvent(_zoneId, DoorId));
        }

        #endregion
    }
}
