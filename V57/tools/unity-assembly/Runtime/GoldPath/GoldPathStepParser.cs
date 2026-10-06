using System;
using System.Collections.Generic;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Parses gold path JSON (see README "Gold path step contract") into <see cref="GoldPathStep"/>s.
    /// A step with both <c>do</c> and <c>expect</c> becomes two steps (input, then check).
    /// <c>expect</c> with <c>timeout_s &gt; 0</c> polls (WaitUntil); without a timeout it checks once (Expect).
    /// </summary>
    public static class GoldPathStepParser
    {
        #region Fields

        public const float DefaultTimeoutSeconds = 10f;

        private static readonly string[] QuotedKeys = { "value" };

        #endregion

        #region Public Methods

        public static GoldPathDocument ParseAcceptanceJson(string json, string sourcePath, List<string> errors)
        {
            GoldPathAcceptanceDto dto = FromJson<GoldPathAcceptanceDto>(json, sourcePath, errors);
            GoldPathDto goldPath = dto?.gold_path;
            return new GoldPathDocument(sourcePath, goldPath?.scene, ParseSteps(goldPath?.steps, errors), goldPath?.status);
        }

        public static GoldPathDocument ParseGoldPathJson(string json, string sourcePath, List<string> errors)
        {
            GoldPathDto dto = FromJson<GoldPathDto>(json, sourcePath, errors);
            return new GoldPathDocument(sourcePath, dto?.scene, ParseSteps(dto?.steps, errors), dto?.status);
        }

        public static List<GoldPathStep> ParseSteps(GoldPathStepDto[] dtos, List<string> errors)
        {
            List<GoldPathStep> steps = new List<GoldPathStep>();
            if (dtos == null)
            {
                return steps;
            }

            for (int i = 0; i < dtos.Length; i++)
            {
                if (dtos[i] == null)
                {
                    errors.Add($"step {i + 1}: null entry");
                    continue;
                }

                ParseOne(dtos[i], i, steps, errors);
            }

            return steps;
        }

        #endregion

        #region Private Methods

        private static T FromJson<T>(string json, string sourcePath, List<string> errors) where T : class
        {
            try
            {
                return JsonUtility.FromJson<T>(JsonPreprocessor.Process(json, QuotedKeys));
            }
            catch (ArgumentException exception)
            {
                errors.Add($"{sourcePath}: invalid JSON ({exception.Message})");
                return null;
            }
        }

        private static void ParseOne(GoldPathStepDto dto, int index, List<GoldPathStep> steps, List<string> errors)
        {
            string id = string.IsNullOrWhiteSpace(dto.id) ? $"S{index + 1:00}" : dto.id.Trim();
            string verb = Normalize(dto.@do);
            string conditionText = !string.IsNullOrWhiteSpace(dto.expect) ? dto.expect : dto.condition;
            bool isCheckVerb = verb == "expect" || verb == "waituntil";
            bool hasInput = verb.Length > 0 && !isCheckVerb;
            bool hasCheck = isCheckVerb || !string.IsNullOrWhiteSpace(dto.expect);
            if (!hasInput && !hasCheck)
            {
                errors.Add($"step {id}: needs 'do' or 'expect'");
                return;
            }

            if (hasInput && !TryAddInput(dto, id, verb, hasCheck, steps, errors))
            {
                return;
            }

            if (hasCheck)
            {
                GoldPathCondition condition = string.IsNullOrWhiteSpace(dto.op)
                    ? GoldPathCondition.Parse(conditionText)
                    : new GoldPathCondition(conditionText, dto.op, dto.value);
                if (!condition.IsValid)
                {
                    errors.Add($"step {id}: invalid condition '{conditionText} {dto.op} {dto.value}'");
                    return;
                }

                bool poll = dto.timeout_s > 0f || verb == "waituntil";
                float timeout = dto.timeout_s > 0f ? dto.timeout_s : (poll ? DefaultTimeoutSeconds : 0f);
                string checkId = hasInput ? id + ".expect" : id;
                GoldPathStepType type = poll ? GoldPathStepType.WaitUntil : GoldPathStepType.Expect;
                steps.Add(new GoldPathStep(type, checkId, string.Empty, dto.value, 0f, 0, condition, timeout, dto.screenshot));
            }
        }

        private static bool TryAddInput(GoldPathStepDto dto, string id, string verb, bool hasCheck, List<GoldPathStep> steps, List<string> errors)
        {
            string target = !string.IsNullOrWhiteSpace(dto.action) ? dto.action.Trim() : (dto.control ?? string.Empty).Trim();
            bool capture = dto.screenshot && !hasCheck;
            GoldPathStepType type;
            switch (verb)
            {
                case "press":
                case "tap":
                    type = GoldPathStepType.Press;
                    break;
                case "hold":
                    type = GoldPathStepType.Hold;
                    break;
                case "release":
                    type = GoldPathStepType.Release;
                    break;
                case "move":
                    type = GoldPathStepType.Move;
                    break;
                case "wait":
                    steps.Add(new GoldPathStep(GoldPathStepType.Wait, id, string.Empty, dto.value, dto.seconds, dto.frames, null, 0f, capture));
                    return true;
                case "capture":
                case "screenshot":
                    string shotName = !string.IsNullOrWhiteSpace(dto.name) ? dto.name : id;
                    steps.Add(new GoldPathStep(GoldPathStepType.Capture, id, string.Empty, shotName, 0f, 0, null, 0f, false));
                    return true;
                default:
                    errors.Add($"step {id}: unknown verb '{dto.@do}'");
                    return false;
            }

            if (target.Length == 0)
            {
                errors.Add($"step {id}: '{verb}' needs 'action' (Map/Action) or 'control' (<Device>/control)");
                return false;
            }

            if (type == GoldPathStepType.Move && dto.seconds <= 0f)
            {
                errors.Add($"step {id}: 'move' needs seconds > 0");
                return false;
            }

            steps.Add(new GoldPathStep(type, id, target, dto.value, dto.seconds, dto.frames, null, 0f, capture));
            return true;
        }

        private static string Normalize(string verb)
        {
            return string.IsNullOrWhiteSpace(verb)
                ? string.Empty
                : verb.Trim().ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty);
        }

        #endregion
    }
}
