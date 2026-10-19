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

### D-260 — Lab doors slide open when the Professor is near (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** The owner asked for the double doors of the B-17 doorways (`AST-ENV-LABDOOR-001`, one leaf per instance, two mirrored leaves per `AST-ENV-B17ARCH-001`) to open, each leaf to its own side, when the character is near. The leaves are scenery: a single mesh each, no collider.
- **Choice:**
  - **Component:** `ProximityDoor` (Gameplay/Environment) on each arch instance (`B17ARCH_01` west, `_02` east), with the two leaves serialized.
  - **Open direction:** each leaf slides 1.6 m (its own width) away from the arch centre along the arch's width axis. The direction comes from where the leaf sits, so mirrored instances need no setup.
  - **Trigger:** `PlayerMovedEvent` (same pattern as `ZoneDoorController`). Opens within 3.5 m horizontally (±3 m height), closes beyond 4.25 m. Open and close take 0.6 s, smoothstep eased.
  - **Collision:** none; the leaves have no collider. If a doorway becomes walkable, the leaf gameplay prefab gets its collider (it moves with the leaf).
- **Verified:** Play Mode. The Professor at x −24 opens the west leaves (z 5.20/6.81 → 3.60/8.41); back at the spawn they close to the authored positions.
- **Reversal cost:** low · **Type:** other

### D-261 — The scene fog reaches the skybox (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** URP fog only affects geometry. The provider's panoramic sky (`MAT_Skybox`, `V4_Skybox.png`) stayed crisp behind fogged 3D rocks.
- **Choice:**
  - **Shader:** `ProfessorSprat/Skybox/Panoramic Fog` (`Settings/Rendering/Shaders/SH_SkyboxPanoramicFog.shader`). It has the same properties as Skybox/Panoramic (lat-long), so `MAT_Skybox` keeps the provider's texture, tint, exposure and rotation; only its shader changes.
  - **Blend:** the sky has no distance, so the scene fog colour (`unity_FogColor`) is blended by view elevation: 1.0 at and below the horizon, fading to 0.35 at elevation sin 0.6. The light shafts painted up high stay visible.
  - **Fog off:** with the scene fog off (no `FOG_*` keyword), the sky is unfogged.
- **Verified:** offscreen render with and without fog. The painted far rocks fade into the fog like the 3D ones.
- **Reversal cost:** low (set `MAT_Skybox` back to Skybox/Panoramic) · **Type:** other

### D-262 — Gold path starts from the moved spawn point (owner edit)
- **When:** 2026-10-13 · **Stage:** post-M4 · **Commit:** pending
- **Context:** The owner moved `Marker_Spawn_Player` to (−40, 9.59, 7.5), on the grate platform at y 9.3 west of the level. It ends at x −36.5; the main lab floor (y 2.2) starts at x −28, with nothing in between. The gold path still started from x −19.4 and collected the first fly by walking back.
- **Choice:**
  - **Opening:** align to the doorway lane (z ≤ 6.2), run at full stick, jump at x ≥ −37, land on the lab floor. Running speed 5.5 m/s and a 6.6 impulse falling 7 m carry about 9 m, so the Professor lands at x ≈ −27.5 in the B-17 doorway, which opens (D-260). Then walk forward through both first flies.
  - **Raised fly at x −10:** after stopping under it, the Professor sat at z ≈ 3.93, 0.45 m from the fly (z 4.36), at the edge of the pickup radius, and 1 run in 5 missed. The route now nudges to z ≥ 4.2, stops, then hops.
- **Verified:** 11 runs in a row green after the alignment fix (last `20261013T165736Z`).
- **Reversal cost:** low · **Type:** other

### D-263 — Algae sway by vertex shader (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:**
  - The owner added preview algae meshes for shader animation, to be re-delivered separated from the rocks in the next scenery iteration: `AST-ENV-VEGETATION-001.fbx` and `AST_ENV_OCEANLEAVES-001.fbx`, plus their maps, in the `Assets/` root.
  - Each is one mesh of about 2.3k vertices, about 0.9–1 m tall, with the pivot at the base. They have no vertex colours, no UV2, no LODs, and only a generic material.
- **Choice:**
  - **Shader:** `ProfessorSprat/Environment/Algae Sway` (`Settings/Rendering/Shaders/AlgaeSway/`), generated from this URP's `Lit.shader`, so the shading is the same (shadows, caustics cookie, fog, SSAO, probes).
    - The sway params sit in the material CBUFFER, so the SRP Batcher is kept.
    - Each geometry pass (forward, shadow, depth, depth-normals, GBuffer) wraps URP's own vertex function and displaces in world space, so the shadows sway too.
    - Motion-vector and 2D passes are removed.
  - **Motion:**
    - A slow current wave travels along `_SwayDirection`, so neighbours follow each other.
    - A slower secondary swell is added, plus a small fast sideways flutter at the tips.
    - The tip drops slightly as it leans, so blades bend instead of stretching.
    - `AlgaePush` (`_Environment/AlgaePush`) sends the Professor's smoothed centre to the shader, so plants bend away within 0.9 m.
  - **Weights:**
    - Now: from object height (`_SwayHeight`, `_SwayStiffness`), with a per-clump phase from the world origin.
    - When the provider adds vertex colours: material toggle `_SWAY_VERTEXCOLOR` (R bend, G phase, B flutter), listed in MISSING_ASSETS.
  - **Materials:** `MAT_AST-ENV-VEGETATION-001_Sway`, `MAT_AST-ENV-OCEANLEAVES-001_Sway` (two-sided, GPU instancing), using the root maps.
    - The normal maps are re-imported as normal maps and MetallicSmoothness as linear (they were Default/sRGB).
    - Repoint the textures when the organised delivery arrives.
  - **Inspector:** `AlgaeSwayShaderGUI` (Editor asmdef), because URP's Lit inspector is internal. It keeps the Lit keywords in sync with the maps.
  - **Placement rule:** algae instances must not be static-batched or lightmap-static (light probes instead).
  - **Test scenery (owner request):** `_Environment/TMP_AlgaeTest`, 8 clumps of the root preview FBX around the spawn (not static, light probes). The root preview FBX and maps stay in `Assets/` until the final delivery replaces them.
- **Verified:**
  - Play Mode with temporary instances (not saved): lit and shadowed; tips move between frames while the bases stay put; `_AlgaePusher` tracks the Professor.
  - Not verified on screen: the push itself, because the camera clipped behind a wall at that spot (no camera collision, D-257).
- **Reversal cost:** low · **Type:** other

