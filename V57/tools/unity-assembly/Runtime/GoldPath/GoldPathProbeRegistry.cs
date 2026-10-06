using System;
using System.Collections.Generic;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Static registry of <see cref="IGoldPathProbe"/> instances. The most recently registered probe that
    /// answers a key wins. Destroyed Unity objects are skipped. Reset on SubsystemRegistration so it is
    /// safe with Domain Reload disabled.
    /// </summary>
    public static class GoldPathProbeRegistry
    {
        #region Private Fields

        private static readonly List<IGoldPathProbe> Probes = new List<IGoldPathProbe>();

        #endregion

        #region Public Methods

        public static int Count => Probes.Count;

        public static void Register(IGoldPathProbe probe)
        {
            if (probe != null && !Probes.Contains(probe))
            {
                Probes.Add(probe);
            }
        }

        public static void Unregister(IGoldPathProbe probe)
        {
            Probes.Remove(probe);
        }

        public static void Clear()
        {
            Probes.Clear();
        }

        public static bool TryGetBool(string key, out bool value)
        {
            for (int i = Probes.Count - 1; i >= 0; i--)
            {
                IGoldPathProbe probe = Probes[i];
                if (IsAlive(probe) && Guard(() => probe.TryGetBool(key, out bool v) ? v : (bool?)null, out bool? result) && result.HasValue)
                {
                    value = result.Value;
                    return true;
                }
            }

            value = false;
            return false;
        }

        public static bool TryGetFloat(string key, out float value)
        {
            for (int i = Probes.Count - 1; i >= 0; i--)
            {
                IGoldPathProbe probe = Probes[i];
                if (IsAlive(probe) && Guard(() => probe.TryGetFloat(key, out float v) ? v : (float?)null, out float? result) && result.HasValue)
                {
                    value = result.Value;
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        public static bool TryGetString(string key, out string value)
        {
            for (int i = Probes.Count - 1; i >= 0; i--)
            {
                IGoldPathProbe probe = Probes[i];
                if (IsAlive(probe) && Guard(() => probe.TryGetString(key, out string v) ? v : null, out string result) && result != null)
                {
                    value = result;
                    return true;
                }
            }

            value = null;
            return false;
        }

        #endregion

        #region Private Methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Probes.Clear();
        }

        private static bool IsAlive(IGoldPathProbe probe)
        {
            if (probe is UnityEngine.Object unityObject)
            {
                return unityObject != null;
            }

            return probe != null;
        }

        private static bool Guard<T>(Func<T> query, out T result)
        {
            try
            {
                result = query();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GoldPathProbeRegistry: probe threw {exception.GetType().Name}: {exception.Message}");
                result = default;
                return false;
            }
        }

        #endregion
    }
}
