using UnityEngine;

namespace ProfessorSprat.Gameplay.Config
{
    /// <summary>TDD §11.5 in-play camera (CameraRig).</summary>
    [CreateAssetMenu(menuName = "ProfessorSprat/Config/Camera", fileName = "CameraConfig")]
    public sealed class CameraConfig : ScriptableObject
    {
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 3.2f, -6.5f);
        [SerializeField] private float _lookHeight = 1.0f;
        [SerializeField] private float _fieldOfView = 50f;
        [SerializeField] private float _zoneBlendDuration = 0.8f;
        [SerializeField] private float _keyShotHold = 1.2f;
        [SerializeField] private float _keyShotBlend = 0.5f;
        [Tooltip("Stick magnitude below which a new camera-zone basis is applied immediately.")]
        [SerializeField] private float _basisSwitchStick = 0.2f;
        [Header("Player look (owner request, D-254)")]
        [Tooltip("Degrees of orbit per pixel of mouse movement.")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [Tooltip("Degrees per second at full right-stick deflection.")]
        [SerializeField] private float _stickLookSpeed = 140f;
        [Tooltip("Pitch range (degrees) around the authored follow offset; positive raises the camera.")]
        [SerializeField] private Vector2 _pitchRange = new Vector2(-20f, 35f);

        public Vector3 FollowOffset => _followOffset;
        public float LookHeight => _lookHeight;
        public float FieldOfView => _fieldOfView;
        public float ZoneBlendDuration => _zoneBlendDuration;
        public float KeyShotHold => _keyShotHold;
        public float KeyShotBlend => _keyShotBlend;
        public float BasisSwitchStick => _basisSwitchStick;
        public float MouseSensitivity => _mouseSensitivity;
        public float StickLookSpeed => _stickLookSpeed;
        public Vector2 PitchRange => _pitchRange;
    }
}
