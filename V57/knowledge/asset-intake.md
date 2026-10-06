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
