using UnityEngine;

namespace ProfessorSprat.Gameplay.Input
{
    /// <summary>
    /// Camera-relative movement basis (TDD §11.5): the stick is read in the yaw of the active camera zone. A new zone yaw
    /// applies when the stick drops below the switch threshold or after the zone blend, so directions never flip
    /// mid-press. Plain class shared by <see cref="InputHandler"/> and tests.
    /// </summary>
    public sealed class MoveBasis
    {
        #region Fields

        private readonly float _switchStick;
        private readonly float _switchDelay;
        private float _pendingYaw;
        private float _pendingSince;

        #endregion

        #region Public Methods

        public MoveBasis(float initialYaw, float switchStick, float switchDelay)
        {
            Yaw = initialYaw;
            _pendingYaw = initialYaw;
            _switchStick = switchStick;
            _switchDelay = switchDelay;
        }

        /// <summary>Yaw (degrees) currently used to convert the stick to world space.</summary>
        public float Yaw { get; private set; }

        /// <summary>Feeds the camera-zone yaw and stick magnitude at <paramref name="time"/> (seconds).</summary>
        public void Update(float cameraYaw, float stickMagnitude, float time)
        {
            if (!Mathf.Approximately(cameraYaw, _pendingYaw))
            {
                _pendingYaw = cameraYaw;
                _pendingSince = time;
            }

            if (!Mathf.Approximately(_pendingYaw, Yaw) && (stickMagnitude < _switchStick || time - _pendingSince >= _switchDelay))
            {
                Yaw = _pendingYaw;
            }
        }

        /// <summary>World XZ direction for a stick value (x = right, y = up).</summary>
        public Vector3 ToWorld(Vector2 stick)
        {
            return ToWorld(stick, 0f);
        }

        /// <summary>World XZ direction with the player's camera orbit added (applies immediately, no deferral).</summary>
        public Vector3 ToWorld(Vector2 stick, float lookYaw)
        {
            return Quaternion.Euler(0f, Yaw + lookYaw, 0f) * new Vector3(stick.x, 0f, stick.y);
        }

        #endregion
    }
}
