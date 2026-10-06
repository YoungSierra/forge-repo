# Playable loop (TDD-driven — no genre catalog)

**Never** classify the game into a fixed genre bucket (kart, collector, arena, generic, etc.).  
Read the **active TDD only** — §1 fantasy, §B mechanics, §9 feedback/UI, §D dependencies — and implement the loop the TDD describes.

## Universal (every playable)

- Full loop: start → core verb(s) from §B → win **or** lose / round end → restart without domain reload.
- Live HUD for every metric the TDD §9 or feedback registry declares — not a generic placeholder.
- Graybox only unless TDD names imported art: boxes / capsules / cylinders / spheres / planes. Matte **URP** colors (no magenta).
- Power-ups / items: **primitive mesh + short label** when TDD lists them — do not invent remote image URLs for the core loop.
- Prefer juice on matte materials (flash, scale pop, brief shake) over new asset pipelines unless TDD requires more.

## Derive loop contracts from §B (mandatory)

For **each** mechanic in run scope:

1. Read its rules, I/O, state machine, and acceptance criteria in §B.
2. Wire HUD elements from §9 that that mechanic consumes.
3. Implement win/lose/restart paths exactly as the TDD states — if silent, infer from §1 fantasy + closest mechanic goals, **document the inference in the implement report**, do not fall back to a template genre.

## Self-check before you stop writing

- [ ] Core verb readable in first seconds?
- [ ] Win **or** lose reachable in normal play per §B?
- [ ] Restart works in Play Mode?
- [ ] HUD numbers move when state changes (§9 metrics)?
- [ ] Materials are URP Lit/Simple Lit (no pink)?

## Game feel (presentation)

Mechanical polish (lerp, HUD motion, atmosphere) follows `GAME_FEEL_STANDARDS.md` — **branch on §11.5 camera profile** (FPS lean, follow smooth damp, bank roll, etc.); never require FPS checks on follow-cam games.
