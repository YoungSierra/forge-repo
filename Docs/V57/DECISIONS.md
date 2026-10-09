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

### D-019 — PlayMode criteria as component tests; acceptance through the gold path
- **When:** 2026-10-07 · **Stage:** F · **Commit:** pending
- **Context:** Several TDD PlayMode criteria are system-level (double `OnZoneComplete`, late fly registration, stun reactions) and cannot be produced by player input; the V57 rule forbids direct gameplay calls only in input-driven acceptance tests (`Tests/PlayMode/Acceptance*`).
- **Choice:** each criterion is a PlayMode test (`*PlayTests`) in its §13.2 test scene with real FixedUpdate physics, driving the mechanic through its public API or publishing the triggering event; no `Physics.Simulate`, time-scale changes or collision disabling. The full input-driven acceptance is the gold path (real Input System devices on the real scene). `MoveBasis` was extracted from `InputHandler` so AC-LOC-05 is testable.
- **Reversal cost:** medium (input-driven variants per criterion) · **Type:** other

### D-020 — M3 closed after round 2 (remaining defects are delivery gaps)
- **When:** 2026-10-07 · **Stage:** M3 · **Commit:** pending
- **Context:** Independent review rounds 1 and 2 scored 5–6 (min 7). After the round-1 fixes (camera, MSAA) the 3 worst defects of round 2 are: the delivered ZoneDoor has no texture (flat colour from the manifest), the delivered sky/glass art is saturated cyan against the TDD §8 "30 % desaturated background", and the key glyph is text because `ICO_AccessKey` is missing; the main menu has no UI art.
- **Options:** A) a 3rd round — no V57-side fix exists without placeholders or editing provider art; B) close M3 as failed-timeboxed and carry the defects to M4.
- **Choice:** B. M3 gate = failed-timeboxed; defects listed as open in the M4 report and in MISSING_ASSETS (provider deliveries). Gameplay items keep their tool-verified status.
- **Reversal cost:** low (re-run the review when the art arrives) · **Type:** timebox

### D-021 — Test route left to right; smoothed body and camera; closed-loop gold path
- **When:** 2026-10-07 · **Stage:** post-M4 owner feedback · **Commit:** pending
- **Context:** Owner review: spawn and exit too close together, flies bunched at one end, a strange camera motion when moving or jumping, and the gold path failing at the crab stomp (timed open-loop steps against a patrolling crab). Cause of the camera motion: the CharacterController moves in FixedUpdate (50 Hz) while frames render at 300+ fps, so the followed transform stepped; also the composer's look-ahead included Y, so the camera pitched on every jump, and Y damping was 0.05 instead of the TDD §11.5 0.3 s.
- **Choice:**
  - **Route (replaces D-016):** the test placement (D-013) runs the whole map from left to right (+X, `Marker_CameraZone_Default` yaw 90°). Spawn at the open mouth of the left tunnel (x −16.6), one fly on each platform, the map's own key anchor (x 8.1) and zone door (right tunnel arch), the crab patrolling across the right tunnel (`Marker_Patrol_MechanicalCrab_01`), exit inside the right tunnel (x 20.5).
  - **Body:** physics stays at a fixed step. `BodyInterpolation` places the model (`SK_Professor`) between the last two physics poses each frame; camera, Sprat and the drop shadow follow `ProfessorLocomotion.Body`.
  - **Camera:** CinemachineBrain update LateUpdate; position damping 0.3 s on every axis; look-ahead 0.25 s ignores Y, with smoothing 0.3.
  - **Gold path:** closed loop. A held `move` (V57 core: `move` without `seconds` is held until `release`) plus `wait_until` on probe positions. A new game probe key `crab.distance` times the stomp.
- **Reversal cost:** low · **Type:** other

## D-091 · I0 INTAKE · conflict · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_AO.png`
- **Decision:** file kept as-is; imported by folder rule; provider should rename in a later delivery
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: cba067e1b240 -->

## D-092 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 41fc36e92971 -->

