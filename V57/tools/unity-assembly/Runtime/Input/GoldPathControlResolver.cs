using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GoldPath
{
    /// <summary>
    /// Resolves a step target onto the simulated devices. Targets are either an InputAction
    /// (<c>Map/Action</c>, searched in enabled actions, then project-wide <c>InputSystem.actions</c>) whose
    /// bindings are matched against the simulated devices, or a raw control path.
    /// </summary>
    public sealed class GoldPathControlResolver
    {
        #region Fields

        private readonly GoldPathDevices _devices;

        #endregion

        #region Public Methods

        public GoldPathControlResolver(GoldPathDevices devices)
        {
            _devices = devices;
        }

        public bool TryResolve(string target, bool wantVector, out GoldPathInputTarget resolved, out string detail)
        {
            InputAction action = FindAction(target);
            if (action != null)
            {
                return TryResolveAction(action, wantVector, out resolved, out detail);
            }

            string path = GoldPathControlPath.Normalize(target);
            resolved = ResolvePath(path, wantVector, target);
            detail = resolved != null ? resolved.Description : $"no action '{target}' and no simulated control matches '{path}'";
            return resolved != null;
        }

        #endregion

        #region Private Methods

        private static InputAction FindAction(string target)
        {
            if (string.IsNullOrEmpty(target) || target.StartsWith("<", StringComparison.Ordinal))
            {
                return null;
            }

            foreach (InputAction action in InputSystem.ListEnabledActions())
            {
                string fullName = action.actionMap != null ? $"{action.actionMap.name}/{action.name}" : action.name;
                if (string.Equals(fullName, target, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(action.name, target, StringComparison.OrdinalIgnoreCase))
                {
                    return action;
                }
            }

            InputActionAsset projectWide = InputSystem.actions; // Input System 1.8+ project-wide actions.
            return projectWide != null ? projectWide.FindAction(target, false) : null;
        }

        private bool TryResolveAction(InputAction action, bool wantVector, out GoldPathInputTarget resolved, out string detail)
        {
            string label = $"action {action.actionMap?.name}/{action.name}";
            List<string> tried = new List<string>();
            string touchPath = null;
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath))
                {
                    continue;
                }

                tried.Add(binding.effectivePath);
                if (GoldPathControlPath.IsTouch(binding.effectivePath))
                {
                    touchPath = binding.effectivePath;
                    continue;
                }

                resolved = ResolvePath(binding.effectivePath, wantVector, label);
                if (resolved != null)
                {
                    detail = resolved.Description;
                    return true;
                }
            }

            resolved = wantVector ? ResolveComposite(action, label, tried) : null;
            if (resolved == null && touchPath != null)
            {
                resolved = ResolvePath(touchPath, wantVector, label);
            }

            detail = resolved != null ? resolved.Description : $"{label}: no binding maps to a simulated device (tried {string.Join(", ", tried)})";
            return resolved != null;
        }

        private GoldPathInputTarget ResolveComposite(InputAction action, string label, List<string> tried)
        {
            GoldPathInputTarget target = new GoldPathInputTarget($"{label} via 2D composite");
            foreach (InputBinding binding in action.bindings)
            {
                if (!binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath))
                {
                    continue;
                }

                tried.Add(binding.effectivePath);
                InputControl<float> part = FindOnDevices(binding.effectivePath) as InputControl<float>;
                switch ((binding.name ?? string.Empty).ToLowerInvariant())
                {
                    case "up": target.Up = target.Up ?? part; break;
                    case "down": target.Down = target.Down ?? part; break;
                    case "left": target.Left = target.Left ?? part; break;
                    case "right": target.Right = target.Right ?? part; break;
                }
            }

            return target.IsComposite ? target : null;
        }

        private GoldPathInputTarget ResolvePath(string path, bool wantVector, string label)
        {
            if (GoldPathControlPath.IsTouch(path))
            {
                return _devices.Touchscreen != null ? new GoldPathInputTarget($"{label} → touch") { Touch = _devices.Touchscreen } : null;
            }

            InputControl control = FindOnDevices(path);
            if (wantVector && control is InputControl<Vector2> vector)
            {
                return new GoldPathInputTarget($"{label} → {control.path}") { Vector = vector };
            }

            if (!wantVector && control is InputControl<float> button)
            {
                return new GoldPathInputTarget($"{label} → {control.path}") { Button = button };
            }

            return null;
        }

        private InputControl FindOnDevices(string path)
        {
            foreach (InputDevice device in _devices.All)
            {
                InputControl control = InputControlPath.TryFindControl(device, path);
                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }

        #endregion
    }
}
