# Unity Context Reading Policy

Use this skill **before** reading source files, full TDD, or implement-reports on implementation or refactor tasks.

| Item | Detail |
|------|--------|
| **Operational guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Write policy** | `CONTEXT.md` / spec writes are autonomous (summary shown, then written — `agents.yaml` `writes`); destructive removals stay confirm-gated |
| **Default** | Read the minimum needed for the active task |

---

## Layer 0 — Pack (always at session start)

| Order | Resource | Scope |
|-------|----------|-------|
| 1 | `CONTEXT.md` | Project identity, modules, conventions |
| 2 | `V57/docs/standards/STANDARDS_CANONICAL.md` | Strict C# rules (single source of truth) |
| 3 | This skill (`unity-context-reading-policy-skill.md`) | Minimal reading policy |
| 4 | `V57/agents/agents.yaml` | Commands, writes policy, `review_standards.read_in_order` |

Do not preload the rest of pack skills every session.

---

## Layer 1 — Active command (slash command skill only)

| Command | Skill to read |
|---------|---------------|
| `/tdd-to-context` | `commands/tdd/unity-tdd-to-context-skill.md` |
| `/tdd-to-spec` | `commands/tdd/unity-tdd-to-spec-skill.md` |
| `/spec` | `commands/spec/unity-spec-skill.md` |
| `/game-setup` | `commands/setup/unity-game-setup-skill.md` |
| `/prototype` | `commands/setup/unity-prototype-skill.md` |
| `/code-reviewer` | `commands/review/unity-code-reviewer-skill.md` |
| `/pr-review` | `commands/review/unity-pr-reviewer-skill.md` |
| `/fix-<Script>` | `commands/review/unity-fix-script-skill.md` |
| `/context-readiness` | `commands/context/unity-context-readiness-skill.md` |
| `/context-index` | `commands/context/unity-context-index-skill.md` |
| `/context-graph` | `commands/context/unity-context-graph-skill.md` |
| `/context-query` | `commands/context/unity-context-query-skill.md` |
| `/context-path` | `commands/context/unity-context-path-skill.md` |
| `/context-drift` | `commands/context/unity-context-drift-skill.md` |
| `/scene-setup` | `commands/editor/unity-scene-setup-skill.md` |
| `/prefab` | `commands/editor/unity-prefab-skill.md` |
| `/data-asset` | `commands/editor/unity-data-asset-skill.md` |
| `/perf-audit` | `commands/perf/unity-performance-audit-skill.md` |
| `/perf-budget` | `commands/perf/unity-perf-budget-skill.md` |
| `/ui-setup` | `commands/ui/unity-ui-toolkit-skill.md` |
| `/game-ui` | `commands/ui/unity-game-ui-skill.md` |
| `/audio-setup` | `commands/audio/unity-audio-skill.md` |
| `/network-setup` | `commands/network/unity-networking-skill.md` |
| `/animation-setup` | `commands/art/unity-animation-skill.md` |
| `/asset-pipeline` | `commands/art/unity-asset-pipeline-skill.md` |
| `/build-setup` | `commands/compilation/unity-build-skill.md` |
| `/compile-heal` | `commands/compilation/unity-compile-heal-skill.md` |
| `/editor-smooth` | `shared/helpers/unity-editor-smooth-ops-skill.md` |
| `/playability-cert` | `commands/setup/unity-playability-cert-skill.md` |
| `/qa` | `commands/review/unity-qa-skill.md` |
| `/render-setup` | `commands/art/unity-render-setup-skill.md` |
| `/lighting-setup` | `commands/art/unity-lighting-setup-skill.md` |
| `/game-visuals` | `commands/art/unity-game-visuals-skill.md` |
| `/vfx-setup` | `commands/art/unity-vfx-setup-skill.md` |
| `/shader-setup` | `commands/art/unity-shader-setup-skill.md` |
| `/camera-setup` | `commands/art/unity-camera-setup-skill.md` |
| `/cinematics` | `commands/art/unity-cinematics-skill.md` |
| `/level-design` | `commands/editor/unity-level-design-skill.md` |
| `/input-setup` | `commands/editor/unity-input-setup-skill.md` |
| `/nav-ai` | `commands/editor/unity-nav-ai-skill.md` |
| `/save-meta` | `commands/editor/unity-save-meta-skill.md` |
| `/localization` | `commands/ui/unity-localization-skill.md` |
| `/accessibility` | `commands/ui/unity-accessibility-skill.md` |

