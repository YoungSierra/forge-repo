using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>
    /// A resolved input target on the simulated devices: a button/axis, a Vector2 control,
    /// a 2D composite (up/down/left/right parts) or the touchscreen.
    /// </summary>
    public sealed class GoldPathInputTarget
    {
        public GoldPathInputTarget(string description)
        {
            Description = description;
        }

        public string Description { get; }

        public InputControl<float> Button { get; set; }

        public InputControl<Vector2> Vector { get; set; }

        public Touchscreen Touch { get; set; }

        public Vector2 TouchPosition { get; set; }

        public InputControl<float> Up { get; set; }

        public InputControl<float> Down { get; set; }

        public InputControl<float> Left { get; set; }

        public InputControl<float> Right { get; set; }

        public bool IsComposite => Up != null || Down != null || Left != null || Right != null;
    }
}
