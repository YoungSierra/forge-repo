# Scene production standards (V57)

**Priority:** scalability and production over prototype shortcuts. Vertical slices and CLI-automated scenes follow these rules so work scales to full production without rework.

| Item | Detail |
|------|--------|
| **Applies from** | Stage **I2** (level scenes built by `AssemblyRunner.BuildLevelScenes()`) and enforced at **M2** (`/scene-setup`, `/vertical-slice`, `/game-setup`) |
| **OVR** | Production invariants are **mandatory** for scene PASS (see `unity-scene-setup-skill.md`) |
| **Code standards** | `V57/docs/standards/STANDARDS_CANONICAL.md` (scripts) — this doc covers **Editor hierarchy & assets** |
| **Layout** | Single layout `Assets/_Game/` — see `shared/references/unity-import-skill.md` |

---

## Core principle

**The saved scene is the game.** What you see in Edit Mode is what plays. A scene that only becomes correct after a runtime script finds, creates, reparents or patches objects is **FAIL** (see `V57/knowledge/runtime-bootstrap.md`). A scene that plays but uses flat root primitives is also **FAIL**.

Prefer:

1. **Organized hierarchy** (containers + logical parents)
2. **Prefabs** for everything repeatable: `Visual` prefab (V57-built from provider art) → `Gameplay` prefab **variant** (behaviour)
3. **Folder conventions** under `Assets/_Game/Prefabs/`
4. **Documented structure** in the game's `Docs/V57/CONTEXT.md` → `sceneProduction`

---

## Root containers (mandatory)

| Container | Contents |
|-----------|----------|
| `_Environment` | Blockout instance (`BLK_<Level>`), kits, decoration, sky, static colliders |
| `_Gameplay` | Player, enemies, interactables, spawn roots (`_Gameplay/Spawned` for runtime instances) |
| `_Systems` | Scene-local managers and the (thin) bootstrap — not DontDestroyOnLoad |
| `_UI` | `UIDocument` roots (UI Toolkit) |
| `_Cameras` | All cameras (camera values from `Docs/Generated/json/camera.json`) |
| `_Lighting` | Lights, probes, Volumes |

**Allowed at scene root:** only the six containers above.
**Prohibited:** loose `Player`, `Coin_1`, `GameManager`, `HUD`, etc. at scene root.

---

## Sub-hierarchy (recommended)

```
_Environment/
  BLK_<Level>              ← blockout FBX instance (Marker_* empties kept, renderers per import rules)
  Kits/ Decoration/ Sky
_Gameplay/
  Player                   ← PRF_<Asset> (gameplay variant) instance at Marker_Spawn_Player
  Collectibles/Coin        ← PRF_<Asset> instances or a spawner with serialized prefab ref
  Level/<Layer>            ← gameplay prefabs placed by the level delivery (LevelMaps layout); with _Environment this is the level content that `AssemblyRunner.RebuildLevelContent()` replaces when a new map arrives — everything else (player, systems, UI, cameras) is kept. The containers `_Environment/_Markers` and `_Gameplay/Level` are never deleted (only emptied), so systems reference those containers and traverse their children at Init — they never hold references to individual level objects
  Spawned                  ← empty parent for runtime-instantiated prefabs (the only runtime-created content allowed)
_Systems/
  GameBootstrap            ← serialized refs + ordered Init only
_UI/
  HUD                      ← UIDocument + PanelSettings + UXML
_Cameras/
  Main Camera
_Lighting/
  Directional Light, Global Volume
```

---

## Prefabs over primitives (mandatory)

| Object type | Requirement |
|-------------|-------------|
| **Visual prefab** | `Assets/_Game/Prefabs/Visual/{Characters,Props,Environment,VFX}/PRF_<Asset>_Visual.prefab` — built by `AssemblyRunner.BuildVisualPrefabs()`; mesh + materials + colliders from `UCX_*`; **no gameplay scripts** |
| **Gameplay prefab** | `Assets/_Game/Prefabs/Gameplay/PRF_<Asset>.prefab` — **prefab variant** of the Visual prefab + behaviour components + serialized config (`SO_*`) |
| **Missing art** | No placeholder: the slot stays empty (no prefab, unassigned reference) and is listed in `Docs/V57/MISSING_ASSETS.md`; when the art arrives, I2 builds its Visual prefab and gameplay variants pick it up |
| **Scene names** | English only. Groups are PascalCase nouns (`_Environment/Structure`, `Ocean`, `Dressing`); repeated instances are `<Name>_NN` (`Jellyfish_01`, `Jellyfish_02`; 3 digits only past 99). Never DCC suffixes (`.001`) or Unity duplicates (`(1)`). The LevelMaps layout is normalized to this by intake |
| **UI** | UXML/USS assets; `SPR_*` sprites from `Assets/_Game/Art/UI/Sprites/<ScreenId>/` |

