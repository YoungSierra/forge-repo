using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.GoldPath
{
    /// <summary>
    /// Probe registered by the driver for engine-level facts, so gold paths need no game code for them:
    /// <c>scene.active</c> (string), <c>scene.&lt;Name&gt;</c> (bool, active scene name), <c>time.level</c>,
    /// <c>time.unscaled</c>, <c>frame</c> (float), <c>marker.&lt;Kind&gt;.&lt;Id&gt;</c> (bool, active V57Marker exists).
    /// Registered first, so game probes (registered later) win on key clashes.
    /// </summary>
    public sealed class GoldPathBuiltinProbe : IGoldPathProbe
    {
        #region Public Methods

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            if (key.StartsWith("scene.", StringComparison.Ordinal) && key != "scene.active")
            {
                value = string.Equals(SceneManager.GetActiveScene().name, key.Substring(6), StringComparison.Ordinal);
                return true;
            }

            if (key.StartsWith("marker.", StringComparison.Ordinal))
            {
                string[] parts = key.Split(new[] { '.' }, 3);
                if (parts.Length == 3 && Enum.TryParse(parts[1], true, out V57MarkerKind kind))
                {
                    value = V57Marker.Find(kind, parts[2]) != null;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFloat(string key, out float value)
        {
            switch (key)
            {
                case "time.level":
                    value = Time.timeSinceLevelLoad;
                    return true;
                case "time.unscaled":
                    value = Time.unscaledTime;
                    return true;
                case "frame":
                    value = Time.frameCount;
                    return true;
                default:
                    value = 0f;
                    return false;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            if (key == "scene.active")
            {
                value = SceneManager.GetActiveScene().name;
                return true;
            }

            value = null;
            return false;
        }

        #endregion
    }
}
