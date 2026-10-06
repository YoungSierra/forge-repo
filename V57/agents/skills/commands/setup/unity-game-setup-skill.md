# Unity Game Setup Skill (`/game-setup`) — Production

Production pipeline for a provider-delivered game. It runs the **same stage driver as `/vertical-slice`** (I0 → M4, autonomous, decide + log) with **Production scope**: all §B mechanics, all §13.2 scenes, and the Production extras (performance audit, code review, release build, context readiness).

| Item | Detail |
|------|--------|
| **Stage driver** | `commands/setup/unity-vertical-slice-skill.md` — stage table, autonomy, timeboxes, revert rule, stop conditions, status words. This skill only adds Production scope and routing |
| **Intake / assembly** | `commands/setup/unity-intake-skill.md` (I0–I2) |
| **Gold path** | `commands/setup/unity-gold-path-skill.md` (M1, re-run after every spec) |
| **Visual verdict** | `commands/review/unity-independent-review-skill.md` (M3, M4) |
| **Reference** | `shared/references/unity-game-setup-reference.md` — TDD section map, craft details, final checklist |
| **CLI policy** | `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md` (CLI primary; `unity mcp` optional) |
| **Scene standards** | `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md` |
| **Lessons** | `V57/knowledge/*.md` |

---

## Invocation

```bash
/game-setup                   # start or resume; autonomous I0 → M4 (+ Production extras)
/game-setup --from F          # resume at a stage (earlier gates recorded in STATUS.json)
/game-setup --staged          # opt-in: stop after each stage (owner debugging only)
/game-setup --status          # STATUS.json summary + next action; no writes
/game-setup --scope slice     # same as /vertical-slice
```

`/game-setup-run-all` is kept as an alias of `/game-setup` (autonomous is now the default).

---

## Variables (resolved, never asked)

| Variable | Source | Fallback |
|---|---|---|
| `slug` | `Docs/Generated/json/package.json → slug` | repo folder name, logged `D-###` |
| `tdd` | `Docs/Design/TDD.md` | — (missing = blocking intake) |
| `prefix` (test assembly prefix) | TDD §A `test_assembly_prefix` | `slug` |
| `scenes` | `scenes.json` (all for Production; `slice: true` for VS) | — |
| `write roots` | brief §2 (`Assets/_Game/...`, `Docs/V57/...`, `V57/specs/<slug>/`) | — |

There is **no INIT confirmation**. Resolved values are written to the top of `Docs/V57/PLAN.md` and `STATUS.json`; the owner can change them by editing PLAN.md between runs.

---

## Operating rules

