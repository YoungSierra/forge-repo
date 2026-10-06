# PLAN — <GameTitle> (`<slug>`)

Written at M1 from `Docs/Generated/json/*`. Re-read at every session start. The owner may edit the **Resolved variables** and **Scope** sections between runs; V57 logs any change it makes as `D-###`.

## Resolved variables

| Variable | Value | Source |
|---|---|---|
| slug | | package.json |
| mode | VerticalSlice \| Production | invocation |
| tdd | Docs/Design/TDD.md | provider |
| add | Docs/ArtDirection/ArtDirectionDocument.md | provider |
| engine | 6000.6.2f1 | V57 pin |
| perspective / physics | | package.json |
| platform / orientation / reference resolution / fps | | package.json |
| test assembly prefix | | TDD §A |
| gold path scene | Assets/_Game/Scenes/SCN_<...>.unity | scenes.json |
| gold path source | acceptance.json \| Docs/V57/gold_path.json | M1 |

## Scope

| Scenes (slice) | Mechanics in scope | Out of scope (reason) |
|---|---|---|
| | | |

## Camera (locked at M1)

| View | Type | FOV/size | Angle | Distance | Follow | Source |
|---|---|---|---|---|---|---|
| | | | | | | camera.json |

## Spec queue (§D order)

| # | specId | Path | Depends on | Core loop step |
|---|---|---|---|---|
| 1 | | V57/specs/<slug>/… | | |

## Stage plan

| Stage | Planned work | Timebox | Gate |
|---|---|---|---|
| M1 | | 240 min | checks.json all pass |
| F | | 90 min / spec | tests + gold path |
| M2 | | 240 min | lint, diff, build smoke |
| M3 | | 240 min | independent review ≥ 7 |
| M4 | | 120 min | cert + QA from evidence |

## Known risks (from INTAKE_REPORT)

- 
