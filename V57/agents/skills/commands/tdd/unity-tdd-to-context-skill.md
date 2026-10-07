# Unity TDD to CONTEXT Skill

Use this skill when the user wants **`CONTEXT.md` updated from a Technical Design Document (TDD)** so the engineering map (identity, modules, dependencies, milestones) matches the **living design source**. Typical use: **consumer Unity game** with `CONTEXT.md` at the project root (same level as `Assets/`); this template repo’s committed `CONTEXT.md` describes the pack itself—run this skill against the **game’s** `CONTEXT.md`.

| Item | Detail |
|------|--------|
| **Primary output** | Edits to `CONTEXT.md` — **autonomous write**: summary shown, then written in the same turn (`agents.yaml` `writes.require_user_confirm_before_context_md: false`) |
| **Inputs** | TDD path; existing or missing `CONTEXT.md` (see **Bootstrap** + **Canonical outline**) |
| **Downstream** | After approval, `/tdd-to-spec` uses the refreshed `CONTEXT.md` |

---

## Invocation

**Default (balanced update)**

```bash
/tdd-to-context <tdd-path> [--context CONTEXT.md]
```

**Scope flags**

| Flag | Effect |
|------|--------|
| `--minimal` | Update **Project Identity** + **Modules Overview** only (YAML block + module table). |
| `--full` | Also propose/refine **Module Dependencies** (mermaid), **Next Milestones**, **Technical Notes** bullets, optional **Current Specs** table rows when paths are obvious. |
| `--dry-run` | Emit the full proposal in chat; **no** file write even after “approve” (use for review). |

**Bootstrap (greenfield game)**

```bash
/tdd-to-context <tdd-path> --bootstrap [--from <template.md>] [--context CONTEXT.md]
```

- Use **`--bootstrap`** when `CONTEXT.md` is **missing** or **nearly empty** (no usable Project Identity / Modules table). Fill it from the TDD using the **Canonical CONTEXT outline** below.
- **`--from <template.md>`** is **optional**. If present, read that file for extra headings or folder conventions, then **merge** TDD-derived identity/modules/deps into it (do not treat it as authoritative over the TDD). If omitted, **do not** require any external template file—build structure from the canonical outline only.
- If **`--from`** path is missing on disk, **stop** and ask for a valid path or rerun without `--from`.
- In this template repository, **`V57/docs/context/CONTEXT_TEMPLATE.md`** is a valid optional **`--from`** source when you want the full starter sections (interfaces sample, folder tree) merged with TDD-derived identity and modules.

**Stale modules (optional, destructive)**

```bash
/tdd-to-context <tdd-path> --remove-stale
```

- Only with explicit user intent: modules in `CONTEXT.md` with **no** TDD evidence (no mechanic **Related Systems**, no `MECH_*` ID reference, no overview mention) are listed as **candidates for removal**; **never delete rows** until the user confirms that list in the same approval step.

Natural language is OK: e.g. “sync CONTEXT from this TDD, modules only, dry run.”

---

## Railguard — the TDD is the only mechanic source

Identity and module extraction read **only the TDD**. A **GDD is narrative/lore only**: if a companion GDD contains `## Mechanic:` / `MECH_*` blocks, **ignore them with a warning** (no modules are derived from GDD mechanics). The TDD §B (mechanics) + §D (dependency graph) are the single source for the module table and dependency mermaid.

---

## Canonical CONTEXT outline (`--bootstrap` without `--from`)

Use this **section order** and minimal scaffolding when creating or rebuilding `CONTEXT.md` from the TDD alone. Align naming bullets with `V57/docs/standards/NAMING_CONVENTIONS.md` and `V57/docs/standards/STANDARDS_CANONICAL.md` by reference (no need to paste full standards).

| Order | Section | Bootstrap content |
|-------|---------|-------------------|
| 1 | Title `# CONTEXT.md` + intro | Living document / update frequency / location at project root. |
| 2 | `## Project Identity` | YAML from TDD (see extraction rules). |
| 3 | `## Modules Overview` | Table from TDD mechanics. |
| 4 | `## Architecture Conventions` | Short **Naming** + **Patterns** bullets; optional **Folder Structure** code block (`Assets/...`) consistent with `V57/agents/skills/shared/references/unity-import-skill.md` defaults unless TDD overrides. |
| 5 | `## Module Dependencies` | Mermaid from TDD if `--full`; else minimal `graph TD` with one placeholder or skip until next `--full` sync. |
| 6 | `## Interfaces (Public API Between Modules)` | Empty or `// TBD` until cross-module contracts are known. |
| 7 | `## Known Systems (Do Not Duplicate)` | Bullets only for modules already agreed to exist; else omit or state “none yet”. |
| 8 | `## Input Configuration` | YAML `input_system: old \| new \| hybrid` from TDD or placeholder. |
| 9 | `## Current Specs in Development` | Empty table or rows only if paths already exist. |
| 10 | `## Next Milestones` | From TDD pipeline / acts if `--full`; else short list from TDD overview. |
| 11 | `## Technical Notes` | TDD-driven bullets (camera, physics, perf) when `--full`. |
| 12 | Footer | `Last updated` + update trigger line. |

