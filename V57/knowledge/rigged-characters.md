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
