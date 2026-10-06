# Unity Context Query (`/context-query`)

Returns a **minimal subgraph** from `project-index.json` for the active task — scripts, deps, interfaces, events — without reading the full repo.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-query <specId>` or `/context-query <ModuleName>` |
| **Requires** | `project-index.json` (run `/context-index` first) |
| **Policy** | `V57/agents/skills/shared/helpers/unity-context-reading-policy-skill.md` |

---

## Usage

```
/context-query player_movement_controller
/context-query PlayerMovementController
/context-query win_lose_system
```

---

## Workflow

1. Resolve target:
   - Match `specId` (snake_case) **or** module `id` (PascalCase) in `project-index.json` → `modules[]`.
2. Load matching spec YAML if present: `V57/specs/**/{specId}.yaml` or infer path from `_tdd_mechanic_index.yaml`.
3. Emit **Context bundle** (markdown) containing only:

| Section | Source |
|---------|--------|
| Module summary | index.modules[] |
| Scripts to open | module.scripts + spec.touches.scripts |
| Direct dependencies | module.dependsOn (1 hop) |
| Interfaces | index.interfaces where implementors/consumers intersect module.classes |
| Events | index.events touching module.classes |
| Tests | spec.touches.tests |
| Prefabs/SO | spec.touches + index.assets filtered by path |

4. **Do not** paste entire `project-index.json`.
5. Proceed with implementation using bundle + `CONTEXT.md` overview only.

---

## Example output shape

```markdown
## Context bundle: player_movement_controller

**Module:** PlayerMovementController
**Scripts:** (open these only)
- Assets/_Game/Scripts/Features/Player/PlayerMovementController.cs
- …

**Depends on (1 hop):** InputSystem, EventBus

**Interfaces:** IPlayerInputReadOnly ← consumers …

**Events:** PrimaryActionExecuted → subscribers …

**Spec:** V57/specs/<slug>/features/player_movement_controller.yaml
```

---

## Fallback without index

If `project-index.json` missing:

1. Read spec YAML `touches` + `components[].files`.
2. Read `CONTEXT.md` dependency mermaid for 1-hop deps.
3. Suggest user run `/context-index`.

---

## Rules

1. Never load all `.cs` under `Assets/_Game/Scripts` when a bundle suffices.
2. Prefer spec `touches` over index when they conflict — file drift means run `/context-drift`.
3. Combine with context reading policy for token budget.