### D-264 — Footsteps: owner-generated SFX, steps synced to the walk/run clips
- **When:** 2026-10-13 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** The owner generated `SFX_Footstep_Steel` (#1 of `SFX_PROMPTS_ELEVENLABS.md`) with ElevenLabs: one 2 s, 48 kHz, 16-bit stereo file holding three separate footsteps (at 0.09 s, 0.89 s and 1.49 s) with silence between them.
- **Choice:**
  - **File:** `Assets/_Game/Audio/SFX/Player/SFX_Footstep_Steel.wav` (V57 name; owner asked to rename and place).
    - It is the second take: the most metallic (strongest highs, 145 ms ring; the other two read as a splash).
    - It starts 3 ms before the hit and lasts 0.4 s, with a 30 ms fade-out; no other processing.
    - The owner chose one metallic step for the single steel surface; repetition is fine.
    - Imported by the V57 SFX rule (DecompressOnLoad).
  - **Trigger:** `CharacterAnimationDriver` publishes `FootstepEvent` (position, speed) while Walk or Run plays, each time the clip crosses a foot contact (~2 % left, ~52 % right). Both delivered clips plant the feet there (sampled foot heights), so steps follow the animation at any speed. Nothing is published in the air, stunned, or during a stomp.
  - **Playback:** `AudioService` plays it on its own `Footsteps` source (same spatial settings as the one-shots), so per-step pitch does not affect other sounds.
    - `AudioCue` still supports variations for other cues (random take, never the same twice in a row).
    - Pitch 0.93–1.07.
    - Volume 0.55 (walk) → 0.9 (full run).
- **Verified:** Play Mode, walking at 2.75 m/s: steps play with varying pitch and stop once airborne.
- **Reversal cost:** low · **Type:** absorption

### D-265 — No camera auto-recentering (owner request; supersedes the recentering of D-255/D-257)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** The owner asked to remove the camera's return behind the Professor after 1.5 s without look input: the camera must stay where the player left it.
- **Choice:**
  - **Recentering:** `CameraRig.ConfigureOrbit` disables recentering on all three orbit axes (horizontal, vertical, zoom).
  - **Config:** `CameraConfig.RecenterDelay` / `RecenterTime` are removed.
  - **Kept:**
    - A camera-zone change still turns the whole orbit and keeps the player's offset.
    - A respawn still re-centers the zone basis without moving the player's offset.
    - Zoom and pitch limits are unchanged.
- **Verified:** Play Mode. The orbit was set to +60° yaw / +15° pitch from its center and held for 7 s with no mouse or gamepad input: the values did not change.
- **Reversal cost:** low · **Type:** other

### D-266 — Intro cinematic before gameplay; jump and land SFX (owner deliveries)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner deliveries) · **Commit:** pending
- **Context:** The owner delivered:
  - `2.wav` (#2 `SFX_Jump`) and `3.wav` (#3 `SFX_Land`) from `SFX_PROMPTS_ELEVENLABS.md`;
  - `Video_Marketing_Final.mp4` (1280×720, 10.0 s, 24 fps, H.264, stereo 32 kHz), to play before gameplay: "the video runs, and when it ends the game starts".
- **Choice:**
  - **SFX files:** `Assets/_Game/Audio/SFX/Player/SFX_Jump.wav` (untouched; starts at 0 ms, peak −6.6 dBFS) and `SFX_Land.wav`.
    - The land file peaked at −21.6 dBFS, about 15 dB under the jump and the footsteps, so it was raised by +13.6 dB to a −8 dBFS peak. No other processing; the original stays with the owner.
    - Both are cues in `AudioService`; their events were already wired (`JumpStartedEvent`, `LandedEvent`).
  - **Video file:** `Assets/_Game/Art/Cinematics/CIN_Intro.mp4` (owner delivery, V57 name). Native H.264 playback, no transcoding.
    - `Packages/manifest.json` enables the built-in `com.unity.modules.video` (it was off).
  - **Scene:** `SCN_Intro_Cinematic` (build list right after `SCN_MainMenu_Boot`).
    - Camera with a black clear and no post; `VideoPlayer` on the camera near plane, fit inside (letterbox).
    - Video audio goes through a 2D `AudioSource` (0.85).
  - **`IntroCinematic`:**
    - Prepares the video in Awake and plays it in Start.
    - Once it is playing, it loads the level in the background at low priority with activation held.
    - When the video ends, or any button is pressed after 0.5 s, it activates the level.
    - If the video fails, the level starts at once.
  - **Menu:** Start = new game → intro → level; Continue → level directly.
  - **Direct scene loads:** the gameplay scene loaded directly (Editor Play on it, the gold path, PlayMode tests) is unchanged and has no intro.
- **Verified:** Play Mode from `SCN_Intro_Cinematic`.
  - The video renders full screen and plays its 10 s; then `SCN_HydroStation_Gameplay` is active and the zone is `Playing`.
  - Publishing jump and land events plays both cues, with no missing-cue warning.
  - In the Editor the first frame of Play stalls about 4 s (Editor overhead), then the decoder takes about 2 s to prepare.
- **Reversal cost:** low · **Type:** other

### D-267 — Seaweed oxygen bubbles VFX (owner spec, owner textures)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** The owner delivered `bubble.png` and `bubblesheet.png` (the 2x2 pop flipbook; the request called it `bubble_pop_sheet.png`). Both are 1254² with alpha, placed in the `Assets/` root. The owner specified a cartoon VFX (Cartoon FX Remaster style) of oxygen bubbles rising from the test algae, with exact values. It was first built under `Assets/VFX/Bubbles/` as the request named. At the owner's follow-up it now follows the `_Game` conventions.
- **Choice (as specified):**
  - **Textures:**
    - Default, Alpha Is Transparency, no mips, Clamp, max 256 / 512.
    - `Assets/_Game/Art/VFX/Bubbles/Textures/T_Bubble.png` / `T_BubblePopSheet.png` (renamed and moved, GUIDs kept).
  - **Materials:** `Assets/_Game/Art/VFX/Bubbles/Materials/M_Bubble`, `M_BubblePop`: URP/Particles/Unlit, Transparent, Alpha blend, no soft particles.
  - **Prefab `Assets/_Game/Prefabs/VFX/VFX_SeaweedBubbles.prefab`:**
    - **Main:** loop; lifetime 2–4; speed 0.2–0.5; size 0.05–0.2; gravity −0.15; World space.
    - **Emission:** 3/s, plus a burst of 2–4 every 1.5 s at probability 0.5.
    - **Noise:** 0.3 / 0.5 / 0.3.
    - **Size over lifetime:** 0 → 1.15 (10 %) → 1.0 (20 %) → 1.0.
    - **Colour over lifetime:** `#BFF4FF`, alpha 1 until 90 %, then 0.
    - **Renderer:** billboard.
    - **Sub-emitter on Death `Pop`:** 2x2 sheet over 0.25 s, burst of 1, start size 1 × Inherit Size.
  - **Box shape:** it emits along its local +Z, so the shape is turned −90° on X with local scale (0.3, 0.3, 0). That gives the specified flat 0.3 × 0 × 0.3 footprint at the base, emitting upward.
  - **Instances:**
    - **Map algae:** one per alga in the current map, all 52 (`_Environment/Ocean`: 28 `OCEANLEAVES`, 24 `VEGETATION` rock clusters), at each pivot +0.05 m.
      - They sit in their own root container `_VFX/SeaweedBubbles`, not under the scenery, so replacing the scenery does not remove them (owner request).
      - The large `VEGETATION` clusters (up to 16 m wide) get a single emitter at their pivot.
    - **Test algae:** one per test alga (8), as a child at the base, so they turn on and off with `TMP_AlgaeTest` (currently off).
    - **Scale:** 1; scaling mode Local ignores the parent's scale.
- **Verified:**
  - Play Mode with the test algae turned on for the session only (not saved): 8 systems, about 100 bubbles alive, pops on every death.
  - No new console errors; only the existing "not delivered" notices.
- **Also:** the unused `V7_Skybox.png` was deleted (owner request; the sky uses V8).
- **Verified (map):** Play Mode: 52 systems playing, about 600 bubbles alive after 3 s, with pops; no new console errors.
- **Reversal cost:** low · **Type:** other

### D-268 — UI art from the owner's sprite sheet v02, integrated in UI Toolkit
- **When:** 2026-10-13 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** The owner delivered `sprite_sheet_ui_v.02.png` (2048², RGBA, transparent background) and asked to import everything needed and integrate it.
  - **Contents:** cyan and amber frames, objective marker, key, starburst, skull, fly, iron bar, small banner, glowing slot, "WORT ADVENTURES" logo panel, 4 button bars, skull panel, plain panel, bracket caps, gems, rivet.
  - **Before:** the UI was text and flat colours.
- **Choice:**
  - **Slicing:** elements detected by alpha (connected components) and cropped tight with 4 px padding. The sheet's divider line was skipped; the cyan frame and the starburst touched and were split at an empty row.
    - **Files:** `Art/UI/Icons/ICO_*`, `Art/UI/Sprites/Common/UI_*`, `Art/UI/Sprites/UI_HUDCounter/*`, `Art/UI/Sprites/UI_MainMenu/UI_Logo`.
    - **Source:** the sheet is kept as `Art/UI/Atlases/UI_SpriteSheet_v02.png` for re-slicing.
  - **Import:** Sprite (single, Full Rect), no mips, Clamp, alpha is transparency, compressed HQ.
    - 9-slice borders on the buttons, panels, frames, iron bar and HUD banner.
    - USS references the Sprite sub-asset (fileID 21300000), so UI Toolkit uses the borders; `-unity-slice-scale` fits them.
  - **Mapping:**
    - **HUD:** fly icon over the small banner with the counter.
      - Last fly: a pulsing amber halo behind the banner (`counter-group--last`); caps and rivets stay on top. It was first an overlay frame, which covered the caps (owner feedback).
      - The starburst flares behind the fly on each collect (`HUDController`).
      - The key sits in the glowing slot while carried.
    - **Buttons:** normal = plain bar; hover = sparkle bar; focus = bright haloed bar (amber text, 1.04×); pressed = tinted, 0.97×.
    - **Pause / Level end:** plain dark panel (9-slice).
    - **Main menu:** logo panel above the buttons.
    - **Loading:** stays a black fade.
  - **Unchanged:** element names and the HUD API (`CounterText`, `KeyGlyphVisible`), so the gold path probe and the tests still hold.
- **Imported, not used yet:** skull panel and skull icon (no death or defeat screen in this game), objective marker, iron bar, cyan frame, alternate button bar, bracket caps, gems, rivet.
- **Verified:**
  - Play Mode captures (end-of-frame screen capture, which includes overlay UI) of the HUD (last fly + key), pause, level end and main menu.
  - No USS warnings after giving `-unity-slice-scale` its unit.
  - PlayMode tests 28/28 green.
- **Still missing:** the rounded sans font (TDD §9.1); text uses the default font.
- **Reversal cost:** low · **Type:** absorption

### D-269 — Fly pickup SFX (owner delivery)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** The owner delivered `09.wav` (#9 `SFX_FlyPop`) and `10.wav` (#10 `SFX_FlyPopLast`) from `SFX_PROMPTS_ELEVENLABS.md`.
  - **Format:** 0.48 s each, 48 kHz stereo, start at 0 ms.
  - **Level:** they peaked at −0.1 and −1.1 dBFS, about 6 dB over the other SFX (jump −6.6, land −8, footstep −4.7).
  - **Ending:** both ended abruptly; FlyPop's last 25 ms rise from −68 to −36 dB, a likely click.
- **Choice:**
  - **Files:** `Assets/_Game/Audio/SFX/Flies/SFX_FlyPop.wav` and `SFX_FlyPopLast.wav`.
  - **Processing:** the same −6 dB on both (their relative level is kept; peaks now −6.2 / −7.2 dBFS) and a 20 ms fade-out at the end. No other processing.
  - **Wiring:** both are cues in `AudioService`; the trigger was already wired (`FlyCollectedEvent`, the last fly plays `SFX_FlyPopLast`).
- **Verified:** Play Mode. Publishing a normal and a last-fly collect plays each cue, with no missing-cue warning.
- **Reversal cost:** low · **Type:** absorption

## D-252 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/Cinematics/CIN_Intro.mp4`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: f625cd850f60 -->

## D-253 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/UI/Atlases/UI_SpriteSheet_v02.png`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: cef545120d13 -->

## D-254 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST_ENV_OCEANLEAVES-001_lambert4_Albedo.jpg`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 3ad99162cd2d -->

## D-255 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST_ENV_OCEANLEAVES-001_lambert4_MetallicSmoothness.png`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 60efa5d26d97 -->

## D-256 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST_ENV_OCEANLEAVES-001_lambert4_Normal.jpg`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 48926ccbd604 -->

## D-257 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST_ENV_OCEANLEAVES-001.fbx`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: d358967d8705 -->

## D-258 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST-ENV-VEGETATION-001_lambert2_Albedo.jpg`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 5418c8168de5 -->

## D-259 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST-ENV-VEGETATION-001_lambert2_Normal.jpg`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 564ae9bdf06f -->

## D-260 · I0 INTAKE · conflict · REPO_OUTSIDE_GAME
- **Issue:** file under Assets/ outside Assets/_Game/
- **Where:** `Assets/AST-ENV-VEGETATION-001.fbx`
- **Decision:** not imported by V57 assembly; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 70c75883688f -->

## D-261 · I0 INTAKE · fixable · LAYOUT_ASSET_ALIASED
- **Issue:** layout asset_id(s) placed with another delivered model (Docs/V57/layout_aliases.json): AST-CHAR-NEONFLY-001→Fly, AST-CHAR-RADIOACTIVESHARK-001→Shark, AST_ENV_OCEANBOAT-001→AST-ENV-OCEANBOAT-001, AST_ENV_OCEANLEAVES-001→AST-ENV-OCEANLEAVES-001, AST_ENV_OCEANROCKLEAVES-001→AST-ENV-OCEANROCKLEAVES-001, AST_ENV_OCEANROCKS-001→AST-ENV-OCEANROCKS-001, AST_ENV_OCEANSKELETON-001→AST-ENV-OCEANSKELETON-001, AST-CHAR-FISH-001→Fish, AST-CHAR-JELLYFISH-001→Jellyfish, marker Door→PRF_ZoneDoor_Lab.prefab, marker Patrol→PRF_MechanicalCrab.prefab
- **Where:** `Docs/Design/LevelMaps/Level_01/unity_scene.json`
- **Decision:** aliased instances use the mapped model and yaw; provider should export the final asset ids
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: dbf7dd6d726c -->

## D-262 · I0 INTAKE · fixable · DOCS_UNEXPECTED
- **Issue:** file not part of the Docs/ contract tree (brief §1)
- **Where:** `Docs/Design/LevelMaps/Level_01/Level_01_Perfil_Alturas.png`
- **Decision:** ignored by intake
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ed3103c0b071 -->

## D-263 · I0 INTAKE · fixable · DOCS_UNEXPECTED
- **Issue:** file not part of the Docs/ contract tree (brief §1)
- **Where:** `Docs/Design/LevelMaps/Level_01/Level_01_Trayectorias_V03.json`
- **Decision:** ignored by intake
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7ab6c731aaa0 -->

## D-264 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-CORALROCK-002/Textures/AST-ENV-CORALROCK-002_lambert35_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVCORALROCK002_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 51352826ef85 -->

## D-265 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANBOAT-001/Textures/AST_ENV_OCEANBOAT-001_lambert5_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANBOAT001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d8c9bf85d318 -->

## D-266 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST_ENV_OCEANLEAVES-001_C_Ocean_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d2834a9c2a47 -->

## D-267 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST_ENV_OCEANLEAVES-001_C_Ocean_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ce06346f8fc0 -->

## D-268 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANLEAVES-001/Textures/AST_ENV_OCEANLEAVES-001_C_Ocean_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANLEAVES001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 96831598afae -->

## D-269 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKLEAVES-001/Meshes/AST-ENV-OCEANROCKLEAVES-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVOCEANROCKLEAVES001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 05e71868e6e8 -->

## D-270 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKLEAVES-001/Textures/AST_ENV_OCEANROCKLEAVES-001_C_Ocean_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKLEAVES001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4d0c4c3de108 -->

## D-271 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKLEAVES-001/Textures/AST_ENV_OCEANROCKLEAVES-001_C_Ocean_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKLEAVES001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f344e59599df -->

## D-272 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKLEAVES-001/Textures/AST_ENV_OCEANROCKLEAVES-001_C_Ocean_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKLEAVES001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 202d5894bf32 -->

## D-273 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANROCKS-001/Textures/AST_ENV_OCEANROCKS-001_lambert3_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANROCKS001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 67cc290391fa -->

## D-274 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-OCEANSKELETON-001/Textures/AST_ENV_OCEANSKELETON-001_lambert5.001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVOCEANSKELETON001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4a832896369c -->

## D-275 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCKVEGETATION-001/Meshes/AST-ENV-ROCKVEGETATION-001.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_ASTENVROCKVEGETATION001); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d7f71bd34821 -->

## D-276 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCKVEGETATION-001/Textures/AST-ENV-ROCKVEGETATION-001_A_Ocean_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVROCKVEGETATION001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1104752ba941 -->

## D-277 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCKVEGETATION-001/Textures/AST-ENV-ROCKVEGETATION-001_A_Ocean_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVROCKVEGETATION001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 67b15561fb80 -->

## D-278 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCKVEGETATION-001/Textures/AST-ENV-ROCKVEGETATION-001_A_Ocean_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVROCKVEGETATION001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 358d65d0dcd0 -->

## D-279 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_AST-ENV-SEABED-001_Albedo.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 067484569f50 -->

## D-280 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_AST-ENV-SEABED-001_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 8ba3777e9410 -->

## D-281 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-SEABED-001/Textures/AST-ENV-SEABED-001_AST-ENV-SEABED-001_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVSEABED001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 552e5ab52624 -->

## D-282 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_A_Ocean_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bec845142db3 -->

## D-283 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_A_Ocean_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 05b84ae6fbe1 -->

## D-284 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/AST-ENV-VEGETATION-001/Textures/AST-ENV-VEGETATION-001_A_Ocean_Normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVVEGETATION001_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1a788c29aebd -->

## D-285 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Kits/AST-ENV-LABFLOOR-001/Textures/AST-ENV-LABFLOOR-001_AST-ENV-LABFLOOR-001.001_Albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_ASTENVLABFLOOR001_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0900a5dba891 -->

## D-286 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Bar_Iron.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIBarIron); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ee80e4262ec2 -->

## D-287 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Button_Alt.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIButtonAlt); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 65264c565fd8 -->

## D-288 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Button_Focus.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIButtonFocus); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bf8a78d562be -->

## D-289 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Button_Hover.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIButtonHover); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d4e290709971 -->

## D-290 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Button_Normal.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIButtonNormal); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6b421b0af0d5 -->

## D-291 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Cap_Left.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UICapLeft); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: f334047a94e9 -->

## D-292 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Cap_Right.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UICapRight); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d093e812c83d -->

## D-293 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Frame_Amber.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIFrameAmber); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 3b8e66fcaae9 -->

## D-294 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Frame_Cyan.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIFrameCyan); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: bfd53f053573 -->

## D-295 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Gem_Large.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIGemLarge); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 493f2eec3ae2 -->

## D-296 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Gem_Small.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIGemSmall); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: b72b92a97b30 -->

## D-297 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Panel_Dark.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIPanelDark); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: dc4b391edbfa -->

## D-298 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Panel_Skull.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIPanelSkull); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 23807428362b -->

## D-299 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/Common/UI_Rivet.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_Common_UIRivet); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: a380a18a3d75 -->

## D-300 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/UI_HUDCounter/FX_Starburst.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_UI_HUDCounter_FXStarburst); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: c4efcdeb2238 -->

## D-301 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/UI_HUDCounter/UI_HUD_Banner.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_UI_HUDCounter_UIHUDBanner); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4fd375d254e2 -->

## D-302 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/UI_HUDCounter/UI_HUD_KeySlot.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_UI_HUDCounter_UIHUDKeySlot); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1fdc48eda76b -->

## D-303 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SPR_<Id>_<State>[_9s-<px>]
- **Where:** `Assets/_Game/Art/UI/Sprites/UI_MainMenu/UI_Logo.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SPR_UI_MainMenu_UILogo); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: ff81cfdcb0a0 -->

## D-304 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/Bubbles/Textures/T_Bubble.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_Bubble); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 2b18eb5f610e -->

## D-305 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/Bubbles/Textures/T_BubblePopSheet.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_BubblePopSheet); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 7ada61faacb7 -->

### D-270 — Level_01 V10 delivery integrated: map contract 1.1 (markers, paths, props), 2 zones, ambient life on paths, lab zone doors, final algae
- **When:** 2026-10-13 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** The owner delivered `Level_01_V10`. The level grew from about 70 m to about 240 m (height profile `Level_01_Perfil_Alturas.png`).
  - **Map:** `unity_scene.json` (contract 1.1) with 1931 objects, a separate `marcadores` array (43 markers), 23 `trayectorias` (11 crab, 8 fish, 4 school) and per-object `props`.
  - **Files:** 36 FBX and 44 textures.
  - **Contract:** 2 zones (Z1: 6 flies, Z2: 12), 2 keys, 2 doors with consoles, 11 crab patrols, kill water at −0.25, a shark loop event.
  - **Ambient life:** 46 fish on paths and 14 jellyfish.
  - **Algae:** now separate meshes (`VEGETATION`, `OCEANLEAVES`; rock versions `ROCKVEGETATION`, `OCEANROCKLEAVES`).
  - **Gap:** V57 read none of the contract 1.1 extras.
- **Choice:**
  - **V57 core (generic, base repo):** see `V57/knowledge/asset-intake.md`, "contract 1.1 extras".
    - `marcadores` → markers (box volumes scaled by `tamano`, Patrol markers moved to their route end).
    - `props` → `V57LayoutProps`; `trayectorias` → `_Environment/_Paths` + `V57Path`.
    - `layout_aliases` `static:false` and `marker_actors`.
    - Fixes: stale FBX material remaps after a re-delivery; the `V57KeepShader` tag.
    - Intake tests 32/32; Assembly 15/15; GoldPath 51/51.
  - **Art (D-252 rules):**
    - FBX replaced in place (GUIDs kept). The five `AST_ENV_*` ids map to their `AST-ENV-*` meshes through aliases (no renamed or duplicate files).
    - New assets in `Decoration/` (`ROCKVEGETATION`, `OCEANROCKLEAVES`). New maps added.
    - Earlier maps kept: 29 assets came with albedo only, and their V06 normal/metallic maps still apply; only the five delivered sets replace theirs.
  - **Visual prefabs:**
    - Six were rebuilt because their FBX lost LODs in V10: `VEGETATION`, `OCEANLEAVES`, `OCEANBOAT`, `OCEANROCKS`, `OCEANSKELETON`, `SEABED`.
    - Default BoxColliders removed from the eight new and rebuilt Visual prefabs.
    - The `B17FLOORPLATE` and `GRATEPLATFORM` gameplay colliders were re-fitted to the smaller V10 meshes.
  - **Actors:**
    - **Crabs:** `PRF_MechanicalCrab` per Patrol marker, named after it, at the route start facing the end.
    - **Zone doors:** `PRF_ZoneDoor_Lab` (ZoneDoorController, leaves mode) per Door marker. The four `AST-ENV-LABDOOR-001` leaves become gameplay prefabs with their collider and `DoorLeaf`; each door takes the leaves within 3 m and slides them apart when unlocked.
    - **Fish:** `PRF_Fish` + `PathSwimmer` (path, speed, phase, offset, scale, clip offset from the props).
    - **Jellyfish:** `PRF_Jellyfish` + `JellyfishDrift` (drift radius `deriva_m`).
    - **Sharks:** `SharkLoop` on `PRF_Shark` swims the `Marker_Event_shark` ellipse (radius 1 × scale 18 × 9).
  - **Zones:** `LevelContent` takes an actor's zone from the map (`zona` on the actor, or on the nearest marker within 1.5 m) before the volumes, because Z1 and Z2 overlap at z 9–14.
  - **Camera:** it starts behind the Professor as spawned, because `Marker_CameraZone_Default` carries no yaw.
  - **Algae:**
    - The final `MAT_AST-ENV-VEGETATION-001` / `MAT_AST-ENV-OCEANLEAVES-001` use Algae Sway (heights 0.85 / 1.0 m, two-sided).
    - The 80 pure algae are dynamic; the rock versions stay static Lit.
    - The preview FBX and maps in the `Assets/` root and the `_Sway` preview materials are deleted, as agreed for the final delivery.
    - Bubbles regenerated on all 160 algae (`_VFX/SeaweedBubbles`); `AlgaePush` moved to `_Systems`.
  - **Docs:** the delivery's JSON, path file and captures replace `Docs/Design/LevelMaps/Level_01`.
- **Verified:**
  - Rebuild: 1944 instances, 43 markers, 0 missing, 0 missing refs.
  - Play Mode:
    - Zone Playing, HUD 0 / 6, 18 flies, 2 keys, 11 crabs.
    - Both doors with 2 leaves each.
    - 46/46 fish moving on their paths, 2 sharks on the loop, 14 jellyfish.
    - Camera behind the Professor at the spawn.
  - Game EditMode 14/14, PlayMode 28/28.
- **Open (MISSING_ASSETS):**
  - **Second shark:** no path; it joins the single loop at its own height.
  - **Algae:** no vertex colours, so sway weight comes from height.
  - **Door consoles (`Switch`):** not a TDD mechanic; markers only (the door opens with the key, as the TDD says).
  - **Z2 crabs:** 8, over the TDD LR-04 limit of 4 per zone (conflict).
  - **Naming:** five `AST_ENV_*` ids.
  - **Mirrored leaves:** two LABDOOR leaves are exported with negative scale (BoxCollider warning; collision forced positive).
- **Still to do:** the gold path still describes the V06 route and must be re-authored for the V10 level (2 zones, 18 flies).
- **Reversal cost:** medium (git history) · **Type:** absorption

### D-271 — One fly count for the whole level; the key appears at the final door; every crab patrols from the start (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** In play the HUD showed 0 / 6 (zone 1 only) although the level has 18 flies. The owner asked for the total count and for the key to move accordingly. Z2 crabs stood still: crabs only started patrolling when their zone began, and zone 2 began after door 1.
- **Choice (`ZoneFlow.BeginRun`, overrides the TDD per-zone count):**
  - **Count:** every zone's flies count toward one total (HUD 0 / 18).
  - **Key:** the last zone's key (`key_anchor_1`, beside the final door 2) materialises when all 18 are collected. The Z1 key stays dormant.
  - **Earlier doors:** door 1 is a passage. It opens on approach without a key (same 1.5 m radius and slide).
  - **Crabs:** all 11 patrol from the start.
  - **Level end:** door 2 open and the Professor in `Marker_Exit_Z2`.
- **Verified:**
  - Play: HUD 0 / 18; door 1 opens on approach; door 2 locked; key 2 dormant in Z2; all crabs moving.
  - Game EditMode and PlayMode suites green.
  - The gold path fails at its first check (`flies.total == 6`): it still describes the V06 route (D-270, still to do).
- **Reversal cost:** low · **Type:** owner request

### D-272 — Collision for every solid scenery piece, LODGroup fixes, caustics only where the sun reaches, faster algae (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:**
  - **Collision:** the owner found crates, doors and edges without collision. Only floors and door leaves had colliders; every other piece was a Visual prefab without one.
  - **Caustics:** they showed inside the roofed spawn corridor. The sun's shadow strength was 0.85, so 15 % of the sun, and its caustics cookie, still reached shadowed areas.
  - **Console:** every LOD renderer was registered in two LODGroups. Several V10 FBX now import with their own LODGroup, and the Visual prefab added a second one. `B17FLOORPLATE`, `CRYSTALPANEL` and `PIPE-002` (LOD0/1/3, no importer group) had no LODGroup at all, so all their levels rendered at once.
- **Choice:**
  - **Collision prefabs** (Gameplay/Environment, the collider lives with the mesh):
    - **Box fitted to LOD0:** `BEACONCRATE`, `STEELCOLUMN`, `CONSOLE-001/002`, `HATCH`, `WARNINGSIGN`, `HOSE`, `PIPE-001/002`, `RAILING`, `SCAFFOLD`, `HATCHPANEL`.
    - **Mesh collider on LOD0:** `B17ARCH`, `BULKHEAD`, `CRYSTALPANEL`, `HYDRODOMETOWER`, `CORALROCK-001/002`, `OCEANROCKS`.
    - **No collision:** lanterns (hanging decoration), algae (they bend, `AlgaePush`), seabed, boat and skeleton (below the kill water).
    - **Door leaves:** the LABDOOR leaf collider is a convex mesh collider, so the two mirrored leaves (negative scale) collide without the BoxCollider warning.
  - **V57 core:**
    - **Static scenery:** a gameplay prefab with no game scripts (collision only) is placed as static scenery under `_Environment`, not in `_Gameplay/Level`.
    - **LODGroups:** the Visual prefab adds no LODGroup when the model has its own, and kept prefabs refresh theirs after a re-delivery.
  - **Lighting:**
    - Sun shadow strength 1.0: no direct light or caustics under roofs.
    - Structure renderers cast two-sided shadows. Roof and wall plates are placed upside down or sideways.
  - **Algae:** sway speed 0.9 → 1.25 and flutter 3.5 → 4.5 on both final algae materials.
- **Verified:**
  - Rebuild: 0 missing. Every solid piece has a collider; 0 Visual prefabs with duplicate or missing LODGroups.
  - Play console: only the listed missing `VFX_LandSplash` warning.
  - Spawn corridor dark under the roof; caustics only in open areas.
  - EditMode 80/80 (game + core), PlayMode 28/28.
- **Reversal cost:** low · **Type:** owner request

### D-273 — Neon Fly glow: TDD emissive + pulsing screen-space outline (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:**
  - **Problem:** the flies (0.27 m) were nearly invisible in the larger V10 level.
  - **TDD §B FlyCollection:** asks for "emissive material + halo sprite", `glowIntensity 2.5`, and no real-time light (INV-11).
  - **Current state:** `glowIntensity` was unused (`MAT_Fly` had no emission). The halo sprite is one of the undelivered VFX textures (`MISSING_ASSETS.md`).
- **Choice:**
  - **Emissive (TDD):** `MAT_Fly` emits its own albedo. `FlyController` sets the tint `FlyConfig.GlowColor` × `GlowIntensity` (2.5) per fly. The new `GlowColor` field is a dark cyan, `(0.04, 0.22, 0.28)`.
  - **Outline (owner's alternative to the missing halo):**
    - **Shader:** `SH_FlyGlowOutline`, material `MAT_Fly_Outline`, the second slot of the fly renderer. It is an additive inverted hull.
    - **Width:** constant in screen pixels (3 px + 6 mm), so a fly reads at any distance.
    - **Pulse and flash:** a soft pulse (0.8 Hz, 45–100 %) plus a short brighter, wider flash every 3 s. Each fly has its own phase from its name.
    - **Fog:** keeps at least 40 % of the glow, so far flies show through the underwater fog.
  - **No real-time light (INV-11).**
  - **The halo sprite stays missing:** it can be added on top when delivered.
- **Verified:**
  - Play: flies read as cyan glows from the spawn corridor and from about 10 m, with no overexposure.
  - EditMode 80/80.
- **Reversal cost:** low · **Type:** owner request

### D-274 — The run starts with the camera fully zoomed in (owner request)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Choice:** `CameraRig.Bind` sets the orbit's radial axis to the minimum of `CameraConfig.ZoomRange` (0.6× the authored distance; the owner chose "lo más cerca"). The player zooms out from there (up to 1.6×). Respawns keep the player's zoom.
- **Verified:** Play: radial 0.6, camera 4.35 m from the Professor at the spawn.
- **Reversal cost:** low · **Type:** owner request

### D-275 — Per-asset rules replace the hand-made collision prefabs; bubbles become layout attachments (V57 core update)
- **When:** 2026-10-13 · **Stage:** post-M4 (owner request: make the integrated work reproducible) · **Commit:** pending
- **Context:** D-270 and D-272 were partly done with one-off scripts. Those were the collision prefabs per asset, colliders re-fitted after V10, two-sided shadows, the Algae Sway shader on two materials and bubbles placed on every algae. A fresh or re-delivered build would not repeat them.
- **Choice:**
  - **V57 core (genre-agnostic):**
    - **Asset rules:** `Docs/V57/asset_rules.json` sets per asset `collision` (none, simple, exact, mesh, convex), `shadows` (on, off, two_sided), `shader` and `attach` prefabs.
    - **Kept prefabs:** Visual prefabs of ruled assets keep their colliders and shadows in sync with the current mesh on every build.
    - **Delivery import:** `delivery.js` places loose-folder deliveries and never deletes.
    - **Lessons:** `asset-intake.md`, `assets-collision-builds.md`, new `lighting.md`.
  - **This game:** 35 rules.
    - **Box:** crates, columns, consoles, hatch, sign, hoses, pipes, railings, scaffolds, wall panels, floors.
    - **Mesh:** arches, bulkheads, crystal panels, dome towers, rocks.
    - **No collision:** lanterns, algae, seabed, boat, skeleton, water, key, zone door, and LABDOOR (its gameplay leaf keeps the convex collider).
    - **Two-sided shadows:** structure pieces.
    - **Algae Sway:** on the two algae.
    - **Bubbles:** `VFX_SeaweedBubbles` attached to the four algae assets.
  - **Removed:** the 22 collision-only gameplay prefabs (collision now lives on the Visual prefabs) and the hand-placed `_VFX/SeaweedBubbles` (now `_Environment/_Attachments/VFX_SeaweedBubbles`, 160).
- **Verified:**
  - **Rebuild:** every solid piece has its collider. Box sizes are identical to the hand-fitted ones (B17 plate 2.00 × 0.22 × 1.43). Arches and rocks use mesh colliders; algae have none.
  - **Shadows and shader:** two-sided shadows on the structure; Algae Sway kept.
  - **Attachments:** 160 bubble emitters.
  - **Play:** HUD 0 / 18, doors and keys as D-271, the spawn corridor unchanged. Console: only the listed `VFX_LandSplash` warning.
  - **Tests:** EditMode 82/82 (game + core, including new asset-rule tests). PlayMode 28/28; the gold path still fails on the V06 route (D-270). Intake 35/35.
- **Reversal cost:** low · **Type:** absorption

### D-276 — Crab scuttle loops play in place (owner report: odd clip in fish and crab animation)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner report) · **Commit:** pending
- **Context:**
  - **Crabs:** `ANIM_MechanicalCrab_ScuttleL/R` move the root node 20 cm over each 1 s loop and snap back every cycle. In Play the body jumped up to 50 cm per cycle relative to the crab, which `CrabController` already moves along its patrol.
  - **Fish:** frame-by-frame capture and per-frame measurement show no jump. The only delivered fish clip is `HoverIdle`, which hovers in place, so fish travel along their paths without a swim cycle.
- **Choice:**
  - **V57 core:** `InPlaceLoopClips` (import postprocess). It removes the linear drift of the travelling root node from looping Generic clips, so they play in place. Only the two scuttles qualified.
  - **Rejected:** a motion node (root motion extraction) did not remove the travel with the copied avatar.
  - **Fish:** a swim cycle is listed in `MISSING_ASSETS.md`; no stand-in.
- **Verified:**
  - Scuttle loops close with zero offset (was 20 cm).
  - Play: the crabs' root bone no longer jumps.
  - Other clips unchanged (fish `HoverIdle`, crab `Idle`).
- **Reversal cost:** low · **Type:** absorption

### D-277 — Crab defeat VFX from the owner's electric explosion sheet (owner delivery)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:**
  - **Delivery:** the owner delivered `explosion_electricity`, a VFX Forge package: 5 × 3 sprite sheet, 15 frames of 256 px at 25 fps (0.6 s, one shot), straight alpha, plus a 7-frame 12 fps flipbook.
  - **Request:** use it when the Professor stomps a crab. TDD §B CrabEncounter: defeat → sparks. `VFXService` already plays `VFX_CrabDefeat` on `CrabDefeatedEvent`, but the slot was empty.
- **Choice:**
  - **Texture:** the 25 fps sheet as `Art/VFX/ExplosionElectricity/Textures/T_ExplosionElectricity_Sheet.png` (renamed to the T_ convention; no mips, clamp, alpha as transparency). The 12 fps flipbook and the concept image were not imported.
  - **Material:** `M_ExplosionElectricity`, URP Particles/Unlit, transparent additive, as the package recommends for energy effects.
  - **Prefab `Prefabs/VFX/VFX_CrabDefeat`:**
    - **Particle:** one billboard, 1.8 m, random rotation, 0.35 m above the crab.
    - **Animation:** texture sheet 5 × 3 over its 0.6 s life, in world space. It is one particle, well inside the INV-12 cap.
  - **Slot:** assigned in the gameplay scene's `VFXService`. The scene was first updated to Miguel's lighting commit (`e369a85`), so only the 3-line slot change is ours.
- **Verified:** Play, real stomp event on `CrabC3`: the crab is defeated and the explosion plays over it and fades in about 0.6 s. Captured as a frame strip.
- **Tuning:** size (`startSize`), height (`shape.position.y`) and duration (`startLifetime`) on the prefab.
- **Reversal cost:** low · **Type:** owner delivery

### D-278 — Fly pickup VFX from the owner's "VFX_MiPrimerEfecto" package (owner delivery)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:**
  - **Delivery:** the owner delivered `VFX_MiPrimerEfecto`, a VFX Forge package: layer sheet, Unity CLI recipe, 4 of its 7 textures. Requested use: when a fly is collected (`VFX_FlyPop`, TDD §B FlyCollection: pop VFX, 16 particles).
  - **Delivered textures:** Flash, Core (2 × 2), Spark and Ring, greyscale.
  - **Not delivered:** SimSmoke, Noise (distortion) and Decal.
- **Choice:**
  - **Textures:** the 4 delivered ones in `Art/VFX/MiPrimerEfecto/Textures/` (delivered names kept), one URP Particles/Unlit material per layer. Additive for flash, core and sparks; alpha for the ring.
  - **Prefab `Prefabs/VFX/VFX_FlyPop`:** the recipe's layers that have textures, scaled from its 1 m design to the 0.27 m fly. 15 particles, within the TDD's 16.
    - **Flash:** 0.08 s, 0.8 m.
    - **Core:** 0.45 s, 0.55 m, 2 × 2 sheet, size 0 → 1.2 → 1.0, white → #7CC8FF → #9B4DFF.
    - **Sparks:** 12, 0.3–0.4 s, 2.5–4.5 m/s, stretched.
    - **Ring:** 0.3 s, 1.2 m, horizontal.
  - **Layers not built:** the smoke, distortion and decal layers have no texture. No stand-in (`MISSING_ASSETS.md`).
  - **Stop Action:** not Destroy, unlike the recipe. `VFXService` reuses one instance per effect.
  - **Code:** `FlyCollectedEvent` now carries the fly's position. Before, the pop was spawned at the `VFXService` object. The pop is drawn 0.7 m toward the camera, because the fly is collected inside the Professor's body and the burst was hidden behind him.
  - **Scene:** only the `VFX_FlyPop` slot was added to `VFXService`, after checking that the scene had no remote changes.
- **Verified:**
  - Play from the game camera: collecting a fly shows the burst in front of the Professor and the counter ticks.
  - Close-up strip: flash → cyan core → violet fade in about 0.45 s.
  - Tests: EditMode 82/82, PlayMode 28/28 (gold path still the known V06 failure).
- **Reversal cost:** low · **Type:** owner delivery

## D-306 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/ExplosionElectricity/Textures/T_ExplosionElectricity_Sheet.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_ExplosionElectricitySheet); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: d28ee630cba4 -->

## D-307 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/MiPrimerEfecto/Textures/T_MiPrimerEfecto_Core.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_MiPrimerEfectoCore); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 4bf768e9e914 -->

## D-308 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/MiPrimerEfecto/Textures/T_MiPrimerEfecto_Flash.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_MiPrimerEfectoFlash); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 6f4c9446809c -->

## D-309 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/MiPrimerEfecto/Textures/T_MiPrimerEfecto_Ring.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_MiPrimerEfectoRing); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 47f30806f150 -->

## D-310 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow VFX_<Name>
- **Where:** `Assets/_Game/Art/VFX/MiPrimerEfecto/Textures/T_MiPrimerEfecto_Spark.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name VFX_MiPrimerEfectoSpark); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 44970113330c -->

### D-279 — Shark re-delivery (Export_02) and free-roaming sharks with manoeuvres (owner request)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner delivery + request) · **Commit:** pending
- **Context:**
  - **Delivery:** the owner delivered `Export_02`: the shark again (`SK_Shark`, textures identical to the ones in the repo) plus new clips `Attack`, `TurnLeft`, `TurnRight`, `TurnUp` and `TurnDown` (2.5 s, no loop), besides `PassiveTransit` and `EyeRotation`.
  - **Request:** replace the fixed shark route with dynamic ones that overlap nothing in the scene, play those clips along the way, and give each shark its own behaviour.
  - **Before:** both sharks swam one 18 × 9 m ellipse (`SharkLoop`) at their placed height, which already crossed platforms and rocks.
  - **Size:** the shark is about 17 m long, 10 m wide and 8 m tall.
- **Choice:**
  - **Import:**
    - **Delivery import:** with the V57 delivery tool. Same-name files are replaced in place (GUIDs kept), and the five new clips go to `Shark/Animations`. That placement is a core fix: new files of an existing family go to that family's folder.
    - **Animator:** `BuildAnimators` adds the new clips as states to the kept `AC_Shark`. That is also a core fix: kept controllers gain the states of new clips.
    - **Clip motion:** the turn and attack clips bend the body in place (no root travel or rotation), so the heading comes from code and the clip dresses the turn.
  - **`SharkCruise`:** replaces `SharkLoop` and the shark's `AmbientCreature`. `SharkLoop.cs` is deleted.
    - **Route network:** at start each shark samples 1800 points within 70 m and −2…+20 m of where the map placed it. It keeps the points where its whole body can turn without touching any collider: a sphere of half length 8.5 m + 1 m at its body centre.
    - **Legs:** taken only if a sphere of the half width (4.9 m + 1 m) sweeps them clear, and if they stay 24 m from the other shark's position and target and 12 m (+ body) from the Professor.
    - **Movement:** along the leg line; it slows to 15 % while turning, so turns happen inside the clear point. Start: the clear point nearest its map position.
    - **Manoeuvres:**
      - TurnLeft/TurnRight when a leg turns more than 30°, TurnUp/TurnDown when it climbs or dives more than 10°.
      - Attack: a lunge (slow wind-up, ×2.6 burst, glide) on a leg with 15 m ahead, every 7–16 s ÷ aggression.
      - EyeRotation: slows down to look, at some points (curiosity) and when the Professor is within 16 m (TDD AmbientCreatures reaction).
    - **Personality:** seeded by the shark's name: speed 1.6–2.5 m/s, turn rate 16–28°/s, aggression, curiosity and preferred depth.
- **Verified:**
  - **60 s Play runs:** 0 body overlaps out of 300 samples per shark (body sphere plus nose and tail spheres). Sharks always ≥ 32 m apart.
  - **Behaviours:** shark 01 has 49 clear points, shark 02 has 125. Shark 01: turns left/right and an attack. Shark 02: right turns, two attacks and two eye rotations.
  - **Frame strip:** the shark swims among the rocks and the wreck without touching them and lunges during Attack.
  - **Tests:** EditMode 82/82; PlayMode 28/28 (gold path: the known V06 failure).
- **Reversal cost:** low · **Type:** owner request

### D-280 — Shark clip transitions without pose jumps; ambient Animators always animate (owner report)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner report) · **Commit:** pending
- **Context:** the owner saw frame jumps between the shark's animations. Measured in Play (per-frame bone steps):
  - **Clip chaining:** every manoeuvre clip (Attack, the four turns, EyeRotation) starts and ends exactly on `PassiveTransit` frame 0. `SharkCruise` switched at any phase of the 8 s swim cycle, where the pose differs by up to 1.5 m.
  - **Culling:** the Animators used `CullUpdateTransforms`. The shark's tail leaves its skinned bounds, so the bones froze while the shark was still on screen and jumped when the bounds came back. Fish, crabs and jellyfish had the same setting, which is likely what looked odd in the fish (D-276).
  - **Authored pop:** `ANIM_Shark_Attack` itself has a tongue pop of about 44 cm in a quarter frame near frame 19.5.
- **Choice:**
  - **Sync phases:** `SharkCruise` samples `PassiveTransit` once and keeps the phases whose pose matches frame 0 within 10 cm: 0, 0.19, 0.40, 0.59, 0.80, one per tail beat (about 1.6 s apart).
  - **Manoeuvres:** a manoeuvre is queued and starts at the next such phase (0.2 s blend). Transit resumes from frame 0, where the clip ended.
  - **Culling:**
    - `PRF_Shark`, `PRF_Fish`, `PRF_Jellyfish` and `PRF_MechanicalCrab` Animators set to `AlwaysAnimate`.
    - The V57 core `AnimatorBinder` now generates `AlwaysAnimate`, with a lesson in `rigged-characters.md`.
  - **Attack tongue pop:** listed in `MISSING_ASSETS.md` as a provider re-export; not hidden in code.
- **Verified:**
  - 50 s Play, both sharks: no bone jump outside the authored Attack tongue (median step 1.7 cm per frame).
  - 139 fps with 73 always-animating Animators.
  - EditMode 82/82; PlayMode 28/28 (gold path: the known V06 failure).
- **Reversal cost:** low · **Type:** owner report

## D-311 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/HydroDomeTower_export.json`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: 461725c50ee9 -->

## D-312 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/HydroDomeTowerLOD01_export.json`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: f4e6f2136689 -->

## D-313 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/HydroDomeTowerLOD02_export.json`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: eaffc156c807 -->

## D-314 · I0 INTAKE · conflict · REPO_FOLDER
- **Issue:** path is outside the V57 contract tree (brief §1)
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/HydroDomeTowerLOD03_export.json`
- **Decision:** not imported by folder rules; listed as orphan
- **Rule:** authority TDD > ADD > disk (brief §1); provider files are not edited
<!-- v57-intake-key: bc6b6495abd3 -->

## D-315 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Meshes/SK_HydroDomeTower.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_HydroDomeTower); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 5c9efe51baf4 -->

## D-316 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Meshes/SK_HydroDomeTowerLOD01.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_HydroDomeTower); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 392878810390 -->

## D-317 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Meshes/SK_HydroDomeTowerLOD02.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_HydroDomeTower); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 289a21d22036 -->

## D-318 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow SM_<Name>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Meshes/SK_HydroDomeTowerLOD03.fbx`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name SM_HydroDomeTower); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 0bba04b72e9d -->

