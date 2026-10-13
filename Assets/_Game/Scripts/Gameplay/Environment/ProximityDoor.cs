using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// Double sliding door of a doorway (owner request, D-260): when the Professor comes within <see cref="_openRadius"/>
    /// of the doorway, each leaf slides away from the doorway centre to its own side; when they leave, the leaves slide
    /// back. Lives on the doorway (arch) so the leaves' open directions follow from where they sit in the scene, whatever
    /// their rotation or mirroring. Scenery only: the leaves carry no collision.
    /// </summary>
    public sealed class ProximityDoor : MonoBehaviour
    {
        #region Fields

        [Tooltip("Door leaves; each slides away from this doorway's centre.")]
        [SerializeField] private Transform[] _leaves = new Transform[0];
        [Tooltip("Horizontal distance (metres) from the doorway at which the door opens.")]
        [SerializeField] private float _openRadius = 3.5f;
        [Tooltip("Extra distance beyond the open radius before the door closes again (no flicker at the edge).")]
        [SerializeField] private float _closeMargin = 0.75f;
        [Tooltip("Max height difference (metres) between the Professor and the doorway for the door to react.")]
        [SerializeField] private float _heightRange = 3f;
        [Tooltip("How far (metres) each leaf slides when fully open.")]
        [SerializeField] private float _slideDistance = 1.6f;
        [Tooltip("Seconds to open or close fully.")]
        [SerializeField] private float _duration = 0.6f;

        private Vector3[] _closedPositions = new Vector3[0];
        private Vector3[] _openOffsets = new Vector3[0];
        private bool _wantOpen;
        private float _openness;

        #endregion

        #region Public Methods

        public bool IsOpen => _openness >= 1f;

        public bool IsClosed => _openness <= 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _closedPositions = new Vector3[_leaves.Length];
            _openOffsets = new Vector3[_leaves.Length];
            for (int i = 0; i < _leaves.Length; i++)
            {
                if (_leaves[i] == null)
                {
                    Debug.LogError($"{nameof(ProximityDoor)} on {name}: leaf {i} missing; disabled.", this);
                    enabled = false;
                    return;
                }

                _closedPositions[i] = _leaves[i].position;
                // Along the doorway's width only, so a leaf set slightly off the arch's plane does not drift sideways.
                Vector3 side = Vector3.Project(_leaves[i].position - transform.position, transform.right);
                _openOffsets[i] = side.sqrMagnitude > 0.0001f ? side.normalized * _slideDistance : Vector3.zero;
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

        private void Update()
        {
            float target = _wantOpen ? 1f : 0f;
            if (Mathf.Approximately(_openness, target))
            {
                return;
            }

            _openness = Mathf.MoveTowards(_openness, target, Time.deltaTime / Mathf.Max(0.01f, _duration));
            float eased = Mathf.SmoothStep(0f, 1f, _openness);
            for (int i = 0; i < _leaves.Length; i++)
            {
                _leaves[i].position = _closedPositions[i] + _openOffsets[i] * eased;
            }
        }

        #endregion

        #region Private Methods

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            Vector3 offset = moved.Center - transform.position;
            bool inHeight = Mathf.Abs(offset.y) <= _heightRange;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (!_wantOpen && inHeight && distance <= _openRadius)
            {
                _wantOpen = true;
            }
            else if (_wantOpen && (!inHeight || distance > _openRadius + _closeMargin))
            {
                _wantOpen = false;
            }
        }

        #endregion
    }
}
