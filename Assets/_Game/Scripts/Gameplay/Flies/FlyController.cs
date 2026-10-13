using System.Collections;
using ProfessorSprat.Gameplay.Animation;
using ProfessorSprat.Gameplay.Config;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flies
{
    /// <summary>
    /// One Neon Fly (gameplay prefab PRF_Fly). Loops its delivered in-place idle; when collected plays PopOut (or
    /// AscentBurstInPlace for the zone's last fly) for the pop duration, then deactivates. Collection is decided by
    /// <see cref="ZoneFlyTracker"/> (distance check, no physics collider). Glow (TDD: emissive, no light): the body
    /// emits its own colours tinted by <see cref="FlyConfig.GlowColor"/> × <see cref="FlyConfig.GlowIntensity"/>, and the
    /// renderer's outline material (D-273) pulses with a per-fly phase.
    /// </summary>
    public sealed class FlyController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private FlyConfig _config;
        [SerializeField] private Animator _animator;
        [Tooltip("Delivered in-place idle: HoverIdle, HangIdle, LazyCircleDriftInPlace, OrbitLoopInPlace, FixedGazeHover.")]
        [SerializeField] private string _idleState = "HoverIdle";
        [Tooltip("Body renderer: emissive glow + outline material (second slot).")]
        [SerializeField] private Renderer _body;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");

        private AnimatorStatePlayer _player;

        #endregion

        #region Public Methods

        public string FlyId => name;

        public bool Collected { get; private set; }

        public FlyConfig Config => _config;

        public bool IsWithinReach(Vector3 point)
        {
            float radius = _config != null ? _config.TriggerRadius : 0.6f;
            return !Collected && (point - transform.position).sqrMagnitude <= radius * radius;
        }

        public void Collect(bool lastOfZone)
        {
            if (Collected)
            {
                return;
            }

            Collected = true;
            _player?.Play(lastOfZone ? "AscentBurstInPlace" : "PopOut", true);
            StartCoroutine(CoDeactivate(_config != null ? _config.PopDuration : 0.25f));
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animator != null)
            {
                _player = new AnimatorStatePlayer(_animator, 0.05f);
            }

            ApplyGlow();
        }

        private void Start()
        {
            _player?.Play(_idleState);
        }

        #endregion

        #region Private Methods

        private void ApplyGlow()
        {
            if (_body == null || _config == null)
            {
                return;
            }

            // Stable per-fly phase from the name, so the flies of a level do not pulse in sync.
            int hash = 17;
            foreach (char c in name)
            {
                hash = hash * 31 + c;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _body.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, _config.GlowColor * _config.GlowIntensity);
            block.SetFloat(PhaseId, (hash & 0xffff) / 6553.5f);
            _body.SetPropertyBlock(block);
        }

        private IEnumerator CoDeactivate(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