## D-319 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** contains spaces; name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Textures/S Prop_MetallicSmoothness.png`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_HydroDomeTower_MS); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: dc4522654b96 -->

## D-320 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** contains spaces; name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Textures/S Prop_normal.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_HydroDomeTower_N); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 261b4995284e -->

## D-321 · I0 INTAKE · fixable · NAME_CONVENTION
- **Issue:** name does not follow T_<Asset>_<BC|N|ORM|E|Mask>
- **Where:** `Assets/_Game/Art/Environment/Decoration/HydroDomeTower/Textures/S_Prop_albedo.jpg`
- **Decision:** file kept as-is; V57 maps it via asset_manifest (canonical name T_HydroDomeTower_BC); provider may rename in a later delivery
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 22b4230507e2 -->

## D-322 · I0 INTAKE · fixable · LAYOUT_ASSET_ALIASED
- **Issue:** layout asset_id(s) placed with another delivered model (Docs/V57/layout_aliases.json): AST-CHAR-NEONFLY-001→Fly, AST-CHAR-RADIOACTIVESHARK-001→Shark, AST-ENV-HYDRODOMETOWER-001→HydroDomeTower, AST_ENV_OCEANBOAT-001→AST-ENV-OCEANBOAT-001, AST_ENV_OCEANLEAVES-001→AST-ENV-OCEANLEAVES-001, AST_ENV_OCEANROCKLEAVES-001→AST-ENV-OCEANROCKLEAVES-001, AST_ENV_OCEANROCKS-001→AST-ENV-OCEANROCKS-001, AST_ENV_OCEANSKELETON-001→AST-ENV-OCEANSKELETON-001, AST-CHAR-FISH-001→Fish, AST-CHAR-JELLYFISH-001→Jellyfish, marker Door→PRF_ZoneDoor_Lab.prefab, marker Patrol→PRF_MechanicalCrab.prefab
- **Where:** `Docs/Design/LevelMaps/Level_01/unity_scene.json`
- **Decision:** aliased instances use the mapped model and yaw; provider should export the final asset ids
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 1b449efe9bfa -->

