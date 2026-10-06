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
