using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Player;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Animation
{
    /// <summary>
    /// TDD §B-S CharacterAnimationDriver: the only place that drives the Professor's Animator. Maps mechanic state to the
    /// delivered clips: Idle / Walk (0.1–3 m/s) / Run / Jump (airborne) / Attack (stomp + recovery) / HitReaction (stun).
    /// Death is not used (no death in this game). Gameplay code never calls the Animator directly.
    /// Footsteps (D-264): while Walk or Run plays, publishes <see cref="FootstepEvent"/> each time the clip crosses a
    /// foot-contact point (both delivered clips plant the left foot at ~0 % and the right at ~50 % of the cycle).
    /// </summary>
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        #region Fields

        private const float WalkSpeed = 0.1f;
        private const float RunSpeed = 3.0f;
        private const float Fade = 0.1f;
        private static readonly float[] FootContacts = { 0.02f, 0.52f };

        [SerializeField] private Animator _animator;
        [SerializeField] private ProfessorLocomotion _locomotion;
        [SerializeField] private ProfessorStomp _stomp;

        private AnimatorStatePlayer _player;
        private string _stepState;
        private float _stepPhase;

        #endregion

        #region Public Methods

        public string CurrentState => _player?.Current;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animator == null || _locomotion == null)
            {
                Debug.LogError($"{nameof(CharacterAnimationDriver)} on {name}: animator or locomotion missing; disabled.", this);
                enabled = false;
                return;
            }

            _animator.applyRootMotion = false;
            _player = new AnimatorStatePlayer(_animator, Fade);
        }

        private void Update()
        {
            _player.Play(Resolve());
            DetectFootstep();
        }

        #endregion

        #region Private Methods

        private void DetectFootstep()
        {
            string state = _player.Current;
            if (state != "Walk" && state != "Run")
            {
                _stepState = null;
                return;
            }

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(state))
            {
                return;
            }

            float phase = Mathf.Repeat(info.normalizedTime, 1f);
            if (_stepState == state)
            {
                foreach (float contact in FootContacts)
                {
                    bool crossed = phase >= _stepPhase ? _stepPhase < contact && contact <= phase : contact > _stepPhase || contact <= phase;
                    if (crossed)
                    {
                        EventBus.Publish(new FootstepEvent(_locomotion.transform.position, _locomotion.HorizontalVelocity.magnitude));
                    }
                }
            }

            _stepState = state;
            _stepPhase = phase;
        }

        private string Resolve()
        {
            if (_locomotion.IsStunned)
            {
                return "HitReaction";
            }

            if (_stomp != null && _stomp.State != StompState.Idle)
            {
                return "Attack";
            }

            if (!_locomotion.IsGrounded)
            {
                return "Jump";
            }

            float speed = _locomotion.HorizontalVelocity.magnitude;
            if (speed > RunSpeed)
            {
                return "Run";
            }

            return speed > WalkSpeed ? "Walk" : "Idle";
        }

        #endregion
    }
}
