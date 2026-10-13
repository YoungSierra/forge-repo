using UnityEngine;
using V57.GoldPath;

namespace ProfessorSprat.Gameplay.Environment
{
    /// <summary>
    /// A jellyfish floating where the map put it (Level_01 V10, D-270): a slow drift inside <c>deriva_m</c> (suggested
    /// radius from the map) plus a gentle bob, pulsing with its clip (<c>clip</c>, <c>clip_offset_frames</c> so neighbours
    /// do not pulse together) at <c>escala</c>. Ambient only: no collider, no gameplay effect.
    /// </summary>
    public sealed class JellyfishDrift : MonoBehaviour
    {
        #region Fields

        private const float ClipFrames = 96f;

        [SerializeField] private Animator _animator;
        [SerializeField] private string _defaultClip = "PulseIdle";
        [Tooltip("Seconds for one full drift loop.")]
        [SerializeField] private float _driftPeriod = 14f;
        [Tooltip("Vertical bob amplitude as a fraction of the drift radius.")]
        [SerializeField] private float _bobFraction = 0.5f;

        private Vector3 _home;
        private float _radius;
        private float _phase;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            V57LayoutProps props = GetComponent<V57LayoutProps>();
            _home = transform.position;
            _radius = props != null ? props.GetFloat("deriva_m", 0.4f) : 0.4f;
            float offsetFrames = props != null ? props.GetFloat("clip_offset_frames", 0f) : 0f;
            _phase = offsetFrames / ClipFrames * Mathf.PI * 2f;
            if (props != null)
            {
                transform.localScale *= props.GetFloat("escala", 1f);
            }

            string clip = props != null ? props.GetString("clip", _defaultClip) : _defaultClip;
            if (_animator != null && _animator.HasState(0, Animator.StringToHash(clip)))
            {
                _animator.Play(Animator.StringToHash(clip), 0, Mathf.Repeat(offsetFrames / ClipFrames, 1f));
            }
        }

        private void Update()
        {
            float t = Time.time * Mathf.PI * 2f / Mathf.Max(1f, _driftPeriod) + _phase;
            // Lissajous drift (1:2) inside the radius, bob on a third frequency: never repeats visibly.
            Vector3 drift = new Vector3(Mathf.Sin(t), Mathf.Sin(t * 1.37f) * _bobFraction, Mathf.Sin(t * 2f + 1.1f) * 0.5f) * _radius;
            transform.position = _home + drift;
        }

        #endregion
    }
}