## D-093 · I0 INTAKE · fixable · LAYOUT_ASSET_ALIASED
- **Issue:** layout asset_id(s) placed with another delivered model (Docs/V57/layout_aliases.json): AST-CHAR-JELLYFISH-001→Jellyfish, AST-CHAR-NEONFLY-001→Fly, AST-CHAR-RADIOACTIVESHARK-001→Shark
- **Where:** `Docs/Design/LevelMaps/Level_01/unity_scene.json`
- **Decision:** aliased instances use the mapped model and yaw; provider should export the final asset ids
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3bc1f6947af4 -->

## D-094 · I0 INTAKE · fixable · LAYOUT_LINKED_BY_ROLE
- **Issue:** layout Level_01 has no TDD scene SCN_Level_01*
- **Where:** `Docs/Design/LevelMaps/Level_01/unity_scene.json`
- **Decision:** linked to the only gameplay scene SCN_HydroStation_Gameplay
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 99436de02f76 -->

## D-095 · I0 INTAKE · fixable · LAYOUT_LAYER_RENAMED
- **Issue:** layer names are not English PascalCase: Oceano→Ocean, Eventos→Events, Estructura→Structure, Vestido→Dressing
- **Where:** `Docs/Design/LevelMaps/Level_01/unity_scene.json`
- **Decision:** scene groups use the English names; provider should export English layer names
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b6b45efdbb26 -->

## D-096 · I0 INTAKE · fixable · DOCS_UNEXPECTED
- **Issue:** file not part of the Docs/ contract tree (brief §1)
- **Where:** `Docs/Design/LevelMaps/Level_01/Level_01_Lateral.png`
- **Decision:** ignored by intake
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3e4ebd19287f -->

## D-097 · I0 INTAKE · fixable · DOCS_UNEXPECTED
- **Issue:** file not part of the Docs/ contract tree (brief §1)
- **Where:** `Docs/Design/LevelMaps/Level_01/Level_01_Perspective.png`
- **Decision:** ignored by intake
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5edce3c3dacb -->

## D-098 · I0 INTAKE · fixable · DOCS_UNEXPECTED
- **Issue:** file not part of the Docs/ contract tree (brief §1)
- **Where:** `Docs/Design/LevelMaps/Level_01/Level_01_Top.png`
- **Decision:** ignored by intake
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8c481e85b774 -->

## D-099 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 04de41e2ff0d -->

## D-100 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f5af0a08ec93 -->

## D-101 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: fc6813d0d381 -->

## D-102 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5864ff265d92 -->

## D-103 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-001/Textures/AST-ENV-CORALROCK-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d766656c9dcc -->

## D-104 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Meshes/AST-ENV-CORALROCK-002.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVCORALROCK002); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 63adb696a23b -->

## D-105 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 578903449eca -->

## D-106 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 733655416f73 -->

## D-107 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0d3d940d9a84 -->

## D-108 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b36fcf7d072b -->

## D-109 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 86ad28619112 -->

## D-110 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2b3a97d4b7c9 -->

## D-111 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3db156143efc -->

## D-112 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a48aa11022c1 -->

## D-113 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8cc18b5b8e35 -->

## D-114 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-HYDRODOMETOWER-001/Textures/AST-ENV-HYDRODOMETOWER-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHYDRODOMETOWER001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0a9aee9d9305 -->

## D-115 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Meshes/AST-ENV-OCEANBOAT-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVOCEANBOAT001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4c85de84ec33 -->

## D-116 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST-ENV-OCEANBOAT-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ae7fa9b5a0ad -->

## D-117 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST-ENV-OCEANBOAT-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2f176c3ab4e3 -->

## D-118 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST-ENV-OCEANBOAT-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0b84131119f2 -->

## D-119 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST-ENV-OCEANBOAT-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: da75cc223ce9 -->

## D-120 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST-ENV-OCEANBOAT-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bf9490ab702e -->

## D-121 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Meshes/AST-ENV-OCEANLEAVES-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVOCEANLEAVES001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: aff842505f67 -->

## D-122 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST-ENV-OCEANLEAVES-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 20156bd24062 -->