### D-281 — Animated jellyfish dome towers (HydroDomeTower) replace the static ones (owner delivery)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:**
  - **Delivery:** the owner delivered `Export_02` with the dome towers animated: `SK_HydroDomeTower` plus three LOD files `SK_HydroDomeTowerLOD01..03`, each with its own copy of the same 53-bone rig, the looping `ANIM_HydroDomeTower_PulseIdle` (97 frames, 4 s), export sidecars and 3 textures.
  - **Before:** the map's 13 `AST-ENV-HYDRODOMETOWER-001` instances used the static model.
- **Validation:**
  - **Rig:** one skeleton hash for all four files; same pivot (base) and height (13 m) as the static model, 11.2 m wide at rest because of the tentacles.
  - **Clip:** loops cleanly.
  - **Textures:** byte-identical to the static asset's albedo, normal and MetallicSmoothness. Two names contain spaces and none follow `T_`.
  - **Export check:** R2 is "warn" in the sidecar, as with the shark.
  - **Unity warning:** each level file warns "inconsistent LOD naming".
- **Choice:**
  - **Import:** the core delivery tool groups the asset into `Art/Environment/Decoration/HydroDomeTower/` (Meshes, Animations, Textures, sidecars). Core fixes:
    - convention-named assets are grouped;
    - `_export.json` sidecars are valid at any asset root.
  - **Prefab:** a single `PRF_HydroDomeTower_Visual`:
    - **LODs:** the core `SiblingLodFiles` merges the three level files into the main model, rebinding them to its skeleton. LODGroup 4 levels, one Animator (`AC_HydroDomeTower`, `PulseIdle`, AlwaysAnimate).
    - **Collision:** mesh collider from the LOD0 bind pose (core: `mesh` collision now covers skinned meshes).
    - **Shadows:** two-sided.
  - **Layout:** alias `AST-ENV-HYDRODOMETOWER-001 → HydroDomeTower` (yaw 180°, the rigged-export rule of D-252); asset rule `HydroDomeTower: mesh, two_sided`.
  - **Material:** `MAT_HydroDomeTower`. The map manifest's maps for the dome id point at the static asset's textures, which are byte-identical, so the look is unchanged.
  - **Not removed:** the static asset files stay (provider-owned) and are no longer placed.
