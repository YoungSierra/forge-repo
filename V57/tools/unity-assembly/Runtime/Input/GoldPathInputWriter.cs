using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace V57.GoldPath
{
    /// <summary>
    /// Queues state events on simulated devices (same technique as InputTestFixture.Set). A state event carries the whole
    /// device, captured from its current state — which can be stale while earlier events are still queued. So the caller
    /// passes every control value the gold path owns (held inputs and released ones at 0), and all of them are written
    /// into each event: queued writes can never undo each other (a refresh re-pressing a button just released, a tap
    /// restoring an old stick value).
    /// </summary>
    public static class GoldPathInputWriter
    {
        #region Public Methods

        /// <summary>Control values for <paramref name="target"/> held at <paramref name="value"/> or released.</summary>
        public static List<KeyValuePair<InputControl, Vector2>> Values(GoldPathInputTarget target, bool down, Vector2 value)
        {
            List<KeyValuePair<InputControl, Vector2>> values = new List<KeyValuePair<InputControl, Vector2>>();
            if (target.Vector != null)
            {
                values.Add(new KeyValuePair<InputControl, Vector2>(target.Vector, down ? value : Vector2.zero));
            }
            else if (target.IsComposite)
            {
                AddPart(values, target.Up, down && value.y > 0.5f);
                AddPart(values, target.Down, down && value.y < -0.5f);
                AddPart(values, target.Left, down && value.x < -0.5f);
                AddPart(values, target.Right, down && value.x > 0.5f);
            }
            else if (target.Button != null)
            {
                float pressValue = Mathf.Approximately(value.x, 0f) ? 1f : value.x;
                values.Add(new KeyValuePair<InputControl, Vector2>(target.Button, new Vector2(down ? pressValue : 0f, 0f)));
            }

            return values;
        }

        /// <summary>Touch begin/end (touch state is its own event type).</summary>
        public static void ApplyTouch(GoldPathInputTarget target, bool down, Vector2 value)
        {
            if (down)
            {
                target.TouchPosition = new Vector2(value.x * Screen.width, value.y * Screen.height);
            }

            UnityEngine.InputSystem.TouchPhase phase = down ? UnityEngine.InputSystem.TouchPhase.Began : UnityEngine.InputSystem.TouchPhase.Ended;
            InputSystem.QueueStateEvent(target.Touch, new TouchState { touchId = 1, phase = phase, position = target.TouchPosition });
        }

        /// <summary>Queues one state event per device with every owned control value written into it.</summary>
        public static void Queue(IReadOnlyDictionary<InputControl, Vector2> owned)
        {
            List<InputDevice> devices = new List<InputDevice>();
            foreach (InputControl control in owned.Keys)
            {
                if (control.device.added && !devices.Contains(control.device))
                {
                    devices.Add(control.device);
                }
            }

            foreach (InputDevice device in devices)
            {
                using (NativeArray<byte> buffer = StateEvent.From(device, out InputEventPtr eventPtr))
                {
                    foreach (KeyValuePair<InputControl, Vector2> pair in owned)
                    {
                        if (pair.Key.device != device)
                        {
                            continue;
                        }

                        if (pair.Key is InputControl<Vector2> vector)
                        {
                            vector.WriteValueIntoEvent(pair.Value, eventPtr);
                        }
                        else if (pair.Key is InputControl<float> axis)
                        {
                            axis.WriteValueIntoEvent(pair.Value.x, eventPtr);
                        }
                    }

                    InputSystem.QueueEvent(eventPtr);
                }
            }
        }

        #endregion

        #region Private Methods

        private static void AddPart(List<KeyValuePair<InputControl, Vector2>> values, InputControl<float> part, bool pressed)
        {
            if (part != null)
            {
                values.Add(new KeyValuePair<InputControl, Vector2>(part, new Vector2(pressed ? 1f : 0f, 0f)));
            }
        }

        #endregion
    }
}
