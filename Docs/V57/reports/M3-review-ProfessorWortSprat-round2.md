# M3 independent review — Professor Wort & Sprat — round 2

Reviewer: fresh-subagent-m3-round2. The review pack is `Docs/V57/evidence/M3/20261008T004911Z/review-pack/`. No reference images were delivered, so I compared against style.md (TDD §8/§9/§11.5) only.

## Scores (min 7)

| Area | Score | Reason |
|---|---|---|
| Camera & composition | 6 | The follow framing roughly matches §11.5. A flat dark slab dominates the centre of every shot, crab_defeated is cluttered, and the key shot shows no visible key. |
| Lighting & exposure | 5 | The tube glass is a flat, blown, saturated blue. The slab and ground planes are flat. The kit shading itself is OK. |
| Materials & palette | 5 | The door/back-wall slab is untextured. Amber is overused (stripes, pipes, lamps) and is not on the door. |
| UI vs mockups | n/a | No UI mockups delivered. |
| Gameplay readability | 5 | The background is not desaturated and uses cyan. Flies are tiny against the dark slab. The carried key is not visible, and the HUD shows the text "KEY" instead of a glyph. |
| VFX & feedback | n/a | No VFX delivered. |
| Animation | n/a | No animation previews delivered. |
| Overall style match | 5 | The genre reads, but the missing material, flat planes and broken palette reservations fall well short of the style. The menu has no art. |

## 3 worst defects

1. **Materials & palette**, `shots/SCN_HydroStation_Gameplay_key_carried_01.png`: a flat, untextured dark rectangle (door/back wall) sits in the centre of every gameplay shot and reads as a missing asset. It is not amber either. Reference: §8 hand-painted kit, with amber reserved for the key and door.
2. **Gameplay readability**, `shots/SCN_HydroStation_Gameplay_start_01.png`: the background is saturated, with cyan jellyfish tanks and bright blue glass. Amber hazard stripes, pipes and lamps appear everywhere, and the flies are tiny specks. Reference: §8 calls for a 30 % desaturated background, cyan only for flies and Sprat, and amber only for the key and door.
3. **Camera & composition**, `shots/SCN_HydroStation_Gameplay_key_carried_01.png`: numbers.json claims the key shot frames the Professor and key together, but no key object is visible in key_carried or zone_complete, and the HUD shows the text "KEY" instead of a glyph. Reference: §11.5 says the key shot frames the Professor and key anchor; §9 calls for a key glyph while carried.

## Verdict

**below_threshold**: all five scored areas are below 7.
