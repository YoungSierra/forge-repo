# Unity Prototype Skill (`/prototype`)

Use this skill when the user wants to **validate one or a few mechanics in isolation** — not a full production game pipeline. The pipeline is **autonomous**: resolve variables once (ask only for what cannot be resolved), then run **all stages end-to-end without asking the user to continue or confirm**. Stop only for a hard failure that self-healing cannot fix, an unresolvable required variable, or a destructive decision.

| Item | Detail |
|------|--------|
| **Contrast** | `/game-setup` = full TDD → production pipeline; `/prototype` = 1–N mechanics → sandbox spike |
| **Spec output** | `V57/specs/prototypes/*.yaml` (not `features/` or `systems/`) |
| **Code output** | `Assets/Prototypes/Scripts/` (+ `Assets/Prototypes/Stubs/` for dependency fakes) |
| **Scene output** | `Assets/Prototypes/Scenes/` — **not** `SCENE_PRODUCTION_STANDARDS.md` |
| **TDD template** | `V57/docs/tdd/TDD_MECHANIC_TEMPLATE.md` (one block per mechanic; full TDD optional) |
| **CLI / MCP policy** | `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md` + `UNITY_MCP_REQUIREMENT.md` — CLI/Pipeline from Stage D onward |
| **MCP setup** | Same as `/game-setup` Stage SETUP — Unity CLI preferred (`V57/docs/mcp/UNITY_CLI_MIGRATION.md`) |
| **Editor OVR** | Functional verify via CLI; no production hierarchy gate |
| **Writes** | Autonomous — show a compact summary, then write in the **same turn** (`agents.yaml` `writes.*: false`); no `yes` prompt |
| **Promotion** | **Not in v1** — see [Promote to production](#promote-to-production-manual-v1) at end |

---

## Invocation

```bash
/prototype
/prototype --continue
/prototype --status
/prototype --stage <id>
/prototype --vars mechanics="Player Movement,Primary Action" tdd=Docs/Design/TDD.md
/prototype --vars mechanic="Primary Action" deps=stub
/prototype --vars spec=V57/specs/prototypes/coin_collectible.yaml
/prototype --vars scene=Assets/Prototypes/Scenes/CoinSpike.unity deps=stub
```

| Command | Behavior |
|---------|----------|
| `/prototype` | Resolve variables (Stage INIT), then run **all stages end-to-end** without stopping. |
| `/prototype --continue` | **Resume** after a stop (failure fixed / variable supplied); continues autonomously. |
| `/prototype --status` | Print variables, mechanic queue, completed stages, next stage, blockers. |
| `/prototype --stage <id>` | Jump to stage `<id>` if variables satisfied, then continue autonomously from there. |
| `/prototype --vars ...` | Pre-fill variables, then run the pipeline (or `--stage`). |

Natural language OK: e.g. “prototype coin pickup”, “spike player movement from my TDD”. `--continue` is a **resume** verb, not a per-stage gate — the pipeline never waits for it between stages.

---

## Session state (track in chat)

```yaml
prototype:
  mechanics: []          # 1–N human-readable mechanic names (from TDD headings)
  specs: []              # filled after Stage C — paths under V57/specs/prototypes/
  tdd: ""                # optional — path to TDD; required unless inline mini-TDD or pre-existing spec
  spec: ""               # optional single spec — skip Stage C for that mechanic if set
  scene: ""              # default derived in INIT — see Sandbox layout
  scene_split: auto      # auto | combined | separate — default auto (see Scene policy)
  deps: stub             # stub | require — default stub (Option A: *Stub.cs fakes)
  pack_integrated: null  # true | false | unknown
  completed_stages: []   # INIT, SETUP, 0, A, B, C, C:<spec>, D, E, E:<spec>, F, DONE
  mechanic_queue: []     # ordered names after Stage A
  mechanic_done: []
  stub_modules: []       # CONTEXT module ids satisfied via *Stub.cs
  mcp_preflight_pass: null
  mcp_stack_installed: null
```

Auto-resolve variables first (Stage INIT); ask the user **only** for what cannot be resolved.

---

## Operating rules (strict)

1. **English only** — all `/prototype` stage summaries, reports, and chat output must be **English**.
2. **Autonomous run — no continue prompts** — run stages **sequentially in one flow**. After each stage, emit the one-line progress block and move straight on. Never ask “continue?”, “OK?”, or “reply `yes`”. `--continue` exists only to **resume after a stop** (failure fixed or variable supplied).
3. **INIT first** — if `mechanics` and `spec` are both empty, ask for them (the only routine user input); everything else auto-resolves or defaults.
4. **Direct writes** — Stages B (CONTEXT patch), C (spec YAML): show a compact summary, then **write in the same turn**. No `propose → yes` gate (`agents.yaml` `writes.*: false`). `--dry-run` only on explicit review-only request.
5. **CLI/Pipeline from Stage D** — run pre-flight per `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md`. If Pipeline/CLI fails → **STOP**; do not ask the human to mount Hierarchy. Do **not** add product `Scripts/Editor/*Authoring` MenuItem scene builders.
6. **Stage C / E loops** — process the whole mechanic queue **sequentially without waiting** between C sub-stages or E sub-stages.
7. **Delegate** — call existing skills (`/tdd-to-spec`, `/tdd-to-context`, `/spec`); this skill **orchestrates** only.
8. **Sandbox only** — never use `/scene-setup` production workflow or `SCENE_PRODUCTION_STANDARDS.md` for Stage F. Primitives and loose hierarchy are OK.
9. **Spec before code** — every prototype still gets a YAML spec under `V57/specs/prototypes/`.
10. **No automated tests** — never create `Assets/_Game/Tests/**` or run Test Runner in prototype. Always `/spec implement … --no-tests`. Validation = compile-heal + `/playability-cert` only.
11. **Default dependency mode: stub (Option A)** — missing dependencies become minimal `*Stub.cs` under `Assets/Prototypes/Stubs/` so the mechanic is Play-Mode testable without implementing the full game.
12. **Wiring ≠ working** — scene objects + compile **never** count as Stage F PASS. Only **`/playability-cert`** (prototype mode) that proves the visible/playable loop can mark DONE. **`/qa` is not used** (`qa.enforcement: game-setup` — production only). Timeout / stuck Editor / unverified ACs → Stage F = **FAIL** (not PARTIAL-as-success).
13. After every C# write: Task **`/compile-heal`**. Before Play: **`/editor-smooth`**. Conditional craft (`/game-visuals`, `/game-ui`, etc.) only when the spike needs them — unused crafts are **silent skip** (no report, no progress line).
14. **Stop conditions — the only reasons to pause the run:** (a) unresolved required variable (rule 3); (b) hard gate FAIL after self-heal (MCP down, `/compile-heal` exhausted, `/playability-cert` FAIL after max 2 fix cycles); (c) destructive action (deleting user assets, editing the user-authored TDD). On stop, emit the Stop output; resume with `/prototype --continue`.
15. **Read order** at start: this skill, `V57/docs/tdd/TDD_MECHANIC_TEMPLATE.md`, `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md`, `V57/SYSTEM_PROMPT.md`.

---

## Hardening gates (mandatory — Lessons from AimArc-class failures)

Apply these on **every** Stage E stub + Stage F scene. Skipping any gate that applies is a **Stage F FAIL**.

### H-01 · Event / singleton subscribe race

If mechanics or stubs use a shared bus / singleton (`EventBus*`, static `Instance`, pub/sub):

| Rule | Requirement |
|------|-------------|
| Bus init order | Bus/stub singleton: `[DefaultExecutionOrder(-1000)]` (or lower than all consumers) and set `Instance` in `Awake` **and** `OnEnable` |
| Consumer subscribe | Subscribe in `OnEnable` **and** `Start`; if `Instance` was null, **retry** until subscribed (e.g. `Update` once / late bind). Never silent-return forever |
| Idempotent | Guard with `_subscribed` so handlers are not double-registered |
| Auto-triggers | Any `autoPulse` / auto-fire on Play must run **after** consumers can subscribe (next frame coroutine, `DefaultExecutionOrder(100+)` publisher, or explicit delay) — never only in publisher `Start` assuming listeners already bound |

**Anti-pattern (forbidden):** `OnEnable { if (Instance == null) return; Subscribe(...); }` with no retry — hierarchy order will drop the first events and leave visuals disabled.

### H-02 · Visible feedback must be observable in the spike

| Rule | Requirement |
|------|-------------|
| Preview / VFX duration | Production timers that hide feedback in < 1 s (e.g. aim preview `0.5 s`) are **too short for a sandbox**. Use a spike-friendly duration (default **≥ 5–10 s**) or keep preview until explicit commit input — document the override in the prototype report |
| Renderer enable | If a `LineRenderer` / mesh / UI starts hidden, Stage F must prove it becomes **enabled with non-zero content** after the trigger event |
| Width / scale | Line / gizmo widths must be camera-readable at the sandbox camera distance (avoid hairline defaults) |

### H-03 · Input API matches project

| Rule | Requirement |
|------|-------------|
| Input System | If Project Settings use the **New Input System** only, stubs **must not** call legacy `UnityEngine.Input` (`GetAxis`, `GetKeyDown`, etc.). Use `UnityEngine.InputSystem` (`Keyboard.current`, `Mouse.current`, …) |
| Pre-Play check | After stub compile, open Console: zero Input System errors before Stage F Play OVR |

### H-04 · Render pipeline materials

| Rule | Requirement |
|------|-------------|
| Shader match | LineRenderer / Mesh materials must use shaders valid for the **active** SRP (URP / HDRP / Built-in). Do not paste a material from another pipeline |
| Camera | Sandbox camera must see the mechanic origin (position + FOV/ortho size). Confirm origin + feedback bounds are in frustum during OVR |

### H-05 · Play Mode OVR is a hard gate (via `/playability-cert`)

| Rule | Requirement |
|------|-------------|
| Required evidence | Task `/playability-cert --mode prototype` on `{scene}` (**do not invoke `/qa`**) |
| Smoke | ≥10s playability |
| Timeout / stuck Play | MCP timeout, Editor stuck in Play, or “could not re-verify” → **FAIL Stage F**. Exit Play Mode, fix bridge, **re-run**. Do **not** mark DONE or write “PARTIAL — wiring PASS” as success |
| Wiring-only | Scene objects + compile **never** count as PASS |
| Report honesty | `Assets/Prototypes/Reports/prototype-{slug}.md` may say PARTIAL only if listed ACs remain open **and** Stage F is not treated as complete; DONE requires all scene ACs PASS |

### H-06 · Hierarchy / execution order checklist (Stage F)

Before Play OVR, verify in the scene (Inspector or MCP dump):

1. Bus / EventBus stub object exists and is active.
2. Mechanic consumers exist and reference config assets (no missing script / null `_config`).
3. Stub publishers that auto-fire are ordered **after** bus + consumers (execution order or delayed fire).
4. Visual components (`LineRenderer`, etc.) present on the expected GameObject.
5. Active render pipeline material assigned (not pink / missing shader).

---

## Sandbox layout

```
Assets/Prototypes/
  Scenes/
    <SceneName>.unity          # default: derived from mechanic slug(s)
  Scripts/
    <MechanicSlug>/            # mechanic implementation (.cs)
  Stubs/
    <ModuleName>Stub.cs        # Option A fakes for missing dependencies
  Reports/
    prototype-<slug>.md        # Stage F + DONE summary
V57/specs/prototypes/
  <mechanic_slug>.yaml
```

Default scene path when `scene` is empty:

- **One mechanic:** `Assets/Prototypes/Scenes/{MechanicSlug}.unity`
- **Multiple mechanics (combined):** `Assets/Prototypes/Scenes/{FirstSlug}_Spike.unity` or user-provided name in INIT

---

## Scene policy (multi-mechanic)

| `scene_split` | Behavior |
|---------------|----------|
| `auto` (default) | **One scene** wires all queued mechanics **unless** a mechanic TDD section explicitly requires isolation (separate scene, menu flow, “must not run alongside X”, dedicated test arena). |
| `combined` | Force single scene for all mechanics. |
| `separate` | One scene per mechanic under `Assets/Prototypes/Scenes/`. |

When `auto` splits a mechanic out, note it in the Stage A output and add a second scene path to session state.

---

## Dependency stubs (Option A — default)

When `deps=stub` and a mechanic’s `dependencies[]` reference modules **not yet implemented** in `Assets/_Game/Scripts/`:

1. List missing module ids in the Stage C spec proposal.
2. For each missing dependency, plan a **minimal stub**:
   - File: `Assets/Prototypes/Stubs/{ModuleName}Stub.cs`
   - Implements or mimics the **smallest surface** the mechanic needs (movement, event publish, score int, etc.).
   - Name suffix **`Stub`**; add XML summary: `/// Prototype-only fake — replace when promoting to production.`
   - Obey **Hardening gates H-01 / H-03** (execution order, subscribe retry, Input System API).
3. Wire stubs in the sandbox scene (Stage F) instead of production prefabs.
4. Record satisfied ids in session `stub_modules`.

When `deps=require`:

- If any dependency is missing from both production code **and** approved stubs → **STOP** in Stage C with a list of blockers. User must implement deps first or switch to `deps=stub`.

**Do not** put stubs under `Assets/_Game/Scripts/` — keep them under `Assets/Prototypes/Stubs/`.

### EventBus / pub-sub stub template (when used)

If the mechanic listens on a bus:

1. `EventBusStub` (or project equivalent): `[DefaultExecutionOrder(-1000)]`, singleton `Instance` bound in `Awake` + `OnEnable`, idempotent `Subscribe`.
2. Consumers: subscribe with retry (`OnEnable` + `Start` + late bind); never assume hierarchy Awake order.
3. Publishers that auto-fire on Play: fire **next frame** or `[DefaultExecutionOrder(100)]` after consumers.
4. Prefer **New Input System** APIs when the project is Input System–only (H-03).

---

## Stage INIT — Variables

Resolve from `--vars` / message text first; defaults apply where listed. Ask the user **only** when at least one of `mechanics` / `tdd` / `spec` is required and missing:

| Variable | Prompt | Required before |
|----------|--------|-----------------|
| `mechanics` | One or more mechanic names (TDD heading text) | Stage A+ (unless `spec` set) |
| `tdd` | Path to TDD containing mechanic block(s) | Stage A (unless inline mini-TDD or `spec` only) |
| `spec` | Existing prototype spec path | Skips Stage C for that mechanic |
| `scene` | Sandbox scene path | Stage F |
| `deps` | `stub` (default) or `require` | Stage C |
| `scene_split` | `auto` \| `combined` \| `separate` | Stage F |
| `pack_integrated` | Is SDD pack already copied next to `Assets/`? | Stage SETUP |

At least one of **`mechanics`**, **`spec`**, or an **inline mini-TDD** (user pastes one `TDD_MECHANIC_TEMPLATE` block) is required.

**Output template:**

```markdown
## Prototype — variables
| Variable | Value |
|----------|-------|
| mechanics | … |
| tdd | … |
| spec | … |
| scene | … (default …) |
| deps | stub |
| scene_split | auto |
| tests | none (prototype — always `--no-tests`) |

Proceeding: Stage SETUP (or Stage 0 if pack already integrated). Autonomous run — will stop only on failure or missing input.
```

---

## Stage SETUP — Pack integration + Unity MCP (CLI preferred)

Same as `/game-setup` Stage SETUP (`unity-game-setup-skill.md`):

1. Verify pack + `Assets/` + `ProjectSettings/`.
2. **CLI (preferred):** `unity pipeline install`, `unity mcp configure cursor --local --project-path <this project>`, Editor open on this project, Test MCP `mode: "cli"` + `toolCount` > 0.
3. **Legacy fallback:** `V57/docs/mcp/UNITY_MCP_SETUP.md` — skip if already configured with bridge **Running**.

Ensure `V57/specs/prototypes/` exists (create directory if missing).

Mark SETUP complete → **Next: Stage 0**.

---

## Stage 0 — MCP pre-flight

Same as `/game-setup` Stage 0 (CLI Test MCP or legacy `Unity.ReadConsole`). Required before D, E, F.

If false → **STOP** with fix steps from `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md`.

---

## Stage A — Mechanic intake

1. Resolve `mechanic_queue`:
   - From `mechanics` list, or
   - From `spec` filename / spec `name` field, or
   - From user-pasted mini-TDD block (save path suggestion only — do not write TDD unless user asks).
2. For each mechanic, locate TDD section `## Mechanic: <name>` (or legacy `### Mechanic: <name>`) per `TDD_MECHANIC_TEMPLATE.md`. If not found, list gaps; user fixes or pastes block.
3. Scan each mechanic for **scene isolation** cues (separate scene, menu-only, exclusivity). Set `scene_split` overrides for `auto` mode.
4. Build ordered queue (dependencies first when obvious from TDD **Related Systems** / `dependencies[]` hints).

No writes. **Next: Stage B** (or **Stage C:** first mechanic if CONTEXT already has rows and user skipped B — rare).

---

## Stage B — CONTEXT minimal patch

For queued mechanics only (not full bootstrap):

```bash
/tdd-to-context {tdd} --minimal
```

If `CONTEXT.md` is missing or empty, use `--bootstrap` instead but **only** Project Identity + Modules Overview for queued mechanics — **omit** `sceneProduction` production block unless user asks for production setup later.

Show a compact summary of the patch, then **write in the same turn** — no `yes` gate.

Add module rows with status **`prototype`** where the table supports a status column.

If user provided `prefix`, ensure `test_assembly_prefix` appears in Project Identity YAML.

**Next: Stage C:** first mechanic in queue.

---

## Stage C — Prototype spec (one mechanic per sub-stage)

For current mechanic `{mechanic}`:

1. Dry-run optional: `/tdd-to-spec {tdd} "{mechanic}" --out V57/specs/prototypes/{slug}.yaml --dry-run` if supported; else propose from template.
2. Run conversion per `unity-tdd-to-spec-skill.md` (single mechanic).
3. **Prototype spec adjustments** (mandatory in proposal):
   - Output path: **`V57/specs/prototypes/{slug}.yaml`**
   - `implementation.files` / `components[].files[].path` under **`Assets/Prototypes/Scripts/`**
   - List **stub plan** for missing deps when `deps=stub`
   - Thin `acceptanceCriteria` focused on **Play Mode feel**, not full DoD
4. Write the spec YAML in the **same turn** (summary shown; no `yes` gate).
5. Append path to `specs` / mark `C:{spec}` complete.

**Next:** next **Stage C:** mechanic (continue immediately) or **Stage D** if queue empty.

---

## Stage D — Bootstrap (conditional)

Run only if core scaffold missing (no EventBus / game state / input actions per `CONTEXT.md` and disk check):

1. `/spec --bootstrap --dry-run` — present plan summary (minimal modules only).
2. Run `/spec --bootstrap` in the **same turn** — no user OK gate.
3. Verify `Docs/V57/reports/bootstrap-report.md` includes **Unity MCP execution = PASS**.

If bootstrap already done, or prototype uses stubs only (`deps=stub`) and production scaffold is intentionally absent → **skip**. Note the skip in **chat** only (one line). **Do not** write `*-skip.md`, `prototype-stage-d-skip.md`, or any other skip/no-op report under `Docs/V57/reports/`.

**Next: Stage E:** first spec in queue.

---

## Stage E — Implement (one spec per sub-stage)

For current spec `{spec}`:

1. MCP pre-flight if not recent.
2. Create **stub scripts first** when `deps=stub` and spec lists stub deps (`Assets/Prototypes/Stubs/*Stub.cs`) — apply **H-01, H-03** (and H-02 duration overrides in config defaults when the mechanic hides feedback quickly).
3. `/spec "implement @{spec}"` with **`--no-tests`** always (legacy alias `implementa` accepted). Prototype never generates `Assets/_Game/Tests/**` — tests are Vertical Slice / Production only.
4. When implementing, prefer paths in spec under `Assets/Prototypes/Scripts/` — do not place prototype gameplay under `Assets/_Game/Scripts/` unless user explicitly promotes.
5. **Consumer subscribe pattern** — if the mechanic uses EventBus/stubs: implement subscribe retry (H-01). Do not ship silent `if (Instance == null) return` in `OnEnable` only.
6. **Spike timing** — if TDD preview/hide windows are < 1 s, set prototype config defaults to a readable duration (H-02) and note the override in the implement report.
7. Save implement report as `Docs/V57/reports/implement-report-prototype-{slug}.md` following **`unity-spec-skill.md` Gate 7** (self-descriptive ACs; no pipeline “Next step” / “Not in scope” process noise). Prototype extras allowed: `Mode: prototype`, list of active stub paths under Artifacts/Stubs, **Hardening notes** (H-01… applied).
8. Task **`/compile-heal`** after code writes — FAIL blocks advance.
9. Mark mechanic in `mechanic_done`.

**Next:** next **Stage E:** spec or **Stage F** if queue empty. (Say this in **chat**, not inside the implement report.)

**Stage E does not certify Play Mode.** Visual/runtime ACs stay **PENDING** until Stage F `/playability-cert` passes.

---

## Stage F — Sandbox scene (CLI/Pipeline)

Requires CLI/Pipeline pass, specs implemented, `scene` path set.

**Do not** invoke `/scene-setup` or `SCENE_PRODUCTION_STANDARDS.md`.

1. Create or open `{scene}` under `Assets/Prototypes/Scenes/`.
2. Via CLI (`unity command` / `eval` + editor verification skill):
   - Add minimal environment (plane, lighting, camera) — primitives OK.
   - Instantiate player / interactables needed for **all mechanics in this scene** (combined default).
   - Attach prototype scripts from `Assets/Prototypes/Scripts/`.
   - Wire stub components from `Assets/Prototypes/Stubs/` for missing deps.
   - Apply **H-04** (pipeline-correct materials) and **H-06** (hierarchy / execution order checklist).
3. **Pre-Play gate (H-06)** — dump or inspect: bus present, configs assigned, stubs + consumers active, materials valid. Fix before Play. Run **`/editor-smooth --before-play`**.
4. **Play Mode cert (H-05) — hard gate:**
   - Task `/playability-cert --scene {scene} --mode prototype`
   - **Do not** run `/qa` (`enforcement: game-setup` only)
   - If CLI/Pipeline **times out**, Editor stuck in Play, or asserts fail → **Stage F FAIL**. Recover, fix, **re-run**. Do **not** proceed to DONE.
5. Optional craft for visual spikes: `/game-visuals`, `/game-ui`, `/shader-setup` **only if needed** before cert — otherwise silent skip.
6. Write `Assets/Prototypes/Reports/prototype-{slug}.md` with PASS/FAIL per mechanic, stubs, hardening gates, link to playability-cert report.
7. **DONE eligibility:** only if playability-cert Overall PASS. “PARTIAL — wiring PASS; runtime not verified” is **not** enough.

**Next: Stage DONE** — only after step 7 passes.

If `scene_split=separate` or `auto` required split → run **Stage F** once per scene (sub-stages `F:{scene}`).

### Stage F FAIL template (chat)

```markdown
## Prototype — Stage F FAIL
- **Blocked by:** H-0X | Play Mode timeout | assert …
- **Evidence:** …
- **Fix plan:** …
- **Do not** mark DONE until OVR re-run PASSes.
```

### Fix mode (`/prototype --fix`)

When the harness **Fix issues** button (or an operator) resumes after FAIL / post-pipeline:

1. Read recent `Assets/Prototypes/Reports/prototype-*.md` and implement reports.
2. Treat listed H-01…H-06 / AC failures as the work queue.
3. Patch stubs/scene/materials via Unity CLI/Pipeline; re-run **Pre-Play (H-06)** + **Play Mode OVR (H-05)**.
4. Timeout / stuck Play ≠ success. Update the report + hardening table.
5. Mark Stage F / DONE only when ACs + H-01…H-06 PASS; otherwise re-emit Stage F FAIL.

---

## Stage DONE — Summary

Only when Stage F OVR **PASS** for every scene in this prototype run.

```markdown
## Prototype complete
| Mechanic | Spec | Scene | Stubs used | Play check |
|----------|------|-------|------------|------------|
| … | V57/specs/prototypes/… | … | …Stub | PASS |

### Hardening gates
| Gate | Result |
|------|--------|
| H-01 Event subscribe race | PASS |
| H-02 Observable feedback duration | PASS |
| H-03 Input API | PASS |
| H-04 Materials / camera | PASS |
| H-05 Play Mode OVR (`/playability-cert`; do not invoke `/qa`) | PASS |
| H-06 Hierarchy checklist | PASS |

### Active stub files (replace on promote)
- Assets/Prototypes/Stubs/…

### Promote to production (manual)
See skill section **Promote to production**.
```

Pipeline complete for this spike.

---

## Progress output (after every stage — do not stop)

```markdown
## Prototype — Stage {id} complete
- **Result:** …
- **Artifacts:** …

Continuing: Stage {next_id} — {one-line description}
```

## Stop output (failure / missing input only)

```markdown
## Prototype — STOPPED at Stage {id}
- **Reason:** gate FAIL after self-heal | unresolved variable | destructive decision
- **Evidence:** …
- **Needed from user:** {fix | variable value | decision}

Resume with `/prototype --continue` once resolved.
```

---

## Sub-skills referenced (do not duplicate)

| Stage | Skill / command |
|-------|-----------------|
| B | `commands/tdd/unity-tdd-to-context-skill.md` |
| C | `commands/tdd/unity-tdd-to-spec-skill.md` |
| D, E | `commands/spec/unity-spec-skill.md` |
| E+ | `commands/compilation/unity-compile-heal-skill.md` |
| F | `shared/helpers/unity-editor-smooth-ops-skill.md`, `commands/setup/unity-playability-cert-skill.md` (do not invoke `/qa`) |
| SETUP, 0 | Same as `/game-setup`; `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md` |

**Explicitly not used:** production `/scene-setup` standards, `/build-setup`, full `/game-setup` batch stages. Craft skills (`/game-visuals`, `/game-ui`, …) **are** allowed when the spike needs them.

---

## Promote to production (manual, v1)

When a prototype validates the mechanic:

1. Copy or regenerate spec: `V57/specs/prototypes/{slug}.yaml` → `V57/specs/<slug>/features/` or `systems/`.
2. Move/refactor code: `Assets/Prototypes/Scripts/` → `Assets/_Game/Scripts/`; delete or replace `Assets/Prototypes/Stubs/*Stub.cs` with real modules.
3. Update `CONTEXT.md` module status from `prototype` → active.
4. Run `/spec "implement @{production-spec}"` with full DoD (tests required — no `--no-tests`).
5. Run `/scene-setup --scene {production-scene}` per `SCENE_PRODUCTION_STANDARDS.md`.

Optional later: `/prototype --promote` command (not in v1).

---

## Apply this skill

1. User invokes `/prototype` (optionally `--vars` / `--stage`; `--continue` resumes after a stop).
2. Stage INIT: ask only for `mechanics`/`tdd`/`spec` if all are missing; everything else defaults.
3. Run **all stages (and C/E/F sub-stages) sequentially in one autonomous flow** — never ask to continue or confirm.
4. Write CONTEXT patch + spec YAMLs directly with a summary (no `yes` gate).
5. From Stage D: CLI/Pipeline mandatory (optional `unity mcp` stdio). No product `*Authoring` MenuItem scene builders.
6. Default `deps=stub` with Option A `*Stub.cs` files under `Assets/Prototypes/Stubs/`.
7. Obey **Hardening gates H-01…H-06** on every stub + sandbox scene.
8. Never mark Stage F / DONE complete without **`/playability-cert` PASS** (H-05). Do not invoke `/qa`. Wiring-only or timeout ≠ success.
9. Stop only per operating rule 13 — then emit the Stop output; resume with `--continue`.
