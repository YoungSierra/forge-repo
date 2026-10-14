# Rigged characters and animations (Blender → FBX → Unity)

## ANIM_ files only animate when their root node is preserved
- **Symptom:** sampled clips laid every character on its back and no bone moved.
- **Cause:** an `ANIM_` FBX exported "armature only" has a single root node (the armature). Unity collapses a single root into the model root, so the curves are bound to `Def-root/…` (and the armature's −90° X lands on the Animator object), while the `SK_` hierarchy is `<Rig>/Def-root/…`. Generic clips bind by path, so nothing matches.
- **Rule:** `ANIM_*` import with `preserveHierarchy = true`; check a clip's root binding path equals the rig node name in `SK_` (e.g. `Professor_Rig`).
- **Enforced by:** `ModelImportRules` (unity-assembly). **Source:** PS (ProfesorSprat character delivery, 2026-10).

## Loop flags come from the provider's export sidecar
- **Symptom:** `Excited`, `CruiseFlight`, `LazyCircleDrift`, `PassiveTransit`, `ScuttleL/R` imported as one-shot.
- **Rule:** loop source order is asset manifest `animations[].loop` → `Characters/<Id>/<Id>_export.json` `clips[].loop` (Blender export sidecar) → name heuristic (idle/walk/run/loop/cycle). The sidecar is a valid delivery file at the character root.
- **Enforced by:** `CharacterExportLookup` + `ModelImportRules`; intake classifies `<Id>_export.json` as `sidecar`. **Source:** PS.

## The −90° X inside Blender rigs is expected
- **Symptom:** inside `SK_`/`ANIM_` the `<Rig>` and mesh nodes import at 270° X although the Blender objects are at 0/0/1.
- **Cause:** Blender's FBX axis conversion (Z-up → Y-up). *Apply Transform* (`bake_space_transform`) zeroes meshes only, not armatures; Unity *Bake Axis Conversion* turns the model 180° and leaves +90° — both rejected.
- **Rule:** accept it: the FBX root imports at (0,0,0)/(1,1,1), the character stands and faces +Z. Contract check for rigs = root 0/1, upright, facing +Z. Zeroing the internal node needs the Blender rotate-apply trick (−90° X, apply, +90° X) on armature + mesh and a re-export of every clip — only with an owner request and a test on one character first.
- **Source:** PS (tested on Fly with *Apply Transform*: mesh 0°, rig still 270°, identical pose).

## Export settings the provider must use
- FBX: Apply Scalings **FBX All**, Apply Unit on, Forward **−Z**, Up **Y**, Apply Transform off.
- `SK_`: Armature + Mesh, Only Deform Bones on, Add Leaf Bones off, Bake Animation off.
- `ANIM_`: Armature only, same skeleton/bone names as `SK_`, Bake Animation on, NLA Strips off, All Actions off (one action per file), Key All Bones + Force Start/End Keying on.
- Manual dialog defaults (All Local, Add Leaf Bones, NLA Strips, All Actions) are wrong for Unity.
- **Source:** PS (provider export script settings, verified in Unity).

## Static stand-ins are replaced when the rigged asset lands
- **Rule:** when a rigged `SK_<Id>` supersedes a static model, swap every scene instance to `PRF_<Id>_Visual`, delete the old art folder, materials and prefab, log the mapping `D-###`, and check 0 missing prefab instances. Mind facing: Blender static exports with `export_yaw_z_deg: 180` face −Z, rigged `SK_` face +Z (add 180° yaw when swapping).
- **Source:** PS (shark, jellyfish, neon fly).

## Looping clips must play in place
- **Symptom:** the crabs' `ScuttleL`/`ScuttleR` loops moved the whole body 20 cm sideways over one second, then snapped back on every cycle while the script moved the crab.
- **Cause:** the clip was exported with travel on the root node. V57 gameplay moves characters from code (no root motion). Setting a motion node does not extract the travel for a Generic `ANIM_` that copies the `SK_` avatar.
- **Rule:** at import (`OnPostprocessAnimation`), a looping Generic clip whose shallowest travelling node ends at least 5 cm from its start loses that node's linear drift. The loop closes exactly, and the sway and bob are kept. Each clip fixed is logged in the import log. Provider fix: export loops in place.
- **Enforced by:** `Import/InPlaceLoopClips.cs` (`V57AssetPostprocessor` rules version 2). **Source:** PS (D-276).

## New clips for a character that already has a controller
- **Symptom:** a re-delivery added `ANIM_Shark_Attack` and four turn clips, but `AC_Shark` kept only its two old states, because existing controllers are kept.
- **Rule:** `BuildAnimators` adds a state, with no transitions, for every clip that has none in a kept controller, and relinks states whose motion was lost. Existing states, transitions and the default state are never touched.
- **Enforced by:** `AnimatorControllerBuilder.AddMissingStates`. **Source:** PS (D-279).

## Animation pops: culling and clip chaining
- **Symptom:** the sharks' animation jumped between clips and sometimes mid-clip.
- **Cause:**
  - **Culling:** Animators used `CullUpdateTransforms`. The 17 m shark's tail leaves its skinned bounds while it swims, so the renderer was judged off screen while still visible, the bones froze, and they jumped when the bounds came back.
  - **Chaining:** the delivered manoeuvre clips all start and end on the transit clip's first pose, but code switched to them at any point of the swim cycle (up to 1.5 m of pose difference).
- **Rule:**
  - **Culling (superseded by D-283):** fit the skinned bounds to every pose of every clip (`AnimatedBounds`) and keep `CullUpdateTransforms`. Off-screen rigs then stop animating with no freeze in view. `AlwaysAnimate` hid the bug at a cost.
  - **Chaining:** when clips are authored to chain from a loop's first pose, start them at a loop phase that matches that pose (sample the loop once and keep the phases within 10 cm). Resume the loop from its first frame.
  - **Check:** sample a clip per quarter frame for authored pops before blaming the blend. Example: the shark `Attack` tongue moves 44 cm in a quarter frame, which is a provider fix.
- **Enforced by:** `AnimatorBinder` + `AnimatedBounds` (culling); the game's `SharkCruise` (sync phases). **Source:** PS (D-280, D-283).

## LOD levels delivered as separate rigged files
- **Symptom:** an animated asset arrived as `SK_HydroDomeTower.fbx` plus `SK_HydroDomeTowerLOD01..03.fbx`, each with its own copy of the rig, instead of `_LOD#` nodes in one file. Unity also warns on each level file: "inconsistent LOD naming (_LOD1 found but no _LOD0)".
- **Rule:** `SiblingLodFiles` merges the level files (`<stem>LOD##` / `<stem>_LOD##` next to the model) into the model's Visual prefab.
  - **Renderers:** move under the main instance; skinned ones are rebound to the main skeleton by bone name. One Animator drives every level.
  - **LODGroup:** the main model is LOD0, then the files in level order.
  - **Limits:** the level files get no prefab of their own. A bone missing from the main rig skips that renderer with a warning. Kept prefabs with level files are rebuilt, not refreshed in place.
  - **Provider fix:** one file with `_LOD#` nodes.
- **Related:**
  - **Mesh collision:** `collision: mesh` now also covers skinned meshes, using their bind pose.
  - **Export sidecars:** `<Asset>[LOD##]_export.json` is valid at the root of any asset folder, not only characters.
  - **Delivery import:** `delivery.js` groups a new convention-named asset (`SK_`/`SM_`/`BLK_`, LOD files, `ANIM_<Asset>_*`, export sidecars, and the loose textures of a single-asset delivery) into one asset folder.
- **Enforced by:** `SiblingLodFiles`, `LodGroupBuilder.ApplyLevels`, `PrefabColliderBuilder`, intake `naming.js`, `delivery.js`. **Source:** PS (D-281).

## Far and off-screen rigs do not animate
- **Rule:**
  - **Off screen:** animated Visual prefabs get skinned bounds that cover all their clips (16 samples per clip, +5 %) and `CullUpdateTransforms`. Nothing off screen is animated, and nothing visible freezes.
  - **Far:** the asset rule `cull_below` (screen-height fraction) adds a one-level LODGroup (or sets the last level of an existing one). Below that size the rig is neither drawn nor animated.
  - **Choosing a value:** check it from the real gameplay camera. Small ambient creatures can take about 1.5 %. Large set pieces keep their own LODs, and gameplay-critical actors (pickups) get no rule.
- **Enforced by:** `AnimatedBounds` (build and kept-prefab refresh), asset rules `cull_below`. **Source:** PS (D-283).
