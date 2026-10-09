using UnityEngine;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// Level music base layer (TDD §10; owner delivery, D-258): plays the cue once from the start (intro), then loops the
    /// section [<see cref="_loopStart"/>, clip end] forever. Two sources alternate on the DSP clock: the next pass is
    /// scheduled to start <see cref="_crossfade"/> seconds before the current one ends, from the matching point before
    /// the loop start, and the two overlap with an equal-power crossfade so the seam has no click or level jump.
    /// </summary>
    public sealed class MusicLooper : MonoBehaviour
    {
        #region Fields

        private const double ScheduleLead = 0.5;

        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioSource _sourceA;
        [SerializeField] private AudioSource _sourceB;
        [Tooltip("Playback volume (the delivered master is loud: peak 0 dBFS, body RMS about -15 dBFS).")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.35f;
        [Tooltip("Seconds into the clip where every repeat starts (end of the intro, on a bar line).")]
        [SerializeField] private float _loopStart = 12f;
        [Tooltip("Overlap in seconds between the end of one pass and the start of the next.")]
        [SerializeField] private float _crossfade = 0.3f;

        private AudioSource _current;
        private AudioSource _next;
        private double _currentEndDsp;
        private double _nextStartDsp;
        private bool _nextScheduled;

        #endregion

        #region Public Methods

        public bool IsPlaying => _current != null && _current.isPlaying;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_clip == null || _sourceA == null || _sourceB == null)
            {
                Debug.LogWarning($"{nameof(MusicLooper)} on {name}: clip or sources missing; music slot empty.", this);
                enabled = false;
                return;
            }

            foreach (AudioSource source in new[] { _sourceA, _sourceB })
            {
                source.clip = _clip;
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
            }

            double start = AudioSettings.dspTime + 0.1;
            _current = _sourceA;
            _next = _sourceB;
            _current.time = 0f;
            _current.volume = _volume;
            _current.PlayScheduled(start);
            _currentEndDsp = start + _clip.length;
        }

        private void Update()
        {
            if (_current == null)
            {
                return;
            }

            double now = AudioSettings.dspTime;
            if (!_nextScheduled && now >= _currentEndDsp - _crossfade - ScheduleLead)
            {
                float from = Mathf.Max(0f, _loopStart - _crossfade);
                _nextStartDsp = _currentEndDsp - _crossfade;
                _next.time = from;
                _next.volume = 0f;
                _next.PlayScheduled(_nextStartDsp);
                _nextScheduled = true;
            }

            if (!_nextScheduled || now < _nextStartDsp)
            {
                return;
            }

            float t = _crossfade > 0f ? Mathf.Clamp01((float)((now - _nextStartDsp) / _crossfade)) : 1f;
            _current.volume = _volume * Mathf.Cos(t * Mathf.PI * 0.5f);
            _next.volume = _volume * Mathf.Sin(t * Mathf.PI * 0.5f);
            if (t >= 1f)
            {
                _current.Stop();
                _currentEndDsp = _nextStartDsp + (_clip.length - Mathf.Max(0f, _loopStart - _crossfade));
                (_current, _next) = (_next, _current);
                _nextScheduled = false;
            }
        }

        #endregion
    }
}
