# Game setup reference (long material for `/game-setup` and `/vertical-slice`)

Reference only — the operational rules live in `commands/setup/unity-vertical-slice-skill.md` (driver) and `commands/setup/unity-game-setup-skill.md` (Production scope). Read sections on demand.

---

## 1. Provider TDD (TDD Standard 2.0.0) → pipeline

Provider TDD path: `Docs/Design/TDD.md`. Intake parses it into `Docs/Generated/json/*.json`; stages read the JSON, and go back to the TDD text only for prose.

| TDD section | Generated file | Consumed at |
|---|---|---|
| §0.2 completeness gate | `INTAKE_REPORT.md` (issues) | I0 — failing items are reported, never fixed in the TDD |
| §A Project Identity | `package.json` | I0/I1 (perspective, physics, platform); M1 context |
| §B mechanics / §B-S support | `mechanics.json`, `entities.json` | M1 (specs), F |
| §C companion spec YAML | `mechanics.json → spec_yaml`, `tuning.json` | M1 (adopt as `V57/specs/<slug>/…`), F |
| §D dependency graph | `PLAN.md` spec queue | F order |
| §3 core loop | gold path authoring | M1 |
| §8 art | `asset_manifest.json` (with ADD briefs) | I2 |
| §9.1 screen registry | `ui.json` | M2 UI |
| §10 audio | `audio.json` | M2 |
| §11.3 input table | `input_map.json` | I1/M1/M2 input, gold path actions |
| §11.4 persistence | CONTEXT + save spec | M2 `/save-meta` |
| §11.5 movement/camera | `camera.json` (+ ADD `Camera/` refs) | I2 (scenes), M1 (locked) |
| §11.6 perf budgets | `rendering.json → budgets` | M3 numbers, M4 `/perf-audit` |
| §13.1 content inventory | `asset_manifest.json` (`source: inventory`) | I2 placeholders |
| §13.2 scene manifest | `scenes.json` | I2 (level scenes), scope |
| §14.2 pending / §14.3 ledger | INTAKE_REPORT | informational; pending rows → placeholder/cut + `D-###` |

Authority: TDD > ADD. ADD sections used: `asset_briefs`, `ui_screens`, `screen_layouts`, `scene_manifest`, `color_palette`, `visual_targets`, `style_guide`, `reference_images`.

**Amendments** (owner-driven, outside the autonomous run): the owner edits the TDD → re-run `/intake --only I0` → V57 regenerates affected specs → F for those specs. V57 itself never edits the TDD.

---

## 2. Scope rules

| Mode | Mechanics | Scenes |
|---|---|---|
| Vertical Slice (`/vertical-slice`) | mechanics whose ACs are covered by slice scenes + §D dependency closure | `scenes.json` where `slice: true` |
| Production (`/game-setup`) | all §B mechanics | all §13.2 scenes |

Scope affects **what** is built, never the quality bar: everything built ships with real prefabs, real UI Toolkit screens, real input tests, and evidence.

Missing media: `AssemblyRunner.BuildPlaceholders()` creates labeled placeholders (`MissingAsset: <asset_name>`); gaps are listed in `Docs/V57/TODO.md` and the M4 report. No synthesized audio (`AudioClip.Create`).

---

## 3. Craft details (verify items)

| Craft | Verify (OVR + evidence) |
|---|---|
| `/game-ui` | every in-scope `ui.json` screen state exists as UI Toolkit (`UIDocument.panelSettings` and `visualTreeAsset` non-null); layout compared to `UIMockups/<ScreenId>.png` in the review pack; uGUI only if TDD names it |
| `/camera-setup` | camera type/fov-or-size/angle/distance equal `camera.json` within tolerance; framing screenshot per view |
| `/lighting-setup`, `/urp-postprocessing` | exposure/luminance numbers in `numbers.json`; not black/blown out |
| `/shader-setup` | 0 pink materials (read-back of `shader.name == "Hidden/InternalErrorShader"`) |
| `/vfx-setup` | VFX visual prefabs from provider sheets; triggered in gold path screenshots where declared |
| `/audio-setup` | every `audio.json` event bound to a clip or listed missing; no `AudioClip.Create` |
| `/physics-setup` | layers/matrix read-back; contacts verified in real-physics PlayMode tests |
| `/level-design` | markers from blockout used; every hazard/interactable has a visible renderer |
| `/save-meta` | save → mutate → load round-trip in Play Mode |
| `/game-feel` | declared smoothing channels measured (damp/lerp times) — numbers, not impressions |

---

## 4. Final checklist (M4)

- [ ] A0 passed; every absorbed `fixable` issue has a `D-###`
- [ ] I1 baseline report all true; engine = pinned version
- [ ] I2 `assembly-report.json`: 0 missing refs, 0 errors
- [ ] Gold path green on the final commit (`checks.json` path in report)
- [ ] Every in-scope spec: EditMode + PlayMode green via `unity test`; XML under `Docs/V57/evidence/`
- [ ] No banned verification tricks (lint + review)
- [ ] Runtime lint exit 0; Edit vs Play hierarchy diff clean
- [ ] Every run-scope actor is a Gameplay prefab variant of its Visual prefab
- [ ] UI Toolkit screens for every in-scope `ui.json` screen
- [ ] Independent review ≥ threshold for every area (or open defects listed after max rounds)
- [ ] `/playability-cert` and `/qa` reports from evidence (no self-PASS)
- [ ] Perf budgets: no red (or open defect + `D-###`)
- [ ] Builds per `platform.targets[]` (configure-only where host cannot build, logged)
- [ ] Provider files untouched (no rename/move/edit), `Source/` untouched; TDD untouched
- [ ] Every item carries a status word: `implemented` / `agent-verified` / `pending owner review`
- [ ] Lessons learned written to `V57/knowledge/` via `/context-postmortem`

---

*Last updated: 2026-09-28*
