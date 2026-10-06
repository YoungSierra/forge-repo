# Unity Gold Path Skill (`/gold-path`) — gate M1, regression after every spec

The **gold path** is one scripted playthrough of the core loop, driven through **real input** on the **real scene**, that proves the game is playable. It is authored once at M1 and re-run after every spec (F), at M2, M3 and M4. From M1 on, a red gold path means the game is broken.

| Item | Detail |
|------|--------|
| **Driver** | `GoldPathDriver` (runtime asmdef `V57.GoldPath` in `com.v57.assembly`) — reads gold path steps, simulates Input System devices, captures screenshots at checkpoints |
| **Game hook** | `IGoldPathProbe` implemented in game code, registered with `GoldPathProbeRegistry` |
| **Steps source** | `Docs/V57/gold_path.json` (V57-authored at M1; `GoldPathSource` prefers it). Seed: intake's `acceptance.json → gold_path`, which is always `status: "draft"` (or `null`) — a draft is a starting point, not a contract |
| **Run** | `unity test . --mode PlayMode --filter V57.GoldPath --output Docs/V57/evidence/goldpath/<UTC>/results.xml` (package tests enabled by `"testables": ["com.v57.assembly"]`) |
| **Gate** | `Docs/V57/evidence/goldpath/<UTC>/checks.json` — every check `pass: true`, 0 console errors/exceptions |
| **Knowledge** | `V57/knowledge/verification.md`, `V57/knowledge/unity-cli.md` |

---

## Invocation

```bash
/gold-path            # author (if needed) + implement probe + run  (M1)
/gold-path --run      # run only (after each spec, M2, M3, M4)
/gold-path --author   # (re)author steps only — M1, or when a spec adds a core-loop step
```

---

## 1. Authoring the steps (M1)

Seed priority: `acceptance.json → gold_path` (draft) → TDD §3 core loop + `acceptance.json → criteria` (PlayMode, P0) → §11.3 input table (`input_map.json`) → §13.2 slice scene (`scenes.json`).

1. List the §3 core-loop steps in order (e.g. launch → hit target → score changes → lose ball → end screen).
2. For each step, write one **`do`** (input) and at least one **`expect`** (observable condition with timeout).
3. Action names must exist in `input_map.json` (`<Map>/<Action>`). Conditions must be names the probe answers.
4. Mark checkpoints with `screenshot: true` (at least: start, mid-loop, end state).
5. Include the **end of loop** (win, lose or exit) — boot-only gold paths are invalid.
6. **Always** write `Docs/V57/gold_path.json` at M1, seeded from the intake draft (or from §3 when the draft is `null`). Steps from the draft may be adapted (reordered, split, re-keyed to real probe conditions, timeouts tuned) because the draft is not a contract; the §3 core loop and its end state must remain covered. Log `D-###` ("gold path authored from draft/§3 — changes: …"), commit. Never edit `Docs/Generated/**`.
7. Once M1 passes, the authored file is the contract for the rest of the run: later changes only add steps (new core-loop coverage) or fix probe keys, each with a `D-###`.

Step shape (the package DTO is authoritative; this is the contract V57 authors against):

```json
{ "scene": "Assets/_Game/Scenes/SCN_<Level>.unity",
  "steps": [
    { "id": "S01", "expect": "session.state", "op": "==", "value": "Playing", "timeout_s": 5, "screenshot": true },
    { "id": "S02", "do": "hold", "action": "Launcher/ChargePlunger", "seconds": 0.8 },
    { "id": "S03", "do": "release", "action": "Launcher/ChargePlunger" },
    { "id": "S04", "expect": "ball.in_play", "op": "==", "value": "true", "timeout_s": 3 },
    { "id": "S05", "expect": "score.value", "op": ">", "value": "0", "timeout_s": 20, "screenshot": true },
    { "id": "S06", "do": "capture", "name": "end_state" }
  ] }
```

