using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.GoldPath
{
    /// <summary>
    /// Runs the gold path on the loaded scene through simulated Input System devices and writes
    /// <c>checks.json</c> + screenshots to <c>Docs/V57/evidence/goldpath/&lt;UTC&gt;/</c>.
    /// Editor and development builds only. Real devices are restored in <c>finally</c> and in OnDisable;
    /// the Editor recovers after a crash/domain reload through <see cref="GoldPathDeviceGuard.Recover"/>.
    /// </summary>
    public sealed class GoldPathDriver : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sources")]
        [Tooltip("Empty = Docs/Generated/json/acceptance.json (project-relative or absolute).")]
        [SerializeField] private string _acceptancePath = string.Empty;
        [Tooltip("Empty = Docs/V57/gold_path.json, used when acceptance.json has no gold_path.")]
        [SerializeField] private string _fallbackPath = string.Empty;
        [Tooltip("Empty = Docs/V57/evidence/goldpath. Env V57_GOLDPATH_EVIDENCE_DIR overrides the run folder.")]
        [SerializeField] private string _evidenceRoot = string.Empty;

        [Header("Behaviour")]
        [SerializeField] private bool _runOnStart;
        [SerializeField] private bool _disableRealDevices = true;
        [SerializeField] private bool _stopOnFirstFailure = true;

        #endregion

        #region Fields

        private GoldPathInputSimulator _input;
        private bool _running;

        #endregion

        #region Public Methods

        public bool LastPass { get; private set; }

        public string LastChecksPath { get; private set; }

        public bool IsRunning => _running;

        public void Configure(string acceptancePath, string fallbackPath, string evidenceRoot, bool disableRealDevices)
        {
            _acceptancePath = acceptancePath ?? string.Empty;
            _fallbackPath = fallbackPath ?? string.Empty;
            _evidenceRoot = evidenceRoot ?? string.Empty;
            _disableRealDevices = disableRealDevices;
        }

        public IEnumerator CoRun()
        {
            LastPass = false;
            List<string> parseErrors = new List<string>();
            GoldPathDocument document = GoldPathSource.Load(
                GoldPathPaths.ResolveInput(_acceptancePath, GoldPathPaths.AcceptanceRelative),
                GoldPathPaths.ResolveInput(_fallbackPath, GoldPathPaths.FallbackRelative),
                parseErrors);
            string evidenceDirectory = GoldPathPaths.CreateRunDirectory(_evidenceRoot);
            GoldPathChecks checks = new GoldPathChecks
            {
                started = UtcNow(),
                source = document.SourcePath,
                scene = SceneManager.GetActiveScene().path,
                evidence_dir = evidenceDirectory,
                parse_errors = parseErrors.ToArray()
            };
            List<GoldPathStepResult> results = new List<GoldPathStepResult>();
            GoldPathConsoleWatcher watcher = new GoldPathConsoleWatcher();
            GoldPathBuiltinProbe builtinProbe = new GoldPathBuiltinProbe();
            float startTime = Time.realtimeSinceStartup;
            int startFrame = Time.frameCount;
            _running = true;
            _input = new GoldPathInputSimulator();
            watcher.Start();
            GoldPathProbeRegistry.Register(builtinProbe);
            try
            {
                _input.Setup(_disableRealDevices);
                GoldPathStepExecutor executor = new GoldPathStepExecutor(_input, evidenceDirectory);
                for (int i = 0; i < document.Steps.Count && parseErrors.Count == 0; i++)
                {
                    GoldPathStep step = document.Steps[i];
                    GoldPathStepResult result = new GoldPathStepResult { index = i, id = step.Id, type = step.Type.ToString(), arg = step.Describe() };
                    results.Add(result);
                    yield return StartCoroutine(executor.CoExecute(step, result));
                    if (!result.pass)
                    {
                        Debug.LogWarning($"GoldPathDriver: step {step.Id} ({result.type}) failed: {result.detail}");
                        yield return StartCoroutine(executor.CoCapture(result, "fail_" + step.Id));
                        if (_stopOnFirstFailure)
                        {
                            break;
                        }
                    }
                }

                GoldPathStepResult final = new GoldPathStepResult { index = results.Count, id = "final", type = "Capture", arg = "final", pass = true };
                yield return StartCoroutine(executor.CoCapture(final, "final"));
                results.Add(final);
            }
            finally
            {
                _input.Teardown();
                GoldPathProbeRegistry.Unregister(builtinProbe);
                watcher.Stop();
                _running = false;
                float duration = Mathf.Max(0.0001f, Time.realtimeSinceStartup - startTime);
                checks.fps_avg = (Time.frameCount - startFrame) / duration;
                checks.console_errors = watcher.ErrorCount;
                checks.console_error_samples = watcher.GetSamples();
                WriteChecks(checks, results, document.HasSteps);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Debug.LogWarning("GoldPathDriver: disabled in non-development players.");
                enabled = false;
            }
        }

        private void Start()
        {
            if (_runOnStart && enabled)
            {
                StartCoroutine(CoRun());
            }
        }

        private void OnDisable()
        {
            if (_running && _input != null)
            {
                _input.Teardown();
                _running = false;
            }
        }

        #endregion

        #region Private Methods

        private void WriteChecks(GoldPathChecks checks, List<GoldPathStepResult> results, bool hasSteps)
        {
            bool allPass = hasSteps && checks.parse_errors.Length == 0 && checks.console_errors == 0;
            foreach (GoldPathStepResult result in results)
            {
                allPass &= result.pass;
            }

            checks.steps = results.ToArray();
            checks.finished = UtcNow();
            checks.pass = allPass;
            LastPass = allPass;
            LastChecksPath = Path.Combine(checks.evidence_dir, "checks.json");
            File.WriteAllText(LastChecksPath, JsonUtility.ToJson(checks, true));
            Debug.Log($"GoldPathDriver: pass={allPass} steps={results.Count} errors={checks.console_errors} → {LastChecksPath}");
        }

        private static string UtcNow()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
