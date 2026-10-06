# Unity TDD to Spec Skill

Use this skill when the user asks to convert TDD mechanic(s) into spec YAML using `V57/specs/template/feature_spec_template.yaml` and `CONTEXT.md`.

**One command surface:** `/tdd-to-spec` with either **`--all`** (every mechanic in the TDD) or a **specific mechanic** (positional name or `--mechanic`). Same conversion rules apply in both modes—batch mode only adds discovery, manifest, and cross-spec checks.

| Item | Detail |
|------|--------|
| **Template** | `V57/specs/template/feature_spec_template.yaml` |
| **Network session template** | `V57/specs/template/network_session_spec_template.yaml` (when TDD §A multiplayer enabled) |
| **Architecture** | `CONTEXT.md` (read every run) — in a game repo this is **`Docs/V57/CONTEXT.md`**; specs go to **`V57/specs/<slug>/{features,systems}/`**; the TDD is `Docs/Design/TDD.md` (read-only — never edited) |
| **Ambiguity** | Decide + log `D-###` in `Docs/V57/DECISIONS.md`; never stop to ask inside the pipeline |
| **Writes** | Autonomous — draft/manifest shown, then saved in the same turn (`agents.yaml` `writes.require_user_confirm_before_spec_yaml: false`) |

---

## Invocation

**All mechanics in the TDD (batch)**

```bash
/tdd-to-spec <tdd-path> --all [--out-dir V57/specs/<slug>/features] [--type-default feature] [--dry-run]
```

**One mechanic**

```bash
/tdd-to-spec <tdd-path> <mechanic-name>
/tdd-to-spec <tdd-path> --mechanic "<mechanic-name>"
/tdd-to-spec <tdd-path> <mechanic-name> --out V57/specs/<slug>/features/<name>.yaml
/tdd-to-spec <tdd-path> <mechanic-name> --type system --out V57/specs/<slug>/systems/<name>.yaml
```

**TDD health check (pre-flight audit)**

```bash
/tdd-to-spec <tdd-path> --tdd-health-check
```

Examples:

```bash
/tdd-to-spec Docs/Design/TDD.md "Unit Selection" --out V57/specs/<slug>/features/unit_selection.yaml
/tdd-to-spec {TDD_PATH} --all --out-dir V57/specs/<slug>/features --type-default feature --map "MECH_<SystemId>:system"
/tdd-to-spec Docs/Design/TDD.md --all --dry-run
/tdd-to-spec Docs/Design/TDD.md --tdd-health-check
```

---

## Mode selection

| Intent | Command shape |
|--------|---------------|
| Single mechanic | `/tdd-to-spec <tdd> <name>` or `--mechanic "<name>"` **without** `--all` |
| Full TDD | `/tdd-to-spec <tdd> --all` |
| TDD health check | `/tdd-to-spec <tdd> --tdd-health-check` |

If **`--all`** and **`--mechanic` / positional mechanic** appear together, **decide and log**: run the single-mechanic mode for the named mechanic (the narrower request), append `D-###` to `Docs/V57/DECISIONS.md`, continue. Do not ask.

---

## Shared goal

Produce validated spec YAML aligned with the template and `CONTEXT.md`.

If **`CONTEXT.md` modules or identity are out of sync with the TDD**, run **`V57/agents/skills/commands/tdd/unity-tdd-to-context-skill.md`** (`/tdd-to-context <tdd>`) first so specs and dependencies match the living design source.

- **Single:** one complete spec — draft shown, then **saved in the same turn** (autonomous default per `agents.yaml` `writes`).
- **Batch:** manifest of mechanics → one spec per row → batch validation → **save all validated specs in the same turn**.

---

## Railguard — the TDD is the only mechanic source

Mechanic discovery (single and `--all`) runs **only on the TDD** passed as `<tdd-path>`:

- A **GDD is narrative/lore only.** If a companion GDD (or any `--from` / linked doc) contains `## Mechanic:` or `MECH_*` blocks, **skip them and emit a warning**: `Mechanics found in narrative GDD are ignored — TDD §B is the only mechanic source.` Never generate a spec from GDD mechanics.
- A mechanic that needs narrative references it via a `narrativeRef:` field in its TDD block; the narrative **content** stays as data (ScriptableObjects), not in the spec.

