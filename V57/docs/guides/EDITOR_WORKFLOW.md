# Editor workflow — index (OVR)

Short index for Unity Editor work in V57. **Executable detail lives in skills** — do not duplicate long Operate/Verify recipes here.

| Item | Path |
|------|------|
| **CLI / Pipeline (primary)** | `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md` |
| **Connectivity + optional `unity mcp`** | `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md` |
| **OVR contract** | `V57/agents/skills/shared/helpers/unity-editor-verification-skill.md` |
| **Production hierarchy** | `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md` |
| `/scene-setup` | `V57/agents/skills/commands/editor/unity-scene-setup-skill.md` |
| `/prefab` | `V57/agents/skills/commands/editor/unity-prefab-skill.md` |
| `/data-asset` | `V57/agents/skills/commands/editor/unity-data-asset-skill.md` |
| `/physics-setup` | `commands/editor/unity-physics-setup-skill.md` (G-PHYS) |
| `/packages` | `shared/helpers/unity-package-management-skill.md` |
| `/unity-cli` | `shared/helpers/unity-cli-ops-skill.md` |
| `/editor-search` | `commands/editor/unity-editor-search-skill.md` |
| `/ui-setup` / `/game-ui` | `commands/ui/unity-ui-toolkit-skill.md`, `unity-game-ui-skill.md` |
| Smooth before Play | `shared/helpers/unity-editor-smooth-ops-skill.md` (`/editor-smooth`) |

---

## Production-first rule

Scene OVR must verify **functional wiring** and **production layout**. A playable flat scene with root-level primitives is **FAIL** for Stage G / `/scene-setup`.

## Flow

```
Spec / TDD → CLI pre-flight → Operate (unity command) → Verify (read-back) → Console clean → Report → PASS | FAIL
```

Drive the live Editor with Pipeline. **Do not** ship product `*Authoring` MenuItem scripts. Full invariant tables: **`unity-editor-verification-skill.md`**.

## Reports

| Operation | Path |
|-----------|------|
| Scene | `Docs/V57/reports/scene-setup-report-{slug}.md` |
| Prefab | `Docs/V57/reports/prefab-report-{slug}.md` |
| Data asset | `Docs/V57/reports/data-asset-report-{slug}.md` |

## Out of scope / asset policy

Agents **import and wire** delivered art (`/asset-pipeline`, craft skills). **Procedural design** (maze layout, spawn recipes, track generation) is allowed — that is code, not fake shipped art.

- Scan the delivered asset root → optional `asset_manifest.yaml` under the project.
- For every TDD-required media slot with no file → **`ASSET_GAPS.md`** in that project root.
- **Do not** synthesize audio (`AudioClip.Create` / waveforms). Missing clips → null + Warning + gaps row.
- **Missing visuals** (props, doors, special walls, item boxes, tires, entity meshes — any genre): visible primitive + world label **`MissingAsset: <ExpectedName>`**, plus a gaps row. Wire real FBX/PNG/WAV when present.
- Code crafts (UI Toolkit, URP renderer features / shaders named by TDD) are still implemented in code.

---

*Last updated: 2026-09-22*
