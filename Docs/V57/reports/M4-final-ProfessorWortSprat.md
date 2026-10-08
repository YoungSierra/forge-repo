# M4 — Final report · Professor Wort & Sprat (vertical slice)

TDD `Docs/Design/TDD.md` 1.0.0 (TDD Standard 2.1.0) · Unity 6000.6.2f1 · branch `v57/setup` · no player build (not requested).

## Gates

| Stage | Gate | Result | Evidence |
|---|---|---|---|
| I0 | intake exit code | passed (0 blocking) | `Docs/V57/INTAKE_REPORT.md` |
| I1 | project baseline | passed | `Docs/V57/reports/I1-project-ProfessorWortSprat.md` |
| I2 | assembly report | passed (0 errors, 0 missing refs) | `Docs/V57/reports/assembly-report.json` |
| M1 | gold path | passed | `Docs/V57/reports/M1-goldpath-ProfessorWortSprat.md` |
| F | 7 specs: EditMode + PlayMode + gold path | passed — EditMode 13/13, PlayMode 28/28 | `Docs/V57/reports/F-*-ProfessorWortSprat.md` |
| M2 | lint, hierarchy diff, collision with mesh, gold path | passed — lint 0 errors, diff pass, 0 collision violations | `Docs/V57/reports/M2-scene-ProfessorWortSprat.md` |
| M3 | independent review (min 7) | **failed-timeboxed** — rounds 1–2 scored 5–6; remaining defects are art not delivered (D-020) | `Docs/V57/evidence/review/20261008T004911Z/review.json` |
| M4 | playability from evidence | gold path 23/23 with 0 console errors on the final commit; the full loop (move → jump → stomp → collect 6 → key → door → exit → level end) plays with real input and physics | `Docs/V57/evidence/goldpath/20261008T004902Z` |

\* latest gold path run folder of the M3 round-2 capture (see `STATUS.json → goldpath`).

## Per-item status

| Item | Status word |
|---|---|
| Locomotion, Jump, Stomp, FlyCollection, KeyMaterialisation, CrabEncounter, SpratCompanion (TDD §B) | pending owner review (agent-verified by tests + gold path) |
| ZoneFlow, InputHandler (camera-relative), CameraRig (Crash-style follow + key shot), RespawnService, SaveService, PauseController, HUD, LevelEnd, MainMenu (TDD §B-S, §9.1) | pending owner review (agent-verified by gold path; save/pause not in the gold path) |
| Audio, VFX, AmbientCreatures | implemented — wired, slots empty until delivery |
| Visual quality (camera composition, lighting, palette, readability) | implemented — independent review below threshold (art gaps) |

## Open defects

1. Delivered `AST-PROP-ZONEDOOR-001` has no texture (flat colour) — dominates the tunnel view (provider).
2. Sky panorama and crystal glass are saturated cyan/blue; TDD §8 asks for a 30 % desaturated background with cyan reserved for flies and Sprat (provider art / ADD missing).
3. Key glyph is text ("KEY"); `ICO_AccessKey` and all UI art/mockups missing (provider).
4. Level contract not in the delivered map: the slice runs on a temporary V57 test placement (D-013) until `LevelMaps` carries markers, crabs and rigged ids; then `AssemblyRunner.RebuildLevelContent()` replaces it.
5. No audio and no VFX delivered — the game is silent and has no effects.

## Missing assets

See `Docs/V57/MISSING_ASSETS.md` (level contract markers, ADD, UI mockups/sprites/icons/font, music/SFX/ambience/VO, VFX, drop-shadow decal, normal/ORM maps, licenses).

## Decisions

D-012 TDD installed · D-013 test placement · D-014 duplicate Visual prefabs · D-015 amber ramp · D-016 test route + key shot · D-017 spec paths · D-018 animator cross-fade · D-019 PlayMode tests vs gold path · D-020 M3 timebox (full text in `Docs/V57/DECISIONS.md`).