---

## Optional flags (natural language OK)

Applies mainly to **`--all`** (single-mechanic uses `--out` / `--type` as today):

- `--out-dir <path>` — default folder for batch outputs (filenames from `MECH_*` slug or title).
- `--type-default <feature|system|mechanic>` — when TDD does not imply type; per-mechanic override e.g. `--map "MECH_FOO:system"`.
- `--dry-run` — manifest + validation only; **no** writes.
- `--tdd-health-check` — run B-Health only; **no** spec generation or writes. Stops after presenting the annotated manifest with friction flags. Use to audit a TDD before committing to a full batch.

---

## Part A — Single mechanic (one spec)

Run **A1–A6** for `/tdd-to-spec <tdd> <mechanic>` (positional or `--mechanic`). Batch mode reuses A1–A5 per row (see Part B).

### A1. Read CONTEXT.md

Load project identity, modules overview, input system, and dependency graph. Note existing module ids — do not duplicate controllers already listed.

### A2. Locate the mechanic in the TDD

Find the section with heading `## Mechanic:` (canonical per `V57/docs/tdd/TDD_MECHANIC_TEMPLATE.md`) matching the requested name or `MECH_*` id. If ambiguous, **decide and log**: prefer the exact `MECH_*` id match, then the exact title match, then the first §B block in document order; append `D-###` to `Docs/V57/DECISIONS.md` and continue. Do not ask.

### A3. Map TDD → spec components

Use the TDD→Spec mapping table in this skill and `unity-context-aware-spec-skill.md`: controllers, enums, ScriptableObjects, events, dependencies from CONTEXT.

### A3b. Network session spec (conditional)

When TDD §A `multiplayer_model != N/A`:

1. If §B contains mechanic **NetworkSession** (or `MECH_NetworkSession`), fill from `V57/specs/template/network_session_spec_template.yaml` instead of feature template.
2. If no session mechanic exists but multiplayer is enabled, **warn** in manifest: `Missing NetworkSession mechanic — add §B block or emit from template (flagged in manifest)`.
3. Set `networking.tier` from §A (`networking_tier` or `multiplayer_model` per `NETWORKING_VOCABULARY.md`).
4. Output path default: `V57/specs/<slug>/systems/network_session.yaml`.
5. Suggest `/network-setup` after batch save (do not auto-run).

### A4. Fill `feature_spec_template.yaml`

- `specId`: snake_case from mechanic/controller name (full name, no truncation)
- `version`: from TDD Spec metadata `version` when present
- `touches`: all paths under `Assets/` (scripts, prefabs, SO instances, scenes, tests)
- `acceptanceCriteria`: measurable, testable, traced to TDD § Acceptance; `verification` from the AC tag (`EditMode` | `PlayMode`)
- `validationGates`: from template defaults unless TDD specifies otherwise
- **Persistence** (standard TDDs): when the mechanic's Persistence ≠ `none`, reflect it in `description` / `implementation` notes and include save-related paths in `touches`
- **status**: skip spec generation for `status: deprecated` mechanics unless the user explicitly asks
- **Legacy `sliceScope`** (v1 TDDs only): tolerated and ignored as a quality signal — TDD Standard 2.0.0 removed it; scope selection happens at prototyping/implementation time, never in the document. It may inform batch ordering as a legacy hint, nothing more

### A5. Validate structure

Check required keys (`name`, `type`, `dependencies`, `components`, `acceptanceCriteria`, `validationGates`, `specId`, `touches`). Future: `V57/tools/validate-spec.ps1` against `feature-spec.schema.json`.

### A6. Present draft and save

Show the YAML draft in chat, then **save in the same turn** (autonomous default). Wait for approval only if `agents.yaml` sets `require_user_confirm_before_spec_yaml: true`. After save, hint: `/spec "implement @V57/specs/..."`.

---

## Part B-Health — TDD Health Check (`--tdd-health-check`)

Run this before a full `--all` batch **and** before building a playable prototype/slice from the TDD. No spec YAML is generated.

The audit has **two layers** with independent verdicts:

- **Layer 1 — Spec readiness (per mechanic):** can each mechanic become a testable spec?
- **Layer 2 — Game playability (whole document):** does the assembled set describe a game a player can actually play — move, act, see feedback, reach content?

