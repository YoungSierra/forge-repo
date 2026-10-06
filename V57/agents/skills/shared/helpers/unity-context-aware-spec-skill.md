# Unity Context-Aware Spec Generator

Use this skill when generating or revising YAML specs so they respect **`CONTEXT.md`**: existing modules, interfaces, and conventions.

| Item | Detail |
|------|--------|
| **Always first** | Read `CONTEXT.md` before any spec output |
| **Write policy** | `CONTEXT.md` edits are autonomous (summary shown, then written — `agents.yaml` `writes`); module removals stay confirm-gated |
| **Bootstrap** | Delegate to `V57/agents/skills/commands/spec/unity-spec-skill.md` Bootstrap mode — no duplicate bootstrap protocol |

---

## How it works

| Step | Action |
|------|--------|
| 1 | User requests a feature/system (natural language) |
| 2 | **Read `CONTEXT.md`** |
| 3 | Detect modules, interfaces, dependencies, conventions |
| 4 | Generate spec that **integrates** with existing architecture |
| 5 | Update `CONTEXT.md` — summary shown, then written in the same turn (autonomous default; `agents.yaml` `writes`) |

---

## First action: read CONTEXT.md

**Always** read `CONTEXT.md` before generating any spec.

```text
// Mental checklist:
// 1. Read CONTEXT.md
// 2. Parse: modules, interfaces, dependencies, conventions
// 3. Emit spec YAML that fits that graph
```

---

## Context usage rules

### 1. Reference existing modules

If `CONTEXT.md` lists `PlayerMovement`, a new `CombatSystem` spec should declare:

```yaml
dependencies:
  - kind: feature
    id: PlayerMovement
```

— do not recreate what already exists.

### 2. Implement existing interfaces

If `CONTEXT.md` defines `IInventoryReader`, the spec must wire implementations (e.g. `implements: IInventoryReader` on the right component).

### 3. Use EventBus when architecture is event-driven

Cross-system signals go through agreed channels (`EventBus`, event channels, etc.) as described in `CONTEXT.md`.

### 4. Respect naming conventions

Mirror whatever `CONTEXT.md` states for fields, events, and public API style.

---

## `CONTEXT.md` updates (autonomous write)

After generating or updating a spec, show a concrete summary of the `CONTEXT.md` changes (tables, bullets, or patch-style), then **write in the same turn** — autonomous default per `agents.yaml` `writes`. Wait for approval only if the confirm flag is `true` or the change removes modules.

**Examples:** new module row, new interface block, new dependency edge — show exactly what was added.

---

## Generation prompt template

When the user says "generate [feature/system]":

| Step | Action |
|------|--------|
| 1 | Read `CONTEXT.md` |
| 2 | Check if feature/spec already exists |
| 3 | If exists: point to `V57/specs/<slug>/features/...` or `V57/specs/<slug>/systems/...` — avoid duplicates |
| 4 | If new: choose `feature` vs `system`, interfaces, dependencies |
| 5 | Generate YAML respecting conventions |
| 6 | Save under `V57/specs/<slug>/features/` or `V57/specs/<slug>/systems/` (summary shown; autonomous default) |
| 7 | Apply `CONTEXT.md` edits in the same turn (summary shown) |
| 8 | Offer: validate → implement |

---

## Example session (abbreviated)

```
User: "generate a spec for the dialogue system"
→ Read CONTEXT: planned modules, IPlayerState, EventBus, naming
→ Emit V57/specs/<slug>/features/dialogue_system.yaml with matching dependencies
→ Update CONTEXT (mark active, add edges) — summary shown, written in the same turn
```

---

## Bootstrap alignment

| User intent | Action |
|-------------|--------|
| "initialize architecture", "bootstrap core", `/spec --bootstrap`, … | Read `CONTEXT.md`, then follow **Bootstrap mode** in `V57/agents/skills/commands/spec/unity-spec-skill.md` only |

Do not define a second bootstrap protocol here.

---

## Context-aware input system

From `CONTEXT.md`:

```yaml
input_system: old   # or new / hybrid
```

Generated specs and code sketches must match: **old** (`Input.GetAxis` / `GetButtonDown`), **new** (`PlayerInput` / `InputAction`), or **hybrid** as documented.

---

## Validation against context

Before generating code from a spec:

| Check | Question |
|-------|----------|
| Dependencies | Do referenced modules exist in `CONTEXT.md` (or are they explicitly new)? |
| Interfaces | Do `implements` / contracts match declared interfaces? |
| Naming | Does generated naming match `CONTEXT.md` conventions? |
| Duplication | Are we avoiding a second module for the same responsibility? |

---

## V57 Context Intelligence (spec v1.1+)

When generating **new** specs or revising for implement:

| Field | Rule |
|-------|------|
| `specVersion` | `"1.1"` for new specs |
| `specId` | snake_case of module (`StingCombatSystem` → `sting_combat_system`) |
| `touches.scripts` | All `components[].files` + `implementation.files` paths |
| `touches.tests` | All `implementation.tests` paths |
| `touches.prefabs` | Prefabs wired via MCP scene setup (if known) |
| `touches.scriptable_objects` | SO **asset** paths from `designerAssetSuggestedPath` |
| `eventChannels` | Cross-module events from spec `events` / EventBus usage |

After implement sign-off, remind user to run `/context-readiness`; if **Yellow** or **Red**, suggest `/context-drift` or user-confirmed `/context-index`.

Policy: `V57/agents/skills/shared/helpers/unity-context-reading-policy-skill.md`

---

## Rules

1. **`CONTEXT.md` first** on every spec-related request.  
2. Bootstrap requests → **`V57/agents/skills/commands/spec/unity-spec-skill.md`** Bootstrap only.  
3. **`CONTEXT.md` writes** are autonomous by default (`agents.yaml` `writes` flags): summary shown, then written; confirm only when the flag is `true` or rows are removed.  
4. If the module already exists, link the existing spec — do not blindly regenerate.  

---

## Apply this skill

1. **Always read `CONTEXT.md` first.**  
2. If bootstrap-oriented → **delegate** to `V57/agents/skills/commands/spec/unity-spec-skill.md` Bootstrap mode.  
3. Else → **check for existing spec/module** before duplicating.  
4. **Generate** architecture-fitting YAML.  
5. Apply `CONTEXT.md` updates with a summary (autonomous default).  
6. Offer full pipeline: spec → validate → generate → implement.
