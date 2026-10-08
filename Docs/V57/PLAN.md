# PLAN — Professor Wort & Sprat (`ProfessorWortSprat`)

Written at M1 from `Docs/Generated/json/*`. Re-read at every session start.

## Resolved variables

| Variable | Value | Source |
|---|---|---|
| slug | ProfessorWortSprat | package.json |
| mode | VerticalSlice | invocation |
| tdd | Docs/Design/TDD.md (1.0.0, TDD Standard 2.1.0) | provider (owner) |
| add | missing (Docs/V57/MISSING_ASSETS.md) | — |
| engine | 6000.6.2f1 | V57 pin |
| perspective / physics | 3d / 3d | package.json |
| platform / orientation / reference resolution / fps | PC + console-ready / landscape / 1920×1080 / 60 | package.json |
| test assembly prefix | ProfessorSprat | TDD §A |
| gold path scene | Assets/_Game/Scenes/SCN_HydroStation_Gameplay.unity | scenes.json (slice) |
| gold path source | Docs/V57/gold_path.json (from the acceptance.json draft + §3) | M1 |
| level | HydroStation ← LevelMaps/Montaje (test map, linked by role) + temporary test placement (D-013) | intake + D-013 |

## Scope

Slice scene `SCN_HydroStation_Gameplay`; all 7 §B mechanics (the §D closure covers every mechanic) and the 12 §B-S systems.

## Spec queue (§D order)

1. locomotion → 2. jump → 3. stomp → 4. crab_encounter → 5. fly_collection → 6. key_materialisation → 7. sprat_companion

Each spec: EditMode + PlayMode tests in its §13.2 test scene (`SCN_<Mechanic>_Test`, synthetic geometry), then a gold path re-run.
