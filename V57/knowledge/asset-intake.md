# Asset intake lessons

## Mirrored left/right meshes
- **Symptom:** player-left flipper driven by the mesh named `_R` (and vice versa); bats rebuilt 3 times (`RepairFlipperBats`, `…2`, `…3`).
- **Cause:** FBX exported with L/R named from the modeller's view, not the player's.
- **Rule:** `side` (left|right|center) comes from the ADD brief and is defined from the **player's view**. Intake checks mesh bounds center vs `side` and flags a `fixable` mismatch; absorbed by the `asset_manifest` mapping (brief `side` → delivered file), logged `D-###`, listed in the provider fix list. Never rename provider files, never swap in gameplay code.
- **Enforced by:** intake `asset_manifest.issues[]`; `/intake` absorption policy. **Source:** HH level-design report.

## Montage FBX (whole level in one file)
- **Symptom:** a 17 MB `ENV01_WoodlandPond_montaje_02.fbx` with 189 transforms (table, props, creatures, gameplay pieces); gameplay then hid parts of it and added primitive colliders.
- **Cause:** provider delivered a scene montage instead of `BLK_` + per-asset meshes.
- **Rule:** a level FBX is only valid as `BLK_<Level>.fbx` with `Marker_*` empties. Gameplay actors must be separate `SM_`/`SK_` assets with briefs. A montage is `fixable` only by treating it as the blockout; missing actor meshes are listed in `Docs/V57/MISSING_ASSETS.md` (`missing`) and left empty — no placeholders. Never extract/hide sub-meshes at runtime.
- **Enforced by:** intake naming + brief checks; runtime lint (renderer toggles). **Source:** HH asset-pipeline report.

## Level layout exported from a DCC (JSON)
- **Symptom:** the level arrived as `unity_scene.json` + `manifest.json` from Blender (175 placements, Spanish layer names, `Name.001` instance names) instead of `BLK_` + `Marker_*`.
- **Rule:** `Docs/Design/LevelMaps/<LevelId>/unity_scene.json` (contract `unity_scene/1.x`, Unity Y-up metres) + optional `manifest.json` is a valid level source. Intake normalizes it into `layouts.json` (English PascalCase layer groups, `<Name>_NN` instance names, delivered model/texture paths) and links it to the TDD scene whose id matches `SCN_<LevelId>…` (else, when it is the only unmatched layout and there is exactly one gameplay scene without a layout, links it to that scene — `LAYOUT_LINKED_BY_ROLE`, so a renamed or new map needs no TDD change; otherwise adds `SCN_<LevelId>` as a layout scene). `BuildLevelScenes` places the Visual prefabs with the exported transforms as-is; manifest material values (flat colours, metallic, smoothness, double-sided) are provider values, not placeholders. Objects whose model is not on disk stay empty (`LAYOUT_ASSET_MISSING`). When a delivered model supersedes a layout asset_id (e.g. a rigged character replacing a static export), V57 maps it in `Docs/V57/layout_aliases.json` (`{ "aliases": { "<asset_id>": { "model": "<model stem>", "yaw_deg": 180 } } }`, logged as a D-###): those instances use the mapped model, their rotation turned by `yaw_deg` about their own up axis (`LAYOUT_ASSET_ALIASED`, fixable). Never rename or edit the provider's layout. Objects named `Marker_<Type>_<Id>` (or `asset_id: "Marker"`) are level markers, not models: same contract as `BLK_` marker nodes, volume when the type is a volume type or `shape: box` (1 m cube × scale).
- **Rule (non-spatial levels):** boards, waves, puzzles and other level content that is data, not geometry, arrive as `Docs/Design/LevelData/<LevelId>.json` = `{ "contract": "level_data/1.x", "level_id", "data": { … } }`. Intake checks only the envelope (`LEVEL_DATA_*`); the assembly copies the file to `Assets/_Game/Data/Levels/<LevelId>.json`; the game validates `data` against its TDD §6 level contract in its own EditMode tests. V57 never invents level data.
- **Enforced by:** intake `lib/layouts.js`, `LayoutSceneBuilder`, `LayoutMaterialBuilder`. **Source:** PS.

## DCC texture names
- **Symptom:** textures delivered as `<Name>_albedo|_normal|_MetallicSmoothness|_metallic|_roughness` built no material (the builder only knew `T_<Asset>_BC/N/ORM`).
- **Rule:** DCC suffixes are aliases (albedo/basecolor/diffuse = BC, normal = N, MetallicSmoothness = MS used as delivered, metallic + roughness packed into `Materials/T_<Asset>_MS.png`, ao = AO, emission = E); the asset is the texture's asset folder. Intake still reports `NAME_CONVENTION` (fixable) with the canonical `T_` name for the provider.
- **Enforced by:** `AssetNaming.TrySplitDccTexture`, `TextureImportRules`, `MaterialBuilder`. **Source:** PS.

## Scale ×10
- **Symptom:** table instanced at scale ≈ 10.29 to look right; physics sizes and camera size then drifted.
- **Cause:** exported in centimetres / wrong unit.
- **Rule:** import rule sets the scale factor so bounds match brief `size_m` (±10 %); scene transforms stay at scale 1. Mismatch > 10 % after import = intake issue.
- **Enforced by:** `ApplyImportRules()` + `assembly-report.json`. **Source:** HH asset-pipeline report.

## UUID-named textures
- **Symptom:** 73 materials wired by hand to `Color_<uuid>` / `NormalGL_<uuid>` maps from a generator (Tripo).
- **Rule:** textures must be `T_<Asset>_<BC|N|ORM|E|Mask>`. Intake maps UUID textures to assets via FBX material names when unambiguous (`fixable`, absorbed by the manifest mapping — files keep their delivered names); otherwise `missing`. `NormalGL` means OpenGL (Y+) normal — Unity expects Y+; `NormalDX` needs green-channel flip at import.
- **Enforced by:** intake naming check; `BuildMaterials()`. **Source:** HH.

## Generator temps and junk
- **Symptom:** `ComfyUI_temp*.png`, `gpt-image-*.png`, `New Folder/`, stray `New Scene.unity` inside Assets.
- **Rule:** junk is left where it is (V57 never moves/deletes provider files or writes `Source/`), excluded from the manifest and never referenced by materials/prefabs; listed in the provider fix list.
- **Enforced by:** intake `fixable`. **Source:** HH `_listings/NOTES.txt`.

## Non-readable meshes
- **Symptom:** 42 BoxColliders approximated from bounds because meshes were not Read/Write; ball behaviour off near rails.
- **Rule:** collision comes from `UCX_*` (convex) or, when brief says `collision: exact`, a MeshCollider with Read/Write enabled by the import rule. Collision sits on the prefab that carries the mesh (`assets-collision-builds.md`).
- **Enforced by:** import rules; physics-setup proof rules. **Source:** HH physics report.

## Stale material remaps
- **Symptom:** after the `Materials/` folders were deleted and rebuilt, every renderer showed magenta: the FBX `.meta` still remapped its material to the old (deleted) `MAT_` GUID, and the remapper skipped the model because a remap hides the embedded material.
- **Rule:** `ModelMaterialRemapper` drops remaps whose target no longer exists, reimports, then remaps to the current `MAT_<Asset>`. Providers never deliver `.meta`; a V57 end-to-end test starts from art without `.meta`.
- **Enforced by:** `ModelMaterialRemapper.DropStaleRemaps`. **Source:** PS end-to-end test.
