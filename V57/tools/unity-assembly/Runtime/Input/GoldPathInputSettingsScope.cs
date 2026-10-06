using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>
    /// Swaps in a temporary copy of <see cref="InputSystem.settings"/> so simulated input is processed even when
    /// the Game View / player window has no focus, then restores the original settings object.
    /// The project settings asset itself is never modified.
    /// </summary>
    public sealed class GoldPathInputSettingsScope
    {
        #region Fields

        private InputSettings _original;
        private InputSettings _temporary;

        #endregion

        #region Public Methods

        public void Enter()
        {
            if (_temporary != null || InputSystem.settings == null)
            {
                return;
            }

            _original = InputSystem.settings;
            _temporary = Object.Instantiate(_original);
            _temporary.name = "V57GoldPathInputSettings";
            _temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Editor-only setting (Project Settings > Input System > Play Mode Input Behavior).
            _temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _temporary;
        }

        public void Exit()
        {
            if (_temporary == null)
            {
                return;
            }

            if (_original != null)
            {
                InputSystem.settings = _original;
            }

            Object.Destroy(_temporary);
            _temporary = null;
            _original = null;
        }

        #endregion
    }
}
