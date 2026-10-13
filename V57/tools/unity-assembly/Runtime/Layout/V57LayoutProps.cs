using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// DCC custom properties of a layout instance or marker (unity_scene/1.1 <c>props</c>), kept on the scene object by the
    /// assembly so gameplay can read level data authored in the map (path name, speed, phase, offsets…) without parsing
    /// JSON at runtime. Values are strings as exported; arrays are comma-joined.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class V57LayoutProps : MonoBehaviour
    {
        #region Nested Types

        [Serializable]
        public struct Entry
        {
            public string key;
            public string value;
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        #endregion

        #region Public Methods

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Edit-mode setup used by the assembly tools.</summary>
        public void Configure(IEnumerable<Entry> entries)
        {
            _entries = new List<Entry>(entries);
        }

        public bool TryGet(string key, out string value)
        {
            foreach (Entry entry in _entries)
            {
                if (string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public string GetString(string key, string fallback = null)
        {
            return TryGet(key, out string value) ? value : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return TryGet(key, out string value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : fallback;
        }

        /// <summary>A comma-joined triple (e.g. <c>offset_path_m</c>); missing components stay at the fallback's.</summary>
        public Vector3 GetVector3(string key, Vector3 fallback = default)
        {
            if (!TryGet(key, out string value))
            {
                return fallback;
            }

            string[] parts = value.Split(',');
            Vector3 result = fallback;
            for (int i = 0; i < 3 && i < parts.Length; i++)
            {
                if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float component))
                {
                    result[i] = component;
                }
            }

            return result;
        }

        #endregion
    }
}
