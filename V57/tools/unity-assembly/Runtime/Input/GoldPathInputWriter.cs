using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace V57.GoldPath
{
    /// <summary>
    /// Queues state events on simulated devices (same technique as InputTestFixture.Set): capture the device's
    /// current state into an event, write the new control values, queue it. Several controls of one device are
    /// written into a single event so they do not overwrite each other within a frame.
    /// </summary>
    public static class GoldPathInputWriter
    {
        #region Public Methods

        public static void Apply(GoldPathInputTarget target, bool down, Vector2 value)
        {
            if (target.Touch != null)
            {
                if (down)
                {
                    target.TouchPosition = new Vector2(value.x * Screen.width, value.y * Screen.height);
                }

                UnityEngine.InputSystem.TouchPhase phase = down ? UnityEngine.InputSystem.TouchPhase.Began : UnityEngine.InputSystem.TouchPhase.Ended;
                InputSystem.QueueStateEvent(target.Touch, new TouchState { touchId = 1, phase = phase, position = target.TouchPosition });
                return;
            }

            if (target.Vector != null)
            {
                WriteVector(target.Vector, down ? value : Vector2.zero);
                return;
            }

            if (target.IsComposite)
            {
                List<InputControl<float>> controls = new List<InputControl<float>>();
                List<float> values = new List<float>();
                AddPart(controls, values, target.Up, down && value.y > 0.5f);
                AddPart(controls, values, target.Down, down && value.y < -0.5f);
                AddPart(controls, values, target.Left, down && value.x < -0.5f);
                AddPart(controls, values, target.Right, down && value.x > 0.5f);
                WriteFloats(controls, values);
                return;
            }

            if (target.Button != null)
            {
                float pressValue = Mathf.Approximately(value.x, 0f) ? 1f : value.x;
                WriteFloats(new List<InputControl<float>> { target.Button }, new List<float> { down ? pressValue : 0f });
            }
        }

        #endregion

        #region Private Methods

        private static void AddPart(List<InputControl<float>> controls, List<float> values, InputControl<float> part, bool pressed)
        {
            if (part != null)
            {
                controls.Add(part);
                values.Add(pressed ? 1f : 0f);
            }
        }

        private static void WriteVector(InputControl<Vector2> control, Vector2 value)
        {
            using (NativeArray<byte> buffer = StateEvent.From(control.device, out InputEventPtr eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        private static void WriteFloats(List<InputControl<float>> controls, List<float> values)
        {
            List<InputDevice> devices = new List<InputDevice>();
            foreach (InputControl<float> control in controls)
            {
                if (!devices.Contains(control.device))
                {
                    devices.Add(control.device);
                }
            }

            foreach (InputDevice device in devices)
            {
                using (NativeArray<byte> buffer = StateEvent.From(device, out InputEventPtr eventPtr))
                {
                    for (int i = 0; i < controls.Count; i++)
                    {
                        if (controls[i].device == device)
                        {
                            controls[i].WriteValueIntoEvent(values[i], eventPtr);
                        }
                    }

                    InputSystem.QueueEvent(eventPtr);
                }
            }
        }

        #endregion
    }
}
