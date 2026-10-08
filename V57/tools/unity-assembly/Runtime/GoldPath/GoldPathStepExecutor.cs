using System.Collections;
using System.IO;
using UnityEngine;

namespace V57.GoldPath
{
    /// <summary>
    /// Executes one <see cref="GoldPathStep"/> and fills its <see cref="GoldPathStepResult"/>.
    /// Press/release always happen on different frames. All waits use unscaled wall-clock time.
    /// </summary>
    public sealed class GoldPathStepExecutor
    {
        #region Fields

        private readonly GoldPathInputSimulator _input;
        private readonly GoldPathScreenshot _screenshot = new GoldPathScreenshot();
        private readonly string _evidenceDirectory;

        #endregion

        #region Public Methods

        public GoldPathStepExecutor(GoldPathInputSimulator input, string evidenceDirectory)
        {
            _input = input;
            _evidenceDirectory = evidenceDirectory;
        }

        public IEnumerator CoExecute(GoldPathStep step, GoldPathStepResult result)
        {
            float start = Time.realtimeSinceStartup;
            switch (step.Type)
            {
                case GoldPathStepType.Press:
                case GoldPathStepType.Hold:
                case GoldPathStepType.Move:
                    yield return CoInput(step, result);
                    break;
                case GoldPathStepType.Release:
                    result.pass = _input.End(step.Target, out string releaseDetail);
                    result.detail = releaseDetail;
                    yield return null;
                    break;
                case GoldPathStepType.Wait:
                    yield return CoWait(step.Seconds, step.Frames);
                    result.pass = true;
                    result.detail = "waited";
                    break;
                case GoldPathStepType.WaitUntil:
                    yield return CoWaitUntil(step, result);
                    break;
                case GoldPathStepType.Expect:
                    result.pass = step.Condition.Evaluate(out string expectDetail);
                    result.detail = expectDetail;
                    break;
                case GoldPathStepType.Capture:
                    yield return CoCapture(result, step.Value);
                    result.pass = string.IsNullOrEmpty(_screenshot.LastError);
                    break;
            }

            if (step.CaptureAfter)
            {
                yield return CoCapture(result, step.Id);
            }

            result.elapsed = Time.realtimeSinceStartup - start;
        }

        public IEnumerator CoCapture(GoldPathStepResult result, string name)
        {
            string file = $"{result.index:00}_{GoldPathPaths.SanitizeFileName(name)}.png";
            string fullPath = Path.Combine(_evidenceDirectory, file);
            yield return _screenshot.CoCapture(fullPath);
            if (string.IsNullOrEmpty(_screenshot.LastError))
            {
                result.screenshot = file;
            }
            else
            {
                result.detail = $"{result.detail} | screenshot failed: {_screenshot.LastError}".Trim(' ', '|');
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator CoInput(GoldPathStep step, GoldPathStepResult result)
        {
            bool isVector = step.Type == GoldPathStepType.Move;
            if (!_input.TryBegin(step.Target, isVector, step.Value, out string detail))
            {
                result.pass = false;
                result.detail = detail;
                yield break;
            }

            result.detail = detail;
            yield return CoWait(step.Seconds, Mathf.Max(1, step.Frames));
            if ((step.Type == GoldPathStepType.Hold || step.Type == GoldPathStepType.Move) && step.Seconds <= 0f)
            {
                result.pass = true;
                result.detail = detail + " (held until release)";
                yield break;
            }

            result.pass = _input.End(step.Target, out string endDetail);
            if (!result.pass)
            {
                result.detail = endDetail;
            }

            yield return null;
        }

        private static IEnumerator CoWait(float seconds, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
        }

        private static IEnumerator CoWaitUntil(GoldPathStep step, GoldPathStepResult result)
        {
            float start = Time.realtimeSinceStartup;
            while (true)
            {
                bool pass = step.Condition.Evaluate(out string detail);
                result.detail = detail;
                if (pass)
                {
                    result.pass = true;
                    yield break;
                }

                if (Time.realtimeSinceStartup - start >= step.TimeoutSeconds)
                {
                    result.pass = false;
                    result.detail = $"timeout {step.TimeoutSeconds}s: {detail}";
                    yield break;
                }

                yield return null;
            }
        }

        #endregion
    }
}