1. **English only** in reports, logs, decisions.
2. **Autonomous chaining (default for Production too — the runner chains `/game-setup` exactly like `/vertical-slice`).** No `continue` between stages; `--staged` is the only exception. On a stop condition: `STATUS.json` `status: "blocked"`, `hard_stop: "<reason>"`, print `HARD-STOP: <reason>` (driver §7). Stop only on the driver's stop conditions (blocking intake, destructive action, `.v57/ALLOW_STOP`).
3. **Decide + log.** Ambiguity → choose (TDD > ADD > V57 defaults), append `D-###` to `Docs/V57/DECISIONS.md`, continue.
4. **Hard stop: editing the TDD or provider art to pass a gate.** `Docs/Design/**`, `Docs/ArtDirection/**`, `Assets/_Game/Art/**`, `Assets/_Game/Audio/**` are read-only: never renamed, moved, deleted or edited; `fixable` issues are absorbed by import rules + `asset_manifest` mapping (logged `D-###`). A failing §0.2 completeness item or `[PENDING]` row is reported and worked around (listed in `MISSING_ASSETS.md` / cut + `D-###`, never a placeholder asset), never "fixed" in the TDD.
5. **Assets, camera and scale are decided before implementation.** They come from intake (`asset_manifest.json`, `camera.json`, `package.json → world`) at I0–I2. Gameplay stages do not re-frame the camera, rescale art, or re-choose assets; a change needs a `D-###` with the reason and a gold path re-run.
6. **No runtime scene building.** The saved scene is the game. A runtime bootstrap may only hold `[SerializeField]` references and call `Init` in a fixed order. Banned in runtime code (lint-enforced at M2): `GameObject.Find*`, `FindObjectOfType/FindObjectsByType/FindFirstObjectByType/FindAnyObjectByType`, `Resources.Load` for config, reflection (`GetField/GetProperty(...).SetValue`, `BindingFlags.NonPublic`), `ScriptableObject.CreateInstance` as config fallback, `AssetDatabase` / `#if UNITY_EDITOR` asset access, `new GameObject` / `AddComponent` / `GameObject.CreatePrimitive` to build level structure, hardcoded world coordinates for layout, renderer enable/disable on provider art, `Camera.main`. Files > 200 lines fail. See `V57/knowledge/runtime-bootstrap.md`.
7. **Real-input PlayMode tests.** Acceptance tests drive Input System devices (press/release on separate frames, devices restored in `finally`) with real physics. Banned: `Physics.Simulate`, `detectCollisions = false`, manual clamps/teleports, calling gameplay methods instead of input.
8. **Compile-heal after every C# write.** `/compile-heal` polls `recompile_status` until complete (no blind retries during domain reload); max 2 heal cycles per write before the revert rule.
9. **OVR read-back for every editor write** (`shared/helpers/unity-editor-verification-skill.md`): Operate → Verify → Report; "command succeeded" is not verification.
10. **Evidence, not self-grading.** Numbers from tools, images reviewed by `/independent-review`. Status words: `implemented` → `agent-verified` → `pending owner review`. Never "accepted".
11. **Delegate crafts.** Craft skills run as Task subagents that read the target skill end-to-end; this skill routes, it does not duplicate them. Unused crafts are skipped silently (no report, no progress line).
12. **Timeboxes** (driver §4): 45 min stuck → fallback/cut + log; tool failure → 2 retries then alternate route; 3 attempts per asset per stage; red > 20 min after green → revert.
13. **State on disk.** Re-read `Docs/V57/{STATUS.json, PLAN.md, TODO.md}` + last DEVLOG entries at session start/resume. Game context lives in `Docs/V57/CONTEXT.md` (never swap a root `CONTEXT.md`); specs in `V57/specs/<slug>/`.

---

## Stage map (Production)

| Stage | Production scope | Skills | Gate |
|---|---|---|---|
| I0 | full intake | `/intake` | A0 exit code |
| I1 | baseline | `/intake`, `/packages` | script check |
| I2 | all assets, all §13.2 scenes | `/intake` (`AssemblyRunner.RunAll`) | assembly-report.json |
| M1 | core loop on the primary gameplay scene | `/tdd-to-context`, `/tdd-to-spec --all`, `/gold-path` | checks.json |
| F | **all** §B mechanics in §D order | `/spec`, `/compile-heal`, `/gold-path --run` | tests + gold path per spec |
| M2 | all scenes, all §9 screens | routed crafts (below) + lint + hierarchy diff + collision-with-mesh check | M2 gates |
| M3 | all views/screens | routed crafts + `/independent-review` | reviewer ≥ threshold |
| M4 | all P0 + P1 ACs | `/playability-cert`, `/qa`, `/perf-audit`, `/code-reviewer` (`/build-setup` only on explicit owner request) | evidence-based; red perf budget = FAIL |
| CR (optional) | context readiness | `/context-readiness`, `/context-postmortem` | informational |

---

## Craft routing (When → stage)

Run a craft only when its **When** is true for the TDD/ADD/specs; otherwise skip with zero output.

| Craft | Command | When | Stage |
|---|---|---|---|
| Input | `/input-setup` | always when `input_map.json` has actions | I1 baseline, M1 actions, M2 polish |
| Render | `/render-setup` | quality tiers / SRP settings beyond I1 baseline | M2 |
| Level | `/level-design` | triggers, hazards, checkpoints beyond blockout markers | M2 |
| Physics | `/physics-setup` | collision matrix, rigidbodies, triggers in TDD/specs | M1 (minimum), M2 |
| UI | `/game-ui` | any `ui.json` screen in scope (mandatory) | M2 |
| Audio | `/audio-setup` | `audio.json` events | M2 |
| Animation | `/animation-setup` | characters with `animations[]` | M2 |
| Save | `/save-meta` | `save_model` ≠ N/A | M2 |
| Nav/AI | `/nav-ai` | agents with pathing | M2 |
| Localization | `/localization` | >1 language or text keys in `localization` | M2 |
| Network | `/network-setup` | multiplayer ≠ N/A | M2 |
| Camera | `/camera-setup` | apply/verify `camera.json` values only (no re-decision) | M1 apply, M3 polish |
| Lighting | `/lighting-setup` | ADD `visual_targets` / mood | M3 |
| Post | `/urp-postprocessing` | ADD grade/vignette/bloom or lighting mood | M3 |
| Shaders | `/shader-setup` | pink materials or TDD/ADD-named effects | M2 (pink), M3 (look) |
| VFX | `/vfx-setup` | `Assets/_Game/Art/VFX/` or spec `vfx:` | M3 |
| Game feel | `/game-feel` | §11.5 smoothed camera channels, §9 HUD motion | M3 |
| Cinematics | `/cinematics` | Timeline / cutscenes in scope | M3 |
| Accessibility | `/accessibility` | TDD a11y requirements | M4 |

