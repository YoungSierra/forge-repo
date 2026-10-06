# Unity skills attribution (V57)

V57 adapts selected skills from [Unity-Technologies/unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) (Unity Companion License) and the related [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) collection.

| V57 skill | Upstream | Status |
|-----------|----------|--------|
| `shared/references/unity-ui-uitk-skill.md` | `ui-uitk`, `ui` | Adapted — MCP/GameForge paths |
| `commands/art/unity-urp-postprocessing-skill.md` | `urp-postprocessing` | Adapted — MCP not unity-cli `eval` for writes |
| `commands/art/unity-game-feel-skill.md` | *(none — V57 original)* | Fills upstream gap (camera/juice) |
| `commands/editor/unity-physics-setup-skill.md` + `shared/references/unity-physics-3d-reference.md` | `physics-3d-collision` | Adapted — G-PHYS + OVR |
| `shared/helpers/unity-package-management-skill.md` + `unity-package-select-reference.md` | `unity-package-management` | Adapted — `/packages` |
| `shared/helpers/unity-cli-ops-skill.md` + `docs/mcp/UNITY_CLI_INTEGRATIONS.md` | `unity-cli` (subset) | Adapted — verify/gates; MCP keeps writes |
| `commands/editor/unity-editor-search-skill.md` | `generate-editor-search-query` | Adapted — `/editor-search` |
| `shared/references/unity-urp-render-graph-validate-skill.md` | `validate-urp-render-graph-renderer-feature` | Adapted — used by `/shader-setup` |
| `shared/references/unity-audio-optimize-reference.md` | `optimize-audio` | Merged into `/audio-setup` |
| `commands/editor/unity-nav-ai-skill.md` | `initialize-ai-navigation` | Enriched workflow |

**Not imported** (omit unless product needs them):

- `optimize-web` — WebGL-only packaging
- `ui-ugui` / `ui-imgui` — legacy; V57 is UITK-first
- `build-live-game`, `implement-in-app-purchases`, `levelplay-unity-integration` — live ops / ads
- `setup-multiplayer-services`, `setup-vivox-voice-chat` — use existing `unity-networking-skill.md` when needed
- `new-unity-project` — superseded by GameForge Workbench + `/game-setup`
- Heavy 2D authoring (`tilemap-*`, `sprite-editor`, `manage-sprite-atlas`, `2d-pixel-perfect`, `sprite-segment-3x3grid`) — add when a TDD is 2D-first
- `migrate-birp-to-urp` — one-shot migration; not a craft
- `shader-graph-create-custom-node` — pull on demand into `/shader-setup` if needed

Install upstream for reference: `npx skills add Unity-Technologies/unity-agent-plugin` (or marketplace plugin install per their README).