## D-123 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST-ENV-OCEANLEAVES-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 72668bb35e0e -->

## D-124 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST-ENV-OCEANLEAVES-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: dcf400ae6583 -->

## D-125 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST-ENV-OCEANLEAVES-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6d186a9f38ad -->

## D-126 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST-ENV-OCEANLEAVES-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 557a98d70145 -->

## D-127 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Meshes/AST-ENV-OCEANROCKS-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVOCEANROCKS001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f6e34a49e58e -->

## D-128 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST-ENV-OCEANROCKS-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 098accc356e4 -->

## D-129 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST-ENV-OCEANROCKS-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 48a074355327 -->

## D-130 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST-ENV-OCEANROCKS-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b7b87996344d -->

## D-131 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST-ENV-OCEANROCKS-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1ee463a497ca -->

## D-132 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST-ENV-OCEANROCKS-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8d552d7d9d1b -->

## D-133 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Meshes/AST-ENV-OCEANSKELETON-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVOCEANSKELETON001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2492dfde3135 -->

## D-134 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST-ENV-OCEANSKELETON-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7a58c49c22a9 -->

## D-135 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST-ENV-OCEANSKELETON-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a3f40907f465 -->

## D-136 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST-ENV-OCEANSKELETON-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 979c65b7c508 -->

## D-137 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST-ENV-OCEANSKELETON-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 74a08252affe -->

## D-138 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST-ENV-OCEANSKELETON-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 21850fee194d -->

## D-139 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Meshes/AST-ENV-SEABED-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVSEABED001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2fc2f297bde3 -->

## D-140 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_Albedo.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 76c5e8f4a652 -->

## D-141 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_Detail_Albedo.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1f6546de2b21 -->

## D-142 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_Detail_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 91a06f3f0864 -->

## D-143 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ccf597f83ad6 -->

## D-144 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: adff879e67dc -->

## D-145 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_Roughness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a79b08bd5744 -->

## D-146 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Meshes/AST-ENV-VEGETATION-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVVEGETATION001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 30589e185681 -->

## D-147 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: db429740b6b9 -->

## D-148 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c4f63d82f904 -->

## D-149 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6049a67bdc0d -->

## D-150 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ab1420ecd6d2 -->

## D-151 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 9f98f2f6f4e2 -->

## D-152 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4ed0e817605d -->

## D-153 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0336adf923f1 -->

## D-154 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2fa7e0ea0604 -->

## D-155 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d5fc522bae6b -->

## D-156 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17ARCH-001/Textures/AST-ENV-B17ARCH-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17ARCH001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c14070566bef -->

## D-157 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7b1bdee75ce1 -->

## D-158 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a854532999e9 -->

## D-159 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 256a63e7c1c0 -->

## D-160 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bb507c3eac14 -->

## D-161 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-B17FLOORPLATE-001/Textures/AST-ENV-B17FLOORPLATE-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVB17FLOORPLATE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 41b01ce7bf35 -->

## D-162 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 59af67e495eb -->

## D-163 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2740a34a2ade -->

## D-164 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 18114117b76a -->

## D-165 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f0e382a5cfa1 -->

## D-166 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-BULKHEAD-001/Textures/AST-ENV-BULKHEAD-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVBULKHEAD001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 54be92f1c60f -->

## D-167 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bfaf631f345d -->

## D-168 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f9ec3521af8e -->

## D-169 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6d2da29f2bdc -->

## D-170 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c686e2fad3e6 -->

## D-171 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-CRYSTALPANEL-001/Textures/AST-ENV-CRYSTALPANEL-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCRYSTALPANEL001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b5b2a67fbf41 -->

## D-172 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 9429318a3897 -->

## D-173 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 36dde223c4b8 -->

## D-174 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: cc838cb8f373 -->

## D-175 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f89242aedacc -->

## D-176 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-GRATEPLATFORM-001/Textures/AST-ENV-GRATEPLATFORM-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVGRATEPLATFORM001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c1055ebc7420 -->