---

## Goals

1. **`CONTEXT.md` reflects the TDD** so agents know **which systems/modules exist** for implementation and spec generation.
2. **Preserve** `V57/docs/` alignment: do not contradict `V57/docs/standards/STANDARDS_CANONICAL.md` / naming docs; only touch architecture narrative inside `CONTEXT.md`.
3. **Write policy (autonomous default):** present a concrete summary/diff of the changed sections, then **write `CONTEXT.md` in the same turn**. Wait for approval only if `agents.yaml` sets `require_user_confirm_before_context_md: true`. `--dry-run` never writes.

---

## Required reads (every run)

1. **TDD** (full or from metadata + all mechanic sections through “Related Systems” / `MECH_*` tables).
2. **`CONTEXT.md`** at the path given by `--context` or default **`CONTEXT.md`** (Unity project root). If **`--from <path>`** is set, read that template for merge. If **`--bootstrap`** and the file is missing or unusable and **`--from`** is omitted, use the **Canonical CONTEXT outline** only—no other file is required.
3. **`agents.yaml`** `writes` flags govern write mode — autonomous (default, flags `false`) vs. confirm-gated (`true`).

---

## Extraction rules (TDD → CONTEXT fields)

### Project Identity (`## Project Identity` YAML block)

Map from TDD **metadata / overview** when present:

