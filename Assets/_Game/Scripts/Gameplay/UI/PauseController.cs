using ProfessorSprat.Core;
using ProfessorSprat.Gameplay.Flow;
using ProfessorSprat.Gameplay.Options;
using ProfessorSprat.Gameplay.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>
    /// TDD §B-S PauseController + §9.1 UI_PauseMenu: the Pause action toggles time scale 0, the gameplay input map and the
    /// menu (Resume / Restart / Settings / Accessibility / Controls / Abort, D-287). Restart reloads the scene: mid-zone
    /// progress is session-only (§11.4). Unpausing restores the player's game speed.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private InputHandler _input;
        [SerializeField] private string _menuScene = "SCN_MainMenu_Boot";
        [SerializeField] private SettingsView _settings;
        [SerializeField] private RunStats _stats;

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
            Time.timeScale = paused ? 0f : GameSettings.GameSpeed;
            if (_input != null)
            {
                _input.SetGameplayEnabled(!paused);
            }
            if (_panel != null)
            {
                _panel.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
                if (paused)
                {
                    Label sub = _panel.Q<Label>("pause-sub");
                    if (sub != null && _stats != null)
                    {
                        sub.text = $"{_stats.FliesCollected} / {_stats.FliesTotal} FLIES · {PresentationView.FormatTime(_stats.Elapsed)}";
                    }

                    _panel.Q<Button>("resume")?.Focus();
                }
                else if (_settings != null && _settings.IsOpen)
                {
                    _settings.Close();
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
                Bind("settings", () => OpenPage(SettingsPage.Settings));
                Bind("access", () => OpenPage(SettingsPage.Accessibility));
                Bind("controls", () => OpenPage(SettingsPage.Controls));
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
                if (_settings != null && _settings.IsOpen)
                {
                    _settings.Close();
                    return;
                }

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

        private void OpenPage(SettingsPage page)
        {
            if (_settings == null)
            {
                return;
            }

            _panel.style.display = DisplayStyle.None;
            _settings.Open(page, () =>
            {
                if (IsPaused)
                {
                    _panel.style.display = DisplayStyle.Flex;
                    _panel.Q<Button>("resume")?.Focus();
                }
            });
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