Each craft: Operate via CLI/Pipeline → OVR verify → `/compile-heal` if C# changed → gold path re-run → report `Docs/V57/reports/<stage>-<craft>-<slug>.md`.

---

## M2 gates (Production detail)

| Gate | Command / check |
|---|---|
| Runtime lint | `node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests` → exit 0 (JSON findings saved to `Docs/V57/evidence/M2/<UTC>/lint.json`) |
| Hierarchy diff | `unity command eval "V57.Assembly.AssemblyRunner.CaptureHierarchyDiff()"` → `Docs/V57/reports/hierarchy-diff.json` with `pass: true` (only prefab instances under `_Gameplay/Spawned` or declared spawn roots differ between Edit and Play) |
| Prefabs | every run-scope actor is a `PRF_<Asset>` variant of `PRF_<Asset>_Visual` |
| Collision with mesh | every collider in level scenes sits on the prefab instance that carries its mesh (no detached collider objects) |
| Gold path | green on the same commit |

---

## M4 (Production detail)

1. `/playability-cert --mode game-setup` — evidence = latest gold path `checks.json` + `/independent-review --final` result.
2. `/qa --mode game-setup` — always enabled at M4 (Q0–Q3 engineering + GQ-01..12 playtest).
3. `/perf-audit` against `rendering.budgets` / TDD §11.6 — red budget = FAIL (fix or log + open defect).
4. `/code-reviewer` on `Assets/_Game/Scripts/**` written in this run — acceptance-breaking findings are fixed.
5. Player builds: none by default. `/build-setup` runs only when the owner explicitly asks, for the targets they name.
6. Final report `Docs/V57/reports/M4-final-<slug>.md` — per mechanic/scene/screen status word, open defects, decisions list, evidence paths. Checklist: `shared/references/unity-game-setup-reference.md` § Final checklist.

---

## Progress and stop output

Same as the driver: one `GF-PROGRESS` line per gate (also appended to `Docs/V57/DEVLOG.md`), Stop block only for the three stop conditions. Reports are listed, never auto-opened.

```text
GF-PROGRESS stage=<id> spec=<spec|none> done=<n> total=<m> gate=<pass|fail|running> status_word=<implemented|agent-verified> action="<short>"
```

---

## Sub-skills (do not duplicate)

| Stage | Skill |
|---|---|
| I0–I2 | `commands/setup/unity-intake-skill.md` |
| M1 | `commands/tdd/unity-tdd-to-context-skill.md` (`--context Docs/V57/CONTEXT.md`), `commands/tdd/unity-tdd-to-spec-skill.md` (`--out-dir V57/specs/<slug>/features`), `commands/setup/unity-gold-path-skill.md` |
| F | `commands/spec/unity-spec-skill.md`, `commands/compilation/unity-compile-heal-skill.md` |
| Editor writes | `commands/editor/unity-scene-setup-skill.md`, `unity-prefab-skill.md`, `unity-data-asset-skill.md`, `shared/helpers/unity-editor-verification-skill.md` |
| Crafts | table above |
| Pre-Play | `shared/helpers/unity-editor-smooth-ops-skill.md` |
| M3/M4 visual | `commands/review/unity-independent-review-skill.md` |
| M4 | `commands/setup/unity-playability-cert-skill.md`, `commands/review/unity-qa-skill.md`, `commands/perf/unity-performance-audit-skill.md`, `commands/review/unity-code-reviewer-skill.md`, `commands/compilation/unity-build-skill.md` |
| CR | `commands/context/unity-context-readiness-skill.md`, `unity-context-postmortem-skill.md` |

---

*Last updated: 2026-09-28*