## D-177 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7ca17e4709c2 -->

## D-178 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3a4f859d3c26 -->

## D-179 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7bbfbcde081a -->

## D-180 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 41e550adc007 -->

## D-181 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-HATCHPANEL-001/Textures/AST-ENV-HATCHPANEL-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVHATCHPANEL001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 12310bae8212 -->

## D-182 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ac0595f41641 -->

## D-183 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 224c4e51d479 -->

## D-184 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 07e4f4976908 -->

## D-185 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8304a34ebe7a -->

## D-186 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABDOOR-001/Textures/AST-ENV-LABDOOR-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABDOOR001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 985485396fe4 -->

## D-187 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 01482429c948 -->

## D-188 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2a9149fa10dd -->

## D-189 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0d08448cce41 -->

## D-190 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 99fb2a211266 -->

## D-191 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ceeb40beb8fd -->

## D-192 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bef808787684 -->

## D-193 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 76691229428b -->

## D-194 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1e2bc3f1cd17 -->

## D-195 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a03841d5f00f -->

## D-196 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 88023b7662ac -->

## D-197 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 057fd61d6f12 -->

## D-198 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1f1a08efe4d1 -->

## D-199 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: e82dc770c1be -->

## D-200 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 02c515c7f728 -->

## D-201 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-PIPE-002/Textures/AST-ENV-PIPE-002_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVPIPE002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 241d1493c887 -->

## D-202 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: be322e2f5cd6 -->

## D-203 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: e07f87cd8f41 -->

## D-204 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 20fb729f04da -->

## D-205 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7dd7a0ac6e90 -->

## D-206 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-SCAFFOLD-001/Textures/AST-ENV-SCAFFOLD-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSCAFFOLD001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f5cb31c650e7 -->

## D-207 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: e04268c363a9 -->

## D-208 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 17ef79751691 -->

## D-209 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ba9889b4819b -->

## D-210 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8762914ae968 -->

## D-211 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-STEELCOLUMN-001/Textures/AST-ENV-STEELCOLUMN-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSTEELCOLUMN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f87f30bc3322 -->

## D-212 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 32671805d690 -->

## D-213 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d9974bbd9b0b -->

## D-214 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a556e36aa3c4 -->

## D-215 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d1922dcce54f -->

## D-216 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-BEACONCRATE-001/Textures/AST-PROP-BEACONCRATE-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPBEACONCRATE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 16110ad5cf7e -->

## D-217 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a4f11f996c52 -->

## D-218 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 718d2ea88d53 -->

## D-219 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: aa4146a54505 -->

## D-220 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c4afb2434024 -->

## D-221 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-CONSOLE-002/Textures/AST-PROP-CONSOLE-002_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE002_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 747489bc1e94 -->

## D-222 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 9e1a59176c99 -->

## D-223 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a24185714ef4 -->

## D-224 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 532aeecf5b87 -->

## D-225 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8f2286b5d73f -->

## D-226 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-HOSE-001/Textures/AST-PROP-HOSE-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHOSE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8e8dbd47055f -->

## D-227 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 05e33028bf60 -->

## D-228 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f3beae998f7d -->

## D-229 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5fb49c2084f6 -->

## D-230 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 44d0df6a08f2 -->

## D-231 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-LANTERN-001/Textures/AST-PROP-LANTERN-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPLANTERN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ba4b29ea1c69 -->

## D-232 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: fceaef26224c -->

## D-233 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 51e91b794d84 -->

## D-234 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7b0f8dc41750 -->

## D-235 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 85d042ea4606 -->

## D-236 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-RAILING-001/Textures/AST-PROP-RAILING-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPRAILING001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d49abbe46f70 -->

## D-237 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5c8164b99cb3 -->

## D-238 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b6401a83470c -->

## D-239 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: dfcb9d5aef59 -->

## D-240 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ac0548735893 -->

## D-241 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Dressing/AST-PROP-WARNINGSIGN-001/Textures/AST-PROP-WARNINGSIGN-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPWARNINGSIGN001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: cf40df4f39c1 -->

