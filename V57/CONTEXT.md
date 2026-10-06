# CONTEXT.md (V57 pack)

Technical elevator pitch for the **V57 pack itself**. A game's architecture lives in that game's repo at **`Docs/V57/CONTEXT.md`** (created at M1 by `/tdd-to-context Docs/Design/TDD.md --bootstrap --context Docs/V57/CONTEXT.md`, template `V57/docs/context/CONTEXT_TEMPLATE.md`). There is no shared root `CONTEXT.md` swapped between games.

---

## What V57 is

A pipeline pack that turns a **provider-delivered Unity repository** (docs + raw art/audio, no Unity files) into a playable vertical slice or production game, autonomously:

```
I0 INTAKE → I1 PROJECT → I2 ASSEMBLY → M1 GREYBOX GOLD PATH → F SPECS → M2 SCENE = GAME → M3 CRAFT → M4 ACCEPTANCE
```

Driver: `V57/agents/skills/commands/setup/unity-vertical-slice-skill.md`. Production scope: `unity-game-setup-skill.md`.

| Path | Purpose |
|------|---------|
| `V57/agents/agents.yaml` | Command registry, layout/paths, pipeline, autonomy, gates, bans |
| `V57/agents/skills/` | Skills: `commands/` (slash commands) + `shared/` (helpers, references, checks) |
| `V57/knowledge/` | Short lessons from real runs (read once per session) |
| `V57/templates/game-state/` | `STATUS.json`, `PLAN.md`, `TODO.md`, `DECISIONS.md`, `DEVLOG.md` templates → copied to `Docs/V57/` |
| `V57/tools/intake/` | Node intake CLI: provider docs/assets → `Docs/Generated/*.yaml` + `json/*.json` + `Docs/V57/INTAKE_REPORT.md` |
| `V57/tools/unity-assembly/` | UPM package `com.v57.assembly`: `AssemblyRunner` (import rules, materials, atlases, Visual prefabs, level scenes, legacy placeholders — never instanced, gaps go to MISSING_ASSETS.md, hierarchy diff) + `V57.GoldPath` (GoldPathDriver, IGoldPathProbe) |
| `V57/tools/lint/runtime-lint.js` | Runtime code lint (M2 gate) |
| `V57/docs/` | Standards, guides, TDD mechanic-block reference, CLI/MCP docs (game reports live in each game repo under `Docs/V57/reports/`) |
| `V57/specs/<slug>/` | Per-game specs (`features/`, `systems/`) adopted from TDD §C |

---

## Project identity (pack)

```yaml
project_name: V57 GameForge pack
repo_kind: unity_pipeline_pack
engine_pin: "6000.6.2f1"          # V57 template ProjectVersion; games use this, TDD engine is informational
render_pipeline: URP
input_system: new
ui: UI Toolkit (uGUI only when the TDD names it)
game_layout: Assets/_Game
agent_runner: Tools/gameforge-agent (Node sidecar, Cursor SDK; port 3857)
```

---

## Game repo layout (summary)

Provider-owned (read-only for V57 — never renamed, moved or edited; `fixable` issues are absorbed by import rules + manifest mapping): `Docs/Design/`, `Docs/ArtDirection/`, `Docs/Audio/`, `Assets/_Game/Art/`, `Assets/_Game/Audio/`.

V57-owned: `Docs/Generated/`, `Docs/V57/`, `Assets/_Game/{Prefabs/Visual,Prefabs/Gameplay,Scenes,Scripts,Data,Settings,Tests}`, `Assets/_Game/Art/**/Materials/`, `Packages/manifest.json`, `ProjectSettings/`, `V57/specs/<slug>/`.

Full tree: `V57/agents/skills/shared/references/unity-import-skill.md`; scene rules: `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md`.

---

## Editor drive: Unity CLI primary, MCP optional

```yaml
unity_cli:
  required: true                    # when the repo has Assets/
  pipeline_package: com.unity.pipeline   # `unity pipeline install`
  writes_and_verify: "unity command / eval"
  tests: "unity test"               # never Test Runner via MCP
  policy_doc: V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md
  on_unavailable: "retry twice, then batch -executeMethod; never ask the human to mount Hierarchy"
unity_mcp:
  required: false                   # optional Cursor stdio binding to the same Pipeline (`unity mcp`)
  policy_doc: V57/docs/mcp/UNITY_MCP_REQUIREMENT.md
  legacy_relay: "com.unity.ai.assistant — fallback only until end 2026 (GAMEFORGE_UNITY_MCP_MODE=legacy)"
```

This matches `agents.yaml → rules.unity_cli` (strict) and `rules.unity_mcp` (optional). Pack-only work (no `Assets/`) — YAML/spec/skill edits — needs neither.

---

## Naming and patterns (project-wide)

Per `V57/docs/standards/STANDARDS_CANONICAL.md` and `NAMING_CONVENTIONS.md`: classes/methods/properties `PascalCase`, private fields `_camelCase`, interfaces `IPascalCase`, events `OnPascalCase`, max 200 lines and one class per file. Asset prefixes (`SM_`, `SK_`, `T_`, `SPR_`, `PRF_`, `MAT_`, `SCN_`, `SO_` …) per `unity-import-skill.md`.

---

## Technical notes

- **Autonomy:** no `continue` between stages; decide + log (`Docs/V57/DECISIONS.md`); stop only on blocking intake, destructive action, or `.v57/ALLOW_STOP`.
- **Verification:** gold path (real input, real physics) from M1; visual verdicts by `/independent-review`; status words `implemented` → `agent-verified` → `pending owner review`.
- **Context Intelligence:** Phase 1 (specs + reading policy) and Phase 2 (`/context-*` tools) — see `V57/docs/context/CONTEXT_INDEX.md`.

---

*Last updated: 2026-09-28*
*Update trigger: pipeline, layout or tooling change in the pack*
