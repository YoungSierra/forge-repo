# Unity Context Drift (`/context-drift`)

Compares **spec `touches`** vs filesystem vs **`project-index.json`** and writes `Docs/V57/reports/drift-report.md`.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-drift` |
| **Tool** | `v57-index --drift` via `V57/tools/index-project.ps1 -Drift` |

---

## Workflow

1. Ensure index exists or regenerate:

```powershell
./V57/tools/index-project.ps1 -Drift
```

2. Read `Docs/V57/reports/drift-report.md`.
3. Present summary to user:

| Category | Meaning |
|----------|---------|
| Missing touch paths | Spec lists file that does not exist — fix spec or implement file |
| Legacy specs | Missing `specId` or empty `touches` — migrate to template v1.1 |
| Index without spec | Class cluster not linked to YAML — add spec or touches |

4. Propose spec fixes (confirm-gated before writing YAML).
5. After fixes, re-run `/context-index`.

---

## Auto-fix guidance (propose only)

For legacy specs, infer initial `touches.scripts` from:

- `components[].files[].path`
- `implementation.files[].path`
- `implementation.tests[].path` → `touches.tests`

Set `specId` from snake_case of `name:`.

Bump `specVersion` to `"1.1"` when adding touches.

---

## Integration with DoD

Before feature sign-off:

- [ ] Spec `touches` match implemented files
- [ ] `/context-drift` shows no MISSING for that spec
- [ ] Optional: `/context-index -ProposeContext` after milestone

---

## Rules

1. Drift fixes to specs → **confirm-gated**.
2. Do not delete modules from index manually — fix specs and re-index.
3. Attach drift summary to implement reports when drift blocked merge.
