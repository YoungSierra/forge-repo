using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>Simulated devices owned by one gold path run (names start with <see cref="NamePrefix"/>).</summary>
    public sealed class GoldPathDevices
    {
        #region Fields

        public const string NamePrefix = "V57GoldPath";

        private readonly List<InputDevice> _all = new List<InputDevice>();

        #endregion

        #region Public Methods

        public Keyboard Keyboard { get; private set; }

        public Gamepad Gamepad { get; private set; }

        public Mouse Mouse { get; private set; }

        public Touchscreen Touchscreen { get; private set; }

        /// <summary>Keyboard, Gamepad, Mouse, Touchscreen — the order used when resolving bindings.</summary>
        public IReadOnlyList<InputDevice> All => _all;

        public void Create()
        {
            Remove();
            Keyboard = InputSystem.AddDevice<Keyboard>(NamePrefix + "Keyboard");
            Gamepad = InputSystem.AddDevice<Gamepad>(NamePrefix + "Gamepad");
            Mouse = InputSystem.AddDevice<Mouse>(NamePrefix + "Mouse");
            Touchscreen = InputSystem.AddDevice<Touchscreen>(NamePrefix + "Touchscreen");
            _all.Add(Keyboard);
            _all.Add(Gamepad);
            _all.Add(Mouse);
            _all.Add(Touchscreen);
        }

        public void Remove()
        {
            foreach (InputDevice device in _all)
            {
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            _all.Clear();
            Keyboard = null;
            Gamepad = null;
            Mouse = null;
            Touchscreen = null;
        }

        public bool Owns(InputDevice device)
        {
            return device != null && _all.Contains(device);
        }

        #endregion
    }
}
