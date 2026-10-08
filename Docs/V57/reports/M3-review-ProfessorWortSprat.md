# M3 Independent Review — Professor Wort & Sprat (round 1)

Reviewer: `fresh-subagent-m3-round1` · Pack: `Docs/V57/evidence/M3/20261008T004146Z/review-pack` · Threshold: 7

## Scores per area

| Area | Score | Reason |
|---|---|---|
| Camera & composition | 5 | The framing varies between shots (high and distant, steep top-down, off-axis yaw), the Professor is small and far away, and the ring frames block the frame edges. This contradicts the claimed fixed offset (0, 3.2, -6.5) and FOV 50. |
| Lighting & exposure | 6 | Exposure is usable and specular is low. The tunnel glass is flat, near-overexposed blue with sawtooth edges, and the dark slab at the end of the tunnel is unlit. |
| Materials & palette | 5 | The kit is hand-painted and uses the palette. The tunnel end is an untextured dark navy slab, and the ground/water and glass are flat blue fills. |
| UI vs mockups | n/a | No UI mockups delivered. |
| Gameplay readability | 6 | The HUD follows §9. The background is saturated, not 30% desaturated, and saturated cyan competes with the flies. The Professor and flies are tiny, and the key is shown as the text "KEY". |
| VFX & feedback | n/a | No VFX delivered. |
| Animation | n/a | No animation previews delivered. |
| Overall style match | 5 | The assets are on-style, but the composed scene looks unfinished (flat plane, flat glass, dark slab) and the main menu is a bare navy screen. |

## 3 worst defects

1. **Materials & palette**, `shots/SCN_HydroStation_Gameplay_key_carried_01.png` (also in zone_complete_01, crab_defeated_01 and start_01): a large untextured, unlit dark navy rectangle closes the end of the tunnel and reads as a missing-material or placeholder slab. *Reference:* §8 asks for hand-painted, kit-based environments, with amber reserved for the key and door.
2. **Camera & composition**, `shots/SCN_HydroStation_Gameplay_zone_complete_01.png`: the camera is yawed off-axis and the Professor is about 8-10 m away. Other shots are high and distant or nearly top-down. This contradicts numbers.json (offset (0, 3.2, -6.5), FOV 50). *Reference:* TDD §11.5, a Crash 3-style authored follow at a constant offset, look target +1 m.
3. **Gameplay readability**, `shots/SCN_HydroStation_Gameplay_start_01.png`: the background is a saturated cyan jellyfish-tank panorama with a saturated blue jellyfish structure, and the ground/water and glass are flat blue fills. *Reference:* §8 Readability, which asks for a 30% desaturated background with cyan reserved for flies and Sprat.

## Verdict

**below_threshold**: every scored area is below 7.