A TDD where every mechanic is ✅ Clear/Solid can still fail Layer 2 (e.g. no locomotion contract, no world owner, content referenced but never inventoried). **Overall PASS requires both layers.** Never report "healthy" from Layer 1 alone.

### Steps

**HC1. Discover mechanics** — same as **B1**: scan for `## Mechanic:` blocks (canonical) and legacy `MECH_*` / **Name** tables, build manifest with `tddMechanicId`, `title`, `sourceSection`.

**HC1b. Detect template standard** — determines which gate table applies:

| Standard | Detection | Policy |
|----------|-----------|--------|
| **2.x (current)** | §0.1 `Template standard: TDD Standard 2.x` row, or gate table contains G-16..G-18 and no `sliceScope` | Full audit below |
| **1.x (legacy standard)** | §0.2 gate present with old shape (`sliceScope` in G-03, G-07 slice view, G-15 roadmap, §14 roadmap, §15 risks) | Audit with the **v1→v2 gate mapping** below; add `⚠️ Legacy standard (v1)` WARN with migration note — never a retroactive FAIL for shape alone |
| **Pre-gate legacy** | No §0.2 gate table | Evaluate the checkable subset (§A fields, ACs, §C parity, §D orphans) + full Layer 2; flag `⚠️ Legacy shape (pre-gate TDD)` |

**v1→v2 gate mapping:** old G-08..G-14 → new G-07..G-13 (pending registry lives in §15.2/§15.3 on v1 docs, §14.2/§14.3 on v2). Old G-07 (slice view) and G-15 (roadmap) are **informational only** — report as `INFO (deprecated)`: scope selection happens at prototyping/implementation time (lab mechanic picker, run scope), not in the TDD. `sliceScope` fields are tolerated and ignored; never require them, never fail on their presence.

**HC2. Friction analysis (Layer 1)** — for each mechanic, apply these heuristics and assign one or more flags:

| Flag | Heuristic | Example |
|------|-----------|---------|
| `⚠️ Ambiguity` | Mechanic description overlaps significantly with another mechanic in the manifest | Stealth System and Environmental Interaction both mention "hiding" and "distraction" |
| `⚠️ Scope Risk` | Mechanic covers two or more distinct concerns that would benefit from separate specs | Enemy AI mixes "disguise/shapeshifting" and "patrol/alert states" |
| `⚠️ Dependency Risk` | Mechanic depends on 4+ other mechanics; may be unfalsifiable until dependencies exist | Progression System earns and spends XP from all other mechanics |
| `⚠️ Insufficient Detail` | Mechanic lacks enough rules, states, I/O, or edge cases to produce a testable spec | Only a name and one-sentence description with no defined behaviors |
| `⚠️ Missing Feedback` | A player-visible outcome has no declared feedback channel (UI id, diegetic/world, audio, camera/transform) | "Player earns combo" but no channel says how the player reads it |
| `⚠️ Event Orphan` | Publishes an event no §B/§B-S block consumes, or subscribes to an event nothing publishes | `TrustChanged` raised, zero subscribers declared |
| `⚠️ Content External` | References external data (`narrativeRef:`, tables, strings) with no §13.1 inventory row (count + owner) | "Triggers defined in narrative doc" but no count of triggers anywhere |
| `⚠️ Integration Orphan` | No §13.2 scene lists it and no core-loop step traces to it — spec-able but unreachable in play | A polish system no scene or loop step ever invokes |
| `✅ Clear` | Defined states, clear I/O, rules, measurable outcomes | Venom Attack — charge mechanic, clear fail state |
| `✅ Solid` | Well-structured; no flagged concerns | Stealth System — line of sight, visibility meters, alert states defined |

> ✅ Clear/Solid means **spec-ready only**. It says nothing about the assembled game — that is Layer 2.

**HC2b. Completeness Gate check** — verify the machine-checkable §0.2 items and report per-item PASS / FAIL / WARN (v2 numbering; map for v1 docs per HC1b):

