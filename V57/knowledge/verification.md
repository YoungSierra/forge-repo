# Verification lessons

## Self-grading
- **Symptom:** every craft and cert report said PASS while screenshots showed broken visuals (placeholder spheres, disabled renderers, wrong framing).
- **Cause:** the implementing agent graded its own work, with an incentive to finish.
- **Rule:** objective gates come from tools (tests, gold path `checks.json`, lint, assembly report); visual gates come from `/independent-review` (fresh context, only screenshots + numbers + references). The implementer never writes a visual verdict. Status words: `implemented` → `agent-verified` → `pending owner review`.
- **Enforced by:** `agents.yaml → gates.self_grading_allowed: false`; playability-cert and QA read evidence files. **Source:** HH, WoO.

## Faked physics in tests
- **Symptom:** PlayMode ACs green using `Physics.Simulate`, `detectCollisions = false`, hand clamps on ball position, and direct method calls; the real game did not behave that way.
- **Rule:** acceptance tests and the gold path run real `FixedUpdate` physics and real Input System input. Banned: `Physics.Simulate`, collision disabling, clamps/teleports, direct gameplay calls instead of input, `timeScale` hacks, `[Ignore]`.
- **Enforced by:** `runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests` (acceptance rules on `Tests/PlayMode/Acceptance*`); `/gold-path` forbidden list. **Source:** HH implement reports.

## Screenshots that miss the UI
- **Symptom:** HUD "verified" while capture showed none; camera-only captures skip UI Toolkit overlays.
- **Rule:** Play Mode, end-of-frame Game View capture (`WaitForEndOfFrame` → `ScreenCapture.CaptureScreenshotAsTexture()`), at the reference resolution, fresh folder per run.
- **Enforced by:** OVR helper screenshot rules; reviewer rejects packs without UI. **Source:** HH playability report (`capture_game_view source=screen`).

## Numbers and images together
- **Rule:** every screenshot has numbers next to it (camera params, read-back values, FPS, luminance); every number claim that concerns something visible has a screenshot. A number that contradicts the image is a defect in the evidence.
- **Source:** WoO, HH.

## Editing the spec to pass
- **Symptom:** TDD §5/§13.1 edited (0.1.0 → 0.1.1) to turn a failing completeness item into PASS.
- **Rule:** the TDD is provider-owned; editing it is a hard stop. Pending rows → `MISSING_ASSETS.md` / cut + `D-###`.
- **Enforced by:** `autonomy.hard_stop_actions`. **Source:** HH stage B report.
