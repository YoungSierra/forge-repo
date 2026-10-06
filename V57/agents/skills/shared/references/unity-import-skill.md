# Unity Import Skill (reference)

Folder layout, ownership, naming and import settings for a V57 game repo. There is **one layout**: `Assets/_Game/`. The provider delivers raw art/audio into it; V57 (`com.v57.assembly`) imports, builds materials/prefabs/scenes around it.

| Item | Detail |
|------|--------|
| **Delivery contract** | Provider repo template (brief §1): no `.meta`, no ProjectSettings/Packages, no materials/prefabs/scenes/scripts |
| **Import rules** | Applied by `AssetPostprocessor` in `V57.Assembly.Editor` (`AssemblyRunner.ApplyImportRules()`), by folder + prefix |
| **Audit / presets** | `/asset-pipeline` (delegates to `com.v57.assembly`) |
| **Naming (C#)** | `shared/references/unity-naming-skill.md` |
| **Lessons** | `V57/knowledge/asset-intake.md` |

---

## Layout and ownership

```text
Assets/_Game/
├── Art/                                   PROVIDER (read-only for V57*)
│   ├── Environment/Blockout/<Level>/BLK_<Level>.fbx
│   ├── Environment/Kits/<Asset>/{Meshes,Textures,Materials†}
│   ├── Environment/Decoration/<Asset>/{Meshes,Textures,Materials†}
│   ├── Environment/Sky/
│   ├── Props/<Category>/<Asset>/{Meshes,Textures,Materials†}
│   ├── Characters/<Asset>/{Meshes,Animations,Textures,Materials†}
│   ├── Shared/Textures/
│   ├── UI/{Sprites/<ScreenId>,Icons,Fonts,Atlases‡}
│   └── VFX/<Asset>/
├── Audio/{Music,SFX/<Category>,Ambience,Voice/<lang>}   PROVIDER
├── Prefabs/Visual/{Characters,Props,Environment,VFX}/PRF_<Asset>_Visual.prefab   V57
├── Prefabs/Gameplay/PRF_<Asset>.prefab                 V57 (variant of Visual)
├── Scenes/SCN_<...>.unity                              V57
├── Scripts/<Area>/  (one asmdef per area)              V57
├── Data/SO_<...>.asset                                 V57
├── Settings/{Rendering,Input,Presets}                  V57
└── Tests/{EditMode,PlayMode}                           V57
```

\* V57 never renames, moves, deletes or edits provider files. Naming/folder deviations are absorbed by the `asset_manifest` mapping (delivered path per `asset_id`) and import rules; junk is simply not referenced.
† `Materials/MAT_<Asset>.mat` are V57-owned files placed beside the provider asset by `AssemblyRunner.BuildMaterials()`.
‡ `UI/Atlases/` holds V57-owned SpriteAtlas assets from `AssemblyRunner.BuildUiAtlases()`.

Nothing is imported from outside `Assets/_Game/`. `Source/` (work files) and `Docs/` are ignored by Unity. Legacy layouts (`Assets/Art`, `Assets/_Game/Scripts`, `Assets/_Game/Prefabs`, `Assets/VerticalSlice`, `Assets/_Isolated/<slug>`) are not used for new games.

---

## Naming prefixes

| Prefix | Asset | Example |
|---|---|---|
| `SM_` | static mesh | `SM_Flipper.fbx` |
| `SK_` | skinned mesh | `SK_Otter.fbx` |
| `ANIM_<Asset>_<Clip>` | animation | `ANIM_Otter_Idle.fbx` |
| `BLK_` | level blockout | `BLK_WoodlandPond.fbx` |
| `T_<Asset>_<BC\|N\|ORM\|E\|Mask>` | texture | `T_Flipper_BC.png` |
| `SPR_<Id>_<State>[_9s-<px>]` | UI sprite (9-slice border px in name) | `SPR_PauseButton_Pressed_9s-24.png` |
| `ICO_`, `FNT_` | icon, font | `ICO_Coin.png`, `FNT_Title.ttf` |
| `VFX_<Asset>_Sheet_<cols>x<rows>` | flipbook | `VFX_Splash_Sheet_4x4.png` |
| `MUS_`, `SFX_`, `AMB_`, `VO_` | audio | `SFX_Bumper_Hit.wav` |
| `MAT_`, `PRF_`, `SCN_`, `SO_` | V57-created material, prefab, scene, data | `MAT_Flipper.mat` |

Inside FBX: `UCX_<Asset>_NN` convex collision, `Socket_<Name>` empties, `Marker_<Type>_<Id>` level markers (`Marker_Spawn_Player`, `Marker_Spawn_<Entity>_NN`, `Marker_Zone_<Type>`, `Marker_Bounds`, `Marker_Camera_<ViewId>`, `Marker_Checkpoint_NN`, `Marker_Patrol_<Id>_NN`). FBX material name == asset name. PascalCase, no spaces/accents/`temp`/`copy`/`New Folder`.

---

## Import rules (applied by `ApplyImportRules()`)

| Asset | Rule |
|---|---|
| Model (`SM_`, `SK_`, `BLK_`) | units meters, Y up, Z forward (`package.world`); scale factor fixed so `size_m` from the brief matches bounds (x10 exports corrected here, logged); materials: no import (V57 builds `MAT_`); Read/Write only when `collision: exact` needs it or a Mesh collider is generated at runtime; `UCX_*` → convex colliders, renderers off |
| Blockout (`BLK_`) | same as model; `Marker_*` empties kept; marker renderers off |
| Skinned + animation | rig Generic/Humanoid per brief `bones`; `ANIM_` clips split per `animations[]`, loop flags and events from the brief |
| Texture `T_*_BC` / `_E` | sRGB; `_N` normal map; `_ORM` / `_Mask` linear; max size = brief `texture_size`; mips on (3D) |
| Sprite `SPR_`, `ICO_` | Sprite (2D and UI), no mips, 9-slice border from `_9s-<px>`; atlas per `<ScreenId>` (`BuildUiAtlases()`) |
| VFX sheet | Sprite/Default per use; grid from `_Sheet_<cols>x<rows>` |
| Audio | SFX: decompress on load, Vorbis 70–100%; Music/Ambience: streaming, Vorbis 50–70%; Voice: compressed in memory |

Platform overrides (mobile ASTC, desktop BC) come from `package.platform.targets`.

---

## Validation checklist

- [ ] Every file under `Assets/_Game/Art` and `Assets/_Game/Audio` matches a naming prefix (or is listed in INTAKE_REPORT)
- [ ] No provider file renamed, moved or edited; deviations absorbed + logged
- [ ] Import settings equal the rule table (read-back via `assembly-report.json`)
- [ ] Every brief asset has a Visual prefab or a placeholder
- [ ] No duplicate asset names across folders

---

*Last updated: 2026-09-28*
