using UnityEngine;

namespace ProfessorSprat.Gameplay.Animation
{
    /// <summary>
    /// Plays named states of a V57-generated controller (<c>AC_&lt;Asset&gt;</c>: one state per delivered clip, no
    /// transitions/parameters) by cross-fading, only when the requested state changes. Missing states log once.
    /// </summary>
    public sealed class AnimatorStatePlayer
    {
        #region Fields

        private readonly Animator _animator;
        private readonly float _fade;
        private string _current;
        private bool _warned;

        #endregion

        #region Public Methods

        public AnimatorStatePlayer(Animator animator, float fade)
        {
            _animator = animator;
            _fade = fade;
        }

        public string Current => _current;

        /// <summary>Cross-fades to <paramref name="state"/>; <paramref name="restart"/> replays it when already current.</summary>
        public void Play(string state, bool restart = false, float speed = 1f)
        {
            if (_animator == null || (!restart && state == _current))
            {
                return;
            }

            int hash = Animator.StringToHash(state);
            if (!_animator.HasState(0, hash))
            {
                if (!_warned)
                {
                    Debug.LogWarning($"Animator on {_animator.name} has no state '{state}' (delivered clips only).", _animator);
                    _warned = true;
                }

                return;
            }

            _animator.speed = speed;
            _animator.CrossFadeInFixedTime(hash, _fade, 0, 0f);
            _current = state;
        }

        /// <summary>Length in seconds of the current state's clip at speed 1, or 0.</summary>
        public float ClipLength(string state)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return 0f;
            }

            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name.EndsWith(state, System.StringComparison.Ordinal))
                {
                    return clip.length;
                }
            }

            return 0f;
        }

        #endregion
    }
}
