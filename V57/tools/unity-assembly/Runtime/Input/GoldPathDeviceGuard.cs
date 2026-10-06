using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>
    /// Disables real keyboards/pointers/gamepads during a run so a human cannot interfere, and restores them.
    /// Disabled device ids are also written to a marker file so the Editor can restore them after a crash,
    /// a stopped coroutine or a domain reload (<see cref="Recover"/>, called by the Editor recovery hook).
    /// </summary>
    public static class GoldPathDeviceGuard
    {
        #region Fields

        private static readonly List<int> DisabledIds = new List<int>();

        #endregion

        #region Public Methods

        public static string MarkerPath => Path.Combine(Application.temporaryCachePath, "v57-goldpath-devices.txt");

        public static void DisableRealDevices()
        {
            List<InputDevice> devices = new List<InputDevice>(InputSystem.devices);
            foreach (InputDevice device in devices)
            {
                bool isSimulated = device.name.StartsWith(GoldPathDevices.NamePrefix, System.StringComparison.Ordinal);
                bool isPlayerInput = device is Keyboard || device is Pointer || device is Gamepad;
                if (isSimulated || !isPlayerInput || !device.enabled)
                {
                    continue;
                }

                InputSystem.DisableDevice(device);
                DisabledIds.Add(device.deviceId);
            }

            WriteMarker();
        }

        public static void RestoreAll()
        {
            foreach (int id in DisabledIds)
            {
                InputDevice device = InputSystem.GetDeviceById(id);
                if (device != null && !device.enabled)
                {
                    InputSystem.EnableDevice(device);
                }
            }

            DisabledIds.Clear();
            if (File.Exists(MarkerPath))
            {
                File.Delete(MarkerPath);
            }
        }

        /// <summary>Restores devices listed in the marker file and removes leftover simulated devices.</summary>
        public static void Recover()
        {
            if (File.Exists(MarkerPath))
            {
                foreach (string line in File.ReadAllLines(MarkerPath))
                {
                    if (int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && !DisabledIds.Contains(id))
                    {
                        DisabledIds.Add(id);
                    }
                }
            }

            RestoreAll();
            List<InputDevice> devices = new List<InputDevice>(InputSystem.devices);
            foreach (InputDevice device in devices)
            {
                if (device.name.StartsWith(GoldPathDevices.NamePrefix, System.StringComparison.Ordinal))
                {
                    InputSystem.RemoveDevice(device);
                }
            }
        }

        #endregion

        #region Private Methods

        private static void WriteMarker()
        {
            List<string> lines = new List<string>();
            foreach (int id in DisabledIds)
            {
                lines.Add(id.ToString(CultureInfo.InvariantCulture));
            }

            File.WriteAllLines(MarkerPath, lines);
        }

        #endregion
    }
}
