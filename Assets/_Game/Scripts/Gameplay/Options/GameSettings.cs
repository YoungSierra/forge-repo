using System;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Options
{
    /// <summary>
    /// Player settings and accessibility options, kept in PlayerPrefs (D-287): audio, camera, graphics and comfort
    /// assists. Systems read the values directly and listen to <see cref="Changed"/> to re-apply.
    /// </summary>
    public static class GameSettings
    {
        #region Fields

        private const string Prefix = "ps.settings.";

        #endregion

        #region Public Methods

        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(Prefix + "music", 0.8f);
            set => SetFloat("music", Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(Prefix + "sfx", 0.9f);
            set => SetFloat("sfx", Mathf.Clamp01(value));
        }

        /// <summary>Multiplier on the camera look gains (0.3–2.5).</summary>
        public static float LookSensitivity
        {
            get => PlayerPrefs.GetFloat(Prefix + "sens", 1f);
            set => SetFloat("sens", Mathf.Clamp(value, 0.3f, 2.5f));
        }

        public static bool InvertY
        {
            get => GetBool("invertY", false);
            set => SetBool("invertY", value);
        }

        /// <summary>High: shadows, full resolution. Low: no realtime shadows, 80 % render scale.</summary>
        public static bool HighQuality
        {
            get => GetBool("quality", true);
            set => SetBool("quality", value);
        }

        public static bool ShowFps
        {
            get => GetBool("fps", false);
            set => SetBool("fps", value);
        }

        /// <summary>Step-by-step training on the next start (cleared once training is finished or skipped).</summary>
        public static bool Tutorial
        {
            get => GetBool("tutorial", true);
            set => SetBool("tutorial", value);
        }

        public static bool CameraShake
        {
            get => GetBool("shake", true);
            set => SetBool("shake", value);
        }

        /// <summary>Off: full-screen flashes drop to a quarter of their strength.</summary>
        public static bool ScreenFlashes
        {
            get => GetBool("flashes", true);
            set => SetBool("flashes", value);
        }

        /// <summary>Light pillars over the flies still to collect.</summary>
        public static bool FlyBeacons
        {
            get => GetBool("beacons", false);
            set => SetBool("beacons", value);
        }

        /// <summary>Gameplay time scale (0.5–1), an accessibility assist.</summary>
        public static float GameSpeed
        {
            get => PlayerPrefs.GetFloat(Prefix + "speed", 1f);
            set => SetFloat("speed", Mathf.Clamp(value, 0.5f, 1f));
        }

        /// <summary>Best completed run time in seconds; 0 when none yet.</summary>
        public static float BestTime
        {
            get => PlayerPrefs.GetFloat(Prefix + "best", 0f);
            set
            {
                PlayerPrefs.SetFloat(Prefix + "best", value);
                PlayerPrefs.Save();
            }
        }

        public static void ResetToDefaults()
        {
            foreach (string key in new[] { "music", "sfx", "sens", "invertY", "quality", "fps", "tutorial", "shake", "flashes", "beacons", "speed" })
            {
                PlayerPrefs.DeleteKey(Prefix + key);
            }

            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        #endregion

        #region Private Methods

        private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) == 1;

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        #endregion
    }
}
