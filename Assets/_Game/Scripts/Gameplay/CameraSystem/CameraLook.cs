using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.CameraSystem
{
    /// <summary>
    /// Player camera control (owner request, D-254/D-255): orbit yaw/pitch from look input, automatic recentering behind
    /// the Professor after <see cref="CameraConfig.RecenterDelay"/> without look input, and a persistent zoom factor on the
    /// follow distance. Plain class shared by <see cref="CameraRig"/> and tests.
    /// </summary>
    public sealed class CameraLook
    {
        #region Fields

        private readonly CameraConfig _config;
        private float _idle;
        private float _yawVelocity;
        private float _pitchVelocity;

        #endregion

        #region Public Methods

        public CameraLook(CameraConfig config)
        {
            _config = config;
        }

        /// <summary>Orbit (degrees) added to the camera-zone yaw.</summary>
        public float Yaw { get; private set; }

        /// <summary>Pitch (degrees) around the authored offset; positive raises the camera.</summary>
        public float Pitch { get; private set; }

        /// <summary>Follow-distance multiplier.</summary>
        public float Zoom { get; private set; } = 1f;

        /// <summary>Look input in degrees (x = yaw, y = pitch); restarts the recenter delay.</summary>
        public void AddLook(Vector2 degrees)
        {
            if (degrees.sqrMagnitude <= 0f)
            {
                return;
            }

            Yaw = Mathf.Repeat(Yaw + degrees.x + 180f, 360f) - 180f;
            Pitch = Mathf.Clamp(Pitch + degrees.y, _config.PitchRange.x, _config.PitchRange.y);
            _idle = 0f;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;
        }

        /// <summary>Zoom input: positive brings the camera closer (relative change of the follow distance).</summary>
        public void AddZoom(float amount)
        {
            Zoom = Mathf.Clamp(Zoom * (1f - amount), _config.ZoomRange.x, _config.ZoomRange.y);
        }

        /// <summary>Advances the recenter timer and eases yaw/pitch back to 0 once the delay has passed.</summary>
        public void Tick(float deltaTime)
        {
            _idle += deltaTime;
            if (_idle < _config.RecenterDelay || deltaTime <= 0f)
            {
                return;
            }

            Yaw = Mathf.SmoothDampAngle(Yaw, 0f, ref _yawVelocity, _config.RecenterTime, Mathf.Infinity, deltaTime);
            Pitch = Mathf.SmoothDamp(Pitch, 0f, ref _pitchVelocity, _config.RecenterTime, Mathf.Infinity, deltaTime);
            if (Mathf.Abs(Yaw) < 0.01f && Mathf.Abs(Pitch) < 0.01f)
            {
                Yaw = 0f;
                Pitch = 0f;
            }
        }

        /// <summary>World-space follow offset for the camera-zone yaw.</summary>
        public Vector3 Offset(float zoneYaw, Vector3 authoredOffset)
        {
            return Quaternion.Euler(0f, zoneYaw + Yaw, 0f) * (Quaternion.Euler(Pitch, 0f, 0f) * (authoredOffset * Zoom));
        }

        #endregion
    }
}