Shared helpers (`unity-naming-skill.md`, `unity-validation-skill.md`, `unity-editor-verification-skill.md`, …) **only on demand** from the active command.

---

## Layer 2 — Game scale (Phase 2+)

- Run `/context-readiness` before adopting automatic indexing.
- With `project-index.json`: `/context-query` and `/context-path` for subgraph — **do not** read additional pack docs.

---

## Reading order by task type

### Implement or review a spec (`/spec implement`, `/code-reviewer` on feature)

| Order | Resource | Scope |
|-------|----------|-------|
| 1 | `CONTEXT.md` | Project Identity, Modules Overview, conventions, mermaid deps — **not** historical implement-reports |
| 2 | Active spec YAML | `V57/specs/<slug>/features/` or `systems/` for the module |
| 3 | `spec.touches.*` | Only scripts, prefabs, SO, tests listed in the spec |
| 4 | `project-index.json` | Subgraph for `specId` — **only if it exists** (Phase 2+) |
| 5 | TDD | Only the active mechanic section, not the full document |

### Bootstrap / new project (`/game-setup`, `/tdd-to-context --bootstrap`)

| Order | Resource |
|-------|----------|
| 1 | TDD (full or per mechanic per stage) |
| 2 | `commands/setup/unity-game-setup-skill.md` (canonical pipeline + CHECKLIST) |
| 3 | `CONTEXT.md` (proposal) |
| 4 | Queued specs (`_tdd_mechanic_index.yaml`) |

### Prototype spike (`/prototype`)

| Order | Resource |
|-------|----------|
| 1 | Active mechanic section in TDD only (not full TDD) |
| 2 | `commands/setup/unity-prototype-skill.md` |
| 3 | `CONTEXT.md` — Modules rows for queued mechanics only |
| 4 | Active spec in `V57/specs/prototypes/` |
| 5 | Stub list in `Assets/Prototypes/Stubs/` when `deps=stub` |

### Cross-cutting refactor (EventBus, rename module, interface change)

| Order | Resource |
|-------|----------|
| 1 | `CONTEXT.md` — interfaces and dependency rules |
| 2 | `/context-query` or `/context-path` — when available (Phase 2+) |
| 3 | Specs of affected modules |
| 4 | Files in those specs' `touches` |

---

## What NOT to read by default

- All of `Assets/_Game/Scripts/**/*.cs` (use scoped grep/search instead)
- Full TDD if YAML spec already exists for the mechanic
- `Docs/V57/reports/implement-report-*.md` except when refactoring the same module
- `Assets/Art/**` (textures, models, materials) unless explicit spec
- Entire `project-index.json` — only task subgraph

---

## Token rules

1. Prefer **spec + touches** over free repo exploration.
2. If `touches` is missing on a legacy spec (`specVersion: "1.0"`), infer paths from spec `components[].files` and `implementation.files`.
3. After implement, complete `touches` in the spec (autonomous write; summary shown).

---

## Relationship with other skills

| Skill | Usage |
|-------|-------|
| `unity-context-aware-spec-skill.md` | Generate specs aligned to CONTEXT |
| `unity-spec-skill.md` | Implement + DoD |
| `CONTEXT_INDEX.md` | Indexing, readiness, `/context-*` commands |

---

## Plan phases (summary)

| Phase | Consumer | Agent action |
|-------|----------|--------------|
| 0 | Any | CONTEXT + spec (current) |
| 1 | Specs with `touches` | This policy mandatory |
| 2 | `project-index.json` | `/context-query` before opening extra files |
| 3 | Cross-module refactors | `/context-path` |
