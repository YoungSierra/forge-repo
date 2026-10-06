using System.IO;
using UnityEditor;
using UnityEngine;
using V57.GoldPath;

namespace V57.Assembly
{
    /// <summary>
    /// Safety net for the gold path driver: after Play Mode ends (or the Editor reloads with a leftover marker file),
    /// re-enables real input devices the driver disabled and removes leftover simulated devices.
    /// The last recovery time is kept in SessionState (<see cref="LastRecoveryKey"/>) for diagnostics.
    /// </summary>
    [InitializeOnLoad]
    public static class GoldPathEditorRecovery
    {
        #region Fields

        public const string LastRecoveryKey = "V57.GoldPath.LastRecovery";

        #endregion

        #region Initialization

        static GoldPathEditorRecovery()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += RecoverIfMarkerExists;
        }

        #endregion

        #region Private Methods

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                bool hadMarker = File.Exists(GoldPathDeviceGuard.MarkerPath);
                GoldPathDeviceGuard.Recover();
                if (hadMarker)
                {
                    Record("after Play Mode");
                }
            }
        }

        private static void RecoverIfMarkerExists()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(GoldPathDeviceGuard.MarkerPath))
            {
                GoldPathDeviceGuard.Recover();
                Record("on Editor load");
            }
        }

        private static void Record(string when)
        {
            SessionState.SetString(LastRecoveryKey, System.DateTime.UtcNow.ToString("o"));
            Debug.LogWarning($"V57 GoldPath: restored input devices left disabled by an interrupted run ({when}).");
        }

        #endregion
    }
}
