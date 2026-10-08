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

### D-011 — Scene names in English, no Blender suffixes
- **When:** 2026-10-07 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** `SCN_Montaje` was built with the Spanish layer groups and the Blender object names from `unity_scene.json` (`Oceano/Estructura/Vestido/Eventos`, `Jellyfish.001`). The owner requires everything generated in a scene to be English and without `.001` duplicates suffixes.
- **Choice:** Layer groups renamed `Ocean/Structure/Dressing/Events`; 175 instances renamed `<Name>_NN` (2 digits). Only `m_Name` values changed; transforms and prefab links untouched. The same rule is now in the core intake (`layouts.json`) and `SCENE_PRODUCTION_STANDARDS.md`. The provider JSON stays as delivered (read-only).
- **Reversal cost:** low · **Type:** other

### D-012 — TDD 1.0.0 (Standard 2.1.0) installed; level contract by markers
- **When:** 2026-10-07 · **Stage:** pre-I0 (owner request) · **Commit:** pending
- **Context:** The owner approved the rebuilt TDD (`Docs/Design/TDD.md`, 1.0.0, gate 19/19): ids aligned with the art delivery (Professor = Wort, Shark replaces the Whale), authored Crash-style camera, Sprat as a no-effect companion, Unity built-in audio, zone-completion saves, no map content in the TDD.
- **Choice:** The TDD scene `SCN_HydroStation_Gameplay` takes the delivered `LevelMaps/Montaje` layout (linked by role, `LAYOUT_LINKED_BY_ROLE`). `SCN_Montaje` stays as the art montage. The playable level needs the §6 level contract from the provider: `Marker_*` empties (spawn, zone, exit, kill, camera zones, patrol ends), `MechanicalCrab` instances, and the layout re-exported with `Fly` / `Jellyfish` / `Shark` ids.
- **Reversal cost:** low · **Type:** other

## D-001 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/DefaultVolumeProfile.asset`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: d42ec780fe3c -->

## D-002 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/Settings/URP_Asset.asset`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: a14abbc523f3 -->

## D-003 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/Settings/URP_ForwardRenderer.asset`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 78a973a0b7b7 -->

## D-004 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/UniversalRenderPipelineGlobalSettings.asset`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 414fde389b2d -->

## D-005 · I0 INTAKE · fixable · LAYOUT_LINKED_BY_ROLE
- **Issue:** layout Montaje has no TDD scene SCN_Montaje*
- **Where:** `Docs/Design/LevelMaps/Montaje/unity_scene.json`
- **Decision:** linked to the only gameplay scene SCN_HydroStation_Gameplay
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 47616c33533d -->

## D-006 · I0 INTAKE · fixable · SLICE_INFERRED
- **Issue:** no scene_manifest entry with slice: true matches a TDD scene
- **Where:** `Docs/Design/TDD.md:§13.2`
- **Decision:** slice scene inferred as SCN_HydroStation_Gameplay (first gameplay scene of §13.2)
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b5b47e428c69 -->

## D-007 · I0 INTAKE · fixable · LAYOUT_LAYER_RENAMED
- **Issue:** layer names are not English PascalCase: Oceano→Ocean, Eventos→Events, Estructura→Structure, Vestido→Dressing
- **Where:** `Docs/Design/LevelMaps/Montaje/unity_scene.json`
- **Decision:** scene groups use the English names; provider should export English layer names
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0a6e454cbbaa -->

## D-008 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fish/Textures/Y_Prop_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fish_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 91255916fcc1 -->

## D-009 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fish/Textures/Y_Prop_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fish_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 31579e21c6df -->

## D-010 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fish/Textures/Y_Prop_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fish_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: cfb4bf85787f -->

## D-011 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fish/Textures/Y_Prop_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fish_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 96a0fad55c43 -->

## D-012 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fly/Textures/Fly_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fly_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 25adef83e756 -->

## D-013 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fly/Textures/Fly_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fly_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 852de5bbe1de -->

## D-014 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fly/Textures/Fly_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fly_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3956015b4a74 -->

## D-015 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fly/Textures/Fly_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fly_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1ca63709fdc4 -->

## D-016 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Fly/Textures/Fly_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Fly_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 820167a7794e -->

