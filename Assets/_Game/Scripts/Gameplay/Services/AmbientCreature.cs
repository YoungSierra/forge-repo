using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Animation;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Services
{
    /// <summary>
    /// TDD §B-S AmbientCreatures (§6 set-piece types): a non-interactive creature that loops its idle/transit clip and, when
    /// configured, plays a one-shot reaction once while the Professor is within range (the Shark's EyeRotation at 6 m).
    /// No collider, no gameplay effect.
    /// </summary>
    public sealed class AmbientCreature : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Animator _animator;
        [SerializeField] private string _loopState = "PassiveTransit";
        [SerializeField] private string _reactionState = "EyeRotation";
        [SerializeField] private float _reactionRange = 6f;

        private AnimatorStatePlayer _player;
        private bool _reacted;
        private float _reactionRemaining;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _player = _animator != null ? new AnimatorStatePlayer(_animator, 0.2f) : null;
        }

        private void Start()
        {
            _player?.Play(_loopState);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
        }

        private void Update()
        {
            if (_reactionRemaining <= 0f)
            {
                return;
            }

            _reactionRemaining -= Time.deltaTime;
            if (_reactionRemaining <= 0f)
            {
                _player?.Play(_loopState);
            }
        }

        #endregion

        #region Private Methods

        private void OnPlayerMoved(PlayerMovedEvent moved)
        {
            if (_reacted || string.IsNullOrEmpty(_reactionState) || _player == null)
            {
                return;
            }

            if ((moved.Position - transform.position).sqrMagnitude > _reactionRange * _reactionRange)
            {
                return;
            }

            _reacted = true;
            _player.Play(_reactionState, true);
            _reactionRemaining = Mathf.Max(0.1f, _player.ClipLength(_reactionState));
        }

        #endregion
    }
}