## D-242 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 286d86c13700 -->

## D-243 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: fc026dcdf2c9 -->

## D-244 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: defb09fdbcaf -->

## D-245 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 498e56377d8f -->

## D-246 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-CONSOLE-001/Textures/AST-PROP-CONSOLE-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPCONSOLE001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6ac926150343 -->

## D-247 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a0c15a02c7a4 -->

## D-248 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_Metallic.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4e58152a2c95 -->

## D-249 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 100aec7bbd8b -->

## D-250 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 9c94f098f14a -->

## D-251 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Props/Gameplay/AST-PROP-HATCH-001/Textures/AST-PROP-HATCH-001_Roughness.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTPROPHATCH001_ORM); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f3599c8fcfbc -->

### D-252 — Level_01 V06 delivery integrated (LODs, full maps, new ocean assets)
- **When:** 2026-10-09 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** Owner delivered `Level_01_V06`: `manifest.json` + `unity_scene.json` (817 objects), 33 FBX with LOD groups, albedo/normal/metallic-smoothness maps (+ metallic/roughness), 7 new ocean assets (CORALROCK-002, OCEANBOAT, OCEANLEAVES, OCEANROCKS, OCEANSKELETON, SEABED, VEGETATION). It supersedes `LevelMaps/Montaje`.
- **Choice:**
  - Placed with the D-002 rules: FBX replaced in place (GUIDs kept), new maps added, old `<id>_<id>_Albedo.jpg` removed, new assets in `Art/Environment/Decoration/<id>/`. JSON and captures go in `Docs/Design/LevelMaps/Level_01/`, and `LevelMaps/Montaje` is removed.
  - The `AST-CHAR-*` static exports are not copied (D-010). `Docs/V57/layout_aliases.json` maps them to the rigged `Fly`/`Jellyfish`/`Shark` with +180° yaw; this is a new V57 core intake feature.
  - V57 core fixes found on the way: `_Detail_<map>` textures are not main maps; layout materials apply the manifest's albedo, normal and metallic-smoothness.
  - The floor gameplay prefabs (LABFLOOR, GRATEPLATFORM, B17FLOORPLATE) had colliders sized for the old meshes. The new meshes are larger (1.0 → 1.5 m, 2.0 → 3.7 m), so each BoxCollider is re-fitted to its LOD0 bounds.
  - The 7 new Visual prefabs came with a default BoxCollider. They are removed: the seabed box was 340 × 14 × 340 m and enclosed the whole level, the rest are unreachable scenery, and Visual prefabs carry no collision.
- **Reversal cost:** low (git history) · **Type:** absorption

### D-253 — Temporary level contract on Level_01 (replaces D-013/D-021 placement)
- **When:** 2026-10-09 · **Stage:** post-M4 · **Commit:** pending
- **Context:** Level_01 V06 has no markers, no ZoneDoor instance and no crabs. `RebuildLevelContent` emptied `_Markers` and the old test placement, as designed.
- **Choice:** temporary contract in `_Environment/_Markers` + `_Gameplay/Level/TestPlacement`. A provider map with markers replaces it on the next `RebuildLevelContent`.
  - **Spawn:** left tunnel at x −19.4; the camera, 6.5 m behind, stays inside the tunnel and in front of the closed lab door.
  - **Zone and kill:** Zone_Z1 covers x −30..28. Kill_01 covers y −7..−2, below the lowest platform (top 0.03).
  - **Camera:** default yaw 90°.
  - **Door, crab, exit:** the ZoneDoor sits at x 15 inside the right tunnel. The arch at x 23.6 already holds the closed LABDOOR pair, so a door there would overlap it. The crab patrols across the tunnel at x 18.5, and the exit is at x 21.5.
  - **Map content:** the flies, key and sharks are the provider's own positions. The fly at x −10 floats 1.4 m above its platform and needs a jump.
  - **Gold path:** 79 closed-loop steps, green 3 runs in a row, 0 respawns.
- **Reversal cost:** low · **Type:** other

