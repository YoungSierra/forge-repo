# Unity MCP — installation (official Unity AI Assistant)

> **Deprecated path (end 2026):** Unity deprecates the in-Editor MCP server in this package. **Use [Unity CLI MCP](UNITY_CLI_MIGRATION.md) instead** (`unity mcp` + `com.unity.pipeline`). This page documents the legacy relay setup for rollback only.

V57 previously required **Unity’s official MCP** from **`com.unity.ai.assistant`**. New projects should follow [`UNITY_CLI_MIGRATION.md`](UNITY_CLI_MIGRATION.md).

| Component | Where it lives |
|-----------|----------------|
| **Unity package** | `com.unity.ai.assistant` in the consumer project `Packages/manifest.json` (Package Manager or **Edit → Project Settings → AI → Unity MCP**) |
| **Editor bridge** | **Edit → Project Settings → AI → Unity MCP** — status **Running** (Start if Stopped) |
| **MCP relay (IDE)** | `~/.unity/relay/` — installed when Unity runs; AI clients invoke the relay with **`--mcp`** |
| **Cursor / SDK config** | Project `.cursor/mcp.json` (or global MCP config) — server id **`unity-mcp`** pointing at the relay, **not** a path under `Tools/` |

Official overview: [Unity MCP documentation](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-overview.html).

**Prerequisites (Unity side):** Unity 6 (6000.0+) with AI Assistant, Unity Cloud–connected project, and Unity AI subscription/trial per Unity’s current policy.

---

## Flow A — New Unity project + V57 pack

1. Copy **`V57/`** next to `Assets/` (not inside `Assets/`).
2. Add **`com.unity.ai.assistant`** to the Unity project (Package Manager or enable via **Project Settings → AI → Unity MCP**).
3. Open the project in Unity → **Edit → Project Settings → AI → Unity MCP** → bridge **Running**.
4. Configure the IDE:
   - **Preferred:** On the Unity MCP settings page, expand **Integrations** → select **Cursor** (or your client) → **Configure**.
   - **Manual:** Add a `unity-mcp` server entry using the platform relay path below and `"args": ["--mcp"]`.
5. Approve the first connection in **Project Settings → AI → Unity MCP** when Unity shows a pending client.
6. **Commit** the game repo: `Assets/`, `ProjectSettings/`, `Packages/` (including manifest + lock), `V57/`, and optionally a team template — see `.cursor/mcp.json` note below.

**Do not** commit `Packages/unity-mcp-plugin`, `Tools/unity-mcp-server`, or run legacy deploy scripts — they are not part of V57.

---

## Flow B — Clone an existing game repo

1. `git clone …` and open the Unity project.
2. Ensure **`com.unity.ai.assistant`** is in `Packages/manifest.json` (Unity resolves packages on open).
3. Open Unity → confirm bridge **Running**.
4. On each developer machine: ensure `.cursor/mcp.json` (or global MCP config) has **`unity-mcp`** → relay + `--mcp`. Use Unity **Integrations → Configure** or copy from `V57/templates/mcp.json.template`.
5. Enable required built-in tools in **Project Settings → AI → Unity MCP** (tool list toggles). Many tools are off by default; enable scene, script, and console tools your workflow needs.

**Do not** invoke `/game-setup` SETUP only because you cloned.

---

## Flow C — Upgrade Unity / AI Assistant

1. Update **`com.unity.ai.assistant`** in Package Manager.
2. Restart Unity; confirm bridge **Running** and re-approve clients if prompted.
3. Refresh IDE MCP config if Unity changes relay paths (re-run **Integrations → Configure**).

---

## Manual `.cursor/mcp.json` (template)

See `V57/templates/mcp.json.template`. Relay paths by platform (Unity blog, 2026):

| OS | Relay executable |
|----|------------------|
| Windows | `%USERPROFILE%\.unity\relay\relay_win.exe` |
| macOS (Apple Silicon) | `~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64` |
| macOS (Intel) | `~/.unity/relay/relay_mac_x64.app/Contents/MacOS/relay_mac_x64` |
| Linux | `~/.unity/relay/relay_linux` |

Example (Windows — adjust path):

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "C:\\Users\\YOU\\.unity\\relay\\relay_win.exe",
      "args": ["--mcp"]
    }
  }
}
```

**Git:** `.cursor/mcp.json` is often machine-specific (absolute relay path). Prefer Unity auto-configure per developer, or commit a template with placeholders and document substitution.

---

## Troubleshooting

| Symptom | Action |
|---------|--------|
| MCP missing in Cursor | Settings → MCP → enable **unity-mcp**; verify relay path and `--mcp` |
| Bridge stopped | Unity open on this project; **Project Settings → AI → Unity MCP → Start** |
| Pending connection | Accept client in Unity MCP settings |
| Agent sees no Unity tools | Bridge Running; client approved; refresh MCP in IDE |
| Tool call fails “disabled” | Enable tool in **Project Settings → AI → Unity MCP** tool list |
| Compile errors after script edits | `Unity.ReadConsole` (Errors) or `Unity.ValidateScript`; fix and re-check |

Full operational policy: [`UNITY_MCP_REQUIREMENT.md`](UNITY_MCP_REQUIREMENT.md).

---

*Last updated: 2026-07-22*