- **Verified:**
  - Rebuild: 13 domes on the animated prefab, all four LOD renderers on one rig, not static.
  - Play: the domes pulse and the tentacles sway (frame strip); console only shows the listed `VFX_LandSplash`; 115 fps.
  - Tests: intake 36/36, EditMode 82/82, PlayMode 28/28 (gold path: the known V06 failure).
- **Provider fixes (MISSING_ASSETS):** one file with `_LOD#` nodes; `T_` texture names without spaces.
- **Reversal cost:** low · **Type:** owner delivery

## D-323 · I0 INTAKE · fixable · LAYOUT_EXTRAS
- **Issue:** 56 V57 object(s) added to Level_01 on top of the provider map
- **Where:** `Docs/V57/layout_extras/Level_01.json`
- **Decision:** appended to layouts.json; the provider map is unchanged
- **Rule:** logged fixable normalization (brief §3); provider files are not edited by intake
<!-- v57-intake-key: 79cb22e9988c -->

### D-282 — More ambient fish and small jellyfish (owner request)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:**
  - **Request:** populate the level with more of the existing fish and small jellyfish (not the domes).
  - **Map:** Level_01 had 46 fish (8 solo fish, one per `PATH_Fish_*`, and 38 in 4 schools) and 14 jellyfish near the seabed.
  - **Constraint:** the scene is rebuilt from the map, so hand-placed scene objects would be lost, and the provider map is read-only.