### D-254 — Player camera orbit with mouse / right stick (owner request)
- **When:** 2026-10-09 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** The owner asked for a camera that moves with the mouse. TDD §11.5 / G-14 says `Look: none (authored camera)`. The TDD is read-only, so this is logged as a conflict won by the owner's direct request.
- **Choice:**
  - **Input:** new `Gameplay/Look` action, bound to `<Mouse>/delta` and `<Gamepad>/rightStick`.
  - **Camera:** `CameraRig.AddLook` orbits the follow camera around the Professor. Yaw is added to the camera-zone yaw. Pitch rotates the authored offset and stays within `CameraConfig.pitchRange` (−20°..35°).
  - **Sensitivity:** mouse 0.15°/px, stick 140°/s. Moving the mouse up looks up.
  - **Movement:** stays camera-relative. `MoveBasis.ToWorld(stick, lookYaw)` adds the orbit immediately, while camera-zone changes keep their deferral.
  - **Key shot:** follows the orbited direction.
  - **Cursor:** locked and hidden while gameplay input is on; released by pause, level end, respawn fade and menus.
  - **Tests:** the gold path's simulated devices have no mouse, so its runs are unchanged.
- **Reversal cost:** low (remove the Look action; LookYaw stays 0) · **Type:** conflict

### D-255 — Camera recentering and zoom (owner request)
- **When:** 2026-10-09 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** Follow-up to D-254: the owner asked for the orbit to recenter and for zoom in/out.
- **Choice:**
  - **Recentering:** after `CameraConfig.recenterDelay` (1.5 s) without look input, yaw and pitch ease back to 0 (`recenterTime` 0.5 s). The camera returns behind the Professor along the camera-zone yaw, and the movement basis follows that ease.
  - **Zoom:** new `Gameplay/Zoom` action, bound to the mouse wheel (`zoomStep` 10 % per notch) and the gamepad d-pad up/down (`stickZoomSpeed` 80 %/s). It scales the follow distance within `zoomRange` 0.6×–1.6× and persists; it is not recentered.
  - **Code:** the logic lives in the plain class `CameraLook`, used by `CameraRig` and covered by 3 EditMode tests.
- **Reversal cost:** low · **Type:** conflict

### D-256 — Lighting: Rayman Legends/Origins-style underwater look, readable shadows, caustics
- **When:** 2026-10-09 · **Stage:** post-M4 (owner request, pending approval) · **Commit:** pending
- **Context:** The owner shared a "Lighting Language" reference, then asked for a look closer to Rayman Legends/Origins and for the scenery to cast visible shadows. No ADD file has been delivered.
- **Choice:**
  - **Key light:** warm sun at 50°/200°, intensity 2.0, shadow strength 0.85.
  - **Shadows:** URP uses 4 cascades over 60 m at 4096 with soft shadows. The crystal panels cast no shadow, so the tunnels receive sun.
  - **Caustics:** a looping flipbook of 64 tiling cookies (`Settings/Rendering/Caustics/T_Caustics_00..63.png`, 256², single channel). They are baked from a procedural Voronoi pattern with domain warping: computed, not drawn, matching the owner's second reference (wobbly bright web, darker cells). `CausticsDrift` plays them at 16 fps on the sun and slides them slowly; tile 5 m, sun 3.0 to compensate the darker cells. The first attempt, a realtime CustomRenderTexture, reached URP black and removed the sun entirely.
  - **Fill:** cyan rim light from the level's forward direction (0.7, no shadows), blue/violet trilight ambient, reflections at 0.5.
  - **Fog:** linear teal from 18 m to 110 m, so the foreground stays crisp and the background layers fade.
  - **Post-processing:** Neutral tonemapping, soft wide bloom, saturation +38, violet-blue shadows and warm highlights, vignette, SSAO renderer feature.
  - **Color grading mode:** LDR. HDR grading rendered the first frame flat cyan.
  - **Camera:** Stop NaN on.
  - **Lamps:** warm point lights on the lantern and beacon-crate Visual prefabs.
