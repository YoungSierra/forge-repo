# Unity Prototype Full Skill (`/prototype-full`)

**Chat-only skill.** Invoked via GameForge Chat (`/` → `/prototype-full`).  
**Not** wired to Workbench Build buttons (`/prototype`, `/vertical-slice`, `/game-setup`).

## Purpose

Build **one cohesive, immediately playable graybox** from the **entire TDD core loop** under
`Assets/Prototypes/<Project>/`, in a **single autonomous run**, at full prototype quality.

`<Project>` = active TDD slug (`Docs/tdds/<slug>/TDD.md` folder name, e.g. `MyGame`).
Prefer the slug over a free-form title so paths stay filesystem-safe and match Workbench selection.

This is **not** a mechanic spike (`/prototype`).  
This is **not** Vertical Slice or Production SDD (no stage farm, no `Assets/_Game/Scripts/` promotion).  
Do **not** invoke `/qa` (`qa.enforcement: game-setup` only). Close with `/playability-cert`.

The user should be able to open the product scene, press Play, and **play the fantasy** without further setup.

---

## Folder bootstrap (mandatory — first write actions)

Before any gameplay files:

1. Ensure **`Assets/Prototypes/`** exists (create if missing).
2. Ensure **`Assets/Prototypes/<slug>/`** exists (project folder named from the TDD slug).
3. Create the usual subfolders under that project root as needed:
   `Scripts/` · `Scenes/` · `Materials/` · `Prefabs/` · `UI/` · `Audio/` (optional)

Do **not** dump scripts into a flat `Assets/Prototypes/Scripts/` shared bucket for `/prototype-full`.
All product content for this run lives under **`Assets/Prototypes/<slug>/`**.

The agent host may pre-create these folders; still verify they exist before writing.

---

## Quality standard (mandatory)

Deliver a product that feels intentional and complete for a graybox prototype:

1. **Readable fantasy in &lt;3 seconds of Play** — lighting, materials, and space communicate the TDD mood (not a lone default cube on an empty plane).
2. **Full core loop** — WAKE → play verbs → WIN and LOSE → RESTART (no domain reload required).
3. **Every §B mechanic required for that loop** implemented — no critical-path stubs.
4. **Atmosphere as systems** when the TDD names lighting / audio / fog / grade / palette — visible and audible, not optional dressing.
5. **Live HUD** (UI Toolkit) shows live game state per TDD §9 (metrics, prompts, win/lose), not file names.
6. **Literals match the TDD** when quantified (speeds, timers, colors, Kelvin, drains).
7. **Compile clean**; Console free of errors after a Play smoke (≥10 s).
8. **Controls work** with the project’s Input System settings (movement, look, interact, restart).

If the Hierarchy in Edit Mode shows almost nothing playable, the product is incomplete — prefer **authored / baked graybox content** in the scene (or a builder that fills the world on Play **and** leaves a visible authored shell).

---

## Unity practices (non-negotiable)

1. Product scene under `Assets/Prototypes/<slug>/Scenes/<Name>.unity`.
2. Organized hierarchy containers:
   `_Environment` · `_Gameplay` · `_Systems` · `_UI` · `_Cameras` · `_Lighting`
3. URP materials (Lit / Simple Lit) — **no magenta**, no leftover Default-Material on gameplay meshes.
4. New Input System APIs when the project is Input System–only (or Both).
5. Prefabs for repeatables under `Assets/Prototypes/<slug>/Prefabs/` when practical.
6. Systems live on scene objects (`RunDirector`, builders, audio, lighting).
7. If levels rebuild at runtime, clear only dedicated **`Dynamic`** folders — keep persistent Player, cameras, UI, and managers. Prefer baking the starting level into the scene so Edit Mode already looks like a game.
8. Camera rig matches **TDD §11.5** (FPS pivot, follow rig, fixed, or ortho side) — tag `MainCamera`, enabled, subject visible in Game View.
9. **Scene mount:** drive the live Editor with **Unity CLI Pipeline** (`unity command` / `eval`; optional `unity mcp`). **Do not** create product `Scripts/Editor/*Authoring` + `[MenuItem]` to build the scene. Runtime directors/builders under non-Editor scripts are OK when they leave a visible Edit Mode shell.

---

## Write roots

| Allowed | Forbidden |
|---------|-----------|
| `Assets/Prototypes/<slug>/Scripts/**` (runtime; **no** product Editor MenuItem authoring) | `Assets/_Game/Scripts/**` |
| `Assets/Prototypes/<slug>/Scenes/**` | `Assets/VerticalSlice/**` |
| `Assets/Prototypes/<slug>/Prefabs/**` | Editing the TDD |
| `Assets/Prototypes/<slug>/Materials/**` | `Packages/`, agent secrets |
| `Assets/Prototypes/<slug>/UI/**`, `Audio/**` | Remote texture URLs |
| `V57/specs/prototypes/**` (optional) | Flat dumps under `Assets/Prototypes/Scripts/` (use `<slug>/` instead) |
| | `Assets/Prototypes/<slug>/Scripts/Editor/**Authoring*` / `.Authoring/` MenuItem scene builders |

---

## Inputs (structure-first)

| Input | Required | Role |
|-------|----------|------|
| TDD (`Docs/tdds/<slug>/TDD.md`) | Yes | Mechanics, INV locks, UI registry, atmosphere |
| Delivered asset root (e.g. `Assets/AssetsBR/`) | Yes for art/audio | Source of meshes/textures/clips — wire when present |
| Level design (JSON / image) | Optional (future) | Placement layout; procedural **design** from TDD is fine without this file |
| GDD / art bible | Optional (future) | Tone/copy; does not override TDD numbers |

