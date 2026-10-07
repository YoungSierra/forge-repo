# DECISIONS — `ProfesorSprat`

Append-only log of choices V57 made instead of asking. Ids are sequential (`D-001`, `D-002`, …) and referenced from commits (`[D-###]`), STATUS.json, TODO.md and reports. The owner reviews asynchronously; to reverse a decision, add a new entry that supersedes it.

Authority order used for every decision: **TDD > ADD > V57 defaults**.

## Entry template

```markdown
### D-001 — <short title>
- **When:** <UTC> · **Stage:** <I0..M4> · **Commit:** <sha or pending>
- **Context:** <what was ambiguous / failing; evidence path>
- **Options:** A) … B) … C) …
- **Choice:** <A/B/C> — <why, citing TDD/ADD section or V57 rule>
- **Reversal cost:** low | medium | high — <what would need redoing>
- **Type:** absorption | conflict | placeholder | cut | timebox | revert | tolerance | other
```

---

<!-- entries below, newest last -->

### D-001 — Owner-directed scenery montage before intake
- **When:** 2026-10-06 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** No TDD/ADD yet; owner asked for a placement-only montage of the 3D kit from `Docs/Design/LevelMaps/Montaje/unity_scene.json` + `manifest.json` (Blender 5.1.2 export, contract `unity_scene/1.1`, `loopforge_unity_pkg/1.0`).
- **Choice:** Placement only: no colliders, no gameplay logic, no lighting/camera/post (scene keeps Unity's default camera + light). Everything is visual until the TDD and ADD arrive.
- **Reversal cost:** low · **Type:** other

### D-002 — Delivery files kept with their original names
- **When:** 2026-10-06 · **Stage:** pre-I0 · **Commit:** pending
- **Context:** Files use `AST-<CAT>-<NAME>-NNN` (hyphens, caps) and `<id>_<id>_Albedo.jpg`, not the contract's `SM_/SK_/T_` naming. Owner: do not rename this time.
- **Choice:** Placed in the contract folders without renaming: `AST-CHAR-*` → `Art/Characters/<id>/`; structural `AST-ENV-*` → `Art/Environment/Kits/<id>/`; CORALROCK, HYDRODOMETOWER, WATERSURFACE → `Art/Environment/Decoration/<id>/`; `AST-PROP-*` → `Art/Props/Gameplay|Dressing/<id>/` by JSON layer (`Gameplay` vs `Vestido`). Each with `Meshes/` and `Textures/`. The two JSON files live in `Docs/Design/LevelMaps/Montaje/` (closest contract slot to a level layout).
- **Reversal cost:** medium (moving files changes paths, GUIDs survive with `.meta`) · **Type:** absorption

### D-003 — Materials from the Blender manifest
- **When:** 2026-10-06 · **Stage:** pre-I0 · **Commit:** pending
- **Context:** Only albedo maps are delivered; NEONFLY, WATERSURFACE, ACCESSKEY and ZONEDOOR have none. The manifest defines base color, metallic, smoothness and double-sided per material.
- **Choice:** `<AssetFolder>/Materials/MAT_<asset_id>.mat` (URP Lit) with exactly the manifest values (albedo → `_BaseMap`, `base_color`, `metallic`, `smoothness`, double-sided → Render Face Both), remapped on each FBX. The four flat-colour materials are the provider's authored values, not placeholders.
- **Reversal cost:** low · **Type:** absorption

### D-004 — Visual prefabs and scene
- **When:** 2026-10-06 · **Stage:** pre-I0 · **Commit:** pending
- **Choice:** `Prefabs/Visual/<Category>/PRF_<asset_id>_Visual.prefab` = empty root at identity + the FBX as child (JSON note and V57 convention). `Scenes/SCN_Montaje.unity`: `_Environment/<capa>/<nombre>` with the JSON position/rotation/scale as-is, all static. FBX import untouched apart from V57 import rules (scale 1, no cameras/lights).
- **Reversal cost:** low · **Type:** other

### D-005 — JSON axis verdict: correct, no corrections needed
- **When:** 2026-10-06 · **Stage:** pre-I0 · **Commit:** pending
- **Context:** Owner asked to verify the FBX come at rotation 0 / scale 1 and whether JSON rotations are faulty.
- **Evidence:** all 29 FBX import with root rotation (0,0,0), scale (1,1,1), a single mesh, metres, Y-up, pivot at base (min Y = 0); bounds match `bounds_blender` (height = Blender Z). Off-screen top and front orthographic captures of SCN_Montaje match the owner's Blender Top/Front screenshots (shark head left with the radiation fin toward the front view, hydrodome towers, coral rocks, bulkhead rooms, crystal-panel roofs closing the rooms). Scene is not mirrored; `export_yaw_z_deg: 180` is already compensated in the FBX.
- **Choice:** no import-side rotation fix. Only non-unit scales are deliberate (HYDRODOMETOWER ×1.385 / ×0.692, WATERSURFACE ×2.5 in XZ).
- **Reversal cost:** low · **Type:** other

### D-006 — Equirectangular skybox
- **When:** 2026-10-06 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** Owner delivered `SkyBox_1.png` (2048×1024 PNG, 360° lat-long) for the montage.
- **Choice:** Original PNG kept unrenamed in `Art/Environment/Sky/`. Import: 2D, sRGB, no mipmaps (avoids the lat-long seam), wrap U repeat / V clamp, max 2048, HQ compression. `Art/Environment/Sky/Materials/MAT_SkyBox_1.mat` = `Skybox/Panoramic` (Latitude-Longitude, 360°, exposure 1, rotation 0), assigned to `SCN_Montaje` RenderSettings; environment lighting (Unity default: ambient from skybox) now follows it. Exposure, rotation and fog wait for the ADD.
- **Reversal cost:** low · **Type:** other

### D-007 — Rigged characters integrated (7 SK_ + 32 ANIM_)
- **When:** 2026-10-07 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** Owner delivered Professor, Shark, Sprat, Fish, Fly, Jellyfish and MechanicalCrab, each with `SK_<Id>.fbx`, `ANIM_<Id>_<Clip>.fbx`, maps (`_albedo`, `_normal`, `_MetallicSmoothness`, plus `_metallic`/`_roughness`) and a Blender sidecar `<Id>_export.json` (clips, frame ranges, loop flags, export settings).
- **Choice:** Contract layout `Art/Characters/<Id>/{Meshes,Animations,Textures}` with original names; the sidecar stays at `Art/Characters/<Id>/<Id>_export.json` (it feeds the import rules). Rig Generic (no manifest says humanoid), avatar created on `SK_` and copied to every `ANIM_`. Materials `<Id>/Materials/MAT_<Id>.mat` (URP Lit: albedo, normal, `_MetallicSmoothness` as metallic map with smoothness from alpha; Fish has no packed map → metallic 0, smoothness 0.5). The crab's `M_Crab_EyeLens` keeps the FBX-authored colours as `MAT_MechanicalCrab_EyeLens`. Texture max size 2048 (4K delivered; PC_Mid budget). `Data/Animation/AC_<Id>.controller` holds every clip as a state with a looping default (Professor/Sprat/Crab `Idle`, Fish/Fly `HoverIdle`, Jellyfish `PulseIdle`, Shark `PassiveTransit`); no transitions or parameters yet (gameplay specs own them). `Prefabs/Visual/Characters/PRF_<Id>_Visual.prefab` = identity root + model with Animator (root motion off).
- **Reversal cost:** low · **Type:** absorption

### D-008 — Import fixes for the character FBX (V57 assembly rules)
- **When:** 2026-10-07 · **Stage:** pre-I0 · **Commit:** pending
- **Context:** (1) The armature and mesh objects carry a −90° X object rotation (`root.rot_deg [-90,0,0]` in every sidecar; rotation not applied in Blender), so inside each FBX the `<Rig>` and mesh nodes import at 270° X; the file root is (0,0,0)/(1,1,1). Unity's *Bake Axis Conversion* was tried and rejected: it flips the nodes to +90° and turns the model 180°. (2) `ANIM_` files contain only the armature; Unity collapsed that single root node, so curves bound to `Def-root/…` instead of `<Rig>/Def-root/…` and the root −90° curve landed on the Animator object: characters lay on their backs and bones did not animate. (3) The name heuristic marked 6 looping clips as one-shot.
- **Choice:** keep axis conversion off; `preserveHierarchy = true` for every `ANIM_` (now a rule in `ModelImportRules`); loop flags read from the export sidecar when no asset manifest entry exists (`CharacterExportLookup`). Verified with sampled-pose captures (`Docs/V57/evidence/characters/`).
- **Provider fix (recommended):** apply rotation/scale on armature + mesh in Blender before export (or export with *Apply Transform*), so internal nodes come at 0° as the contract asks.
- **Reversal cost:** low · **Type:** absorption

### D-009 — Facing: rigged characters +Z, static AST models −Z (superseded in the scene by D-010)
- **When:** 2026-10-07 · **Stage:** pre-I0 · **Commit:** pending
- **Context:** Side-by-side top capture with the same yaw: `SK_Shark` faces +Z (Unity convention), the static `AST-CHAR-RADIOACTIVESHARK-001` faces −Z (its `export_yaw_z_deg: 180`); the montage JSON rotations were authored for the AST orientation.
- **Choice:** `SCN_Montaje` keeps the static AST characters for now. Swapping them for the rigged prefabs needs +180° yaw per instance (owner to confirm).
- **Reversal cost:** low · **Type:** other

### D-010 — Static AST characters replaced by the rigged deliveries
- **When:** 2026-10-07 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** The rigged deliveries supersede three static montage assets: `AST-CHAR-RADIOACTIVESHARK-001` → `Shark` (same mesh), `AST-CHAR-JELLYFISH-001` → `Jellyfish` (same mesh), `AST-CHAR-NEONFLY-001` (flat-colour sphere) → `Fly`.
- **Choice:** In `SCN_Montaje` the 13 instances (1 shark, 6 jellyfish, 6 flies) now use `PRF_Shark/Jellyfish/Fly_Visual`, keeping the JSON position/scale and adding +180° yaw (D-009 facing). The old `Art/Characters/AST-CHAR-*` folders (FBX, textures, materials) and `PRF_AST-CHAR-*_Visual` prefabs are deleted; 0 missing prefab instances. The provider's `unity_scene.json` still names the AST ids (read-only; mapping above). Captures: `evidence/montage/*_rigged.png` (layout unchanged vs the Blender reference).
- **Reversal cost:** low (git history) · **Type:** other
