# V57 Unity GameForge — Agent boundaries

- **Commands:** when a request starts with `/vertical-slice` or `/game-setup` (in any agent or chat), follow `V57/agents/RUN.md` — read, preflight (branch, Node deps, Unity Editor open and `ready`), then run the skill autonomously. Cursor and Claude Code also expose them as slash commands (`.cursor/commands/`, `.claude/commands/`).
- **Pipeline:** `/vertical-slice` (driver) or `/game-setup` (Production scope) — I0 INTAKE → I1 PROJECT → I2 ASSEMBLY → M1 GREYBOX GOLD PATH → F SPECS → M2 SCENE = GAME → M3 CRAFT → M4 ACCEPTANCE. Autonomous: no `continue` between stages; decide + log in `Docs/V57/DECISIONS.md`; stop only on blocking intake, destructive action, or `.v57/ALLOW_STOP`.
- **Product truth:** the provider's `Docs/Design/TDD.md` (TDD Standard 2.x, template `V57/docs/tdd/TDD_Template.md`) wins over the ADD (`Docs/ArtDirection/ArtDirectionDocument.md`). Both are **read-only**; editing the TDD is a hard stop.
- **Provider-owned (read-only):** `Docs/Design/`, `Docs/ArtDirection/`, `Docs/Audio/`, `Assets/_Game/Art/`, `Assets/_Game/Audio/` — V57 never renames, moves or edits them; `fixable` issues are absorbed by import rules + `asset_manifest` mapping (logged `D-###`). Never patch asset problems in gameplay code.
- **V57 writes:** `Docs/Generated/` (intake only), `Docs/V57/`, `Assets/_Game/{Prefabs,Scenes,Scripts,Data,Settings,Tests}`, `Assets/_Game/Art/**/Materials/`, `Packages/manifest.json`, `ProjectSettings/`, `V57/specs/<slug>/`. Game context is `Docs/V57/CONTEXT.md` (never a swapped root `CONTEXT.md`).
- **Engine pin:** Unity **6000.6.2f1** (single source: V57 template `ProjectSettings/ProjectVersion.txt`, mirrored in `V57/agents/agents.yaml → layout.engine_pin`). The TDD engine value is informational.
- **Stack:** Unity 6 + URP + Input System + UI Toolkit; `com.v57.assembly` (`AssemblyRunner`, `GoldPathDriver`).
- **Editor drive:** Unity CLI + Pipeline (`unity command`, `eval`, `test`) for writes and verification. `unity mcp` is optional (same Pipeline over stdio). Never run the Test Runner via MCP. See `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md`.
- **Scene authoring:** the saved scene is the game. No runtime scene builders/directors; a bootstrap only binds serialized references and calls Init in order. No product `Scripts/Editor/*Authoring` MenuItem scene builders. Don't ask the human to mount Hierarchy.
- **Verification:** real input + real physics in PlayMode acceptance and the gold path; visual verdicts by `/independent-review` (fresh context). Status words: `implemented` → `agent-verified` → `pending owner review`.
- **Missing assets:** never generate placeholders (primitives, placeholder materials, generated/synthesized art or audio, fallback sprites). List every missing resource in `Docs/V57/MISSING_ASSETS.md` (written at I0, kept current) and leave the slot empty. See `V57/knowledge/assets-collision-builds.md`.
- **Collision:** a collider always lives on the same prefab as the mesh it represents (gameplay prefab per piece, Visual prefab nested) so moving the piece moves its collision — no detached collider objects. Physics space follows the delivered art: a 3D (Y-up) scene uses 3D physics constrained to the play plane even if the TDD says 2D (log the conflict `D-###`).
- **Builds:** the pipeline never produces player builds; verification is in the Editor. Build only when the owner explicitly asks in the current request.
- **Lessons:** read `V57/knowledge/*.md` once per session; `/context-postmortem` adds new ones.

## Backlog (do not drop)

- **Stop run:** Workbench **Stop** + Chat send button toggles ↑ → ■ while busy; modes locked during run. `POST /api/sessions/cancel`.

## After finishing work

Check compile state with `unity command recompile_status` (poll to completion) and the console for `error CS` / GameForge failures; fix blockers before calling the task done. Old `Editor.log` lines from deleted files are noise.
