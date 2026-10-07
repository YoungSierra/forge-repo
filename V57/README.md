# V57 GameForge

Unity 6 base project that turns a **provider delivery** (design docs + raw art/audio, no Unity files) into a playable **Vertical Slice** or **Production** game — autonomously, with evidence-based gates. Genre-agnostic: everything game-specific comes from the delivery (TDD, ADD, assets), never from this repo.

## What is in this repo

| Path | Purpose |
|---|---|
| `V57/` | The V57 pack: agent skills, `agents.yaml`, standards, knowledge (lessons), templates |
| `V57/tools/intake/` | Node: reads TDD + ADD + assets → `Docs/Generated/*.yaml` + `json/` and `Docs/V57/INTAKE_REPORT.md` |
| `V57/tools/lint/` | Node: runtime lint for gameplay code (no scene building at runtime, no faked physics in tests) |
| `V57/tools/schema/` | JSON schemas for every generated file |
| `V57/tools/unity-assembly/` | UPM package `com.v57.assembly`: import rules, materials (contract or Blender texture names), skybox, atlases, animator controllers, visual prefabs, level scenes from `BLK_` markers or a LevelMaps layout JSON, gold-path driver. No placeholders |
| `V57/tools/install-core.ps1` | Installs this core into a provider game repo (V57/, agent commands, packages, project settings, `_Game` skeleton) |
| `V57/agents/RUN.md` | Single run entry for `/vertical-slice` and `/game-setup` (read → preflight → run → report), used by every agent |
| `.cursor/commands/`, `.claude/commands/` | `/vertical-slice` and `/game-setup` as native slash commands in Cursor and Claude Code (both point to `V57/agents/RUN.md`) |
| `V57/templates/provider-repo/` | What the provider receives: folder tree, naming, commit order |
| `V57/templates/game-state/` | Seeds for `Docs/V57/` (STATUS, PLAN, TODO, DECISIONS, DEVLOG) |
| `Packages/com.v57.unity-game-forge/` | Unity Workbench window (chat, pipeline view) that talks to the sidecar |
| `Tools/gameforge-agent/` | Node sidecar (Cursor SDK + LLM providers) on `http://127.0.0.1:3857` |
| `Assets/Settings/` | URP baseline (pipeline asset + renderer) |

A game's own content lives under `Docs/Design`, `Docs/ArtDirection`, `Docs/Audio`, `Assets/_Game/Art` and `Assets/_Game/Audio` (provider-owned, read-only for V57). V57 writes `Docs/Generated`, `Docs/V57`, `Assets/_Game/{Prefabs,Scenes,Scripts,Data,Settings,Tests}`, `Assets/_Game/Art/**/Materials` and `V57/specs/<slug>/`.

## Requirements

- Unity **6000.6.2f1** (`ProjectSettings/ProjectVersion.txt`, mirrored in `V57/agents/agents.yaml → layout.engine_pin`).
- **Node.js 22+**.
- **Unity CLI** with the Pipeline package (`com.unity.pipeline`, already in `Packages/manifest.json`). Keep the Editor open on this project during a run. Optional: `unity mcp configure cursor`.
- **Git LFS** (`git lfs install`) — binaries are tracked by `.gitattributes`.
- API keys in `%USERPROFILE%\.v57\gameforge.env` (see `gameforge.env.example`).

## First setup

```powershell
git lfs install
cd Tools/gameforge-agent; npm install; cd ../..
cd V57/tools/intake; npm install; cd ../../..
```

Open the folder in Unity Hub with 6000.6.2f1, then **V57 → GameForge → Open Workbench**.

### New game repo

1. Clone the provider repo (delivery committed per `V57/templates/provider-repo/README.md`).
2. From this core checkout: `pwsh V57/tools/install-core.ps1 -Target <provider repo>`; commit the result on a branch (e.g. `v57/core`).

### Running (Cursor, Claude Code or any agent)

