# Unity CLI Ops Skill (`/unity-cli`)

Operational playbook for the **Unity CLI** + Pipeline in GameForge. Complements `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md`. Adapted from [unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) `unity-cli` (subset used by V57 gates).

| Item | Detail |
|------|--------|
| **enforcement** | `agents.yaml` → `rules.unity_cli` + `rules.scene_authoring` |
| **writes** | Prefer `unity command` / `eval` for scene/GO/asset mutations |
| **verify / tests** | Prefer this skill’s CLI commands (`unity test` for gates) |
| **forbidden** | Product `Scripts/Editor/*Authoring` + `MenuItem` as scene builder; human Hierarchy checklists as primary path |

---

## Invocation

```bash
/unity-cli preflight
/unity-cli status
/unity-cli doctor
/unity-cli eval "return Application.unityVersion;"
/unity-cli save
/unity-cli recompile
/unity-cli play-smoke
/unity-cli test --mode EditMode
```

---

## Always do first

```bash
unity status --format json --project-path <repo-root>
unity pipeline list --format json --project-path <repo-root>
```

- State **ready** → drive Editor with `unity command …` (writes **and** verify).
- **Safe Mode** → fix compile errors on disk, restart Editor; do not blind-edit `.unity` YAML.
- Pass `--project-path` when multiple Editors may be open.
- Prefer `--format json` for agent parsing.

### `eval` rules

Statement block only: **no `using`**, fully qualify types (`UnityEngine.GameObject`, `UnityEditor.AssetDatabase`).

---

## V57 command map

| Need | Command |
|------|---------|
| Scene open / create | `unity command open_scene` / `create_scene` |
| Hierarchy / GOs | `unity command create_gameobject` / `set_parent` / `batch` / `set_serialized_field` |
| Hierarchy / OVR | `unity command get_scene_hierarchy` / `find_gameobjects` / `eval` |
| Save | `unity command save_all` |
| Compile | `unity command recompile` → poll `recompile_status` |
| Play smoke | `unity command editor_play --timeout 60` |
| Screenshot | `unity command screenshot` |
| Tests | `unity test <root> --mode EditMode\|PlayMode --output Docs/V57/evidence/<stage>/<UTC>/…` (reports: `Docs/V57/reports/`) |
| Optional MCP stdio | `unity mcp configure cursor --local` (setup docs) |
| Discover tools | `unity command` / `unity list --format json` |

Sidecar: `GET /api/unity-cli/preflight`, `POST /api/unity-cli/eval|command|test`.

---

## Outside V57 gates (use when needed)

Install/auth/project create/build CI: follow upstream `unity-cli` references (editors, auth, `unity build`, `unity doctor`). Do **not** replace `/game-setup` with `new-unity-project`.

Never hand-edit `.unity` / `.prefab` / `.asset` while a live Editor is reachable — use Pipeline commands (`unity command` / optional `unity mcp`).

Never add product-side Editor MenuItem authoring scripts to mount the play scene; use CLI Operate or `com.v57.assembly` (`AssemblyRunner`). Runtime scene builders/directors are **banned** (see `V57/knowledge/runtime-bootstrap.md`).

---

## Integration

Every skill’s preflight / OVR / `/compile-heal` / `/playability-cert` / Stage E tests. Full policy: `UNITY_CLI_INTEGRATIONS.md`.

---

*Last updated: 2026-09-22*