| Item | Check |
|------|-------|
| G-01/G-02 | §A `[REQUIRED]` keys concrete; `engine` exact pin; `render_pipeline` + `dimension` declared; `save_model` / `multiplayer_model` explicit (`N/A` counts, absence fails) |
| G-03/G-04 | Every §B mechanic: quantified rules, I/O, `dependencies[]`, state machine or `N/A`, ≥ 1 AC tagged EditMode/PlayMode |
| G-05 | §C spec YAML count and `name`s match §B 1:1 |
| G-06 | Every dependency / §D id resolves to a §B mechanic or §B-S entry (no orphans) |
| G-07 | Zero `[PENDING]` / legacy producer markers (`[FORGE:*]`, `[PROJECTED]`, …); pending registry empty (§14.2 on v2, §15.2 on v1) |
| G-08 | Consistency ledger has no `FLAG` rows (§14.3 on v2, §15.3 on v1) |
| G-09..G-12 | Persistence / input / UI / scene cross-coverage (§B declarations vs §11.4, §11.3, §9.1, §13.2) |
| G-13 | Perf budgets per platform in §11.6 |
| G-14..G-18 | Playability items — resolved by the HC2c audit below (PC results roll up into these gate rows) |

**HC2c. Playability Audit (Layer 2, whole document)** — nine checks, each PASS / WARN / FAIL **with evidence** (section + offending ids). These run on **every** TDD, including legacy shapes — a missing answer is missing regardless of template age:

| # | Check | What must hold | Gate |
|---|-------|----------------|------|
| **PC-01** | Agency & locomotion | If the fantasy/core loop implies navigating space, §11.5 declares a **Control mode** (`player-driven | click-to-move | auto | none` — `auto`/`none` justified) and player-driven/click modes have Move/Look (or equivalent) in §11.3 with a §B consumer. A navigation technology (NavMesh, A*) **without** a control mode is a FAIL — it says how the character path-finds, not who drives it | G-14 |
| **PC-02** | Core loop trace | Every §3 loop step maps to: mechanic id + triggering input (§11.3) or system event + feedback channel. Emit the trace table; any unmapped step is a break in the loop | G-15 |
| **PC-03** | Session bootstrap | Player spawn, session/run start, and initial player capabilities are declared (in a §B/§B-S block or the entry scene row) | G-16 |
| **PC-04** | Play space owner | Every gameplay scene in §13.2 names a **world owner** — the §B/§B-S id that populates the play space. No owner = empty-world risk (the exact failure mode of an unpopulated final build) | G-16 |
| **PC-05** | In-play camera | Camera behavior **during play** is declared (§11.5 In-play camera): follow/fixed/orbit, who controls Look — not only cinematics | G-14 |
| **PC-06** | Feedback coverage | Every mechanic outcome names its channel (Feedback by outcome). Stricter when a §1 pillar declares "no HUD"/diegetic: UI-only channels contradict the pillar | G-11/G-15 |
| **PC-07** | Content inventory | Every externally referenced data asset (`narrativeRef:` targets, tables, string files) has a §13.1 row with count + owner, and the first playable content set is enumerable from the document | G-17 |
| **PC-08** | Event graph closure | Pub/sub is bipartite-complete: every published event has ≥ 1 resolvable consumer (or explicit `none (reason)`), every subscription has a publisher, §D agrees | G-18 |
| **PC-09** | Isolation readiness | Every mechanic's `dependencies[]` is complete enough to prototype it **isolated** (stubs derivable from declared deps) or **together** — no hidden coupling (reads of world/context state not declared as a dependency). This is what lets any tool expose all mechanics solo/merged | G-03/G-06 |

**HC3. Present results** — two-layer report:

```markdown
## TDD Health Check Results
- **TDD:** <path>
- **Template standard:** 2.x | 1.x legacy | pre-gate legacy
- **Mechanics found:** <N>

### Layer 1 — Spec readiness (per mechanic)
| # | Mechanic | Proposed Type | Flags |
|---|----------|---------------|-------|
| 1 | <name> | feature | ✅ Clear / ⚠️ <flag>: <evidence> |

### Layer 2 — Game playability (document level)
| # | Check | Status | Evidence |
|---|-------|--------|----------|
| PC-01 | Agency & locomotion | PASS/WARN/FAIL | <section + ids> |
| … | … | … | … |

### Completeness Gate
| Item | Status | Notes |
|------|--------|-------|
| G-01..G-18 (or mapped) | PASS / FAIL / WARN / INFO | <failing sections / offending ids> |

### Summary
- Layer 1: N spec-ready, N flagged (list flag counts)
- Layer 2: N PASS, N WARN, N FAIL
- Migration: <none | v1→v2 notes: drop sliceScope, move §14 roadmap out, add §11.5 Control mode, …>

### Verdict (two layers)
- **Spec batch:** READY | NOT READY
- **Playable game:** READY | NOT READY
- **Overall:** PASS only if both are READY
```