## D-017 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Jellyfish/Textures/H_Prop_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Jellyfish_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3d867e1b1e6b -->

## D-018 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Jellyfish/Textures/H_Prop_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Jellyfish_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 43e740330504 -->

## D-019 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Jellyfish/Textures/H_Prop_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Jellyfish_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a8cd0673a0e9 -->

## D-020 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Jellyfish/Textures/H_Prop_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Jellyfish_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 62f50a110837 -->

## D-021 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Jellyfish/Textures/H_Prop_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Jellyfish_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2c480a6d9ecd -->

## D-022 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/MechanicalCrab/Textures/Mechanical_Crab_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_MechanicalCrab_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: cbb8d16e4375 -->

## D-023 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/MechanicalCrab/Textures/Mechanical_Crab_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_MechanicalCrab_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 92f9ab09f578 -->

## D-024 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/MechanicalCrab/Textures/Mechanical_Crab_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_MechanicalCrab_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 77c264cc4043 -->

## D-025 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/MechanicalCrab/Textures/Mechanical_Crab_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_MechanicalCrab_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 496202723dd1 -->

## D-026 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/MechanicalCrab/Textures/Mechanical_Crab_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_MechanicalCrab_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 84a74128c3b9 -->

## D-027 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Professor/Textures/Professor_Wort_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Professor_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 33724c6b8b07 -->

## D-028 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Professor/Textures/Professor_Wort_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Professor_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7a507f8b87c5 -->

## D-029 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Professor/Textures/Professor_Wort_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Professor_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 42df953365ba -->

## D-030 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Professor/Textures/Professor_Wort_normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Professor_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 96a7a37340ea -->

## D-031 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Professor/Textures/Professor_Wort_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Professor_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a7bd669e779e -->

## D-032 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Shark/Textures/Radioactive_Shark_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Shark_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: da99412bbd49 -->

## D-033 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Shark/Textures/Radioactive_Shark_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Shark_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6fad8d89baf6 -->

## D-034 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Shark/Textures/Radioactive_Shark_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Shark_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5ae1850bc824 -->

## D-035 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Shark/Textures/Radioactive_Shark_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Shark_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b432672150d6 -->

## D-036 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Shark/Textures/Radioactive_Shark_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Shark_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f00f74fdd2bc -->

## D-037 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Sprat/Textures/Sprat_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Sprat_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2909bc5318ea -->

## D-038 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Sprat/Textures/Sprat_metallic.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Sprat_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2bfd4600bcb3 -->

## D-039 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Sprat/Textures/Sprat_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Sprat_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: fced321798f4 -->

## D-040 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Sprat/Textures/Sprat_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Sprat_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1feead9a09a2 -->

## D-041 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Characters/Sprat/Textures/Sprat_roughness.JPEG`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_Sprat_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d469a162afeb -->

## D-042 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Meshes/AST-ENV-CORALROCK-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVCORALROCK001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: e5188debee22 -->

## D-043 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_AST-ENV-CORALROCK-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b2b6a9372af7 -->

## D-044 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Meshes/AST-ENV-HYDRODOMETOWER-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVHYDRODOMETOWER001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 671b0b2c3145 -->

## D-045 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_AST-ENV-HYDRODOMETOWER-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 78a375e5e3b5 -->

## D-046 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-WATERSURFACE-001/Meshes/AST-ENV-WATERSURFACE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVWATERSURFACE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 195a2670a1ae -->

## D-047 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Meshes/AST-ENV-B17ARCH-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVB17ARCH001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d10c7ed918a6 -->

## D-048 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_AST-ENV-B17ARCH-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 08482deb91c4 -->

## D-049 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Meshes/AST-ENV-B17FLOORPLATE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVB17FLOORPLATE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 40b2e860c188 -->

## D-050 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_AST-ENV-B17FLOORPLATE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 06a387aedace -->

## D-051 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Meshes/AST-ENV-BULKHEAD-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVBULKHEAD001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7e5dcb003248 -->

## D-052 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_AST-ENV-BULKHEAD-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 90eef667a0e4 -->

## D-053 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Meshes/AST-ENV-CRYSTALPANEL-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVCRYSTALPANEL001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7c29e771584a -->

