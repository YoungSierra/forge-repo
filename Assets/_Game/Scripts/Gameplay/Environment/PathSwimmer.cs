using UnityEngine;
using V57.GoldPath;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// A fish swimming along its map path (Level_01 V10 <c>trayectorias</c>, D-270), driven by the layout props the map puts on
    /// the instance: <c>trayectoria</c> (path name), <c>velocidad_mps</c>, <c>fase</c> (0–1 of the path length),
    /// <c>offset_path_m</c> = [lateral (left of travel), vertical, along], <c>escala</c>, <c>clip</c> and
    /// <c>clip_offset_frames</c> (so a school does not flap in sync). Closed paths loop; open ones go back and forth.
    /// Ambient only: no collider, no gameplay effect.
    /// </summary>
    public sealed class PathSwimmer : MonoBehaviour
    {
        #region Fields

        private const float ClipFrames = 96f;

        [SerializeField] private Animator _animator;
        [Tooltip("Animator state played when the props name no clip.")]
        [SerializeField] private string _defaultClip = "HoverIdle";
        [Tooltip("How fast the body turns to the swim direction (1/s).")]
        [SerializeField] private float _turnRate = 5f;

        private V57Path _path;
        private float _speed;
        private float _distance;
        private Vector3 _offset;
        private float _direction = 1f;

        #endregion

        #region Public Methods

        public V57Path Path => _path;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            V57LayoutProps props = GetComponent<V57LayoutProps>();
            if (props == null)
            {
                enabled = false;
                return;
            }

            float scale = props.GetFloat("escala", 1f);
            transform.localScale *= scale;
            PlayClip(props.GetString("clip", _defaultClip), props.GetFloat("clip_offset_frames", 0f));

            _path = V57Path.Find(props.GetString("trayectoria", string.Empty));
            if (_path == null || _path.Points.Count < 2)
            {
                Debug.LogWarning($"{nameof(PathSwimmer)} on {name}: path '{props.GetString("trayectoria", "?")}' not found; swims in place.", this);
                enabled = false;
                return;
            }

            _speed = props.GetFloat("velocidad_mps", _path.Speed > 0f ? _path.Speed : 0.8f);
            _offset = props.GetVector3("offset_path_m", Vector3.zero);
            _distance = props.GetFloat("fase", 0f) * _path.Length + _offset.z;
            Place(1f);
        }

        private void Update()
        {
            _distance += _speed * _direction * Time.deltaTime;
            if (!_path.Loop)
            {
                float length = _path.Length;
                if (_distance >= length || _distance <= 0f)
                {
                    _distance = Mathf.Clamp(_distance, 0f, length);
                    _direction = -_direction;
                }
            }

            Place(1f - Mathf.Exp(-_turnRate * Time.deltaTime));
        }

        #endregion

        #region Private Methods

        private void Place(float turnBlend)
        {
            _path.Evaluate(_distance, out Vector3 point, out Vector3 tangent);
            Vector3 travel = tangent * _direction;
            Vector3 left = Vector3.Cross(travel, Vector3.up);
            left = left.sqrMagnitude > 0.0001f ? left.normalized : Vector3.left;
            transform.position = point + left * _offset.x + Vector3.up * _offset.y;
            if (travel.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(travel, Vector3.up), turnBlend);
            }
        }

        private void PlayClip(string clip, float offsetFrames)
        {
            if (_animator == null || string.IsNullOrEmpty(clip))
            {
                return;
            }

            int hash = Animator.StringToHash(clip);
            if (_animator.HasState(0, hash))
            {
                _animator.Play(hash, 0, Mathf.Repeat(offsetFrames / ClipFrames, 1f));
            }
        }

        #endregion
    }
}
