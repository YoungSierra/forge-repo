using System;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>Agent / Ask popover for the chat compose row.</summary>
    public sealed class GameForgeChatModePicker
    {
        public event Action<string> SelectionChanged;

        private readonly VisualElement _root;
        private readonly Button _trigger;
        private readonly Label _triggerText;
        private readonly VisualElement _popover;
        private bool _open;
        private string _mode = "agent";

        public VisualElement Root => _root;
        public string Mode => _mode;

        public GameForgeChatModePicker()
        {
            _root = new VisualElement();
            _root.AddToClassList("mp");
            _root.AddToClassList("mp-mode");

            _trigger = new Button(Toggle) { text = "" };
            _trigger.AddToClassList("mp-trigger");
            _trigger.AddToClassList("mp-trigger-compact");
            _trigger.tooltip = "Mode (Shift+Tab)";
            _triggerText = new Label("Agent");
            _triggerText.AddToClassList("mp-trigger-text");
            var chev = new Label("▾");
            chev.AddToClassList("mp-chev");
            _trigger.Add(_triggerText);
            _trigger.Add(chev);

            _popover = new VisualElement();
            _popover.AddToClassList("mp-mode-menu");
            _popover.style.display = DisplayStyle.None;
            _popover.Add(MakeOption("agent", "Agent", "Edit files"));
            _popover.Add(MakeOption("ask", "Ask", "Read-only"));

            _root.Add(_popover);
            _root.Add(_trigger);
            SetMode(GameForgeSession.ChatMode, notify: false);
        }

        public void SetMode(string mode, bool notify = true)
        {
            _mode = Normalize(mode);
            _triggerText.text = LabelFor(_mode);
            if (notify) SelectionChanged?.Invoke(_mode);
        }

        public void Close()
        {
            _open = false;
            _popover.style.display = DisplayStyle.None;
        }

        private Button MakeOption(string mode, string title, string desc)
        {
            var btn = new Button(() =>
            {
                SetMode(mode);
                Close();
            });
            btn.AddToClassList("mp-mode-option");
            btn.userData = mode;
            var t = new Label(title);
            t.AddToClassList("mp-option-title");
            var d = new Label(desc);
            d.AddToClassList("mp-option-desc");
            btn.Add(t);
            btn.Add(d);
            return btn;
        }

        private void Toggle()
        {
            if (!_trigger.enabledSelf) return;
            _open = !_open;
            _popover.style.display = _open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_open) return;
            foreach (var child in _popover.Query<Button>(className: "mp-mode-option").ToList())
            {
                var m = child.userData as string;
                child.EnableInClassList("is-active", m == _mode);
            }
        }

        public void SetInteractable(bool enabled)
        {
            _trigger.SetEnabled(enabled);
            if (!enabled) Close();
            _root.EnableInClassList("is-locked", !enabled);
            _trigger.tooltip = enabled ? "Mode (Shift+Tab)" : "Mode locked while a run is active";
        }

        private static string Normalize(string mode) =>
            mode is "ask" or "agent" ? mode : "agent";

        private static string LabelFor(string mode) => mode == "ask" ? "Ask" : "Agent";
    }
}