## D-054 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_AST-ENV-CRYSTALPANEL-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a24f90821424 -->

## D-055 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Meshes/AST-ENV-GRATEPLATFORM-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVGRATEPLATFORM001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6fad4b6a7ca8 -->

## D-056 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_AST-ENV-GRATEPLATFORM-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 084e2981f330 -->

## D-057 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Meshes/AST-ENV-HATCHPANEL-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVHATCHPANEL001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 302228b5fa21 -->

## D-058 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_AST-ENV-HATCHPANEL-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: adedf5dafdf9 -->

## D-059 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Meshes/AST-ENV-LABDOOR-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVLABDOOR001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 390a095834f2 -->

## D-060 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_AST-ENV-LABDOOR-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a9187da2dec5 -->

## D-061 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Meshes/AST-ENV-LABFLOOR-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVLABFLOOR001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 26f41f28699d -->

## D-062 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_AST-ENV-LABFLOOR-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: db6ac178d703 -->

## D-063 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVPIPE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d80406b29a96 -->

## D-064 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_AST-ENV-PIPE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 746465d23c3b -->

## D-065 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Meshes/AST-ENV-PIPE-002.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVPIPE002); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7c4ca167091d -->

## D-066 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_AST-ENV-PIPE-002_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8c6e3ba90265 -->

## D-067 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PLATFORMMODULE-001/Meshes/AST-ENV-PLATFORMMODULE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVPLATFORMMODULE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8ab035f87809 -->

## D-068 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PLATFORMMODULE-001/Textures/AST-ENV-PLATFORMMODULE-001_AST-ENV-PLATFORMMODULE-001_Albedo.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPLATFORMMODULE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1464560fce56 -->

## D-069 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Meshes/AST-ENV-SCAFFOLD-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVSCAFFOLD001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 937ec3409880 -->

## D-070 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_AST-ENV-SCAFFOLD-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: cc5ffc39ce63 -->

## D-071 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Meshes/AST-ENV-STEELCOLUMN-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVSTEELCOLUMN001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 9ebd43489858 -->

## D-072 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_AST-ENV-STEELCOLUMN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 39fad0774755 -->

## D-073 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Meshes/AST-PROP-BEACONCRATE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPBEACONCRATE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: befdccc890e5 -->

## D-074 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_AST-PROP-BEACONCRATE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: e9fb0359af59 -->

## D-075 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Meshes/AST-PROP-CONSOLE-002.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPCONSOLE002); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 76e22435b75f -->

## D-076 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_AST-PROP-CONSOLE-002_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ef5feae611f9 -->

## D-077 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Meshes/AST-PROP-HOSE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPHOSE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d2a8063496b7 -->

## D-078 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_AST-PROP-HOSE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4dd83f3411ee -->

## D-079 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Meshes/AST-PROP-LANTERN-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPLANTERN001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d3011951ee00 -->

## D-080 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_AST-PROP-LANTERN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d254468d4af1 -->

## D-081 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Meshes/AST-PROP-RAILING-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPRAILING001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bfada8749da8 -->

## D-082 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_AST-PROP-RAILING-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f70a5a2a5e76 -->

## D-083 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Meshes/AST-PROP-WARNINGSIGN-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPWARNINGSIGN001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: edb023156303 -->

## D-084 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_AST-PROP-WARNINGSIGN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ffe94981b86d -->

## D-085 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-ACCESSKEY-001/Meshes/AST-PROP-ACCESSKEY-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPACCESSKEY001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b64f328b1a23 -->

## D-086 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Meshes/AST-PROP-CONSOLE-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPCONSOLE001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 107fdf0f9877 -->

## D-087 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_AST-PROP-CONSOLE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a99989e795cc -->

## D-088 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Meshes/AST-PROP-HATCH-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPHATCH001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: fb09fcec65a2 -->

## D-089 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_AST-PROP-HATCH-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 828ff75ffdd7 -->

## D-090 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-ZONEDOOR-001/Meshes/AST-PROP-ZONEDOOR-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTPROPZONEDOOR001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 37a7a5169583 -->

