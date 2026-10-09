using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>
    /// Facade used by the step executor: creates simulated devices, resolves targets, tracks held inputs
    /// and releases everything on <see cref="Teardown"/> (idempotent, safe to call from finally and OnDisable).
    /// Every control value the gold path wrote is remembered (released ones at 0) and written in full on each event, and
    /// re-written before every input update while something is held (keep-alive after a device reset or dropped event).
    /// </summary>
    public sealed class GoldPathInputSimulator
    {
        #region Fields

        private readonly GoldPathDevices _devices = new GoldPathDevices();
        private readonly GoldPathInputSettingsScope _settings = new GoldPathInputSettingsScope();
        private readonly Dictionary<string, GoldPathInputTarget> _held = new Dictionary<string, GoldPathInputTarget>();
        private readonly Dictionary<InputControl, Vector2> _owned = new Dictionary<InputControl, Vector2>();
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

            InputSystem.onBeforeUpdate += RefreshHeld;
            IsActive = true;
        }

        public void Teardown()
        {
            if (!IsActive)
            {
                return;
            }

            InputSystem.onBeforeUpdate -= RefreshHeld;
            foreach (GoldPathInputTarget target in _held.Values)
            {
                Write(target, false, Vector2.zero);
            }

            _held.Clear();
            _owned.Clear();
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
            if (_held.TryGetValue(target, out GoldPathInputTarget previous))
            {
                resolved = previous;
            }

            _held[target] = resolved;
            Write(resolved, true, ParseVector(value, defaultValue));
            return true;
        }

        public bool End(string target, out string detail)
        {
            if (_held.TryGetValue(target, out GoldPathInputTarget held))
            {
                _held.Remove(target);
                Write(held, false, Vector2.zero);
                detail = $"released {held.Description}";
                return true;
            }

            if (IsActive && _resolver.TryResolve(target, false, out GoldPathInputTarget resolved, out detail))
            {
                Write(resolved, false, Vector2.zero);
                detail = $"released (was not held) {resolved.Description}";
                return true;
            }

            detail = $"cannot release '{target}': not held and not resolvable";
            return false;
        }

        #endregion

        #region Private Methods

        private void Write(GoldPathInputTarget target, bool down, Vector2 value)
        {
            if (target.Touch != null)
            {
                GoldPathInputWriter.ApplyTouch(target, down, value);
                return;
            }

            foreach (KeyValuePair<InputControl, Vector2> pair in GoldPathInputWriter.Values(target, down, value))
            {
                _owned[pair.Key] = pair.Value;
            }

            GoldPathInputWriter.Queue(_owned);
        }

        private void RefreshHeld()
        {
            if (IsActive && _held.Count > 0 && _owned.Count > 0)
            {
                GoldPathInputWriter.Queue(_owned);
            }
        }

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
