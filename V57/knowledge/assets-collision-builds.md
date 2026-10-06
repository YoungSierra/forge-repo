# Missing assets, collision and builds

## No placeholders — report missing resources instead
- **Symptom:** a magenta cube stood in for the plunger, synthesized tones for every SFX, a sprite fallback for a missing portrait; reviewers scored them as broken art and the owner could not tell delivered content from V57 filler.
- **Rule:** V57 never generates placeholder art or audio (primitives, `MAT_V57_Placeholder`, generated textures/meshes, synthesized or recorded sounds, "closest match" sprite fallbacks). Every missing resource is listed in **`Docs/V57/MISSING_ASSETS.md`** (written at I0, updated whenever a stage finds a gap) and the slot stays empty: the serialized reference is unassigned, the feature logs one warning, and the scene keeps working without it.
- **Gameplay that needs a missing mesh** (e.g. a plunger) keeps only its functional parts (collider, behaviour) on the gameplay prefab, with no stand-in visual; the gap is listed as `blocks: visual` in `MISSING_ASSETS.md`.
- **`MISSING_ASSETS.md` sources:** intake `missing` issues, `DELIVERY.md` / provider notes, TDD §13.1 inventory vs files on disk (meshes, LODs, textures, UI sprites per §9.1 screen, audio stems/SFX/stingers/VO), ADD `asset_briefs` without files, and anything a later stage finds. Columns: asset / category / required by (TDD §, mechanic or screen) / expected path / impact (`blocks: gameplay | visual | audio | none`) / status (`missing | delivered`).
- **Enforced by:** `/intake` I0 writes the file; M4 final report links it; `/independent-review` packs never contain V57 filler. Legacy `AssemblyRunner.BuildPlaceholders()` output is never instanced in a scene (pack follow-up: remove the step).
- **Source:** HH run 2 (2026-09).

## Collision lives with its mesh
- **Symptom:** table collision was authored as standalone `COL_*` objects copied from the dressing poses; moving a rail in the scene left its collider behind.
- **Rule:** every collider lives on the same prefab as the mesh it represents — the gameplay prefab of that piece (root = collider + behaviour, Visual prefab nested). Static kit pieces (rails, corners, walls, deflectors) get a gameplay prefab too, instanced at their marker; never a detached collider object, never a collider "layer" rebuilt from marker poses. Moving or deleting the piece moves or deletes its collision.
- **Physics space follows the delivered art:** when the provider scene is 3D (Y-up table/level in XZ), use 3D physics (Rigidbody + constraints to the play plane) even if the TDD says "2D plane"; log the conflict `D-###`. Do not rotate the level into XY to force Physics2D — it splits colliders from meshes and puts real-scale objects outside the solver's comfortable range.
- **Enforced by:** `/prefab`, `/physics-setup`, hierarchy review at M2. **Source:** HH run 2 (Physics2D on an XZ table: detached colliders, resting-ball jitter).

## No player builds unless the owner asks
- **Rule:** the pipeline does not produce player builds. M2 and M4 verify in the Editor (tests, gold path, lint, hierarchy diff, console). `/build-setup` runs only when the owner explicitly requests a build in the current request, for the platforms they name. Build folders (`Builds/`) are never committed.
- **Source:** HH run 2.
