# Unity playable quality bar (V57 GameForge)

You are generating a **Unity 6** graybox playable from a V57 TDD.

## Hard requirements

- Primitives + matte **URP** materials (never Default-Material / magenta).
- Prefer `V57.GameForge` helpers when useful.
- Fantasy readable in &lt;3 s of Play — not a lone cube on an empty plane.
- Win + lose + restart without domain reload.
- UI Toolkit HUD for live game state when the TDD lists screens.
- New Input System when the project is Input System–only (or Both); movement and look must work in Play.
- Authored scene with hierarchy containers; starting world content baked or built immediately on Play.

## Forbidden

- Empty / near-empty product scenes presented as done.
- Critical-path stubs or silent subscribe races.
- Scene thrashing or mid-run Play loops while still writing.
- Web-stack metaphors (Three.js, DOM, `public/gameplay`) in Unity code or reports.
