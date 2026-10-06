# Unity Editor connectivity (CLI primary + optional `unity mcp`)

**Policy:** All SDD work that touches the Unity project must have the **Unity Editor open** on this project with **`com.unity.pipeline`** reachable via **Unity CLI** (`unity pipeline list` / `unity command`).

**Terminology (do not conflate):**

| Name | What it is |
|------|------------|
| **Unity CLI + Pipeline** | Primary — `unity command`, `eval`, `test`. Agent drives the live Editor here. |
| **`unity mcp`** | Optional Cursor stdio binding to the **same** Pipeline (`unity mcp configure cursor`) |
| **Legacy assistant MCP** | Deprecated relay in `com.unity.ai.assistant` — fallback until end 2026 only |

**Split responsibility:**

| Layer | Use for | Doc |
|-------|---------|-----|
| **Unity CLI Pipeline** | Pre-flight, **scene/GO/asset writes**, OVR read-back, compile poll, save, **`unity test`**, play smoke, screenshots | [`UNITY_CLI_INTEGRATIONS.md`](UNITY_CLI_INTEGRATIONS.md) |
| **`unity mcp` stdio** | Same Pipeline tools when the agent prefers MCP tool calls in the IDE | this doc |
| **Legacy relay** | Only if `GAMEFORGE_UNITY_MCP_MODE=legacy` | [`UNITY_CLI_MIGRATION.md`](UNITY_CLI_MIGRATION.md) |

If **CLI/Pipeline preflight** fails → fix and retry twice, then use the alternate route (batch `-executeMethod`); inside `/vertical-slice` / `/game-setup` this is a timeboxed problem (45 min → log `D-###`), not a pipeline stop condition. The per-row "STOP" below means "do not continue Editor work until fixed". **Do not** ask the human to mount Hierarchy by hand. **Do not** blind-edit `.unity` YAML while an Editor should be reachable.

Installation: [`UNITY_CLI_MIGRATION.md`](UNITY_CLI_MIGRATION.md) (primary) · [`UNITY_MCP_SETUP.md`](UNITY_MCP_SETUP.md) (legacy).

---

## Installation (consumer projects)

| Component | Detail |
|-----------|--------|
| **Pipeline package** | `com.unity.pipeline` in `Packages/manifest.json` (`unity pipeline install`) |
| **CLI** | `unity` on PATH (or `UNITY_CLI_PATH`) |
| **Optional CLI MCP** | `.cursor/mcp.json` — `unity` + `mcp` (see `V57/templates/mcp.json.template`) |
| **Editor** | Unity 6+ open on **this** project |
| **Legacy bridge** | `com.unity.ai.assistant` + relay — only when `GAMEFORGE_UNITY_MCP_MODE=legacy` |

V57 does **not** deploy `Packages/unity-mcp-plugin`, `Tools/unity-mcp-server`, or `install-mcp-stack.ps1`.

---

## Pre-flight (required at the start of each session / command)

Before `/spec --bootstrap`, `/spec implementa`, or scene assembly:

| Step | Action | On failure |
|------|--------|------------|
| 1 | **CLI:** `unity pipeline list --format json` or `GET /api/unity-cli/preflight` — not Safe Mode; Pipeline reachable | STOP — fix compile errors, restart Editor |
| 2 | **Liveness:** `unity command eval "return Application.unityVersion;"` | STOP |
| 3 | **Optional:** Workbench **Test MCP** / `POST /api/mcp/ping` → `ok: true` if using Cursor MCP tools | Prefer CLI if MCP stdio down |
| 4 | **Compile:** `/compile-heal` or CLI `recompile_status` — zero blocking CS errors | STOP |
| 5 | Confirm `Assets/` and `ProjectSettings/` exist | STOP — not a consumer Unity project |

**Rule:** Without pre-flight PASS, **do not** write gameplay `.cs`, **do not** create assets, **do not** mark bootstrap/implement as done.

