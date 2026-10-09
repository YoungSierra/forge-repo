using System;
using ProfessorSprat.Gameplay.Config;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProfessorSprat.Gameplay.CameraSystem
{
    /// <summary>
    /// Cinemachine input bridge for the FreeLook orbit (owner request, D-254/D-257). Drives the
    /// <see cref="CinemachineOrbitalFollow"/> axes from <c>Gameplay/Look</c> and <c>Gameplay/Zoom</c>. Mouse values are
    /// per-frame deltas and gamepad values are rates, so each reader scales them separately (the stock
    /// <see cref="CinemachineInputAxisController"/> has one gain per axis). Reads nothing while the Gameplay map is
    /// disabled (pause, level end, respawn fade). Gains come from <see cref="CameraConfig"/>.
    /// </summary>
    [ExecuteAlways]
    public sealed class FreeLookInput : InputAxisControllerBase<FreeLookInput.Reader>
    {
        #region Fields

        [SerializeField] private CameraConfig _config;
        [SerializeField] private InputActionReference _look;
        [SerializeField] private InputActionReference _zoom;

        #endregion

        #region Nested Types

        /// <summary>Reads one axis of an action; mouse deltas are converted to a rate, stick values are already rates.</summary>
        [Serializable]
        public sealed class Reader : IInputAxisReader
        {
            public InputActionReference Action;
            [Tooltip("Units per mouse count (wheel: per notch).")]
            public float MouseGain = 1f;
            [Tooltip("Units per second at full stick / d-pad deflection.")]
            public float GamepadGain = 1f;
            [Tooltip("Mouse wheel: one step per notch, whatever the platform's scroll scale.")]
            public bool MouseAsSteps;

            public float GetValue(UnityEngine.Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
            {
                InputAction action = Action != null ? Action.action : null;
                if (action == null || !action.enabled)
                {
                    return 0f;
                }

                float value = action.expectedControlType == "Vector2"
                    ? (hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? action.ReadValue<Vector2>().y : action.ReadValue<Vector2>().x)
                    : action.ReadValue<float>();
                if (value == 0f)
                {
                    return 0f;
                }

                bool mouse = action.activeControl != null && action.activeControl.device is Mouse;
                if (!mouse)
                {
                    return value * GamepadGain;
                }

                float perFrame = (MouseAsSteps ? Mathf.Sign(value) : value) * MouseGain;
                return Time.deltaTime > 0f ? perFrame / Time.deltaTime : 0f;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>Points every axis at its action and applies the config gains.</summary>
        public void ApplyConfig()
        {
            foreach (Controller controller in Controllers)
            {
                Configure(controller.Name, controller);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (Application.isPlaying)
            {
                UpdateControllers();
            }
        }

        #endregion

        #region Private Methods

        protected override void InitializeControllerDefaultsForAxis(in IInputAxisOwner.AxisDescriptor axis, Controller controller)
        {
            Configure(axis.Name, controller);
        }

        private void Configure(string axisName, Controller controller)
        {
            if (_config == null || controller == null)
            {
                return;
            }

            controller.Input ??= new Reader();
            controller.Driver.AccelTime = 0f;
            controller.Driver.DecelTime = 0f;
            switch (axisName)
            {
                case "Look Orbit X":
                    controller.Input.Action = _look;
                    controller.Input.MouseGain = _config.MouseSensitivity;
                    controller.Input.GamepadGain = _config.StickLookSpeed;
                    break;
                case "Look Orbit Y":
                    // Mouse/stick up looks up: the camera goes down (lower elevation).
                    controller.Input.Action = _look;
                    controller.Input.MouseGain = -_config.MouseSensitivity;
                    controller.Input.GamepadGain = -_config.StickLookSpeed;
                    break;
                case "Orbit Scale":
                    // Wheel forward / d-pad up brings the camera closer (smaller distance scale).
                    controller.Input.Action = _zoom;
                    controller.Input.MouseGain = -_config.ZoomStep;
                    controller.Input.GamepadGain = -_config.StickZoomSpeed;
                    controller.Input.MouseAsSteps = true;
                    break;
            }
        }

        #endregion
    }
}
