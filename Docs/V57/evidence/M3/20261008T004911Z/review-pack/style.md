# Style (verbatim from Docs/Design/TDD.md — no Art Direction Document was delivered)

# 8 · Art Direction & Visual Style  `[REQUIRED]`

- **Style.** Hand-painted stylized 3D on a closed palette (`#FF8C00` Station Amber, `#0A1F3C` Deep Ocean Blue, `#4A3728` Rust Brown, `#00E5FF` Accent Cyan, `#FFD600` Caution Yellow, `#FFFFFF` Lab White). Near-zero specular; flat planar water; panoramic sky.
- **Readability.** Characters read over a `30 %` desaturated background; cyan reserved for flies and Sprat; amber reserved for the key and door; landing surfaces lighter on top with hazard-striped leading edges; the drop shadow is always visible under the Professor.
- **Scope coherence.** Locked palette, kit-based environments, a white-listed post stack (neutral tonemapping, colour grading only — no bloom, DOF, motion blur, SSR, volumetrics, upscalers) and in-place character clips keep art cost bounded.

> Per-asset briefs, texture sizes and file lists belong to the ADD / art delivery.

# 9 · UI / UX  `[REQUIRED]`

- **Principle.** The world speaks: the only gameplay HUD is the fly counter `N / T` (plus the key glyph while carried). No arrows, minimap or quest list; nothing appears during the first `90 s` beyond the counter.
- **Accessibility.** `[RECOMMENDED]` Keyboard + gamepad with focus states and remapping through Input System action maps; colour never carries meaning alone (shape + sound + animation on every signal); counter legible at `16 px` @1080p; subtitles `N/A` (non-verbal VO).

## TDD §11.5 In-play camera row

| **In-play camera** | `[REQUIRED]` Owner **`CameraRig`**. Crash Bandicoot 3 style authored follow: Cinemachine 3 camera following the Professor with a world-space offset `(0, 3.2, −6.5) m` rotated by the camera-zone yaw, look target `+1.0 m` above the Professor, damping `0.3 s`, look-ahead `0.25 s`, FOV `50°`. Camera zones (trigger volumes, level content) change yaw/offset with a `0.8 s` ease-in-out blend; the new input basis applies when the stick drops below `0.2` or the blend ends. Key shot on `OnKeySpawned` (§B KeyMaterialisation). Cut (no blend) on `OnPlayerRespawned`. **Look: none (authored).** |
