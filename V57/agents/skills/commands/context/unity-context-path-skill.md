# Unity Context Path (`/context-path`)

Finds a **short dependency path** between two modules/classes in `project-index.json` for refactors and onboarding.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-path <FromModuleOrClass> <ToModuleOrClass>` |
| **Requires** | `project-index.json` |
| **Example** | `/context-path PlayerController RunEndSystem` |

---

## Workflow

1. Load `project-index.json`.
2. Normalize `From` and `To` to module ids or class names:
   - Search `modules[].id`, `modules[].classes[]`, `modules[].specId`.
3. Build adjacency from:
   - `modules[].dependsOn` (class-level deps aggregated to modules)
   - `events[]` (publisher → subscriber edges)
   - `interfaces[]` (implementor → consumer)
4. BFS shortest path from From → To.
5. Output path as ordered list with edge type:

```markdown
## Path: PlayerController → RunEndSystem

1. **PlayerController** (module)
   → event **PrimaryActionExecuted**
2. **ScoreSystem** (subscriber)
   → class dep **RunEndSystem**
3. **RunEndSystem** (module)
```

6. List **files to inspect** along the path (union of module.scripts on path nodes).

---

## No path found

- Report no structural path in index (may still exist via scene/prefab wiring).
- Suggest Unity export + `/context-index` refresh.
- Suggest grep for string references as fallback.

---

## Use cases

| Scenario | Action |
|----------|--------|
| Primary action → run end | `/context-path PlayerController RunEndSystem` |
| HUD decoupling audit | `/context-path GameplayHud RunEndSystem` |
| EventBus refactor | `/context-path <any> EventBus` |

---

## Rules

1. Path query is **read-only** — no file edits.
2. Prefer this over reading all intermediate systems during refactors.
3. Cross-check with `CONTEXT.md` mermaid; report mismatches in output.
