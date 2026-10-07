# Unity CLI lessons

## Compile race: poll, do not retry
- **Symptom:** commands failed or returned stale results right after a C# write; the agent retried the same command several times during the domain reload and saw noise errors.
- **Rule:** after any C# / asmdef / package write: `unity command recompile`, then poll `unity command recompile_status` (e.g. every 2 s, timeout 180 s) until `completed` and not compiling; only then run the next command. A failure during a reload is not a real failure — wait, then run once.
- **Source:** HH compile-heal loops.

## Stale Editor.log noise
- **Rule:** judge compile state by `recompile_status` / `EditorUtility.scriptCompilationFailed`, not by grepping old `Editor.log` lines. Old errors from deleted files are noise.
- **Source:** HH stage B / G-CAM reports.

## Serialized wiring must be saved
- **Symptom:** `eval` wiring did not persist an object reference; the field was null after reload.
- **Rule:** set references with `set_serialized_field` (or `SerializedObject` + `ApplyModifiedProperties`), mark dirty, `save_all`, then read back after reload.
- **Source:** HH input-setup report.

## Input simulation
- **Rule:** press and release on **separate frames** (`InputSystem.QueueStateEvent` press → `yield return null` (≥1 frame) → release). Same-frame press+release is often never seen by `WasPressedThisFrame` / `performed`.
- **Rule:** add test devices with `InputSystem.AddDevice<…>()` and in `finally` remove them and re-enable real devices; otherwise the Editor keeps a fake keyboard and real input stops working for the owner.
- **Rule:** after domain reload during a test, state lives in `SessionState` (GoldPathDriver handles this); do not hold device references across reloads.
- **Source:** HH input tests; brief §4 (GoldPathDriver).

## Screenshots
- **Rule:** capture in Play Mode at end of frame; camera render textures miss UI Toolkit overlays. Set the Game View resolution first.

## Build Settings
- **Rule:** set scene list explicitly at I2/M1 (gold path scene at index 0); do not rely on what another project left there.
- **Source:** HH (index 0 was a prototype scene).

## Multiple Editors
- **Rule:** pass `--project-path <repo>` on every `unity command` when more than one Editor may be open.

## Long Editor work: schedule, then poll the output
- **Symptom:** `unity command eval "V57.Assembly.AssemblyRunner.RunAll()"` returned `Main thread operation timed out after 5000ms`; while the Editor was still importing, the call never ran at all.
- **Rule:** wait for `recompile_status` idle, then schedule long work with `EditorApplication.delayCall += () => …; return 1;` and poll its output file (`Docs/V57/reports/assembly-report.json` is rewritten at the end). Never treat the 5 s timeout as a failure or a success.
- **Source:** PS end-to-end assembly test (2026-10).

## Shell paths in generated C# / JS
- **Symptom:** a capture script wrote to `C:UsersUsuario…` (a drive-relative folder at the drive root) because backslashes in a path were eaten by shell quoting; a failed `cd` in a chained shell command ran `rm` in the wrong repo.
- **Rule:** write scripts with paths through the file tool (not inline shell strings); in shell chains guard every `cd` with `|| exit 1` and never `|| true` a `cd`.
- **Source:** PS end-to-end test.
