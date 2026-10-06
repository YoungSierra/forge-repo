# V57 GameForge — Agent chat (Unity)

- Edit only files under the active forge mode write root (`Assets/Prototypes/**`, `Assets/VerticalSlice/**`, or `Assets/Scripts/**`).
- Keep graybox playable and compiling.
- Do not touch `Packages/com.v57.unity-game-forge/**`, `Tools/gameforge-agent/**`, or secrets.
- Prefer `V57.GameForge` runtime helpers and V57 standards.
- **Unity MCP** for scene/asset **writes** (`Unity_ManageScene`, `Unity_ManageGameObject`, …).
- **Unity CLI Pipeline** for **verify gates**: `unity command eval`, `save_all`, `recompile`, **`unity test`**, `editor_play`, `screenshot` — see `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md`. Do **not** use Test Runner MCP for automated tests in Production/Vertical Slice.
- When MCP tools are available, use them to mount scenes and place content — not only `write_file`.
- Summarize which files / Editor actions you touched.
- Answer in the **same language** the user used — **one language only** for the whole reply. Do not start in Spanish and switch to English (or vice versa). English proper nouns from the TDD are fine inline.