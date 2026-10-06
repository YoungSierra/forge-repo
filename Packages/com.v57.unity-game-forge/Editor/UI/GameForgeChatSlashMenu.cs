using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Cursor-like slash skill menu. Type <c>/</c> in chat to pick a skill (e.g. debugmode).
    /// Arrow Up/Down moves the highlight; Enter confirms. List filters as you type (<c>/d</c> → debugmode).
    /// </summary>
    public sealed class GameForgeChatSlashMenu
    {
        public event Action<string> SkillChosen;

        private readonly VisualElement _root;
        private readonly List<(string id, string title, string desc, Func<string> detail)> _skills = new();
        private readonly List<(string id, Button btn)> _visible = new();
        private bool _open;
        private int _selectedIndex = -1;

        public VisualElement Root => _root;
        public bool IsOpen => _open;
        public int VisibleCount => _visible.Count;
        public bool HasSelection => _open && _selectedIndex >= 0 && _selectedIndex < _visible.Count;

        public GameForgeChatSlashMenu()
        {
            _root = new VisualElement();
            _root.AddToClassList("gf-slash-menu");
            _root.style.display = DisplayStyle.None;

            _skills.Add((
                "debugmode",
                "/debugmode",
                "Toggle SYSTEM / tool / session logs in Chat",
                () => GameForgeSession.ChatDebugMode ? "On — click to turn off" : "Off — click to turn on"
            ));
            _skills.Add((
                "prototype-full",
                "/prototype-full",
                "Full TDD graybox under Assets/Prototypes/<slug>/ — creates folders if missing",
                () => "Chat-only · Assets/Prototypes + project folder · gap-fill if exists"
            ));
            _skills.Add((
                "game-setup-run-all",
                "/game-setup-run-all",
                "Full /game-setup Production pipeline — chain stages (no continue pauses after INIT)",
                () => "Chat-only · Production roots · INIT confirm still required · hard STOP on FAIL"
            ));

            Rebuild("");
        }

        public void Show(string filter = "/")
        {
            var q = NormalizeFilter(filter);
            Rebuild(q);
            _open = true;
            _root.style.display = DisplayStyle.Flex;
            if (_visible.Count > 0)
                SetSelectedIndex(0);
            else
                _selectedIndex = -1;
        }

        public void Close()
        {
            _open = false;
            _selectedIndex = -1;
            _root.style.display = DisplayStyle.None;
        }

        public void UpdateFilter(string input)
        {
            if (string.IsNullOrEmpty(input) || input[0] != '/')
            {
                Close();
                return;
            }

            Show(input);
        }

        /// <summary>delta +1 = Down, -1 = Up. Returns true if handled.</summary>
        public bool MoveSelection(int delta)
        {
            if (!_open || _visible.Count == 0) return false;
            if (_selectedIndex < 0) _selectedIndex = 0;
            else _selectedIndex = (_selectedIndex + delta + _visible.Count) % _visible.Count;
            ApplySelectionStyles();
            return true;
        }

        /// <summary>Confirm highlighted skill. Returns true if a skill was chosen.</summary>
        public bool ConfirmSelection()
        {
            if (!HasSelection) return false;
            var id = _visible[_selectedIndex].id;
            Close();
            SkillChosen?.Invoke(id);
            return true;
        }

        private static string NormalizeFilter(string filter)
        {
            var q = (filter ?? "/").Trim();
            if (q.Contains("\n")) q = q.Split('\n')[0];
            q = q.TrimStart('/').ToLowerInvariant();
            // Drop trailing space after skill id while typing "/debug "
            var sp = q.IndexOf(' ');
            if (sp >= 0) q = q.Substring(0, sp);
            return q;
        }

        private static bool MatchesFilter(string id, string title, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            var bare = (title ?? "").TrimStart('/');
            return id.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
                   || bare.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
        }

        private void Rebuild(string filter)
        {
            _root.Clear();
            _visible.Clear();

            var head = new Label("Skills");
            head.AddToClassList("gf-slash-head");
            _root.Add(head);

            foreach (var s in _skills)
            {
                if (!MatchesFilter(s.id, s.title, filter))
                    continue;

                var id = s.id;
                var btn = new Button(() =>
                {
                    Close();
                    SkillChosen?.Invoke(id);
                });
                btn.AddToClassList("gf-slash-item");
                var title = new Label(s.title);
                title.AddToClassList("gf-slash-title");
                var desc = new Label(s.desc);
                desc.AddToClassList("gf-slash-desc");
                var detail = new Label(s.detail?.Invoke() ?? "");
                detail.AddToClassList("gf-slash-detail");
                btn.Add(title);
                btn.Add(desc);
                btn.Add(detail);
                _root.Add(btn);
                _visible.Add((id, btn));
            }

            if (_visible.Count == 0)
            {
                var empty = new Label("No matching skill");
                empty.AddToClassList("gf-slash-empty");
                _root.Add(empty);
                _selectedIndex = -1;
            }
            else if (_selectedIndex >= _visible.Count)
            {
                _selectedIndex = 0;
            }

            ApplySelectionStyles();
        }

        private void SetSelectedIndex(int index)
        {
            _selectedIndex = index;
            ApplySelectionStyles();
        }

        private void ApplySelectionStyles()
        {
            for (var i = 0; i < _visible.Count; i++)
                _visible[i].btn.EnableInClassList("gf-slash-item-selected", i == _selectedIndex);
        }
    }
}
