# Unity QA Skill (`/qa`)

> **Always enabled at M4** of `/vertical-slice` and `/game-setup` (`agents.yaml → qa.stage: M4`).
> `/prototype` and `/prototype-full` do not invoke `/qa` (they use `/playability-cert --mode prototype`).
> Standalone use is allowed when the owner asks for it.

Full **product QA** for the assembled game: engineering suites **plus** game-QA playtest (GQ-01..12). Certifies the player experience on the slice scenes, not only green unit tests. Every row needs an evidence file; the implementing agent never marks a subjective row PASS on its own.

| Item | Detail |
|------|--------|
| **requires** | Unity CLI Pipeline (`unity command`, `unity test`); `unity mcp` optional |
| **Task subagent** | Required from pipelines |
| **Depends on** | `/compile-heal`, `/editor-smooth`, `/gold-path`, `/independent-review --final`, `/playability-cert` |
| **Evidence root** | `Docs/V57/evidence/M4/<UTC>/qa/` |

---

## Invocation

```bash
/qa --scene Assets/_Game/Scenes/SCN_<Level>.unity --mode game-setup
/qa --feature @V57/specs/<slug>/features/foo.yaml
/qa --playtest-only
/qa --regression
/qa --soak-only
```

---

## Why two tracks

Q0–Q3 alone are **dev testing**. Past runs proved green EditMode ≠ playable game. Track B (GQ) validates like a **game QA tester**, through real input on the real scene.

```text
Q0-Q3 Engineering → Q4 Playability cert → Q5 GQ playtest → Q6 Soak → Q8 Regression
```

---

## Track A — Engineering

| Layer | What | PASS rule |
|-------|------|-----------|
| **Q0** | CLI preflight; `/compile-heal`; `/editor-smooth`; Build[0] = gold path scene | Any fail → fix, re-run |
| **Q1** | AC matrix from all in-scope specs (`V57/specs/<slug>/**`) + `acceptance.json` criteria | Every row has evidence |
| **Q2** | Full EditMode suite via **`unity test`** | All green; XML under evidence root |
| **Q3** | Full PlayMode suite; ≥1 test loads the gold path scene; runtime lint clean for `Tests/PlayMode/Acceptance*` | Green; synthetic-only coverage or banned tricks → FAIL |

---

## Track B — Game QA playtest

Driven through Input System devices in Play Mode (same rules as the gold path: press/release on separate frames, real physics, devices restored). Screenshots are end-of-frame Game View captures (UI Toolkit included). Build the playtest script from TDD §3 + `acceptance.json`, not from "files exist".

| ID | Question | How | FAIL if |
|----|----------|-----|---------|
| **GQ-01 Boot** | Starts into intended experience? | gold path S01 + screenshot | Black/pink, wrong scene, missing player |
| **GQ-02 Controls** | Primary actions work? | each `input_map.json` action in scope → observable change | No observable response |
| **GQ-03 Core loop** | One full loop? | gold path checks.json | Stuck mid-loop |
| **GQ-04 Feedback** | See/hear responses? | HUD/VFX/audio/anim cue after trigger (read-back + screenshot) | Event fires but no player feedback |
| **GQ-05 Fail state** | Lose/fail without softlock? | reach fail; assert UI + restart/exit | Softlock / frozen input |
| **GQ-06 Win / exit** | Win/complete/exit reachable? | reach win/exit; assert end state | Unreachable |
| **GQ-07 Softlock hunt** | Permanently stuck? | bounds, empty gates, input after cinematic, pause | Any permanent stuck |
| **GQ-08 UI flow** | Menus/HUD usable? | open/close pause/results/dialogue | UI traps input |
| **GQ-09 Cinematic** | Skip/end restores control? | when cinematics in scope | Input stays locked |
| **GQ-10 Save/load** | Round-trip if save in scope? | save → mutate → load | Data loss / crash |
| **GQ-11 Soak** | Stable while playing? | ≥60 s scripted play; 0 errors/exceptions | Exception spam |
| **GQ-12 Regression** | Fixes break other loops? | re-run GQ-01..11 + suites after fix | Prior pass flipped |

GQ-05/06/09/10 = `n/a` (with reason) only when the TDD excludes that loop from scope.

Visual rows (GQ-01, GQ-04, GQ-08) take their visual judgement from `review.json` of `/independent-review --final`; the agent supplies screenshots and read-back numbers only.

---

## Evidence rules (strict)

- Every GQ row and AC id → artifact path (checks.json, test XML line, read-back log, screenshot + review.json reference)
- "Felt OK" / "looks correct" without an artifact = FAIL
- Stale evidence (older than the tested commit) = FAIL
- Banned in any QA harness: `Physics.Simulate`, `detectCollisions = false`, clamps/teleports, direct gameplay calls instead of input, `Time.timeScale` hacks
- No WARN state: a row passes or fails

---

## Q7 Craft contracts

For each craft that ran (M2/M3 reports), re-check its verify items on the final commit. Missing → FAIL.

## Q8 Regression

After any fix from a QA FAIL: re-run Q2–Q6, GQ-01..11 and the gold path.

---

## Report

`Docs/V57/reports/M4-qa-<slug>.md`:

```markdown
# QA report — <slug>
- **Commit:** <sha>
- **Engineering (Q0–Q3):** …
- **Playability cert:** link
- **Game QA playtest (GQ-01..12):** table + evidence paths
- **AC matrix:** id → pass/fail → evidence
- **Soak / Regression:** …
- **Status word:** agent-verified | implemented → pending owner review
```

---

## Integration

- `/vertical-slice` and `/game-setup` **M4** — always runs; failing rows are fixed or reported as open defects in the final report
- `/prototype`, `/prototype-full` — not used
- Standalone — when the owner asks

---

*Last updated: 2026-09-28*