**Thin setup:** bootstrap folders + wire TDD systems + ingest assets. Craft skills only when the TDD names them. **Do not** run a full Production G-* farm for `/prototype-full`.

### Asset policy (mandatory) — any genre

1. Build `asset_manifest.yaml` under `Assets/Prototypes/<slug>/` listing delivered files (path, type, suggested TDD slot).
2. For every TDD-required media slot with **no** delivered file → row in **`ASSET_GAPS.md`** (id, TDD ref, expected type/path, impact). Same file for every project (Mario Kart clone, liminal horror, etc.).
3. **Procedural design is allowed** — maze recipes, chunk layout, spawn tables, racing line generation, etc. are **code/design**, not “invented art.”
4. **Forbidden synthesized media:** `AudioClip.Create` / procedural waveforms; claiming generated clips/textures are shipped finals. Missing audio → **null clip + one Warning** + gaps row.
5. **Missing visual assets (any genre):** if a prop / special wall / door / item box / tire / entity mesh / etc. is not delivered, place a **visible primitive** (cube/cylinder) with an in-scene world label **`MissingAsset: <ExpectedName>`** (e.g. `MissingAsset: AlmondWaterBottle`). Still list the expected path in `ASSET_GAPS.md`. No invisible bare colliders.
6. **Wire delivered media when present** (FBX, PNG atlas, WAV) onto the procedural design slots they fill.
7. **Allowed as code:** UI Toolkit UXML/USS, Input Actions, URP `ScriptableRendererFeature` + shaders when the TDD locks post (e.g. VHS).
8. Before Play smoke: `ASSET_GAPS.md` must exist and list all known media holes.

---

## Inventory + gap-fill

Before writes: inventory TDD needs vs delivered assets → **present / broken / missing** → `ASSET_GAPS.md` → reuse present; keep procedural design; missing visuals → labeled primitives; **never synthesize audio**.  
Re-invoke continues from disk (gap-fill). Wipe only if the user asks to rebuild from scratch.

---

## Pipeline (autonomous)

| Step | Action |
|------|--------|
| **0** | Read full TDD; list §B mechanics for the core loop; resolve `<slug>`. |
| **0a** | **Folder bootstrap** — `Assets/Prototypes/` + `Assets/Prototypes/<slug>/` (+ subfolders). |
| **0b** | Scan asset root → `asset_manifest.yaml` + `ASSET_GAPS.md`. |
| **1** | Implement scripts under `Assets/Prototypes/<slug>/Scripts/`. |
| **2** | Wire **delivered** media onto systems; missing visuals → labeled `missing asset` primitives + gaps rows. |
| **3** | Author the product scene once under `…/<slug>/Scenes/` via **CLI/Pipeline** (hierarchy, player, systems, UI, procedural design + wired/gap visuals, URP wired, save). No product `*Authoring` MenuItem scripts. |
| **3b** | TDD-gated crafts: VHS/custom look → `/shader-setup` + Render Graph; §9 UI → UITK `/game-ui` (**not** silent-skippable if TDD requires). |
| **4** | `/compile-heal` until clean. |
| **5** | One Play smoke: movement works, world readable, loop reachable. Fix offline if not; one more Play. |
| **DONE** | Checklist below. |

### Discipline

- One scene mount; do not thrash reopen/reposition loops.
- Do not enter Play repeatedly while writing — assemble offline, smoke at the end.
- Conditional crafts (`/lighting-setup`, `/audio-setup`, `/game-visuals`, `/game-ui`, `/input-setup`, `/shader-setup`) **only** when the TDD names those domains — except G-UI / VHS-class shader when §9 / §8.5 locks apply (mandatory).

Hardening **H-01…H-06** from `/prototype` still apply where relevant.

---

## Tests (forbidden)

Prototype runs **never** generate or run automated tests — saves tokens and keeps focus on Play smoke.

| Rule | Detail |
|------|--------|
| **No test files** | Do **not** create `Assets/_Game/Tests/**`, test asmdef, EditMode/PlayMode test classes, or Test Runner jobs |
| **`/spec implement`** | If used, always pass **`--no-tests`** |
| **Validation** | Compile-heal + one Play smoke + optional `/playability-cert` — not Test Framework |
| **Scope** | **Vertical Slice** and **Production** (`/vertical-slice`, `/game-setup`) still require tests per `agents.yaml` |

Do not add EditMode/PlayMode rows to prototype specs for implement-time test generation. TDD AC tags are manual/Play verification only in this mode.

---

## Stop conditions

Pause only for: missing TDD slug; cannot author a playable scene and scripts; compile-heal exhausted; playability FAIL after 2 fix cycles; destructive ask.

Resume with the same `/prototype-full` (gap-fill) or Chat **Continue** after Stop checkpoint.

---

## DONE checklist

```text
## /prototype-full DONE
- TDD: <slug>
- Project root: Assets/Prototypes/<slug>/
- Scene: Assets/Prototypes/<slug>/Scenes/<Name>.unity
- asset_manifest.yaml: present
- ASSET_GAPS.md: present (missing media listed; no synthesized audio; missing visuals labeled)
- Hierarchy: containers present; procedural design + delivered/gap stand-ins
- Loop: WAKE → play → WIN/LOSE → Restart
- Mechanics shipped: <§B list>
- Atmosphere: lighting / audio / fog-palette as required = yes|n/a|gaps
- UI Toolkit / VHS (if TDD): shipped
- Controls: move / look / interact / restart verified
- How to play: Open scene → Play · <keys>
```

Anything less is **not** DONE.
