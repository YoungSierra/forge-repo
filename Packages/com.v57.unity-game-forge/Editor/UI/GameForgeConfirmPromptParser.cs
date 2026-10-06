using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace V57.GameForge.Editor
{
    public enum ConfirmPromptKind
    {
        Unknown,
        InitDefaults,
        StageContinue,
        Stopped
    }

    public sealed class ConfirmVariableRow
    {
        public string Name;
        public string ProposedValue;
        public string Source;
        public bool Editable;
    }

    public sealed class ConfirmPrompt
    {
        public ConfirmPromptKind Kind = ConfirmPromptKind.Unknown;
        public string Title;
        public string Subtitle;
        public string StageId;
        public string Reason;
        public string NeededFromUser;
        public string BodySummary;
        public readonly List<ConfirmVariableRow> Variables = new();
    }

    /// <summary>
    /// Detects game-setup / prototype confirmation blocks emitted by V57 skills.
    /// </summary>
    public static class GameForgeConfirmPromptParser
    {
        private static readonly string[] InitCoreVars = { "tdd", "prefix", "scene" };

        private static readonly Regex InitHeader = new(
            @"##\s+(?:Game setup|Prototype)\s+[—\-]\s+INIT defaults\s+\(confirm required\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex StageCompleteHeader = new(
            @"##\s+(?:Game setup|Prototype)\s+—\s+Stage\s+([^\r\n]+?)\s+complete",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex StoppedHeader = new(
            @"##\s+(?:Game setup|Prototype)\s+—\s+STOPPED at Stage\s+([^\r\n]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static ConfirmPrompt TryParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var normalized = text.Replace("\r\n", "\n");

            if (InitHeader.IsMatch(normalized))
                return ParseInit(normalized);

            var stage = StageCompleteHeader.Match(normalized);
            if (stage.Success)
                return ParseStageContinue(normalized, stage.Groups[1].Value.Trim());

            var stopped = StoppedHeader.Match(normalized);
            if (stopped.Success)
                return ParseStopped(normalized, stopped.Groups[1].Value.Trim());

            // Fallback: waiting line without full header (partial stream)
            if (normalized.IndexOf("INIT defaults", StringComparison.OrdinalIgnoreCase) >= 0 &&
                normalized.IndexOf("confirm required", StringComparison.OrdinalIgnoreCase) >= 0 &&
                normalized.IndexOf("| Variable |", StringComparison.OrdinalIgnoreCase) >= 0)
                return ParseInit(normalized);

            if (normalized.IndexOf("Reply `continue`", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Reply to run Stage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var m = StageCompleteHeader.Match(normalized);
                return ParseStageContinue(normalized, m.Success ? m.Groups[1].Value.Trim() : "");
            }

            return null;
        }

        /// <summary>INIT sheet should wait for core vars — avoids opening on a single streamed table row.</summary>
        public static bool IsInitTableComplete(string text)
        {
            var prompt = TryParse(text);
            return prompt?.Kind == ConfirmPromptKind.InitDefaults && IsInitTableComplete(prompt);
        }

        public static bool IsInitTableComplete(ConfirmPrompt prompt)
        {
            if (prompt?.Kind != ConfirmPromptKind.InitDefaults || prompt.Variables.Count == 0)
                return false;
            if (prompt.Variables.Count >= 4) return true;
            var found = 0;
            foreach (var core in InitCoreVars)
            {
                foreach (var row in prompt.Variables)
                {
                    if (string.Equals(row.Name, core, StringComparison.OrdinalIgnoreCase))
                    {
                        found++;
                        break;
                    }
                }
            }
            return found >= 2;
        }

        private static ConfirmPrompt ParseInit(string text)
        {
            var prompt = new ConfirmPrompt
            {
                Kind = ConfirmPromptKind.InitDefaults,
                Title = "Game setup — confirm defaults",
                Subtitle = "Accept proposed values or override editable fields."
            };

            var lines = text.Split('\n');
            var inTable = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("| Variable |", StringComparison.OrdinalIgnoreCase))
                {
                    inTable = true;
                    continue;
                }

                if (!inTable || !line.StartsWith("|", StringComparison.Ordinal)) continue;
                if (line.IndexOf("---", StringComparison.Ordinal) >= 0) continue;

                var cells = SplitTableRow(line);
                if (cells.Count < 4) continue;

                var name = NormalizeVarName(StripTicks(cells[0]));
                if (string.Equals(name, "Variable", StringComparison.OrdinalIgnoreCase)) continue;

                prompt.Variables.Add(new ConfirmVariableRow
                {
                    Name = name,
                    ProposedValue = StripTicks(cells[1]),
                    Source = StripTicks(cells[2]),
                    Editable = IsYes(cells[3])
                });
            }

            prompt.Variables.Sort((a, b) =>
            {
                var ai = Array.IndexOf(InitCoreVars, a.Name);
                var bi = Array.IndexOf(InitCoreVars, b.Name);
                if (ai < 0) ai = 100;
                if (bi < 0) bi = 100;
                var cmp = ai.CompareTo(bi);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            if (prompt.Variables.Count == 0) return null;
            return prompt;
        }

        private static ConfirmPrompt ParseStageContinue(string text, string stageId)
        {
            var prompt = new ConfirmPrompt
            {
                Kind = ConfirmPromptKind.StageContinue,
                StageId = stageId,
                Title = string.IsNullOrEmpty(stageId)
                    ? "Stage complete — continue?"
                    : $"Stage {stageId} complete",
                Subtitle = "Run the next pipeline stage when ready.",
                BodySummary = ExtractBulletSummary(text, 6)
            };
            return prompt;
        }

        private static ConfirmPrompt ParseStopped(string text, string stageId)
        {
            var prompt = new ConfirmPrompt
            {
                Kind = ConfirmPromptKind.Stopped,
                StageId = stageId,
                Title = string.IsNullOrEmpty(stageId)
                    ? "Pipeline stopped"
                    : $"Stopped at stage {stageId}",
                Subtitle = "Provide the requested input to resume.",
                Reason = ExtractField(text, "Reason"),
                NeededFromUser = ExtractField(text, "Needed from user"),
                BodySummary = ExtractBulletSummary(text, 4)
            };

            // STOP at INIT may embed the defaults table
            if (text.IndexOf("| Variable |", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var init = ParseInit(text);
                if (init?.Variables.Count > 0)
                {
                    prompt.Kind = ConfirmPromptKind.InitDefaults;
                    prompt.Title = init.Title;
                    prompt.Subtitle = init.Subtitle;
                    prompt.Variables.Clear();
                    prompt.Variables.AddRange(init.Variables);
                }
            }

            return prompt;
        }

        private static List<string> SplitTableRow(string line)
        {
            var parts = line.Split('|');
            var cells = new List<string>();
            for (var i = 0; i < parts.Length; i++)
            {
                if (i == 0 && parts[i].Length == 0) continue;
                if (i == parts.Length - 1 && parts[i].Length == 0) continue;
                cells.Add(parts[i].Trim());
            }
            return cells;
        }

        private static string StripTicks(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '`' && s[s.Length - 1] == '`')
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private static bool IsYes(string s) =>
            string.Equals(StripTicks(s), "yes", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeVarName(string raw)
        {
            var name = StripTicks(raw).Trim();
            if (string.IsNullOrEmpty(name)) return name;

            foreach (var known in new[] { "tdd", "prefix", "scene", "skip_review", "pack_integrated" })
            {
                if (name.Equals(known, StringComparison.OrdinalIgnoreCase)) return known;
                if (name.StartsWith(known + " ", StringComparison.OrdinalIgnoreCase)) return known;
                if (name.StartsWith(known + " /", StringComparison.OrdinalIgnoreCase)) return known;
            }

            return name;
        }

        private static string ExtractField(string text, string label)
        {
            var pattern = $@"-\s*\*\*{Regex.Escape(label)}:\*\*\s*(.+)$";
            var m = Regex.Match(text, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        private static string ExtractBulletSummary(string text, int maxLines)
        {
            var lines = text.Split('\n');
            var picked = new List<string>();
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (!line.StartsWith("- **", StringComparison.Ordinal)) continue;
                if (line.IndexOf("Reply `", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                picked.Add(line.TrimStart('-', ' '));
                if (picked.Count >= maxLines) break;
            }
            return picked.Count == 0 ? "" : string.Join("\n", picked);
        }

        public static string BuildInitReply(IReadOnlyList<ConfirmVariableRow> rows, Func<string, string> getValue)
        {
            var overrides = new List<string>();
            foreach (var row in rows)
            {
                if (!row.Editable) continue;
                var current = getValue(row.Name) ?? row.ProposedValue ?? "";
                var proposed = row.ProposedValue ?? "";
                if (!string.Equals(current.Trim(), proposed.Trim(), StringComparison.Ordinal))
                    overrides.Add($"{row.Name}={current.Trim()}");
            }

            return overrides.Count == 0 ? "confirm" : string.Join(" ", overrides);
        }
    }
}
