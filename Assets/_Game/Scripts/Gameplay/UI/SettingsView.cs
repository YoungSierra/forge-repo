using System;
using ProfessorSprat.Gameplay.Options;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProfessorSprat.Gameplay.UI
{
    /// <summary>Which page the settings panel shows.</summary>
    public enum SettingsPage
    {
        Settings,
        Accessibility,
        Controls
    }

    /// <summary>
    /// Settings, accessibility and controls pages (owner request, D-287), shared by the main menu and pause. Rows are
    /// built from <see cref="GameSettings"/>; every change is saved at once. Back (or Cancel) closes the panel.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsView : MonoBehaviour
    {
        #region Fields

        private VisualElement _root;
        private Label _title;
        private Label _sub;
        private ScrollView _body;
        private Action _onClose;

        #endregion

        #region Public Methods

        public bool IsOpen => _root != null && _root.style.display == DisplayStyle.Flex;

        public void Open(SettingsPage page, Action onClose)
        {
            Bind();
            _onClose = onClose;
            _body.Clear();
            switch (page)
            {
                case SettingsPage.Settings:
                    SetTitle("SETTINGS", "AUDIO · CAMERA · GRAPHICS");
                    AddSlider("Music", "", 0f, 1f, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v, Percent);
                    AddSlider("Sound effects", "", 0f, 1f, GameSettings.SfxVolume, v => GameSettings.SfxVolume = v, Percent);
                    AddSlider("Camera sensitivity", "", 0.3f, 2.5f, GameSettings.LookSensitivity, v => GameSettings.LookSensitivity = v, v => $"{v:0.0}×");
                    AddToggle("Invert camera Y", "", GameSettings.InvertY, v => GameSettings.InvertY = v);
                    AddToggle("High quality graphics", "Off: no realtime shadows", GameSettings.HighQuality, v => GameSettings.HighQuality = v);
                    AddToggle("Show FPS meter", "", GameSettings.ShowFps, v => GameSettings.ShowFps = v);
                    break;
                case SettingsPage.Accessibility:
                    SetTitle("ACCESSIBILITY", "COMFORT · ASSISTS");
                    AddToggle("Training on next start", "Step-by-step tutorial on the entrance deck", GameSettings.Tutorial, v => GameSettings.Tutorial = v);
                    AddToggle("Camera shake", "", GameSettings.CameraShake, v => GameSettings.CameraShake = v);
                    AddToggle("Screen flashes", "Off: flashes at a quarter of their strength", GameSettings.ScreenFlashes, v => GameSettings.ScreenFlashes = v);
                    AddToggle("Fly beacons", "Light pillars over flies still to catch", GameSettings.FlyBeacons, v => GameSettings.FlyBeacons = v);
                    AddSlider("Game speed", "Slows the whole game down", 0.5f, 1f, GameSettings.GameSpeed, v => GameSettings.GameSpeed = v, Percent);
                    break;
                default:
                    SetTitle("CONTROLS", "KEYBOARD · GAMEPAD");
                    AddKeys("Move", "W A S D / Arrows · Left stick");
                    AddKeys("Look", "Mouse · Right stick");
                    AddKeys("Zoom", "Mouse wheel · D-pad up / down");
                    AddKeys("Jump", "Space · A");
                    AddKeys("Stomp (in the air)", "Space / Left Ctrl · A / B");
                    AddKeys("Pause", "Esc · Start");
                    AddKeys("Skip training", "Tab · Select");
                    break;
            }

            _root.style.display = DisplayStyle.Flex;
            _root.Q<Button>("settings-back")?.Focus();
        }

        public void Close()
        {
            if (_root == null || !IsOpen)
            {
                return;
            }

            _root.style.display = DisplayStyle.None;
            Action onClose = _onClose;
            _onClose = null;
            onClose?.Invoke();
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Bind();
            _root.style.display = DisplayStyle.None;
        }

        #endregion

        #region Private Methods

        private static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

        private void Bind()
        {
            if (_root != null)
            {
                return;
            }

            VisualElement doc = GetComponent<UIDocument>().rootVisualElement;
            _root = doc.Q<VisualElement>("settings-root");
            _title = doc.Q<Label>("settings-title");
            _sub = doc.Q<Label>("settings-sub");
            _body = doc.Q<ScrollView>("settings-body");
            Button back = doc.Q<Button>("settings-back");
            if (back != null)
            {
                back.clicked += Close;
            }

            _root.RegisterCallback<NavigationCancelEvent>(_ => Close());
        }

        private void SetTitle(string title, string sub)
        {
            _title.text = title;
            _sub.text = sub;
        }

        private VisualElement Row(string label, string hint)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("settings-row");
            VisualElement text = new VisualElement();
            Label name = new Label(label);
            name.AddToClassList("settings-label");
            text.Add(name);
            if (!string.IsNullOrEmpty(hint))
            {
                Label small = new Label(hint);
                small.AddToClassList("settings-hint");
                text.Add(small);
            }

            row.Add(text);
            _body.Add(row);
            return row;
        }

        private void AddSlider(string label, string hint, float min, float max, float value, Action<float> set, Func<float, string> format)
        {
            VisualElement row = Row(label, hint);
            VisualElement right = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            Slider slider = new Slider(min, max) { value = value };
            Label shown = new Label(format(value));
            shown.AddToClassList("settings-value");
            slider.RegisterValueChangedCallback(e =>
            {
                set(e.newValue);
                shown.text = format(e.newValue);
            });
            right.Add(slider);
            right.Add(shown);
            row.Add(right);
        }

        private void AddToggle(string label, string hint, bool value, Action<bool> set)
        {
            VisualElement row = Row(label, hint);
            Toggle toggle = new Toggle { value = value };
            toggle.RegisterValueChangedCallback(e => set(e.newValue));
            row.Add(toggle);
        }

        private void AddKeys(string action, string binding)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("keys-row");
            Label name = new Label(action);
            name.AddToClassList("keys-action");
            Label keys = new Label(binding);
            keys.AddToClassList("keys-binding");
            row.Add(name);
            row.Add(keys);
            _body.Add(row);
        }

        #endregion
    }
}