**Prohibited:** five separate scene-only spheres named `Coin_1`…`Coin_5` without a prefab source; gameplay components added to a Visual prefab; gameplay code that disables/enables provider renderers to "fix" visuals.

---

## Asset folders (V57-owned)

```
Assets/_Game/
├── Art/**/Materials/MAT_<Asset>.mat   (V57 — beside provider meshes/textures)
├── Prefabs/Visual/{Characters,Props,Environment,VFX}/
├── Prefabs/Gameplay/
├── Scenes/SCN_<...>.unity
├── Scripts/<Area>/   (one asmdef per area)
├── Data/SO_<...>.asset
├── Settings/{Rendering,Input,Presets}
└── Tests/{EditMode,PlayMode}
```

Provider-owned (read-only for V57 — never renamed, moved or edited): `Assets/_Game/Art/**` (meshes, textures, sprites, fonts), `Assets/_Game/Audio/**`, `Docs/Design/**`, `Docs/ArtDirection/**`.

---

## Naming

| Item | Convention | Example |
|------|------------|---------|
| Container GO | `_PascalCase` | `_Gameplay` |
| Visual prefab | `PRF_<Asset>_Visual.prefab` | `PRF_Flipper_Visual.prefab` |
| Gameplay prefab | `PRF_<Asset>.prefab` | `PRF_Flipper.prefab` |
| Scene | `SCN_<LevelOrPurpose>.unity` | `SCN_WoodlandPond.unity` |
| Data asset | `SO_<Name>.asset` | `SO_FlipperConfig.asset` |
| Material | `MAT_<Asset>.mat` | `MAT_Flipper.mat` |

---

## CONTEXT block (game `Docs/V57/CONTEXT.md`)

```yaml
sceneProduction:
  enforceContainers: true
  prefabFolders:
    - Assets/_Game/Prefabs/Visual
    - Assets/_Game/Prefabs/Gameplay
  requiredContainers: [_Environment, _Gameplay, _Systems, _UI, _Cameras, _Lighting]
  runtimeSpawnRoot: _Gameplay/Spawned
```

---

## OVR verification (M2)

| Invariant | Expected |
|-----------|----------|
| Root containers | All six exist at scene root |
| No loose gameplay roots | Player, pickups, managers not direct scene roots |
| Gameplay prefabs | Every run-scope actor is an instance of `Assets/_Game/Prefabs/Gameplay/PRF_*` whose base is a `PRF_*_Visual` |
| Edit vs Play hierarchy diff | `AssemblyRunner.CaptureHierarchyDiff()` → `Docs/V57/reports/hierarchy-diff.json` `pass: true` — only additions under `_Gameplay/Spawned` (or declared spawn roots) from prefabs; no new non-prefab objects, no reparenting, no added components, no renderer toggles on provider art |
| Serialized refs | 0 missing references in scene and prefabs |

Report: `Docs/V57/reports/M2-scene-setup-{slug}.md` → section **Production invariants**.

---

## Anti-patterns (banned)

- Runtime bootstrap that builds or patches the scene (`GameObject.Find`, `new GameObject` + `AddComponent` for level structure, hardcoded coordinates, reflection `SetValue`, `#if UNITY_EDITOR AssetDatabase` in runtime code)
- Flat scene root with unrelated GameObjects
- Primitives as final art without a prefab asset
- Gameplay scripts on `_Environment` static objects
- Hiding provider meshes and substituting invisible/primitive colliders to make gameplay work (absorb via import rule / manifest mapping, or empty slot + `MISSING_ASSETS.md` + provider fix list)

---

## Related docs

- `V57/docs/guides/EDITOR_WORKFLOW.md`
- `V57/agents/skills/commands/editor/unity-scene-setup-skill.md`
- `V57/agents/skills/commands/editor/unity-prefab-skill.md`
- `V57/agents/skills/commands/setup/unity-vertical-slice-skill.md` — stage driver (M2)
- `V57/knowledge/runtime-bootstrap.md`

---

*Last updated: 2026-09-28*
