using UnityEngine;

namespace V57.GameForge
{
    /// <summary>
    /// Follow / look-at camera rig for graybox playables.
    /// </summary>
    public class GameForgeCameraRig : MonoBehaviour
    {
        public enum Mode
        {
            Follow,
            Fps
        }

        [SerializeField] private Transform target;
        [SerializeField] private Mode mode = Mode.Follow;
        [SerializeField] private Vector3 followOffset = new(0f, 6f, -8f);
        [SerializeField] private float damp = 8f;
        [SerializeField] private float lookSensitivity = 0.15f;

        private float _yaw;
        private float _pitch;

        public void SetTarget(Transform t) => target = t;

        public void SetMode(Mode m) => mode = m;

        private void LateUpdate()
        {
            if (target == null) return;

            if (mode == Mode.Follow)
            {
                var desired = target.position + followOffset;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-damp * Time.deltaTime));
                transform.LookAt(target.position + Vector3.up * 1.2f);
            }
            else
            {
                var input = GameForgeInput.Instance;
                if (input != null)
                {
                    _yaw += input.LookDelta.x * lookSensitivity;
                    _pitch = Mathf.Clamp(_pitch - input.LookDelta.y * lookSensitivity, -80f, 80f);
                }

                transform.position = target.position + Vector3.up * 1.6f;
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }
    }
}