- **Choice:**
  - **V57 core:** the intake appends `Docs/V57/layout_extras/<LevelId>.json` (`objetos`, map object format) to the provider map (`LAYOUT_EXTRAS`); lesson in `asset-intake.md`.
  - **This game, `Docs/V57/layout_extras/Level_01.json`:** 56 objects marked `origen: V57`, generated in the editor against the scene colliders (seed 57).
    - **Fish +40:**
      - two companions trailing each solo fish (groups of 3; lateral ±0.9 m, height ±0.35 m, 0.8–2.2 m behind, scale 0.75–1.0);
      - one fish at the opposite phase of each solo path;
      - 4 more per school (school spread and scale).
      - Each has its own animation offset, and every offset is accepted only if the body touches no collider at 90 points around the whole loop.
    - **Jellyfish +16:**
      - Placement: 4–12 m around the existing ones, y −2.8…−0.9, scale 0.7–1.1, drift 0.3–0.6 m, at least 3 m apart.
      - Check: body grown by its drift + 20 cm touches no collider.
- **Verified:**
  - Rebuild: 86 fish, 30 jellyfish. The scene keeps the lighting and the VFX slots.
  - Play: 0 contacts with the scenery in 40 samples over 20 s; fish moving.
  - Captures show jellyfish at several heights and fish groups.
  - About 102 fps (115 before).
  - EditMode 82/82; PlayMode 28/28 (gold path: the known V06 failure); intake 37/37.