### D-013 — Temporary test placement on the delivered test map
- **When:** 2026-10-07 · **Stage:** M1 · **Commit:** pending
- **Context:** The delivered layout (`LevelMaps/Montaje`, owner: test map only) has no §6 level-contract markers, no crabs, and its flies/creatures use old `AST-CHAR-*` ids with no model, so no zone can be played.
- **Options:** A) stop until the provider delivers the contract; B) temporary V57 test placement inside the level containers; C) hand-edit the provider JSON (forbidden).
- **Choice:** B. A compact test zone on the flat start platform (x −26…−15, y 2.2, z 3…9): `Marker_Spawn_Player`, `Marker_Zone_Z1`, `Marker_Exit_Z1`, `Marker_Kill_01`, `Marker_CameraZone_Default` (yaw 90°, level forward = +X), 6 `Fly`, 1 static `MechanicalCrab` (with a fly above it, LR-07), a test `AccessKey` and `ZoneDoor`, and the `Shark` at its layout position. It lives under `_Environment/_Markers` and `_Gameplay/Level/TestPlacement`, so `RebuildLevelContent` removes it when the final map arrives. The provider's own key/door (outside `Marker_Zone_Z1`) are ignored with a contract warning.
- **Reversal cost:** low (delete `TestPlacement`) · **Type:** other

### D-014 — Duplicate Visual prefabs after the intake naming change
- **When:** 2026-10-07 · **Stage:** I2 · **Commit:** pending
- **Context:** I2 built 26 `PRF_ASTENV…/ASTPROP…_Visual` prefabs (new sanitized asset names) for models that already had `PRF_AST-ENV-…_Visual` prefabs used by both scenes.
- **Choice:** deleted the 26 unused duplicates; core fix (V57 `5fa392c`, "one Visual prefab per model") reuses the existing prefab for a model instead of creating a second one.
- **Reversal cost:** low · **Type:** revert

### D-015 — Amber ramp spans the whole key ceremony
- **When:** 2026-10-07 · **Stage:** M1 · **Commit:** pending
- **Context:** TDD §B KeyMaterialisation feedback says "dissolve-in 0.4 s, amber 0 → 4.0" while AC-KEY-02 requires the light to reach 4.0 at 1.2 s ± 0.05 s.
- **Choice:** the acceptance criterion wins: the amber light ramps 0 → 4.0 over the 1.2 s ceremony (`AccessKeyConfig.amberRampDuration = 1.2`); the visual appears at ceremony start.
- **Reversal cost:** low (config value) · **Type:** conflict

### D-016 — Test route direction and key-shot framing
- **When:** 2026-10-07 · **Stage:** M1 · **Commit:** pending
- **Context:** The first green gold path (20261008T002239Z) had the follow camera behind the start bulkhead (Professor hidden) and the 60° key shot inside a glass wall of the tunnel.
- **Choice:** the test route (D-013) runs towards −X from the open end of the tunnel (`Marker_CameraZone_Default` yaw 270°, spawn x −15.6, door + exit next to the bulkhead); the key shot keeps the follow-camera direction, higher and farther (`CameraRig`, offset (0, 4.2, −8.5) rotated by the active yaw).
- **Reversal cost:** low · **Type:** other

### D-017 — Adopted specs follow the implementation layout
- **When:** 2026-10-07 · **Stage:** M1 · **Commit:** pending
- **Context:** TDD §C paths (`Scripts/Gameplay/Locomotion/…`) differ from the implemented areas (`Gameplay/Player`, `Gameplay/Config`, `Gameplay/Flow`, …).
- **Choice:** the V57-owned specs in `V57/specs/ProfessorWortSprat/features/` point to the real files (28 paths aligned); the TDD is unchanged (its paths are suggestions per TDD Standard 2.1).
- **Reversal cost:** low · **Type:** absorption

### D-018 — Animator states by cross-fade instead of parameters
- **When:** 2026-10-07 · **Stage:** M1 · **Commit:** pending
- **Context:** TDD §B-S lists Animator parameters (Speed, Grounded…), but the V57-generated `AC_<Asset>` controllers have one state per delivered clip and no transitions.
- **Choice:** `CharacterAnimationDriver` (and the Sprat/crab/fly/creature controllers) cross-fade to the named clip state from mechanic state (`AnimatorStatePlayer`); gameplay code never calls the Animator directly.
- **Reversal cost:** low · **Type:** other
