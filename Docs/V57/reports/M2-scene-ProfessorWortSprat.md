# M2 — Scene = game · ProfessorWortSprat

| Gate | Result | Evidence |
|---|---|---|
| Runtime lint (`--root Assets/_Game/Scripts --root Assets/_Game/Tests`) | pass — exit 0 (0 errors; warnings are `FindAnyObjectByType` inside tests only) | `node V57/tools/lint/runtime-lint.js` |
| Edit vs Play hierarchy diff | pass — no created objects outside `_Gameplay/Spawned`; 2 activation changes = the Access Key visuals hidden until their zone completes (TDD §B KeyMaterialisation: "no world presence until OnZoneComplete") | `Docs/V57/reports/hierarchy-diff.json` |
| Collision with mesh | pass — 36 colliders on prefab instances that carry their mesh (33 walkable kit pieces, 2 zone doors, the Professor's CharacterController); 3 marker volumes (triggers, no mesh by contract); 0 violations | eval check (collision_check) |
| Gold path | pass — 23/23, 0 console errors (menu scene at build index 0) | `Docs/V57/evidence/goldpath/20261008T003925Z/checks.json` |
| Tests | EditMode 13/13 · PlayMode 28/28 | Unity Test Runner via CLI |

## Scene content

- `SCN_MainMenu_Boot` (build index 0, TDD entry scene): UI_MainMenu (Start / Continue when a save exists / Quit), SaveService, camera.
- `SCN_HydroStation_Gameplay` (index 1): level content rebuilt from LevelMaps/Montaje with gameplay prefabs placed automatically (`layout_gameplay` 35), test placement D-013, Professor + Sprat, ZoneFlow and support systems, Cinemachine follow + key-shot cameras, HUD / pause / level-end / loading UI documents.
- Gameplay prefabs: variants of the Visual prefabs (behaviour never on Visual prefabs); colliders on the piece's own prefab.

## Wired with empty slots (no placeholders)

Audio cues (`SFX_*`, `MUS_*`) and VFX effects (`VFX_*`) are bound to their events; their clips/prefabs stay empty and log one warning each until delivered (Docs/V57/MISSING_ASSETS.md). UI is text + palette until mockups and sprites arrive. Drop-shadow decal material missing.

**Status word:** agent-verified (tool gates). Visual quality is for `/independent-review` at M3.
