using UnityEngine;

namespace ProfessorSprat.Gameplay.Player
{
    /// <summary>
    /// Smooths the Professor's visible body between physics steps. Locomotion moves the root in FixedUpdate (50 Hz) while
    /// frames render faster, so each frame the body (the model child) is placed at the root pose interpolated between the
    /// last two physics steps. Camera, Sprat and the drop shadow follow <see cref="Body"/>, never the stepped root.
    /// A jump larger than <see cref="SnapDistance"/> in one step (spawn, respawn teleport) snaps instead of sliding.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class BodyInterpolation : MonoBehaviour
    {
        #region Fields

        private const float SnapDistance = 2f;

        [SerializeField] private Transform _body;

        private Vector3 _bodyLocalPosition;
        private Quaternion _bodyLocalRotation = Quaternion.identity;
        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private Quaternion _previousRotation = Quaternion.identity;
        private Quaternion _currentRotation = Quaternion.identity;

        #endregion

        #region Public Methods

        public Transform Body => _body != null ? _body : transform;

        public void Snap()
        {
            _currentPosition = transform.position;
            _currentRotation = transform.rotation;
            _previousPosition = _currentPosition;
            _previousRotation = _currentRotation;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_body == null || _body == transform)
            {
                Debug.LogError($"{nameof(BodyInterpolation)} on {name}: body child missing; disabled.", this);
                enabled = false;
                return;
            }

            _bodyLocalPosition = _body.localPosition;
            _bodyLocalRotation = _body.localRotation;
            Snap();
        }

        private void OnEnable()
        {
            Snap();
        }

        private void FixedUpdate()
        {
            _previousPosition = _currentPosition;
            _previousRotation = _currentRotation;
            _currentPosition = transform.position;
            _currentRotation = transform.rotation;
            if ((_currentPosition - _previousPosition).sqrMagnitude > SnapDistance * SnapDistance)
            {
                Snap();
            }
        }

        private void Update()
        {
            if ((transform.position - _currentPosition).sqrMagnitude > SnapDistance * SnapDistance)
            {
                Snap();
            }

            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            Vector3 position = Vector3.LerpUnclamped(_previousPosition, _currentPosition, alpha);
            Quaternion rotation = Quaternion.Slerp(_previousRotation, _currentRotation, alpha);
            _body.SetPositionAndRotation(position + rotation * _bodyLocalPosition, rotation * _bodyLocalRotation);
        }

        #endregion
    }
}
