# Unity Context Readiness (`/context-readiness`)

Lightweight **read-only audit** for when to adopt **Phase 2 Context Intelligence**. Does **not** compile or run `v57-index`.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-readiness` |
| **Tool** | `V57/tools/check-context-readiness.ps1` |
| **Guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Operator guide** | `V57/docs/context/CONTEXT_INDEX.md` |

---

## Pre-flight

| Check | Action |
|-------|--------|
| V57 pack | `V57/tools/check-context-readiness.ps1` exists |
| Consumer Unity | `Assets/` at workspace root — full metrics |
| Template only | No `Assets/` — report is orientative (spec counts only) |

No .NET SDK required.

---

## Workflow

From Unity project root (PowerShell):

```powershell
./V57/tools/check-context-readiness.ps1 -WriteReport
```

Or without writing file:

```powershell
./V57/tools/check-context-readiness.ps1
```

1. Run the script.
2. Present the markdown output in chat.
3. Interpret status and suggest **one** next step — do not auto-run `/context-index`.

---

## Status interpretation

| Status | Meaning | Typical action |
|--------|---------|----------------|
| **Green** | Phase 1 sufficient | None; keep CONTEXT + specs + touches |
| **Yellow** | Consider Phase 2 | Complete `specId`/`touches`; optional `/context-drift` |
| **Red** | Phase 2 recommended | Export Unity assets + `/context-index` (user confirms) |

### Example scenarios

| Scripts | Specs | Typical status |
|---------|-------|----------------|
| 10 | 5 | Green |
| 20 | 20 | Yellow (many specs, little code) |
| 100 | 20 | Red (specs + code combo) |
| 160 | 8 | Red (script threshold) |

Yellow with many specs but few `.cs` files is **expected** — recommend drift/touches first, not full index unless user agrees.

---

## When to run

- User asks “should I use context-index?”
- Before activating Phase 2 (`/context-index`)
- After `/spec implementa` sign-off (Gate 8 advisory — consumer projects)
- `/game-setup --status` or Stage CR (optional)

---

## Failure handling

| Symptom | Fix |
|---------|-----|
| Script not found | Copy/update `V57/` from `Base_Unity_V57` |
| Execution policy | `Set-ExecutionPolicy -Scope Process Bypass` or run via `powershell -File` |

---

## Rules

1. **Recommend only** — never block SDD, bootstrap, or implement.
2. **Do not** run `/context-index` without explicit user confirmation.
3. Run `/context-readiness` **before** suggesting `/context-index` when thresholds are unclear.
4. Red exit code (`1`) is informational for CI; do not treat as agent STOP.
