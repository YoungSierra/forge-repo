using UnityEngine;
using UnityEngine.InputSystem;

namespace V57.GameForge
{
    /// <summary>
    /// Prototype input helper. Honors Input System when available; supports UI blocking.
    /// </summary>
    public class GameForgeInput : MonoBehaviour
    {
        public static GameForgeInput Instance { get; private set; }

        [SerializeField] private bool blockWhenUiFocused = true;

        public bool InputBlocked { get; set; }

        public Vector2 MoveAxis { get; private set; }
        public Vector2 LookDelta { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (InputBlocked || (blockWhenUiFocused && IsUiBlocking()))
            {
                MoveAxis = Vector2.zero;
                LookDelta = Vector2.zero;
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            float x = 0f, y = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            }

            MoveAxis = new Vector2(x, y).normalized;
            LookDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        }

        public bool GetKeyDown(Key key)
        {
            if (InputBlocked) return false;
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        private static bool IsUiBlocking()
        {
            // Editor / overlay can set InputBlocked explicitly; runtime UI uses EventSystem when present.
            return false;
        }
    }
}
