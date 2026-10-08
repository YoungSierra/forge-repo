using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// TDD §B-S PauseController + §9.1 UI_PauseMenu: the Pause action toggles time scale 0, the gameplay input map and the
    /// menu (Resume / Restart zone / Quit to menu). Restart reloads the scene: mid-zone progress is session-only (§11.4).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private InputHandler _input;
        [SerializeField] private string _menuScene = "SCN_MainMenu_Boot";

        private InputAction _pause;
        private VisualElement _panel;

        #endregion

        #region Public Methods

        public bool IsPaused { get; private set; }

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            _input?.SetGameplayEnabled(!paused);
            if (_panel != null)
            {
                _panel.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
                if (paused)
                {
                    _panel.Q<Button>("resume")?.Focus();
                }
            }

            EventBus.Publish(new PauseChangedEvent(paused));
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _pause = _actions != null ? _actions.FindAction("Gameplay/Pause", true) : null;
        }

        private void OnEnable()
        {
            _panel = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("pause-menu");
            if (_panel != null)
            {
                _panel.style.display = DisplayStyle.None;
                Bind("resume", () => SetPaused(false));
                Bind("restart", Restart);
                Bind("quit", Quit);
            }

            _pause?.Enable();
        }

        private void OnDestroy()
        {
            if (IsPaused)
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (_pause != null && _pause.WasPressedThisFrame())
            {
                SetPaused(!IsPaused);
            }
        }

        #endregion

        #region Private Methods

        private void Bind(string buttonName, System.Action action)
        {
            Button button = _panel.Q<Button>(buttonName);
            if (button != null)
            {
                button.clicked += action;
            }
        }

        private void Restart()
        {
            SetPaused(false);
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void Quit()
        {
            SetPaused(false);
            if (Application.CanStreamedLevelBeLoaded(_menuScene))
            {
                SceneManager.LoadScene(_menuScene);
            }
        }

        #endregion
    }
}
