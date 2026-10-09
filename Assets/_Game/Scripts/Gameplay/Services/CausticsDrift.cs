using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// Underwater caustics (owner request, D-256): plays a looping flipbook of tiling caustics cookies on the sun and
    /// slides it slowly, so a wobbling web of light moves over everything the sun reaches (shadowed areas get none).
    /// Visual only; no gameplay effect.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class CausticsDrift : MonoBehaviour
    {
        #region Fields

        [Tooltip("Looping caustics frames (tiling cookies).")]
        [SerializeField] private Texture2D[] _frames = new Texture2D[0];
        [Tooltip("Flipbook speed in frames per second.")]
        [SerializeField] private float _framesPerSecond = 16f;
        [Tooltip("World size (metres) one cookie tile covers.")]
        [SerializeField] private Vector2 _tileSize = new Vector2(5f, 5f);
        [Tooltip("Drift speed in metres per second.")]
        [SerializeField] private Vector2 _speed = new Vector2(0.18f, 0.11f);

        private Light _light;
        private UniversalAdditionalLightData _lightData;
        private int _frame = -1;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _light = GetComponent<Light>();
            _lightData = GetComponent<UniversalAdditionalLightData>();
            if (_lightData != null)
            {
                _lightData.lightCookieSize = _tileSize;
            }
        }

        private void Update()
        {
            if (_frames.Length > 0)
            {
                int frame = (int)(Time.time * _framesPerSecond) % _frames.Length;
                if (frame != _frame && _frames[frame] != null)
                {
                    _frame = frame;
                    _light.cookie = _frames[frame];
                }
            }

            if (_lightData != null)
            {
                Vector2 offset = _speed * Time.time;
                _lightData.lightCookieOffset = new Vector2(Mathf.Repeat(offset.x, _tileSize.x), Mathf.Repeat(offset.y, _tileSize.y));
            }
        }

        #endregion
    }
}
