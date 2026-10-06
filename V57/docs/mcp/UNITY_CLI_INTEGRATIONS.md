# Unity CLI integrations (GameForge sidecar)

**Single rule:** **Unity CLI + Pipeline** (`unity command` / `eval` / `test`) drives the **live Editor** for **writes and verify**. Optional **`unity mcp`** is only a Cursor stdio binding to the same Pipeline — not a separate write stack. Legacy `com.unity.ai.assistant` relay is fallback until end 2026.

Do not use Test Runner MCP when `agents.yaml` → `run_unity_test_runner_via_cli: true`.

Pipeline commands and `unity test` target the warm GUI Editor (`command` / `eval`) or a **separate batch process** (`unity test`).

Official reference: [Unity CLI](https://docs.unity.com/en-us/unity-cli/unity-cli). Setup: `UNITY_CLI_MIGRATION.md`. Connectivity / optional stdio: `UNITY_MCP_REQUIREMENT.md`.

---

## When to use what

| Task | Prefer | Fallback |
|------|--------|----------|
| Scene / GO / asset **writes** | `unity command` (`open_scene`, `create_gameobject`, `batch`, `set_serialized_field`, `eval`, …) | Optional `unity mcp` tools over Pipeline; legacy assistant MCP only if `GAMEFORGE_UNITY_MCP_MODE=legacy` |
| OVR read-back, hierarchy | `unity command eval` / `get_scene_hierarchy` / `find_gameobjects` | — |
| Save before Play | `unity command save_all` | — |
| Compile poll after C# write | `unity command recompile` + `recompile_status` | — |
| Compilation error list | `unity command eval` (CompilationPipeline / console) | Console MCP tools if CLI eval unavailable |
| Editor preflight (Safe Mode?) | `unity pipeline list --format json` | — |
| EditMode / PlayMode tests | **`unity test`** (batch Editor) | **Never** Test Runner MCP for gates |
| Play smoke | `unity command editor_play` (+ screenshot) | — |
| Viewport capture | `unity command screenshot` (Play Mode, end-of-frame Game View so UI Toolkit overlays are included) | — |

**Rule:** Prefer CLI/Pipeline for **everything** the agent does in the Editor. `unity mcp` is optional IDE sugar. **Do not** ask the human to mount Hierarchy by hand. **Do not** ship product `Scripts/Editor/*Authoring` + `MenuItem` as the scene builder — use CLI Operate or `com.v57.assembly` (`AssemblyRunner`). Runtime scene builders/directors are banned (`V57/knowledge/runtime-bootstrap.md`).

---

## Sidecar HTTP API

Base: `http://127.0.0.1:3857` (same token as other `/api/*` routes).

| Route | Method | Purpose |
|-------|--------|---------|
| `/api/unity-cli/preflight` | GET | CLI + pipeline + editor reachable + eval probe |
| `/api/unity-cli/eval` | POST | `{ "code": "return Application.unityVersion;" }` |
| `/api/unity-cli/command` | POST | `{ "name": "save_all", "args": [] }` |
| `/api/unity-cli/test` | POST | `{ "mode": "EditMode", "filter": "MyTests" }` |

Shell equivalents: `unity command …`, `unity test …`, `unity pipeline list`.

---

## Shell examples

```bash
unity pipeline list --format json --project-path <repo-root>
unity command open_scene --path Assets/_Game/Scenes/Scene_Baked.unity --project-path <repo-root>
unity command recompile --timeout 120 --project-path <repo-root>
unity command recompile_status --timeout 30 --project-path <repo-root>
unity command editor_play --project-path <repo-root> --timeout 60
unity test <repo-root> --mode EditMode --output Docs/V57/reports/editmode.xml
```

---

## Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `GAMEFORGE_UNITY_CLI` | `1` | `0` disables sidecar CLI helpers |
| `GAMEFORGE_UNITY_TEST_VIA_CLI` | `1` | `0` = legacy Test Runner MCP (discouraged; gates expect CLI) |
| `UNITY_CLI_PATH` | — | Full path to `unity` if not on PATH |
| `UNITY_PROJECT_PATH` | project root | Disambiguate multi-Editor |

---

## Skills

| Skill | Role |
|-------|------|
| `/unity-cli` | Playbook: status, Safe Mode, eval rules, command map — `shared/helpers/unity-cli-ops-skill.md` |
| `/compile-heal` | `recompile` + `recompile_status` |
| `/editor-smooth` | `save_all`; compile poll via `recompile_status` |
| `/scene-setup` | Operate via CLI/Pipeline; Verify via CLI |
| `/playability-cert` | preflight CLI; `editor_play` optional |
| `/editor-search` | optional `eval` to open Search window |

`agents.yaml` → `rules.unity_cli`, `rules.scene_authoring`, `implementation_done_gates.on_spec_implement.run_unity_test_runner_via_cli`.

---

## Multi-Editor

When more than one Editor may be running, every `unity command` / `status` / `pipeline` call must include `--project-path <repo-root>` (or set `UNITY_PROJECT_PATH`).

---

## Safe Mode

When compile errors block package load, Pipeline is **off**. `unity pipeline list` reports Safe Mode; fix C# on disk, restart Editor, then retry CLI. Do **not** fall back to hand-editing scenes or asking the user to click through Hierarchy while Safe Mode is the only blocker.

---

## Upstream CLI (outside V57 gates)

Install editors, auth/license, `unity projects create`, `unity build`, `unity doctor`, UVCS: use upstream [unity-cli skill](https://github.com/Unity-Technologies/unity-agent-plugin/tree/main/skills/unity-cli) references. V57 product bootstrap remains `/game-setup`, not `new-unity-project`.

---

*Last updated: 2026-09-22*
