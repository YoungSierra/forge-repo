# Unity Context Graph (`/context-graph`)

Regenerates **`project-index.json`** (optional skip) and writes an interactive **HTML** viewer for the Context Intelligence graph, then opens it in the default browser.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-graph` · `/context-graph --skip-index` · `/context-graph --no-open` |
| **Script** | `V57/tools/export-context-graph.ps1` |
| **Template** | `V57/tools/viewer/context-graph-template.html` |
| **Guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Requires** | Consumer Unity project with `Assets/_Game/Scripts` (indexer convention) + .NET SDK 8+ |

---

## What it produces

| Output | Path |
|--------|------|
| Index | `project-index.json` (unless `--skip-index`) |
| Viewer | `Docs/V57/reports/context-graph-viewer/index.html` |
| Index copy | `Docs/V57/reports/context-graph-viewer/project-index.json` |

The HTML embeds the real index (no sample data). Layers: modules/`dependsOn`, interfaces, events, optional assets.

---

## Pre-flight

| Check | Action |
|-------|--------|
| Consumer project | `Assets/` at workspace root |
| Scripts | Prefer `Assets/_Game/Scripts/**/*.cs` (what the Roslyn indexer scans) |
| Tools | `V57/tools/index-project.ps1` + `V57/tools/viewer/context-graph-template.html` |
| .NET | `dotnet --version` ≥ 8 |
| Unity assets (optional) | **V57 → Export Unity Assets Index** for prefabs/SO/scenes |

---

## Workflow (agent)

1. Confirm Unity project root (sibling of `V57/`, contains `Assets/`).
2. From that root, run:

```powershell
./V57/tools/export-context-graph.ps1
```

Flags:

| Flag | Effect |
|------|--------|
| *(none)* | Re-run indexer, write HTML, open browser |
| `-SkipIndex` | Use existing `project-index.json` only |
| `-NoOpen` | Write HTML but do not launch browser |
| `-Drift` | Pass through to `index-project.ps1` |
| `-ProposeContext` | Pass through (CONTEXT patch still confirm-gated) |
| `-SkipUnityCheck` | Suppress missing `v57-unity-assets.json` warning |

3. Report paths + module/interface/event counts + `generatedAt`.
4. If indexer returns 0 modules: stop and explain (missing `Assets/_Game/Scripts`, specs without `name:`, or need `/context-index` diagnostics).

### Equivalent slash forms

| User says | Agent runs |
|-----------|------------|
| `/context-graph` | `export-context-graph.ps1` |
| `/context-graph --skip-index` | `-SkipIndex` |
| `/context-graph --no-open` | `-NoOpen` |
| `/context-graph --drift` | `-Drift` |

---

## When to run

- After a batch of features when exploring architecture visually
- Onboarding / reviews (see module deps without reading all specs)
- After `/context-index` if you only want a fresh visual (`--skip-index` if index is already current)

---

## Relation to other context commands

| Command | Role |
|---------|------|
| `/context-index` | Index only (JSON + optional drift/CONTEXT patch) |
| `/context-graph` | Index (default) + **HTML viewer** + open browser |
| `/context-query` | Text subgraph for the **active task** (token savings) |
| `/context-path` | Shortest dependency path between two modules |

`/context-graph` is for **human visualization**. It does not replace `/context-query` during implement.

---

## Rules

1. Never invent graph data — only embed `project-index.json` from disk.
2. Overwriting `Docs/V57/reports/context-graph-viewer/` is OK (generated artifact; regenerate after pack updates).
3. Do not ship a pre-baked consumer HTML inside the pack template — only the template under `V57/tools/viewer/`.
4. Indexer scans **`Assets/_Game/Scripts` only** — prototype-only trees under `Assets/Prototypes` need a production Scripts layout (or a one-off mirror) before a useful graph.