`value` is always a JSON **string** (parsed against the probe's bool/float/string answer). `do` verbs: `press` / `tap` (press, then release on a later frame), `hold` (press for `seconds`), `release`, `move` (vector action for `seconds`), `wait` (`seconds` or `frames`), `capture` / `screenshot` (named checkpoint image). `expect` with `timeout_s > 0` polls until true or timeout; without a timeout it checks once. Exact parser: `V57/tools/unity-assembly/Runtime/GoldPath/GoldPathStepParser.cs`. The run folder can be forced with env `V57_GOLDPATH_EVIDENCE_DIR`.
Timeouts are wall-clock in Play Mode at `timeScale = 1`.

---

## 2. Implementing the probe (game code)

| Rule | Detail |
|---|---|
| Location | `Assets/_Game/Scripts/GoldPath/<Slug>GoldPathProbe.cs`, own asmdef referencing the game areas + `V57.GoldPath` |
| Shape | MonoBehaviour on `_Systems/GoldPathProbe` in the slice scene; **serialized references** to the systems it reads (bound in Edit Mode); registers in `OnEnable`, unregisters in `OnDisable` via `GoldPathProbeRegistry` |
| Answers | `TryGetBool` / `TryGetFloat` / `TryGetString(key, out value)`; return `false` for unknown keys (the check fails as unknown), never a default `true` |
| Read-only | Probes **observe** public state/events. They never set state, call gameplay methods, or move objects |
| Observable first | Prefer what the player sees (UI value shown, object active & visible, position inside a zone) over private flags |
| No lookups | No `GameObject.Find*`, `FindObjectsByType`, reflection; the lint applies to probe code too |

Exact interface signatures: `V57/tools/unity-assembly/Runtime/GoldPath/` (`IGoldPathProbe`, `GoldPathProbeRegistry`).

---

## 3. Running

1. `/compile-heal` green; `/editor-smooth --before-play` (save scenes).
2. Run the PlayMode test (table above). The driver: loads the scene, waits for the probe, executes steps, presses/releases on separate frames, restores real devices in `finally`, writes `checks.json` + screenshots (end-of-frame Game View, includes UI Toolkit overlay).
3. Read `checks.json` and **open every checkpoint screenshot**. A check that passes while the screenshot shows a broken state (black, pink, missing HUD, wrong camera) is a probe bug → fix the probe and re-run.
4. Record in `STATUS.json → goldpath { last_run_utc, pass, checks_path }` and one DEVLOG line.

Gate: all checks `pass: true`, 0 errors/exceptions in the run log. Anything else = FAIL.

---

## 4. Forbidden verification tricks (lint + review enforce; any hit = gate FAIL)

| Trick | Why it is banned |
|---|---|
| `Physics.Simulate` / `Physics2D.Simulate` in gold path or PlayMode acceptance | Fakes the physics step; hides tunneling and timing bugs |
| `detectCollisions = false`, disabling colliders, layer changes to avoid contacts | Removes the gameplay being tested |
| Manual position/velocity clamps, teleports, `transform.position =` to reach an `expect` | Fakes the outcome |
| Calling gameplay methods (`Flipper.Activate()`, `Score.Add()`) instead of input actions | Skips the input path the player uses |
| `Time.timeScale` changes, frame skipping, fixed `deltaTime` hacks | Changes the game being certified |
| Probes that mutate state, return constant `true`, or read debug-only flags when an observable exists | Self-grading |
| Cheats / debug shortcuts (`skip to win`, god mode) unless the TDD step itself is that | Not the player's path |
| After M1: weakening `Docs/V57/gold_path.json` expectations/thresholds, or editing `acceptance.json` or the TDD, to make a run pass | Moves the goalposts (TDD edit = hard stop) |
| `[Ignore]`, `Assert.Pass`, try/catch around checks, retry-until-green loops | Hides failure |
| Scene-view or camera-only captures, reused evidence folders | Screenshots must be fresh Game View captures of this run |

---

## 5. When it fails

1. Read the first failing check, its step id and nearest screenshot.
2. Fix **gameplay or content** (never the check). Re-run.
3. Red > 20 min after a green commit → revert rule (`unity-vertical-slice-skill.md` §5).
4. Probe bug (screenshot contradicts check) → fix probe, log `D-###`, re-run.
5. A step that the TDD genuinely cannot satisfy in the slice: steps that came from a **draft** (intake `status: "draft"`) or from V57 authoring may be marked `cut` in `Docs/V57/gold_path.json` with a `D-###`. Only a **non-draft** gold path (provider-authored, `status` other than `draft`) is protected: its steps are never cut — report them as open defects at M4. The end-of-loop step is never cut.

---

## Report

`Docs/V57/reports/M1-goldpath-<slug>.md` (M1) and one line per re-run in `Docs/V57/DEVLOG.md`:

```markdown
# Gold path — <slug>
- **Run:** Docs/V57/evidence/goldpath/<UTC>/checks.json
- **Result:** all pass | N failing (ids)
- **Steps:** N (Docs/V57/gold_path.json, seeded from: acceptance.json draft | §3)
- **Screenshots:** list
- **Status word:** agent-verified | implemented
```

---

*Last updated: 2026-09-28*
