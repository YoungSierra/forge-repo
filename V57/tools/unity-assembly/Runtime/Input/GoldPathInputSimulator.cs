using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Facade used by the step executor: creates simulated devices, resolves targets, tracks held inputs
    /// and releases everything on <see cref="Teardown"/> (idempotent, safe to call from finally and OnDisable).
    /// </summary>
    public sealed class GoldPathInputSimulator
    {
        #region Fields

        private readonly GoldPathDevices _devices = new GoldPathDevices();
        private readonly GoldPathInputSettingsScope _settings = new GoldPathInputSettingsScope();
        private readonly Dictionary<string, GoldPathInputTarget> _held = new Dictionary<string, GoldPathInputTarget>();
        private GoldPathControlResolver _resolver;

        #endregion

        #region Public Methods

        public bool IsActive { get; private set; }

        public void Setup(bool disableRealDevices)
        {
            if (IsActive)
            {
                return;
            }

            _settings.Enter();
            _devices.Create();
            _resolver = new GoldPathControlResolver(_devices);
            if (disableRealDevices)
            {
                GoldPathDeviceGuard.DisableRealDevices();
            }

            IsActive = true;
        }

        public void Teardown()
        {
            if (!IsActive)
            {
                return;
            }

            foreach (GoldPathInputTarget target in _held.Values)
            {
                GoldPathInputWriter.Apply(target, false, Vector2.zero);
            }

            _held.Clear();
            _devices.Remove();
            GoldPathDeviceGuard.RestoreAll();
            _settings.Exit();
            IsActive = false;
        }

        public bool TryBegin(string target, bool isVector, string value, out string detail)
        {
            if (!IsActive)
            {
                detail = "input simulator not active";
                return false;
            }

            if (!_resolver.TryResolve(target, isVector, out GoldPathInputTarget resolved, out detail))
            {
                return false;
            }

            Vector2 defaultValue = resolved.Touch != null ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            GoldPathInputWriter.Apply(resolved, true, ParseVector(value, defaultValue));
            _held[target] = resolved;
            return true;
        }

        public bool End(string target, out string detail)
        {
            if (_held.TryGetValue(target, out GoldPathInputTarget held))
            {
                GoldPathInputWriter.Apply(held, false, Vector2.zero);
                _held.Remove(target);
                detail = $"released {held.Description}";
                return true;
            }

            if (IsActive && _resolver.TryResolve(target, false, out GoldPathInputTarget resolved, out detail))
            {
                GoldPathInputWriter.Apply(resolved, false, Vector2.zero);
                detail = $"released (was not held) {resolved.Description}";
                return true;
            }

            detail = $"cannot release '{target}': not held and not resolvable";
            return false;
        }

        #endregion

        #region Private Methods

        private static Vector2 ParseVector(string value, Vector2 fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string[] parts = value.Split(',');
            float x;
            float y = 0f;
            bool okX = float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            bool okY = parts.Length < 2 || float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y);
            return okX && okY ? new Vector2(x, y) : fallback;
        }

        #endregion
    }
}