**HC4. Action recommendation** — after the verdict, state one of:

- *"TDD is healthy on both layers. Safe to run `/tdd-to-spec <tdd> --all` and to build a playable prototype/slice from it."* — all gate items PASS, Layer 1 clean, Layer 2 all PASS.
- *"Specs can be generated, but the assembled game will not be playable as documented — fix the Layer 2 failures first (each names its section)."* — Layer 1 clean but any PC item FAIL. **Never** summarize this state as "healthy".
- *"Fix the failing gate items before the full batch — each failure names the section to complete."* — any G-item FAIL (blockers for a production run; the user may still force the batch explicitly).
- *"Fix or confirm the flagged mechanics before running the full batch — these are warnings, not blockers."* — only ⚠️ friction flags remain.
- Legacy docs additionally get: *"Consider migrating to TDD Standard 2.0.0 (the provider owns the template; V57 only reads the TDD) — see Migration notes above."*

---

## Part B — Batch (`--all`)

### B1. Discover mechanics

Canonical: `## Mechanic:` blocks per `V57/docs/tdd/TDD_MECHANIC_TEMPLATE.md` (in a standard TDD they live under §B). Legacy: sections with property tables (`MECH_*`, **Name**) or `### Mechanic:` headings. Build manifest table: `tddMechanicId`, `title`, `sourceSection`, `proposedSpecPath`, `proposedType`. De-duplicate by ID. Skip `status: deprecated` blocks unless explicitly requested.

### B2. Per-mechanic conversion

For each manifest row, run **A1–A5** (reuse template/context reads once).

**Ordering:** dependency order from §D when available (topological — dependencies before dependents), else document order, or `core` → `secondary` → `progression` when **Type** exists. Report dependency cycles.

### B3. Batch validation

1. Each draft passes **A5**.
2. Cross-spec: dependency `id`s point to `CONTEXT.md` or another spec in this batch.
3. No accidental duplicate primary `name` / controller unless intended.

### B4. Batch save

Present manifest + per-row validation, then **write all validated YAML in the same turn** (autonomous default; rows failing validation are listed and skipped). If `agents.yaml` sets the confirm flag `true`, wait for approval of all or named files instead. Optional index: `V57/specs/_tdd_mechanic_index.yaml` after save.

### Output (batch)

Present a **TDD Batch Spec Plan** markdown block:

```markdown
## TDD Batch Spec Plan
- **TDD:** <path>
- **Mechanics:** <N>

### Manifest
| # | Mechanic | specId | Proposed path | Type | Status |
|---|----------|--------|---------------|------|--------|

### Cross-dependencies
- …

### Validation (per row)
- A5 PASS/FAIL + notes

### Assumptions / Blocked
- …
```

---

## Rules (all modes)

1. Never implement C# during TDD-to-spec unless the user explicitly asks.
2. Prefer concrete, testable spec text over speculation.
3. Every saved spec must include **`acceptanceCriteria`** (from TDD § Acceptance) and **`validationGates`** (from template); map each AC id to `implementation.tests.coverage` where possible.
4. If a spec file already exists, update in place and summarize deltas.
5. Cross-module mechanics: explicit interfaces/events in the spec.
6. Batch: do not silently skip mechanics—list **Blocked** until clarified.
7. Large TDDs: honor staged saves if the user requests (e.g. core first).

---

## Apply this skill

1. Parse mode: `--tdd-health-check` → run Part B-Health only → stop.
2. Otherwise, parse mode: `--all` vs single mechanic (positional or `--mechanic`).
3. **Single:** A1–A6 → `/spec "implement @..."` as next step hint.
4. **Batch:** B1–B4 → same hint per saved file.
