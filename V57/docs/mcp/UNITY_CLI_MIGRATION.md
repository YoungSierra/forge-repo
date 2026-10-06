# Unity CLI MCP — migration guide

*Replaces the deprecated in-Editor MCP server in `com.unity.ai.assistant` (supported until end 2026).*

Official references:

- [Unity CLI](https://docs.unity.com/en-us/unity-cli/unity-cli)
- [Replace in-Editor MCP with CLI](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
- [Unity Skills](https://github.com/Unity-Technologies/skills)
- [Announcement thread](https://discussions.unity.com/t/announcing-the-unity-cli-a-new-way-to-connect-your-tools-and-agents/1731104)

---

## Why migrate

| Legacy (deprecated) | Unity CLI |
|---------------------|-----------|
| Relay `~/.unity/relay/* --mcp` | `unity mcp` stdio |
| Bridge in Project Settings → AI → Unity MCP | `com.unity.pipeline` local server |
| Client approval / Capacity limit | CLI + Editor open (no relay approval) |
| Editor-only, slow domain reloads | Roslyn eval; optional runtime targeting |

GameForge sidecar ([`Tools/gameforge-agent`](../../Tools/gameforge-agent)) prefers **CLI first**, falls back to legacy relay when `GAMEFORGE_UNITY_MCP_MODE=legacy` or CLI is unavailable.

---

## One-time setup (per machine)

```powershell
unity --version
cd <project-root>
unity pipeline install
unity mcp configure cursor
unity skill install cursor
```

Requirements:

- Unity **6.0+** Editor
- Unity CLI on PATH (or set `UNITY_CLI_PATH`)
- Editor **open** on this project when agents use MCP

After `pipeline install`, confirm `com.unity.pipeline` in `Packages/manifest.json`.

---

## MCP client config

`unity mcp configure cursor` writes `.cursor/mcp.json` (or updates global config) with a server using `unity` + `mcp`.

Manual template: [`V57/templates/mcp.json.template`](../templates/mcp.json.template).

GameForge also accepts legacy `unity-mcp` → relay entries for backward compatibility.

### Environment variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `GAMEFORGE_UNITY_MCP` | `1` | `0` disables Forge Chat MCP (let Cursor hold MCP alone) |
| `GAMEFORGE_UNITY_MCP_MODE` | `auto` | `cli` \| `legacy` \| `auto` (CLI first) |
| `UNITY_CLI_PATH` | — | Full path to `unity` executable if not on PATH |
| `UNITY_PROJECT_PATH` | project root | Passed to CLI MCP when auto-configuring |

---

## Verify

1. Unity Editor **open on this project** (not another clone/worktree).
2. `unity pipeline list` → this project path, `Pipeline: true`, `Server Reachable: true`.
3. Workbench → **Test MCP** → `ok: true`, `mode: "cli"`, `toolCount` > 0, `pipelineInstalled: true`.
4. Sidecar health: `GET http://127.0.0.1:3857/api/health` → `unityCliAvailable: true`, `unityPipelineInstalled: true`, `unityMcpMode: "cli"`, `unityCliIntegrationEnabled: true`.
5. CLI preflight: `GET http://127.0.0.1:3857/api/unity-cli/preflight` → `ok: true` (or `unity pipeline list --format json`).

If Test MCP returns `toolCount: 0` with `mode: "cli"`, the CLI stdio server connected but the Editor/Pipeline is not serving tools — open the correct project and wait for domain reload.

---

## Tool mapping (CLI MCP — confirmed tool ids)

OpenAI aliases use dots → underscores (`Unity.RunCommand` → `Unity_RunCommand`).

| Task | Legacy (relay) | CLI MCP tool id |
|------|----------------|-----------------|
| Console errors | `Unity.ReadConsole`, `Unity.GetConsoleLogs` | `Unity_GetConsoleLogs` |
| C# eval / read-back | `Unity.RunCommand` | `Unity_RunCommand` |
| Viewport capture | `Unity.SceneView.Capture2DScene` | `Unity_SceneView_Capture2DScene` |
| Multi-angle capture | — | `Unity_SceneView_CaptureMultiAngleSceneView` |
| Camera capture | `Unity.Camera.Capture` | `Unity_Camera_Capture` |
| AI asset generation | `Unity.AssetGeneration.*` | `Unity_AssetGeneration_GenerateAsset`, `Unity_AssetGeneration_GetModels` |
| Scenes / GameObjects | `Unity.ManageScene`, `Unity.ManageGameObject` | **Verify:** `unity command eval` / `get_scene_hierarchy`. **Write:** MCP or Pipeline commands — see `UNITY_CLI_INTEGRATIONS.md` |
| Tests (Stage E) | Test Runner MCP | **`unity test`** (batch Editor; sidecar `POST /api/unity-cli/test`) |
| Compile poll / save | `Unity_RunCommand` | `unity command recompile` / `recompile_status` / `save_all` |
| Play smoke | MCP Play | `unity command editor_play` |
| Screenshot | MCP capture tools | `unity command screenshot` |

Full routing table: [`UNITY_CLI_INTEGRATIONS.md`](UNITY_CLI_INTEGRATIONS.md).

Update prompts/skills if Unity adds or renames tools in a future pipeline version.

---

## Rollback to legacy

```powershell
$env:GAMEFORGE_UNITY_MCP_MODE = "legacy"
```

Ensure relay exists, bridge **Running**, Allow «GameForge Chat». Not recommended long-term.

---

## Branch workflow

**Active integration branch:** `feature/unity-cli-integrations` (CLI command helpers + skill routing).

`feature/unity-cli-mcp` was merged to `main` and deleted — transport migration is on `main`.

```powershell
git fetch origin
git checkout feature/unity-cli-integrations
# validate: unity pipeline list, Test MCP, GET /api/unity-cli/preflight
```

Merge to `main` when preflight + smoke tests pass in Unity Workbench.

---

*Last updated: 2026-08-31*
