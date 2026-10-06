# V57 GameForge — Unity generate (Prototype / Vertical Slice / Production)

Write roots, forge-mode rules and the quality bar are stated once in the blocks
around this one — follow them there; this block covers delivery only.

## Unity MCP (writes) + Unity CLI (verify / tests)

GameForge connects via **Unity CLI MCP** (`unity mcp` + `com.unity.pipeline`). See `V57/docs/mcp/UNITY_CLI_MIGRATION.md` and **`UNITY_CLI_INTEGRATIONS.md`**.

| Task | Tool |
|------|------|
| Scene/asset **writes** | Unity MCP (`open_scene`, `create_gameobject`, `add_component`, `set_transform`, `eval`, …) |
| OVR read-back, compile poll, save | **`unity command`** (`eval`, `recompile`, `save_all`, …) |
| **EditMode / PlayMode tests** | **`unity test`** or `POST /api/unity-cli/test` — **never** Test Runner MCP |
| Play smoke / screenshot | `unity command editor_play`, `screenshot` |

1. Write scripts + materials/prefabs under the mode root.
2. Mount the product scene **once** with organized hierarchy (MCP).
3. If runtime rebuilds exist, only clear `Dynamic` folders — keep Player, cameras, UI, managers.
4. Compile clean via `/compile-heal` (CLI `recompile`); verify movement in Play smoke (CLI `editor_play` preferred).
5. Do not thrash Play Mode or domain reload loops while writing — use CLI for verify gates.

## Entry

1. The TDD extract below is the spec for this run. `read_file` the TDD path only for a section the extract lists but does not include.
2. Deliver open-scene → Play → playable loop.
3. Never write under `Packages/com.v57.unity-game-forge/`, `Tools/gameforge-agent/`, or `.env*`.
