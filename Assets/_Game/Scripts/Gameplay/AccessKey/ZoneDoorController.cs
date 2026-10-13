using System.Collections;
using System.Collections.Generic;
using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.AccessKey
{
    /// <summary>
    /// TDD §B KeyMaterialisation door (gameplay prefab of the ZoneDoor asset; its blocking collider lives on this prefab
    /// with the mesh). With the zone key carried and the Professor within 1.5 m, the door slides up by its own height in
    /// 0.8 s (collider moves with it) and publishes <see cref="DoorUnlockedEvent"/>. Without the key it stays closed.
    /// Leaves mode (no blocker, D-270): placed at the map's Door marker; the lab door leaves (<see cref="DoorLeaf"/>, each
    /// with its own collider) attached by LevelContent slide apart, each to its own side, by their width.
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
        private readonly List<DoorLeaf> _leaves = new List<DoorLeaf>();

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

        public IReadOnlyList<DoorLeaf> Leaves => _leaves;

        /// <summary>Leaves mode: a lab door leaf that opens with this door (bound by LevelContent at Init).</summary>
        public void AttachLeaf(DoorLeaf leaf)
        {
            if (leaf != null && !_leaves.Contains(leaf))
            {
                _leaves.Add(leaf);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(ZoneDoorController)} on {name}: config missing; disabled.", this);
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

            if (!TryClosestPoint(moved.Center, out Vector3 closest))
            {
                return;
            }

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

        private bool TryClosestPoint(Vector3 point, out Vector3 closest)
        {
            if (_blocker != null)
            {
                closest = _blocker.ClosestPoint(point);
                return true;
            }

            closest = point;
            float best = float.MaxValue;
            foreach (DoorLeaf leaf in _leaves)
            {
                if (leaf == null || leaf.Blocker == null)
                {
                    continue;
                }

                Vector3 candidate = leaf.Blocker.ClosestPoint(point);
                float distance = (candidate - point).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    closest = candidate;
                }
            }

            return best < float.MaxValue;
        }

        private IEnumerator CoOpen()
        {
            if (_blocker == null)
            {
                yield return CoOpenLeaves();
                yield break;
            }

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
            Opened();
        }

        private IEnumerator CoOpenLeaves()
        {
            _opening = true;
            Vector3[] starts = new Vector3[_leaves.Count];
            Vector3[] ends = new Vector3[_leaves.Count];
            for (int i = 0; i < _leaves.Count; i++)
            {
                Transform leaf = _leaves[i].transform;
                Vector3 side = leaf.position - transform.position;
                side.y = 0f;
                side = side.sqrMagnitude > 0.0001f ? side.normalized : transform.right;
                starts[i] = leaf.position;
                ends[i] = leaf.position + side * _leaves[i].WidthAlong(side);
            }

            float elapsed = 0f;
            while (elapsed < _config.DoorOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _config.DoorOpenDuration);
                for (int i = 0; i < _leaves.Count; i++)
                {
                    _leaves[i].transform.position = Vector3.Lerp(starts[i], ends[i], t);
                }

                yield return null;
            }

            for (int i = 0; i < _leaves.Count; i++)
            {
                _leaves[i].transform.position = ends[i];
            }

            Opened();
        }

        private void Opened()
        {
            _opening = false;
            IsOpen = true;
            EventBus.Publish(new DoorUnlockedEvent(_zoneId, DoorId));
        }

        #endregion
    }
}