- **Reversal cost:** low (delete the file and rebuild) · **Type:** owner request

### D-283 — Far and off-screen creatures stop animating; school fish no longer jump (owner request + report)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:**
  - **Request:** whatever is far away or off camera should not animate until it comes into play. D-280 had set `AlwaysAnimate`, because `CullUpdateTransforms` froze rigs that were visible but outside their rest-pose bounds.
  - **Report (screenshot):** the fish beside the platform still "jump". Per-frame measurement found the fish offset sideways from their path (D-282 companions, provider schools) jumping up to 7 m/s for a frame, twice per loop.
- **Choice (V57 core):**
  - **`AnimatedBounds`:**
    - **Bounds:** at prefab build and kept-prefab refresh, every SkinnedMeshRenderer's bounds are fitted to all poses of all clips (16 samples per clip, +5 %).
    - **Culling:** Animators go back to `CullUpdateTransforms`. Nothing off screen animates, and nothing visible freezes.
  - **Asset rule `cull_below`:** a screen-height fraction. It adds a one-level LODGroup (or sets the last level of an existing one), so below that size the rig is neither drawn nor animated. Intake, schema, DTO and test.
  - **Collision refresh:** only when a rule sets collision (`collision_ruled`), so a `cull_below`-only rule never touches colliders.
  - **`V57Path.Evaluate`:** continuous tangent (vertex = mean of its two segments, blended along the segment). Cause of the fish jump: a per-segment tangent turned in steps at every vertex, and a lateral offset jumped with it.