- **Reversal cost:** low (volume, cookie, lights and URP settings) · **Type:** other

### D-257 — Follow camera is a Cinemachine FreeLook (owner request)
- **When:** 2026-10-09 · **Stage:** post-M4 (owner request, pending approval) · **Commit:** pending
- **Context:** The owner asked to use Cinemachine's FreeLook camera for the camera movement, because the custom orbit (D-254/D-255) was not smooth enough.
- **Choice:**
  - **Camera:** `CM_Follow` = CinemachineCamera + `CinemachineOrbitalFollow` (sphere, world-space binding, position damping 0.25/0.35/0.25) + RotationComposer (damping 0.25/0.35) + `FreeLookInput`.
  - **Input:** `FreeLookInput` is an `InputAxisControllerBase` with its own reader. It turns mouse per-frame deltas into rates and uses stick and d-pad rates as they are, so both feel the same at any frame rate. The wheel moves one step per notch. Gains come from `CameraConfig`.
  - **Orbit:** `CameraRig` derives the radius and elevation from the authored follow offset. The pitch range, zoom range and recentering (1.5 s wait, 0.5 s ease, axis center) come from `CameraConfig`. The horizontal center follows the camera-zone yaw, and a zone change turns the whole orbit.
  - **Movement basis:** `CameraRig.LookYaw` = orbit value − zone yaw, added immediately.
  - **Cleanup:** the custom `CameraLook` class and its tests are removed.
- **Limitation:** no camera collision. Cinemachine's Deoccluder needs colliders, and the tunnel glass and frames are scenery without collision.
- **Reversal cost:** low · **Type:** conflict

### D-258 — Level music delivered by the owner: intro once, loop the body, playback gain
- **When:** 2026-10-09 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** The owner delivered `Puff_and_Rebellion_2026-10-09T223844.wav` (60 s, 48 kHz, 16-bit stereo) for gameplay and flagged that the volume may be too high. Measured: peak 0 dBFS (2 clipped samples), intro 0–12 s at about −20 dB RMS, then +5–6 dB (body about −15 dB RMS). Tempo about 150 BPM: 12–60 s = 48 s = 30 bars. The file ends at full level, so a whole-file loop would jump back to the soft intro with a click.
- **Choice:**
  - **File:** stored as `Assets/_Game/Audio/Music/MUS_PuffAndRebellion.wav` (V57 name; the content is untouched). Imported by the V57 Music rule: streaming, Vorbis 0.7, load in background.
  - **Looping:** `MusicLooper` (`_Systems/…/AudioService/LevelMusic`, 2 sources, 2D) plays the intro once, then loops 12 s → end. The next pass is scheduled on the DSP clock 0.3 s before the end, from 11.7 s, with an equal-power crossfade, so there is no click or level jump.
  - **Volume:** playback 0.35 (about −9 dB), so the music sits under future SFX; the file is not edited.
- **Not covered yet:** the TDD §10 intensity layer, menu cue, level-end sting, key motif and door cadence (MISSING_ASSETS).
- **Reversal cost:** low · **Type:** absorption

### D-259 — Gold path input: no stale device state between queued events; stable route at the raised fly
- **When:** 2026-10-09 · **Stage:** post-M4 · **Commit:** pending
- **Context:** The gold path failed intermittently at different steps (the Professor not moving, or moving the wrong way after a stick change). The V57 input simulator queues full-device state events built from the device's *current* state. A second write queued before the first one is processed (stick change + tap, release + next press) can carry a stale value and undo it. Separately, the hop for the raised fly at x −10 was done running; momentum carried the Professor to the platform edge (x −8.3), and the next diagonal jump sometimes fell.
- **Choice:**
  - **V57 core:** `GoldPathInputSimulator` remembers every control value it wrote (released ones at 0) and writes all of them into each event (`GoldPathInputWriter.Queue`). While something is held, it re-writes them before every input update (keep-alive).
  - **Gold path:** stop under the fly, hop in place, then walk to the take-off point. 5 runs in a row green.
- **Reversal cost:** low · **Type:** other
