# Unity Spec-Driven Development Skill (`/spec`)

Create, validate and implement feature/system specs. In a game repo this runs inside the pipeline: specs are adopted at **M1** and implemented one at a time at **F** (see `commands/setup/unity-vertical-slice-skill.md`).

**Implementation standard:** all C# under `Assets/_Game/Scripts/**` conforms to `V57/docs/standards/STANDARDS_CANONICAL.md` (strict). `shared/references/unity-naming-skill.md` and `V57/docs/standards/CODING_STANDARDS.md` elaborate; on conflict the canonical document wins.

| Item | Detail |
|------|--------|
| **Specs** | `V57/specs/<slug>/features/*.yaml`, `V57/specs/<slug>/systems/*.yaml` (adopted from TDD §C; V57-owned) |
| **Spec template** | `V57/specs/template/feature_spec_template.yaml` |
| **Game context** | `Docs/V57/CONTEXT.md` (never a root `CONTEXT.md`) |
| **Code** | `Assets/_Game/Scripts/<Area>/` — one asmdef per area (`<Prefix>.<Area>`), ≤ 200 lines per file |
| **Tests** | `Assets/_Game/Tests/EditMode/`, `Assets/_Game/Tests/PlayMode/` |
| **Data** | `Assets/_Game/Data/SO_<Name>.asset` (values from `Docs/Generated/json/tuning.json`) |
| **Reports** | `Docs/V57/reports/F-<specId>-<slug>.md`; test XML under `Docs/V57/evidence/F/<UTC>/` |
| **Context-aware specs** | `shared/helpers/unity-context-aware-spec-skill.md` |
| **Post-codegen checks** | `shared/checks/unity-validation-skill.md` |
| **Test scaffold** | `V57/docs/standards/UNITY_TEST_SCAFFOLD.md` (paths mapped to `Assets/_Game/Tests`) |
| **Editor drive** | Unity CLI Pipeline (`unity command` / `eval` / `test`) for writes and verification; `unity mcp` optional |

### Editor connectivity

