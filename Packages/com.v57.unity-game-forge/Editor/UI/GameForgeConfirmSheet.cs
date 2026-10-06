using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace V57.GameForge.Editor
{
    /// <summary>
    /// Interactive confirmation sheet for game-setup INIT / continue / STOP prompts.
    /// </summary>
    public sealed class GameForgeConfirmSheet
    {
        public event Action<string> ReplySubmitted;

        private readonly VisualElement _sheet;
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly Label _summary;
        private readonly VisualElement _fields;
        private readonly ScrollView _fieldScroll;
        private readonly TextField _freeInput;
        private readonly VisualElement _actions;
        private readonly Dictionary<string, TextField> _inputs = new(StringComparer.OrdinalIgnoreCase);

        private ConfirmPrompt _prompt;
        private bool _open;

        public VisualElement Root => _sheet;
        public bool IsOpen => _open;

        public GameForgeConfirmSheet()
        {
            _sheet = new VisualElement();
            _sheet.AddToClassList("gf-confirm-sheet");
            _sheet.style.display = DisplayStyle.None;

            var head = new VisualElement();
            head.AddToClassList("gf-confirm-head");
            _title = LabelCls("gf-confirm-title", "");
            _subtitle = LabelCls("gf-confirm-subtitle", "");
            var close = new Button(Close) { text = "✕" };
            close.AddToClassList("gf-confirm-close");
            close.tooltip = "Dismiss (reply manually in chat)";
            head.Add(_title);
            head.Add(close);
            _sheet.Add(head);
            _sheet.Add(_subtitle);

            _summary = LabelCls("gf-confirm-summary", "");
            _summary.style.display = DisplayStyle.None;
            _sheet.Add(_summary);

            _fieldScroll = new ScrollView(ScrollViewMode.Vertical);
            _fieldScroll.AddToClassList("gf-confirm-scroll");
            _fieldScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _fieldScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _fields = _fieldScroll.contentContainer;
            _fields.AddToClassList("gf-confirm-fields");
            _sheet.Add(_fieldScroll);

            _freeInput = new TextField { label = "Your reply" };
            _freeInput.AddToClassList("gf-confirm-free");
            _freeInput.style.display = DisplayStyle.None;
            _freeInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                if (evt.shiftKey) return;
                evt.StopPropagation();
                SubmitFreeInput();
            });
            _sheet.Add(_freeInput);

            _actions = new VisualElement();
            _actions.AddToClassList("gf-confirm-actions");
            _sheet.Add(_actions);
        }

        public void Show(ConfirmPrompt prompt)
        {
            if (prompt == null) return;
            var preserved = CaptureInputValues();
            _prompt = prompt;
            _open = true;
            _sheet.style.display = DisplayStyle.Flex;
            Rebuild();
            RestoreInputValues(preserved);
        }

        public ConfirmPromptKind PromptKind => _prompt?.Kind ?? ConfirmPromptKind.Unknown;

        public int VariableCount => _prompt?.Variables?.Count ?? 0;

        public void Close()
        {
            _open = false;
            _prompt = null;
            _sheet.style.display = DisplayStyle.None;
            _inputs.Clear();
            _fields.Clear();
            _actions.Clear();
            _freeInput.style.display = DisplayStyle.None;
            _summary.style.display = DisplayStyle.None;
        }

        private void Rebuild()
        {
            _inputs.Clear();
            _fields.Clear();
            _actions.Clear();

            _title.text = _prompt.Title ?? "Confirm";
            _subtitle.text = _prompt.Subtitle ?? "";

            if (!string.IsNullOrEmpty(_prompt.BodySummary))
            {
                _summary.text = _prompt.BodySummary;
                _summary.style.display = DisplayStyle.Flex;
            }
            else
            {
                _summary.style.display = DisplayStyle.None;
            }

            switch (_prompt.Kind)
            {
                case ConfirmPromptKind.InitDefaults:
                    BuildInitFields();
                    AddAction("Yes", primary: true, () => SubmitInit());
                    AddAction("No", primary: false, Close);
                    break;
                case ConfirmPromptKind.StageContinue:
                    BuildContinueHint();
                    AddAction("Yes — Continue", primary: true, () => Submit("continue"));
                    AddAction("Not yet", primary: false, Close);
                    break;
                case ConfirmPromptKind.Stopped:
                    BuildStoppedFields();
                    AddAction("Send reply", primary: true, SubmitFreeInput);
                    AddAction("Dismiss", primary: false, Close);
                    break;
            }
        }

        private void BuildInitFields()
        {
            _fieldScroll.style.display = DisplayStyle.Flex;
            _freeInput.style.display = DisplayStyle.None;

            if (!string.IsNullOrEmpty(_prompt.Reason))
            {
                _fields.Add(LabelCls("gf-confirm-note", _prompt.Reason));
            }

            foreach (var row in _prompt.Variables)
            {
                var rowEl = new VisualElement();
                rowEl.AddToClassList("gf-confirm-row");

                var meta = new VisualElement();
                meta.AddToClassList("gf-confirm-row-meta");
                meta.Add(LabelCls("gf-confirm-var-name", row.Name));
                if (!string.IsNullOrEmpty(row.Source))
                    meta.Add(LabelCls("gf-confirm-var-source", row.Source));
                rowEl.Add(meta);

                if (row.Editable)
                {
                    var input = new TextField { value = row.ProposedValue ?? "" };
                    input.AddToClassList("gf-confirm-var-input");
                    input.tooltip = $"Override {row.Name}";
                    _inputs[row.Name] = input;
                    rowEl.Add(input);
                }
                else
                {
                    var readOnly = LabelCls("gf-confirm-var-readonly", row.ProposedValue ?? "—");
                    readOnly.tooltip = "Read-only";
                    rowEl.Add(readOnly);
                }

                _fields.Add(rowEl);
            }
        }

        private void BuildContinueHint()
        {
            _fieldScroll.style.display = DisplayStyle.None;
            _freeInput.style.display = DisplayStyle.None;
            if (string.IsNullOrEmpty(_prompt.BodySummary))
            {
                _summary.text = "The agent finished this stage and is waiting for you to continue.";
                _summary.style.display = DisplayStyle.Flex;
            }
        }

        private void BuildStoppedFields()
        {
            _fieldScroll.style.display = DisplayStyle.None;

            if (!string.IsNullOrEmpty(_prompt.Reason))
            {
                _summary.text = string.IsNullOrEmpty(_prompt.NeededFromUser)
                    ? _prompt.Reason
                    : $"{_prompt.Reason}\n\nNeeded: {_prompt.NeededFromUser}";
                _summary.style.display = DisplayStyle.Flex;
            }

            _freeInput.style.display = DisplayStyle.Flex;
            _freeInput.label = string.IsNullOrEmpty(_prompt.NeededFromUser)
                ? "Your reply"
                : _prompt.NeededFromUser;
            _freeInput.value = SuggestStoppedDefault();
        }

        private string SuggestStoppedDefault()
        {
            var needed = _prompt.NeededFromUser ?? "";
            if (needed.IndexOf("continue", StringComparison.OrdinalIgnoreCase) >= 0)
                return "continue";
            if (needed.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0)
                return "confirm";
            return "";
        }

        private void AddAction(string label, bool primary, Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("gf-btn");
            btn.AddToClassList(primary ? "gf-btn-primary" : "gf-btn-ghost");
            btn.AddToClassList("gf-confirm-btn");
            _actions.Add(btn);
        }

        private void SubmitInit()
        {
            GameForgeProjectIdentity.SyncPlayerSettingsFromSlug(GameForgeSession.SelectedSlug);
            var reply = GameForgeConfirmPromptParser.BuildInitReply(
                _prompt.Variables,
                name => _inputs.TryGetValue(name, out var tf) ? tf.value : null);
            Submit(reply);
        }

        private void SubmitFreeInput()
        {
            var msg = (_freeInput.value ?? "").Trim();
            if (string.IsNullOrEmpty(msg)) return;
            Submit(msg);
        }

        private void Submit(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Close();
            ReplySubmitted?.Invoke(message.Trim());
        }

        private Dictionary<string, string> CaptureInputValues()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _inputs)
                d[kv.Key] = kv.Value.value ?? "";
            return d;
        }

        private void RestoreInputValues(Dictionary<string, string> preserved)
        {
            if (preserved == null || preserved.Count == 0) return;
            foreach (var kv in preserved)
            {
                if (_inputs.TryGetValue(kv.Key, out var tf))
                    tf.value = kv.Value;
            }
        }

        private static Label LabelCls(string cls, string text)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            return l;
        }
    }
}