Type `/vertical-slice` (or `/game-setup`) in the agent chat at the repo root — Cursor and Claude Code load them from `.cursor/commands/` and `.claude/commands/`; other agents get the same instruction from `AGENTS.md`. Every route lands on `V57/agents/RUN.md`, which does the preflight itself (branch `v57/setup`, `npm ci` for the Node tools, opens Unity 6000.6.2f1 with `unity open .` and waits for `ready`) and then runs I0 → M4 without `continue`. No prompt needs to be pasted. Resume or inspect with `/vertical-slice --from <stage>` or `--status`; state is read from `Docs/V57/STATUS.json`.

## Building a game

1. The provider's delivery is committed into this repo following `V57/templates/provider-repo/README.md` (commits `P1 design` … `P10 delivery`).
2. Create branch `v57/setup` and run `/vertical-slice` (or `/game-setup` for Production):

| Stage | What | Gate |
|---|---|---|
| I0 INTAKE | `node V57/tools/intake/index.js --repo .` + `Docs/V57/MISSING_ASSETS.md` | exit code; blocking → stop |
| I1 PROJECT | baseline settings, packages, 0 console errors | script check |
| I2 ASSEMBLY | `V57.Assembly.AssemblyRunner.RunAll()` | `assembly-report.json`, 0 missing refs |
| M1 GREYBOX GOLD PATH | core loop playable, `GoldPathDriver` drives real input | `checks.json` all pass |
| F SPECS | one TDD §C spec at a time, real tests | tests + gold path green |
| M2 SCENE = GAME | gameplay prefabs (collider on the same prefab as its mesh), UI from mockups, audio/VFX | runtime lint, Edit vs Play diff, colliders-with-mesh check |
| M3 CRAFT | camera/light/post/VFX/audio polish vs ADD | independent fresh-context review |
| M4 ACCEPTANCE | P0 acceptance, QA, final report (no player build unless the owner asks) | status `pending owner review` |

### Rules every run follows

- **No placeholders.** Missing art/audio is never faked (no primitives, placeholder materials, generated or synthesized assets, fallback sprites). Every gap is listed in `Docs/V57/MISSING_ASSETS.md` and the slot stays empty.
- **Collision stays with its mesh.** Each collider lives on the gameplay prefab (a variant of the Visual prefab) of the piece it represents, so moving the piece moves its collision.
- **Physics follows the delivered art.** A 3D scene (Y-up, play surface in XZ) uses 3D physics constrained to the play plane, even if the TDD says 2D; the conflict is logged `D-###`.
- **English scene names.** Scene groups are English PascalCase (`Structure`, `Ocean`, `Dressing`) and repeated instances are `<Name>_NN` — never DCC suffixes like `.001` (the LevelMaps layout is normalized by intake).
- **No player builds by default.** Verification happens in the Editor (tests, gold path, lint, hierarchy diff, console). Builds only on explicit owner request.

Details: `V57/knowledge/assets-collision-builds.md`.

No `continue` between stages: the agent decides and logs in `Docs/V57/DECISIONS.md`, and stops only on blocking intake, destructive actions or the owner stop token `.v57/ALLOW_STOP`. State lives in `Docs/V57/`, so any session can resume.

Details: `V57/SYSTEM_PROMPT.md`, `V57/agents/skills/commands/setup/unity-vertical-slice-skill.md`, `V57/docs/guides/HEADLESS_AGENT_ORCHESTRATION.md`, lessons in `V57/knowledge/`.

## Modes

| Mode | Command | Output |
|---|---|---|
| Prototype | `/prototype`, `/prototype-full` | `Assets/Prototypes/<slug>/` (sandbox spike, no tests) |
| Vertical Slice | `/vertical-slice` | `Assets/_Game/` (slice scenes + their mechanics) |
| Production | `/game-setup` | `Assets/_Game/` (all mechanics and scenes + perf, review; builds only on request) |

## Tests

```powershell
cd V57/tools/intake; node --test
cd V57/tools/lint; node --test
cd Tools/gameforge-agent; npm run smoke:v57
unity test . --mode EditMode   # com.v57.assembly (testables)
```

## Secrets

Keys go in `%USERPROFILE%\.v57\gameforge.env` or a gitignored `.env.local`. Never under `Assets/` or `Packages/`, never committed.