1. Pre-flight: `GET /api/unity-cli/preflight` or `unity pipeline list --format json`; `recompile_status` complete.
2. On failure: fix (Safe Mode → fix C# on disk, restart Editor) and **retry twice**, then use the **alternate route** (batch `Unity -batchmode -executeMethod …`, `unity test` in batch). Log `D-###`. Inside the pipeline this is a timeboxed problem (45 min), not a stop condition. Never fall back to asking the human to click through the Editor.

---

## Invocation

```bash
/spec "implement @V57/specs/<slug>/features/foo.yaml"
/spec "implement @V57/specs/<slug>/features/foo.yaml --no-tests"   # prototype only
/spec --scaffold [--dry-run] [--area <Area>]    # area asmdefs, event infrastructure, interfaces, test asmdefs
/spec --setup-scene --scene Assets/_Game/Scenes/SCN_<Level>.unity   # delegates to /scene-setup
# Legacy aliases: "implementa @…", --bootstrap (= --scaffold)
```

---

## Spec sources

| Situation | Action |
|---|---|
| Pipeline M1 | `/tdd-to-spec Docs/Design/TDD.md --all --out-dir V57/specs/<slug>/features --map "MECH_<SystemId>:system"` adopts every §C companion spec (systems land in `V57/specs/<slug>/systems/`) |
| New spec outside the TDD | Only for V57 support code (no new mechanics); draft from the template, log `D-###` |
| Spec needs a change during F | Update the spec first (V57-owned), log `D-###`; the TDD is never edited |

### Validate spec

| Check | Requirement |
|-------|-------------|
| `specVersion`, `name`, `type`, `description`, `components` | Present |
| `type` | `feature` \| `system` \| `mechanic` |
| `dependencies[]` | `kind`, `id`, optional `minVersion` |
| `files[]` | `.cs` only, under `Assets/_Game/Scripts/<Area>/` or `Assets/_Game/Tests/...` (rewrite legacy `Assets/_Game/Scripts/...` paths and log) |
| Data assets | `designerAssetSuggestedPath` under `Assets/_Game/Data/` |
| Alignment | Dependencies/interfaces match `Docs/V57/CONTEXT.md` |
| `acceptanceCriteria` | each row `id`, `description`, `verification` (`EditMode` \| `PlayMode` \| `Manual`) |
| `validationGates` | present |
| Traceability | every AC id maps to `implementation.tests[].coverage` and/or `stateMachine` / `publicAPI` |

### Forge mode test policy

| Mode | Tests on implement |
|------|--------------------|
| Prototype (`/prototype`, `/prototype-full`) | Forbidden — always `--no-tests` |
| Vertical Slice / Production | Required (`agents.yaml → implementation_done_gates`) |

---

## Test scaffold

| Check | PASS |
|-------|------|
| `com.unity.test-framework` in `Packages/manifest.json` | listed (added at I1) |
| Runtime asmdef per area | `Assets/_Game/Scripts/<Area>/<Prefix>.<Area>.asmdef` |
| EditMode asmdef | `Assets/_Game/Tests/EditMode/<Prefix>.Tests.EditMode.asmdef` (`UNITY_INCLUDE_TESTS`, references the area asmdefs) |
| PlayMode asmdef | `Assets/_Game/Tests/PlayMode/<Prefix>.Tests.PlayMode.asmdef` (references area asmdefs + `Unity.InputSystem`, `Unity.InputSystem.TestFramework`) |
| `.meta` GUIDs | exactly 32 hex characters |

`<Prefix>` priority: spec `implementation.testScaffold.assemblyPrefix` → `Docs/V57/CONTEXT.md → test_assembly_prefix` → `package.json → slug`.
**EnsureTestScaffold** creates only what is missing (idempotent). Map spec paths `Tests/EditMode/...` → `Assets/_Game/Tests/EditMode/...`, `Tests/PlayMode/...` → `Assets/_Game/Tests/PlayMode/...`.

---

## Implement workflow (F, one spec)

1. CLI pre-flight (above).
2. Networking gate (only if spec has `networking:`): `/network-setup` report must pass.
3. EnsureTestScaffold.
4. Write C# from `components` / `publicAPI` / `stateMachine` into `Assets/_Game/Scripts/<Area>/`.
5. Data: `/data-asset` creates `SO_*` instances from `tuning.json` values.
6. Scene/prefab touches: `/prefab` (Gameplay variant of Visual) and `/scene-setup` in Edit Mode; wire `[SerializeField]` references there.
7. `/compile-heal` (poll `recompile_status`; no blind retries during reload).
8. Tests via `unity test` (below).
9. `/gold-path --run` — must stay green.
10. OVR read-back for every asset/scene row (`shared/helpers/unity-editor-verification-skill.md`).
11. Perf static scan of touched scripts (`unity-performance-audit-skill.md` Layer A); block on Critical.
12. `/code-reviewer` on new/changed production scripts.
13. Report `Docs/V57/reports/F-<specId>-<slug>.md`; commit `V57 F:<specId>`; update STATUS/TODO; update `Docs/V57/CONTEXT.md` module status.

### Runtime code rules (lint-enforced at M2)

- The saved scene is the game. A bootstrap component (e.g. `_Systems/GameBootstrap`) may **only** hold `[SerializeField]` references and call `Init(...)` on systems in a fixed order. No runtime `GameManager`/`GameSystems` that builds, finds or patches the scene.
- Banned: `GameObject.Find*`, `FindObjectOfType/FindObjectsByType/FindFirstObjectByType/FindAnyObjectByType`, reflection `SetValue`/`BindingFlags.NonPublic`, `ScriptableObject.CreateInstance` config fallbacks, `Resources.Load` config fallbacks, `AssetDatabase` (also inside `#if UNITY_EDITOR`) in runtime assemblies, `new GameObject`/`AddComponent`/`CreatePrimitive` for level structure, hardcoded layout coordinates, renderer toggles on provider art, `Camera.main`.
- Missing serialized reference → `Debug.LogError` + disable the component; never search for a substitute.
- Runtime spawns only instantiate prefabs held in serialized fields, under `_Gameplay/Spawned`.
- Locomotion/camera feel channels declared in TDD §11.5 use frame lerp/smooth damp per `GAME_FEEL_STANDARDS.md`.

---

## Definition of Done (per spec)

| Gate | Rule |
|---|---|
| 1 Spec structural | required fields + AC traceability table in the report |
| 2 Compile | 0 errors (`/compile-heal`); tests compile in test asmdefs, not `Assembly-CSharp` |
| 3 Standards | `unity-validation-skill.md`; critical findings per `STANDARDS_CANONICAL.md` §10 block |
| 4 Code review | `/code-reviewer`; unresolved critical findings block; output `Docs/V57/reports/F-<specId>-codereview-<slug>.md` |
| 5 Tests | EditMode + PlayMode green via `unity test`; every AC id covered |
| 6 Acceptance | every AC passes with evidence; `Manual` rows become PlayMode tests or gold path steps before M4 |
| 7 Gold path | green on the same commit |
| 8 Report | written; status word `agent-verified` only when 1–7 pass, else `implemented` |

### Gate 5 — tests

```bash
unity test . --mode EditMode --output Docs/V57/evidence/F/<UTC>/editmode-<specId>.xml
unity test . --mode PlayMode --output Docs/V57/evidence/F/<UTC>/playmode-<specId>.xml
# or sidecar: POST /api/unity-cli/test { "mode": "EditMode" | "PlayMode", "filter": "…" }
```

- Never the Test Runner via MCP.
- PlayMode acceptance tests (`Assets/_Game/Tests/PlayMode/Acceptance*`) use **real input** (Input System test devices; press and release on separate frames; devices removed/restored in `finally`) and **real physics** (`WaitForFixedUpdate`). Banned: `Physics.Simulate`, `detectCollisions = false`, clamps/teleports, calling gameplay methods instead of input, `Time.timeScale` hacks, `[Ignore]`.
- CLI test failure (tooling, not test result): retry twice, then batch route; log `D-###`.

### Report (`Docs/V57/reports/F-<specId>-<slug>.md`)

```markdown
# F — <FeatureName>
## Summary            (2–6 sentences: what landed, outcome)
## Artifacts          | Path | Change | Role |
## Gates              | Gate | Result | Evidence path |
## Acceptance criteria | id | Criterion (full text) | How verified | Result |
## Decisions          D-### list
## Status word        implemented | agent-verified
```

Every AC row quotes the full criterion text. No pipeline narration ("next stage…") in the report.

---

## Scaffold mode (`--scaffold`)

Creates the architectural base from `Docs/V57/CONTEXT.md`, idempotently:

- Area asmdefs under `Assets/_Game/Scripts/<Area>/` (e.g. `Core`, `Gameplay`, `UI`, `Audio`, `GoldPath`)
- Event infrastructure (`EventBus` / event channel SOs), public interfaces listed in CONTEXT
- A thin `GameBootstrap` (serialized references + ordered `Init`) — no scene construction
- Test asmdefs (EnsureTestScaffold)
- Report `Docs/V57/reports/M1-scaffold-<slug>.md` (created / reused / conflicts)

Tags/layers and input actions are not created here (I1 baseline and `/input-setup` own them). Flags: `--dry-run` (plan only), `--area <Area>`, `--force` (overwrite only files this mode generated).

---

## Scene setup mode (`--setup-scene`)

Delegates to `/scene-setup` (`commands/editor/unity-scene-setup-skill.md`): Edit Mode authoring via CLI, OVR read-back, Edit vs Play hierarchy diff, report `Docs/V57/reports/M2-scene-setup-<slug>.md`.

---

## Rules

1. The spec is the source of truth for code; the TDD is the source of truth for the spec. Change the spec first (logged), never the TDD.
2. Never let implementation diverge from the spec without updating it.
3. Method signatures, delegate names and doc comments match `publicAPI`.
4. Every spec carries `acceptanceCriteria` and `validationGates`.
5. `Docs/V57/CONTEXT.md` and specs are written directly (no confirm prompts; `agents.yaml → writes`).
6. English only.

---

*Last updated: 2026-09-28*
