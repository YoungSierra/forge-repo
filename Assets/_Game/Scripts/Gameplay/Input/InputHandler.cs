using ProfessorSprat.Gameplay.CameraSystem;
using ProfessorSprat.Gameplay.Config;
using ProfessorSprat.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProfessorSprat.Gameplay.Input
{
    /// <summary>
    /// TDD §B-S InputHandler: reads the Input System actions (§11.3) and dispatches them. <c>Move</c> is converted to world
    /// XZ in the active camera-zone basis; a new basis applies only when the stick drops below the switch threshold or
    /// after the zone blend, so directions never flip mid-press. Jump and Stomp share a button; each mechanic gates itself.
    /// <c>Look</c> (mouse delta / right stick, D-254) orbits the camera and <c>Zoom</c> (wheel / d-pad, D-255) changes its
    /// distance; the cursor is locked while gameplay input is on.
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
        private InputAction _look;
        private InputAction _zoom;
        private MoveBasis _basis;

        #endregion

        #region Public Methods

        public bool GameplayEnabled { get; private set; } = true;

        public float BasisYaw => _basis != null ? _basis.Yaw : 0f;

        public void SetGameplayEnabled(bool enabledState)
        {
            GameplayEnabled = enabledState;
            Cursor.lockState = enabledState ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabledState;
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
            _look = _actions.FindAction("Gameplay/Look", true);
            _zoom = _actions.FindAction("Gameplay/Zoom", true);
            _basis = new MoveBasis(_cameraRig != null ? _cameraRig.ActiveYaw : 0f, _basisSwitchStick, _basisSwitchDelay);
        }

        private void OnEnable()
        {
            if (_actions != null)
            {
                _actions.FindActionMap("Gameplay", true).Enable();
            }
        }

        private void OnDisable()
        {
            if (_actions != null)
            {
                _actions.FindActionMap("Gameplay", true).Disable();
            }
        }

        private void Update()
        {
            if (!GameplayEnabled)
            {
                return;
            }

            Vector2 stick = _move.ReadValue<Vector2>();
            _basis.Update(_cameraRig != null ? _cameraRig.ActiveYaw : _basis.Yaw, stick.magnitude, Time.time);
            ApplyLook();
            _locomotion.SetMoveInput(_basis.ToWorld(stick, _cameraRig != null ? _cameraRig.LookYaw : 0f));
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

        private void ApplyLook()
        {
            if (_cameraRig == null || _cameraRig.Look == null)
            {
                return;
            }

            CameraConfig config = _cameraRig.Config;
            float zoom = _zoom.ReadValue<float>();
            if (zoom != 0f)
            {
                bool wheel = _zoom.activeControl != null && _zoom.activeControl.device is Mouse;
                _cameraRig.Look.AddZoom(wheel ? Mathf.Sign(zoom) * config.ZoomStep : zoom * config.StickZoomSpeed * Time.unscaledDeltaTime);
            }

            Vector2 look = _look.ReadValue<Vector2>();
            bool mouse = _look.activeControl != null && _look.activeControl.device is Mouse;
            Vector2 degrees = mouse
                ? look * config.MouseSensitivity
                : look * (config.StickLookSpeed * Time.unscaledDeltaTime);
            _cameraRig.Look.AddLook(new Vector2(degrees.x, -degrees.y));
        }

        #endregion
    }
}
