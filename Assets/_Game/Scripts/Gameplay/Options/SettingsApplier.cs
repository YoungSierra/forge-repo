using ProfessorSprat.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProfessorSprat.Gameplay.Options
{
    /// <summary>
    /// Applies the scene-wide settings (D-287): game speed (time scale outside pause) and graphics quality (realtime
    /// shadows on the gameplay camera). Audio and camera gains are read where they are used.
    /// </summary>
    public sealed class SettingsApplier : MonoBehaviour
    {
        #region Fields

        [SerializeField] private Camera _camera;

        private bool _paused;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
            EventBus.Subscribe<PauseChangedEvent>(OnPause);
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Apply;
            EventBus.Unsubscribe<PauseChangedEvent>(OnPause);
        }

        #endregion

        #region Private Methods

        private void OnPause(PauseChangedEvent pause)
        {
            _paused = pause.Paused;
            Apply();
        }

        private void Apply()
        {
            if (!_paused)
            {
                Time.timeScale = GameSettings.GameSpeed;
            }

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderShadows = GameSettings.HighQuality;
            }
        }

        #endregion
    }
}