---

## How the agent mounts scenes (allowed)

Drive the **live Editor** with Pipeline commands, for example:

| Need | Prefer |
|------|--------|
| Open / create scene | `unity command open_scene` / `create_scene` |
| Hierarchy / GOs | `unity command create_gameobject`, `set_parent`, `set_serialized_field`, `batch` |
| One-off C# mutate | `unity command eval` |
| Save | `unity command save_all` |
| Optional IDE tools | `unity mcp` equivalents of the same Pipeline commands |

**Forbidden as the product authoring path:**

- Shipping `Assets/**/Scripts/Editor/*Authoring.cs` (or `.Authoring/`) with `[MenuItem]` whose job is to build the play scene
- Human “open Editor and wire these objects” checklists as the primary path when CLI is up
- Blind hand-edits of `.unity` / `.prefab` YAML while Pipeline is reachable

**Banned product code:** runtime directors / builders that create, find, reparent or patch scene objects on Play/Awake. A runtime bootstrap may only bind serialized references and call init in order (`V57/knowledge/runtime-bootstrap.md`). Scene content is authored in Edit Mode by CLI Operate or `com.v57.assembly` (`AssemblyRunner`).

### Prefer CLI for gates (see `UNITY_CLI_INTEGRATIONS.md`)

| Task | CLI |
|------|-----|
| OVR read-back / hierarchy | `unity command eval`, `get_scene_hierarchy`, `find_gameobjects` |
| Compile poll | `unity command recompile`, `recompile_status` |
| Save dirty scenes | `unity command save_all` |
| **EditMode / PlayMode tests** | **`unity test`** or `POST /api/unity-cli/test` |
| Play smoke | `unity command editor_play` |
| Screenshot | `unity command screenshot` |

### What the agent must do (not the user in the Inspector)

| Category | Examples |
|----------|----------|
| **Bootstrap** | `GameSystems`, tags/layers, input actions |
| **ScriptableObjects** | `.asset` from spec `.cs` types |
| **UI Toolkit** | UXML/USS, `UIDocument`, wiring |
| **Scene** | GOs, camera, references between systems — via CLI/Pipeline |
| **Validation read-back** | **CLI** OVR per `unity-editor-verification-skill.md` |
| **Tests execution** | **CLI** `unity test` |
| **OVR writes** | `/scene-setup`, `/prefab`, `/data-asset` per skills |

**Types** (`.cs`) may be generated in the workspace; **instances** (`.asset`), the **scene**, and **wiring** are agent+CLI responsibility.

---

## From-scratch flow

1. Copy **`V57/`** next to `Assets/`.
2. `unity pipeline install`; optionally `unity mcp configure cursor` — see `/game-setup` Stage SETUP.
3. Commit `V57/`, `Packages/`, `ProjectSettings/`, `Assets/` as appropriate.
4. CLI preflight → TDD/spec/implement pipeline.

---

## Reports

| Document | Fields |
|----------|--------|
| `Docs/V57/reports/bootstrap-report.md` | CLI/Pipeline execution (PASS/FAIL); optional MCP stdio |
| `Docs/V57/reports/implement-report-{feature}.md` | Compile, **CLI test XML paths**, assets, OVR |
| `Docs/V57/reports/scene-setup-report-{slug}.md` | OVR structural + production invariants |

---

## Legacy mapping (assistant / AB names)

| Former | Use instead |
|--------|-------------|
| `unity_execute_code` | **`unity command eval`** |
| `unity_console_log` / compile errors | CLI eval / console tools |
| `unity_scene_*` writes | `unity command` scene/GO commands |
| Test Runner MCP | **`unity test`** |
| Product `MenuItem` Authoring | CLI Operate or `AssemblyRunner` (never a runtime director) |

See [`UNITY_CLI_MIGRATION.md`](UNITY_CLI_MIGRATION.md).

---

*Last updated: 2026-09-22*
