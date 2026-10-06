# Unity Context Index (`/context-index`)

Regenerates **`project-index.json`** at the Unity project root and optionally proposes a **confirm-gated** patch for the AUTO block in `CONTEXT.md`.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-index` or `/context-index --drift` |
| **Guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Operator guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Confirm-gating** | Never merge `context-auto-patch.md` into `CONTEXT.md` without user `yes` |

---

## Pre-flight

| Check | Action |
|-------|--------|
| Consumer Unity project | `Assets/` exists at workspace root |
| Tool present | `V57/tools/roslyn-index/V57.RoslynIndex.csproj` |
| .NET SDK | `dotnet --version` ≥ 8.0 |
| Unity assets (recommended) | Run **V57 → Export Unity Assets Index** in Editor first |

If Unity Editor is open, prefer exporting `v57-unity-assets.json` before indexing (prefabs/SO/scenes).

---

## Workflow

### Step 1 — Export Unity assets (when Editor available)

1. Ensure `Assets/Editor/V57/V57IndexExporter.cs` exists (copy from `V57/tools/unity-export/Editor/` if missing).
2. Unity menu: **V57 → Export Unity Assets Index**
3. Confirms `v57-unity-assets.json` at project root.

Or via Unity MCP `Unity.RunCommand` executing the menu item if supported.

### Step 2 — Run indexer

From Unity project root (PowerShell):

```powershell
./V57/tools/index-project.ps1
```

With drift + CONTEXT proposal:

```powershell
./V57/tools/index-project.ps1 -ProposeContext -Drift
```

Equivalent dotnet:

```powershell
dotnet run --project V57/tools/roslyn-index --configuration Release -- `
  --root . `
  --output ./project-index.json `
  --propose-context `
  --drift
```

### Step 3 — Outputs

| Output | Path |
|--------|------|
| Index | `project-index.json` |
| CONTEXT patch proposal | `Docs/V57/reports/context-auto-patch.md` |
| Drift report | `Docs/V57/reports/drift-report.md` |

### Step 4 — Apply CONTEXT patch (confirm-gated)

1. Present `context-auto-patch.md` to the user.
2. Wait for explicit **`yes`**.
3. Insert/replace block between `<!-- v57:auto-index:start -->` and `<!-- v57:auto-index:end -->` in root `CONTEXT.md`.
4. Never edit content outside markers automatically.

---

## When to run

- After implementing a feature (`/spec implement` sign-off)
- Before large refactors
- Weekly on active branches (optional CI in consumer repo — run `index-project.ps1 -Drift`; see `V57/docs/context/CONTEXT_INDEX.md` § CI)

---

## Failure handling

| Symptom | Fix |
|---------|-----|
| `dotnet` not found | Install .NET 8 SDK |
| No modules in index | Ensure `V57/specs/**/*.yaml` exist with `name:` field |
| Empty assets | Run Unity export menu |
| Drift: missing touches | Add `specId` + `touches` to specs (template v1.1) |

---

## Rules

1. **Propose** index write; default overwrite `project-index.json` is OK (generated artifact).
2. **Confirm-gate** any `CONTEXT.md` merge from auto patch.
3. Do not index `Assets/Art/**` binary content — tools already filter to Scripts + export paths.