- **Game:**
  - **Rules:** `Fish` and `Jellyfish` `cull_below 0.015`, `MechanicalCrab 0.012`. Sharks, domes (own LODs) and flies (pickups) get no distance rule.
  - **Prefabs:** the `AlwaysAnimate` overrides of D-280 on PRF_Shark/Fish/Jellyfish/MechanicalCrab were reverted.
- **Verified:**
  - **Fish (Play, 86 fish over 40 s):** median 0.81 m/s; the 7 m/s single-frame jumps are gone. The fastest is 3.2 m/s, held smoothly over several frames by a school fish on the outside of a tight curve.
  - **Screen size:** from the reported spot the nearby fish fill 11–32 % of the screen, far above the 1.5 % cull.
  - **Sharks:** with `CullUpdateTransforms`, no bone jump in view over 50 s except the Attack clip's own tail whip (authored).
  - **Performance:** 109 fps (102 with everything always animating).
  - **Tests:** EditMode 82/82; PlayMode 28/28 (gold path: the known V06 failure); intake 37/37.
- **Reversal cost:** low · **Type:** owner request

### D-284 — Half of the V57 jellyfish raised into the player's view (owner request)
- **When:** 2026-10-14 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** the 16 V57 jellyfish of D-282 all floated near the seabed (y −2.8…−0.9), below the player. The owner asked for some at platform height or a little higher, in the player's view.
- **Choice:** 8 of them (`JELLY_V57_02, 04 … 16`) moved in `Docs/V57/layout_extras/Level_01.json`, generated in the editor (seed 83).
  - **Placement:** one per route fly spread along the level, 3–8 m out from the fly, at −0.5…+3 m of its height.
  - **Checks:** open water (body + 0.6 m drift + 30 cm touches nothing); nothing within 5 m below, so they float beside the walkways, not over them; at least 4 m from every other jellyfish.
  - **The other 8 stay low.**
- **Verified:**
  - Rebuild: raised jellyfish at y 2.5–6.6.
  - Play: one seen from the gameplay camera between the domes; 0 contacts with the scenery for the 86 fish and 30 jellyfish over 20 s.
- **Reversal cost:** low · **Type:** owner request

### D-285 — Intro hand-off: the level opens on the video's last shot and blends to play (owner request)
- **When:** 2026-10-19 · **Stage:** post-M4 (owner request) · **Commit:** pending
- **Context:** the intro video (`CIN_Intro.mp4`, D-266) ends on a static wide shot of the B-17 gate with the Professor in the start tunnel; the level then cut to the follow camera. The owner asked for the middle option: keep the video and make only the ending in-engine with Cinemachine.
- **Choice:**
  - **Same framing:** the video's last frame matches the level's own start (`AST-ENV-BULKHEAD-001_03` gate with the "RESEARCH WING B-17" and "SECTOR 7" signs, beacon crates on the grate platform). `CM_Opening` (under `_Cameras`) sits 15 m in front of `Marker_Spawn_Player`, 2.47 m above it, FOV 50, hard look-at on the Professor (+1.6 m).
  - **Hand-off:** when the video plays to its end, `IntroCinematic` pauses on the last frame (instead of `Stop`, which cleared it) and flags the hand-off. `CameraRig.Bind` consumes it: `CM_Opening` goes live (priority 30), holds `CameraConfig.OpeningHold` (0.6 s), then drops to 0.
  - **Blend:** brain custom blend `Assets/_Game/Settings/Camera/CMB_CameraBlends.asset`: `CM_Opening` → any camera, ease in/out 2.5 s. Both cameras aim at the Professor, so the move is a crane through the gate and over his head to the usual follow framing. Every other camera change keeps the default 0.5 s blend.
  - **Not triggered:** skipping the video, restarting the level, or loading the level directly start on the follow camera as before.
- **Verified (Play):** intro scene played to the end → the level opened at `CM_Opening` (−30.70, 11.92, 7.75), blended, and settled behind the Professor (−49.60, 11.24, 7.75; pitch 13°) with the orbit axes unchanged. No new console errors.
- **Reversal cost:** low · **Type:** owner request

### D-286 — Level music replaced by the owner's new theme (MP3 for now)
- **When:** 2026-10-19 · **Stage:** post-M4 (owner delivery) · **Commit:** pending
- **Context:** the owner generated a new level theme with ElevenLabs Music (`music_v2_5`, 120 s, prompted as a loopable 112 BPM D-dorian piece with no intro or ending) and prefers it to `MUS_PuffAndRebellion.wav` (D-258). The API delivers MP3 on the owner's plan; a WAV master is still to be decided.
- **Choice:**
  - **File:** `Assets/_Game/Audio/Music/MUS_Level01_Intensity.mp3` (owner delivery, content untouched); import settings copied from the previous music (streaming, Vorbis 0.7, load in background).
  - **Looping:** `MusicLooper` now plays it from 0 s with no intro (`_loopStart` 0) and a 1.5 s equal-power crossfade at the seam (was 12 s / 0.3 s), since a generated loop's ends are not sample-matched. Volume stays 0.35.
  - **Kept:** `MUS_PuffAndRebellion.wav` stays in the folder, unreferenced, until the owner decides.
- **Pending:** replace the MP3 with the WAV master when available (same name, `.wav`; re-point the looper). The TDD §10 two-layer setup (base + intensity) is still not built: this single cue plays as the level's only music layer.
- **Reversal cost:** low · **Type:** owner delivery
