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
- **Rule (contract 1.1 extras):**
  - **Markers:** a separate `marcadores` array is read as markers.
    - Names: `Marker_*` kept; `MarkerPatrol_X` → `Marker_Patrol_X`; otherwise `Marker_<tipo>_<id>`.
    - A `volumen` BOX scales by `tamano`. Other shapes are points, with `volume_shape` / `volume_radius` props.
    - A Patrol marker moves to the END of its path, which is the V57 meaning of `Marker_Patrol`.
  - **Props:** every object's and marker's `props` reach the scene as `V57LayoutProps`, flattened to strings.
  - **Paths:** `trayectorias` become `_Environment/_Paths/<Name>` with a `V57Path` (world polyline, `Find(name)`, `Evaluate(distance)`).
  - **`layout_aliases.json` options:**
    - `aliases.<id>.static: false` keeps instances dynamic, e.g. vertex-animated plants. Static batching breaks object-space shaders.
    - `marker_actors.<tipo>` = `{ prefab, layer, anchor: marker|path }` instances a gameplay prefab per marker, named after the marker id. With `anchor: path` it sits at the start of the path, with local +X towards the end.
  - **Re-deliveries:**
    - A remap whose source material is no longer in the FBX (renamed by the provider) is dropped and re-pointed.
    - A material tagged `V57KeepShader=true` keeps its Lit-compatible game shader through `BuildMaterials`.
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

## LODGroups on re-delivery
- **Symptom:** console flooded with "Renderer … is registered with more than one LODGroup". Some models also drew LOD0, LOD1 and LOD3 on top of each other.
- **Cause:**
  - **Duplicate groups:** a re-delivered FBX imported with its own LODGroup, and the Visual prefab added a second one on its root.
  - **Missing group:** a kept prefab was built when the FBX had no LOD nodes and was never updated.
- **Rule:**
  - `LodGroupBuilder` adds no LODGroup when the imported model already has one.
  - `BuildVisualPrefabs` refreshes the LODGroup of kept prefabs to the model's current `_LOD#` nodes. Gaps such as LOD0/1/3 are valid levels.
- **Enforced by:** `LodGroupBuilder.Refresh`, `VisualPrefabBuilder.RefreshLods`. **Source:** PS V10.

## Per-asset rules instead of one-off fixes
- **Symptom:** in PS V10 a lot was fixed by hand or with throwaway scripts: which pieces collide and how, colliders re-fitted after a re-delivery, two-sided shadows on flipped plates, a game shader on two materials, effects placed on every plant. None of it would survive the next delivery or a rebuild.
- **Rule:** what a delivery without ADD briefs cannot say goes into **`Docs/V57/asset_rules.json`** (V57 game data). Each rule has a `match` (asset id, `*` wildcard; case, `-` and `_` ignored) plus:
  - `collision`: `none | simple | exact | mesh | convex`.
  - `shadows`: `on | off | two_sided`.
  - `shader`: a game shader the generated material keeps.
  - `attach`: a list of `{prefab, offset, align}`.
- **How it applies:**
  - **Intake:** merges the rules into `asset_manifest`, later rules winning, and reports unused or invalid rules.
  - **Visual prefabs:** get the colliders and shadows on every build. Kept prefabs of ruled assets are refreshed when the model changes, so colliders follow re-delivered meshes. UCX colliders are never touched.
  - **Materials:** `BuildMaterials` keeps the rule's shader.
  - **Layout:** places the `attach` prefabs at every instance, under `_Environment/_Attachments/<Prefab>`, rebuilt with the map.
- **Scope:** genre-agnostic. The values are per game (e.g. a vertex-animated plant shader or bubbles on algae). Collision that needs behaviour (doors, moving parts) still lives on a gameplay prefab; a rule `collision: none` keeps the Visual prefab free of a second collider.
- **Enforced by:** `intake/lib/asset-rules.js`, `PrefabColliderBuilder`, `AssetRulesApplier`, `VisualPrefabBuilder.RefreshKept`, `MaterialBuilder`, `LayoutSceneBuilder.PlaceAttachments`. **Source:** PS V10 (D-275).

## Deliveries that arrive as a loose folder
- **Symptom:** a V10 delivery came as a downloaded folder. A hand-written copy script would have deleted the earlier normal and metallic maps, which the new delivery did not resend.
- **Rule:** place it with `node V57/tools/intake/delivery.js --from <folder> --repo . [--new-root Assets/_Game/Art/<Category>]`. It runs dry by default; `--apply` copies.
  - **Same name:** replaced in place; `.meta` and GUID kept.
  - **Same name ignoring `-`, `_` and case:** replaced in place and listed for the provider fix list.
  - **New models:** go to `<new-root>/<Asset>/Meshes`, with their `<stem>_*` textures in `Textures`.
  - **Everything else:** listed as unplaced.
  - **Earlier files the delivery lacks:** reported as kept. Nothing is ever deleted or renamed.
  - Run the intake afterwards.
- **Enforced by:** `intake/lib/delivery.js` (+ test). **Source:** PS V10.

## Collision-only prefabs are scenery
- **Rule:** a gameplay prefab `PRF_<Asset>` with no game scripts carries only collision (Unity's own components such as URP light data don't count). Examples are walls, floors, crates and railings. The layout places it under `_Environment` as static scenery, not in `_Gameplay/Level`, so it keeps static batching.
- **Enforced by:** `LayoutSceneBuilder` (`HasGameScripts`). **Source:** PS V10.

## Non-readable meshes
- **Symptom:** 42 BoxColliders approximated from bounds because meshes were not Read/Write; ball behaviour off near rails.
- **Rule:** collision comes from `UCX_*` (convex) or, when brief says `collision: exact`, a MeshCollider with Read/Write enabled by the import rule. Collision sits on the prefab that carries the mesh (`assets-collision-builds.md`).
- **Enforced by:** import rules; physics-setup proof rules. **Source:** HH physics report.

## Stale material remaps
- **Symptom:** after the `Materials/` folders were deleted and rebuilt, every renderer showed magenta: the FBX `.meta` still remapped its material to the old (deleted) `MAT_` GUID, and the remapper skipped the model because a remap hides the embedded material.
- **Rule:** `ModelMaterialRemapper` drops remaps whose target no longer exists, reimports, then remaps to the current `MAT_<Asset>`. Providers never deliver `.meta`; a V57 end-to-end test starts from art without `.meta`.
- **Enforced by:** `ModelMaterialRemapper.DropStaleRemaps`. **Source:** PS end-to-end test.