| YAML key | Typical TDD source |
|----------|-------------------|
| `project_name` | Game **Name** / title |
| `repo_kind` | Omit or set `game` / `unity_game` if the file is for a shipped game (not this template) |
| `engine` | **Engine** + version — prefer the exact pin from TDD §A (standard TDDs pin e.g. `Unity 6000.0.32f1`) |
| `render_pipeline` | TDD §A `render_pipeline` — copy when present (standard TDDs) |
| `dimension` | TDD §A `dimension` — copy when present (standard TDDs) |
| `language` | **Optional — omit by default.** Copy TDD **Language** only when explicitly stated. Otherwise resolve from `engine` via `STANDARDS_CANONICAL.md` §3 mapping (do not hardcode a pack-wide C# version). |
| `pattern` | Infer from TDD (e.g. component-based, event-driven, data-oriented) — one short line |
| `target_platform` | **Target Platform** / platform table |
| `save_model` | TDD §A or §11 **Save** row |
| `multiplayer_model` | TDD §A or §11 **Multiplayer** row — normalize to `NETWORKING_VOCABULARY.md` enums |
| `networking_tier` | TDD §A optional; must agree with `multiplayer_model` |
| `max_players` | TDD §A when multiplayer enabled |
| `session_visibility` | TDD §A when multiplayer enabled |

### Networking block (`networking:` YAML) — when `multiplayer_model != N/A`

Map TDD §A to optional CONTEXT block per `V57/docs/context/CONTEXT_TEMPLATE.md` and `V57/docs/networking/NETWORKING_VOCABULARY.md`:

| CONTEXT key | TDD source |
|-------------|------------|
| `networking.enabled` | `true` when `multiplayer_model != N/A` |
| `networking.tier` | `networking_tier` or inferred from `multiplayer_model` |
| `networking.topology` | `listen_host` or `dedicated_server` per model |
| `networking.maxPlayers` | `max_players` |
| `networking.sessionVisibility` | `session_visibility` |
| `networking.framework` | `netcode_for_gameobjects` (default for V57 NGO path) |

Add **NetworkSession** row to Modules Overview (Type: System, Status: planned) when multiplayer enabled.

**After proposal (do not auto-run):** suggest `/network-setup` when user approves CONTEXT with networking enabled.

Use quoted strings for unknowns only when the user asked to leave placeholders; prefer concrete values from the TDD.

### Standard TDD sections (provider TDD Standard 2.x — full template `V57/docs/tdd/TDD_Template.md`)

| TDD section | CONTEXT target |
|-------------|----------------|
| **§B-S Support Systems Registry** | One module row each (Type: `System` or `Core`, Status: `planned`) — same merge policy as mechanics; never leave §B-S ids out of the module table |
| **§11.3 Input map** | `## Input Configuration` — `input_system` + action map/action summary |
| **§11.4 Persistence spec** | `save_model` in identity + a Technical Notes bullet summarizing what persists and when |
| **§11.6 Performance budgets** | `performanceBudgets` block (or Technical Notes bullets) — feeds `/perf-budget` |
| **§13.2 Scene manifest** | `sceneProduction` block per `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md` (scene ids, purpose, systems present) |
| **§9.1 Screen registry** | Technical Notes bullet (UI screens + consuming modules); UI feature modules keep their rows from §B |

### Modules Overview (table)

Build rows from:

- Mechanic sections with **ID** (`MECH_*`), **Name**, **Type** (core / system / secondary…), **Description**, **Related Systems**.
- Deduplicate **Related Systems** strings into **one module row per PascalCase** system name (normalize: `snake_case` / spaces → `PascalCase`).

| Column | Rule |
|--------|------|
| **Module** | `PascalCase`, stable across TDD versions (prefer MECH ID slug only as internal note in Description if helpful). |
| **Type** | Map TDD **Type** + role: `Core` (foundational infra), `System` (rules/simulation), `Feature` (player-facing), `Singleton` only when TDD or existing CONTEXT implies global manager. |
| **Description** | One line from mechanic **Description** or combined Related Systems; no speculation beyond TDD. |
| **Status** | Default **`planned`** for new rows from TDD; preserve **`active`** if already in `CONTEXT.md` unless TDD explicitly deprecates. |

**Merge policy**

- **Add** rows for TDD systems not in the table.
- **Update** description/type when the same module appears again with clearer TDD text.
- **Remove** rows only under **`--remove-stale`** and explicit user confirmation of the removal list.

### Module Dependencies (mermaid) — `--full` or when user asks

- Edges from **Related Systems**, cause→effect chains, and explicit “System Impact” lines.
- Use **node ids** matching the **Module** column (no spaces).
- If ambiguous (cycle or unclear direction), output **two** options in the proposal and ask the user to pick before write.

### Interfaces — `--full` and only when justified

- Add or update **Interfaces** C# blocks only when the TDD clearly implies **cross-module reads** (e.g. “Combat reads inventory”). Otherwise **omit** or leave “TBD” comment in proposal for the user to fill—do not invent large APIs from thin air.

### Known Systems / Current Specs / Next Milestones / Technical Notes

- **`--full`:** propose bullet updates grounded in TDD (camera, physics, save, networking). Do not invent file paths under `Assets/` unless the TDD or existing `CONTEXT.md` names them.
- **Specs table:** add rows only when matching `V57/specs/<slug>/features/*.yaml` or `V57/specs/<slug>/systems/*.yaml` already exist or the user asked to pre-register paths.

### Footer

- Set `*Last updated: YYYY-MM-DD*` to **today** (user environment) on any successful write.

---

## Steps (agent checklist)

| Step | Action |
|------|--------|
| 1 | Parse flags: `--minimal`, `--full`, `--dry-run`, `--bootstrap`, `--from`, `--context`, `--remove-stale`. |
| 2 | Read TDD + target `CONTEXT.md` if it exists and is usable; if `--from`, read that template; if `--bootstrap` with no usable `CONTEXT.md` and no `--from`, use **Canonical CONTEXT outline** only. |
| 3 | Build **proposal markdown**: identity YAML, modules table (full merged table or unified diff-style “add/change/remove candidates”). |
| 4 | If `--full`, add mermaid + milestones + technical notes + optional interfaces/specs. |
| 5 | List **assumptions** and **questions** (ambiguous deps, missing engine version) — informational; do not block on them. |
| 6 | Show the proposal summary. Stop for confirmation **only** if `agents.yaml` confirm flag is `true` or `--remove-stale` lists removals. |
| 7 | Unless `--dry-run`, write **`CONTEXT.md`** in the same turn (single atomic replace or search-replace per section—keep conventions and unrelated sections intact). |
| 8 | Suggest next step: **`/tdd-to-spec <tdd> --all`** or per-mechanic specs. |

---

## Output template (chat)

```markdown
## TDD → CONTEXT proposal
- **TDD:** <path>
- **Target:** <context path>
- **Mode:** minimal | full | bootstrap | dry-run

### Project Identity (YAML)
\`\`\`yaml
...
\`\`\`

### Modules Overview
| Module | Type | Description | Status |
...

### Other sections (if full)
...

## Stale candidates (only if --remove-stale)
- ...

## Assumptions / open questions
- ...

**Written to:** CONTEXT.md (autonomous) | dry-run — no write | awaiting confirm (flag/remove-stale only)
```

---

## Rules

1. Write mode follows `agents.yaml` `writes.require_user_confirm_before_context_md` — default **false** → write directly after showing the summary. `--dry-run` never writes.
2. **Do not** silently remove modules unless **`--remove-stale`** and the user confirms the removal list (destructive — always confirm-gated regardless of write mode).
3. **Template repo:** do not overwrite the pack’s own `CONTEXT.md` (describes Base_Unity_V57) unless the user explicitly targets it—prefer warning and `CONTEXT.md` at the **game** root.
4. **Naming:** module names **PascalCase**; align with `V57/docs/standards/NAMING_CONVENTIONS.md` for classes.
5. **No C# implementation** in this skill—architecture doc only.
6. After a successful sync, **read order** for later work remains: `CONTEXT.md` first, then `V57/agents/skills/commands/tdd/unity-tdd-to-spec-skill.md`.

---

## Apply this skill

1. User invokes `/tdd-to-context <tdd>` (with optional flags).
2. Run the **checklist**; present the **summary**; write in the same turn (autonomous default).
3. Then point to **`/tdd-to-spec`** for YAML specs.
