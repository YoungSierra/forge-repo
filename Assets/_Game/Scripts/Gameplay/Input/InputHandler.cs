using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProfessorSprat.Gameplay.Input
{
    /// <summary>
    /// TDD §B-S InputHandler: reads the Input System actions (§11.3) and dispatches them. <c>Move</c> is converted to world
    /// XZ in the active camera-zone basis; a new basis applies only when the stick drops below the switch threshold or
    /// after the zone blend, so directions never flip mid-press. Jump and Stomp share a button; each mechanic gates itself.
    /// </summary>
    public sealed class InputHandler : MonoBehaviour
    {
        #region Fields

        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private ProfessorLocomotion _locomotion;
        [SerializeField] private ProfessorJump _jump;
        [SerializeField] private ProfessorStomp _stomp;
        [SerializeField] private CameraRig _cameraRig;
        [Tooltip("Seconds after a camera-zone change before the new basis applies while the stick is held.")]
        [SerializeField] private float _basisSwitchDelay = 0.8f;
        [SerializeField] private float _basisSwitchStick = 0.2f;

        private InputAction _move;
        private InputAction _jumpAction;
        private InputAction _stompAction;
        private float _basisYaw;
        private float _pendingYaw;
        private float _pendingTime;

        #endregion

        #region Public Methods

        public bool GameplayEnabled { get; private set; } = true;

        public float BasisYaw => _basisYaw;

        public void SetGameplayEnabled(bool enabledState)
        {
            GameplayEnabled = enabledState;
            if (!enabledState && _locomotion != null)
            {
                _locomotion.SetMoveInput(Vector3.zero);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_actions == null || _locomotion == null || _jump == null || _stomp == null)
            {
                Debug.LogError($"{nameof(InputHandler)} on {name}: actions or Professor references missing; disabled.", this);
                enabled = false;
                return;
            }

            _move = _actions.FindAction("Gameplay/Move", true);
            _jumpAction = _actions.FindAction("Gameplay/Jump", true);
            _stompAction = _actions.FindAction("Gameplay/Stomp", true);
            _basisYaw = _cameraRig != null ? _cameraRig.ActiveYaw : 0f;
            _pendingYaw = _basisYaw;
        }

        private void OnEnable()
        {
            _actions?.FindActionMap("Gameplay", true).Enable();
        }

        private void OnDisable()
        {
            _actions?.FindActionMap("Gameplay", true).Disable();
        }

        private void Update()
        {
            if (!GameplayEnabled)
            {
                return;
            }

            Vector2 stick = _move.ReadValue<Vector2>();
            UpdateBasis(stick.magnitude);
            Vector3 world = Quaternion.Euler(0f, _basisYaw, 0f) * new Vector3(stick.x, 0f, stick.y);
            _locomotion.SetMoveInput(world);
            if (_jumpAction.WasPressedThisFrame())
            {
                _jump.PressJump();
            }

            if (_stompAction.WasPressedThisFrame())
            {
                _stomp.PressStomp();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateBasis(float stickMagnitude)
        {
            float cameraYaw = _cameraRig != null ? _cameraRig.ActiveYaw : _basisYaw;
            if (!Mathf.Approximately(cameraYaw, _pendingYaw))
            {
                _pendingYaw = cameraYaw;
                _pendingTime = Time.time;
            }

            if (Mathf.Approximately(_pendingYaw, _basisYaw))
            {
                return;
            }

            if (stickMagnitude < _basisSwitchStick || Time.time - _pendingTime >= _basisSwitchDelay)
            {
                _basisYaw = _pendingYaw;
            }
        }

        #endregion
    }
}
