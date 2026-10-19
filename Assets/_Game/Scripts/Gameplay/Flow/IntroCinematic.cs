using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Intro cinematic before gameplay (owner request, D-266). Plays the owner-delivered video full screen
    /// (SCN_Intro_Cinematic). The level scene starts loading in the background (low priority) only once the video is
    /// playing, so the video starts at once; the level is activated when the video ends. Any
    /// button (keyboard, gamepad, mouse) skips after <see cref="_skipDelay"/>. If the video cannot play, the level starts
    /// right away. When the video plays to the end it stays on its last frame (the B-17 gate) until the level replaces it,
    /// and the level opens on the same framing before blending to play (<see cref="ConsumeHandOff"/>, D-285).
    /// </summary>
    public sealed class IntroCinematic : MonoBehaviour
    {
        #region Fields

        private static bool s_handOff;

        [SerializeField] private VideoPlayer _video;
        [SerializeField] private string _nextScene = "SCN_HydroStation_Gameplay";
        [Tooltip("Seconds before a button press can skip (avoids skipping with the press that started the game).")]
        [SerializeField] private float _skipDelay = 0.5f;

        private AsyncOperation _loading;
        private IDisposable _anyButton;
        private float _startTime;
        private bool _finished;

        #endregion

        #region Public Methods

        public bool IsFinished => _finished;

        /// <summary>True once after the video played to its end (not skipped): the level opens on the video's last shot.</summary>
        public static bool ConsumeHandOff()
        {
            bool handOff = s_handOff;
            s_handOff = false;
            return handOff;
        }

        #endregion

        #region Unity Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_handOff = false;

        private void Awake()
        {
            // Start decoding as early as possible; Play() in Start uses the prepared player.
            if (_video != null && _video.clip != null)
            {
                _video.Prepare();
            }
        }

        private void Start()
        {
            Cursor.visible = false;
            _startTime = Time.unscaledTime;
            if (_video == null || _video.clip == null)
            {
                Debug.LogWarning($"{nameof(IntroCinematic)}: no video; starting the level.", this);
                Finish();
                return;
            }

            _video.loopPointReached += OnVideoEnded;
            _video.errorReceived += OnVideoError;
            _video.started += OnVideoStarted;
            _video.Play();
            _anyButton = InputSystem.onAnyButtonPress.Call(OnAnyButton);
        }

        private void OnDestroy()
        {
            _anyButton?.Dispose();
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
            if (_video != null)
            {
                _video.loopPointReached -= OnVideoEnded;
                _video.started -= OnVideoStarted;
                _video.errorReceived -= OnVideoError;
            }
        }

        #endregion

        #region Private Methods

        private void OnAnyButton(InputControl control)
        {
            if (Time.unscaledTime - _startTime >= _skipDelay)
            {
                Finish();
            }
        }

        private void OnVideoStarted(VideoPlayer player) => BeginLoading(ThreadPriority.Low);

        private void OnVideoEnded(VideoPlayer player)
        {
            s_handOff = true;
            Finish(keepLastFrame: true);
        }

        private void BeginLoading(ThreadPriority priority)
        {
            if (_loading != null)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_nextScene))
            {
                Debug.LogError($"{nameof(IntroCinematic)}: scene '{_nextScene}' is not in the build list.", this);
                return;
            }

            Application.backgroundLoadingPriority = priority;
            _loading = SceneManager.LoadSceneAsync(_nextScene);
            _loading.allowSceneActivation = false;
        }

        private void OnVideoError(VideoPlayer player, string message)
        {
            Debug.LogWarning($"{nameof(IntroCinematic)}: video error ({message}); starting the level.", this);
            Finish();
        }

        private void Finish(bool keepLastFrame = false)
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            _anyButton?.Dispose();
            _anyButton = null;
            if (_video != null)
            {
                // Stop clears the picture; at the natural end the last frame stays up until the level is activated.
                if (keepLastFrame)
                {
                    _video.Pause();
                }
                else
                {
                    _video.Stop();
                }
            }

            Cursor.visible = true;
            BeginLoading(ThreadPriority.High);
            Application.backgroundLoadingPriority = ThreadPriority.High;
            if (_loading != null)
            {
                _loading.allowSceneActivation = true;
            }
        }

        #endregion
    }
}
