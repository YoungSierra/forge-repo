# Production game feel standards (V57)

**Scope:** `/game-setup` Stage **G-FEEL**, `/game-feel`, and locomotion/HUD specs under `Assets/_Game/Scripts/`.

Structural SDD gates (compile, OVR, playability-cert) are necessary but not sufficient when the TDD quantifies camera motion, HUD presentation, or atmosphere.

**Genre-agnostic:** never assume FPS, platformer, or horror. Read **TDD §11.5** (and locomotion §B blocks) to pick the camera profile, then run **only** the checks that profile declares.

---

## Core principle

**Functional ≠ polished.** Passing compile and playability with bare colliders and **instant** camera/HUD/atmosphere snaps is **FAIL** when the TDD quantifies smoothed presentation for that channel.

Prefer **lerped / smoothed** values every frame over one-shot assignment on input edges.

If the TDD is silent on a channel, mark **n/a** in the report — do **not** fail for missing FPS lean on a follow-cam jumper, or missing follow damp on a declared first-person lean game.

---

## Step 0 — Pick camera profile from §11.5

Read **§11.5 In-play camera** + locomotion mechanic rules. Classify (multiple may apply):

| Profile | TDD signals | Primary smooth channels |
|---------|-------------|-------------------------|
| **FPS / first-person look** | Look action, lean, crouch, sprint FOV, headbob, eye height | Lean roll/offset, pitch, bob, FOV |
| **Follow / side / vertical** | Follow camera, smooth damp, ortho size, kill line, bank roll on steer | Camera Y/X follow, ortho bounds, optional roll |
| **Fixed / cinematic** | Fixed camera, orbit cut, no player Look | Transitions only if TDD quantifies |
| **2D / ortho side** | Side-scroll, ortho follow on X/Y | Follow offset, bounds clamp |
| **Auto / none** | Control mode `auto` or `none` | Camera motion n/a unless TDD still quantifies shake/zoom |

All numeric literals come **only** from the active TDD — never hard-code title- or genre-specific defaults.

---

## Camera & locomotion (apply declared profile only)

### FPS / first-person (when TDD declares Lean / Look / Crouch / Sprint / headbob)

| Channel | Rule | Anti-pattern (FAIL) |
|---------|------|---------------------|
| **Lean roll** | `Mathf.Lerp(current, targetRoll, Time.deltaTime * rate)` — rate from TDD or ~8/s | Instant roll on Q/E |
| **Lean lateral offset** | Lerp pivot X toward TDD offset | Snap X on key down |
| **Eye height (crouch)** | Lerp pivot Y toward crouch/stand eye height | Teleport Y on toggle |
| **Headbob** | Sin phase × TDD amplitudes × speed factor | Static camera when bob declared |
| **FOV (sprint)** | Lerp `fieldOfView` toward TDD stand/sprint values | Instant FOV swap |
| **Look** | Mouse delta × TDD sensitivity; pitch clamp per TDD | — |

Typical hierarchy: `Player` → `CameraPivot` (bob/lean/pitch) → `Camera` (FOV). Tag `MainCamera`.

### Follow / vertical / side (when TDD declares follow, smooth damp, ortho, bank roll)

| Channel | Rule | Anti-pattern (FAIL) |
|---------|------|---------------------|
| **Follow position** | `SmoothDamp` or lerp toward target at TDD rate (e.g. `0.12 s` on Y) | Camera teleports to player each frame |
| **Max scroll / drop speed** | Respect TDD cap (e.g. camera does not chase fall faster than `2 m/s`) | Runaway chase or frozen camera |
| **Ortho / framing** | `orthographicSize` or distance matches TDD; subject stays in frame | Wrong scale / lost subject |
| **Bank / tilt on steer** | Lerp roll Z toward `−steer × TDD°` when TDD declares bank | Instant roll snap on steer |
| **Kill / bounds line** | Lose line offset per TDD when declared | — |

**Do not** require Q/E lean or sprint FOV when §11.5 does **not** declare them.

### Fixed / auto

Run feel checks only for channels the TDD quantifies (shake amplitude, blend duration, letterbox fade, etc.). Otherwise **n/a**.

---

## HUD / UI Toolkit (when TDD §9 lists screens)

| Requirement | Detail |
|-------------|--------|
| **PanelSettings** | Assigned on every `UIDocument`; MCP read-back non-null |
| **State / mood rim** | Opacity driven by TDD §9 metrics — not a debug label alone |
| **Meters** | Track + fill; show/hide with state per TDD |
| **Interact prompt** | Copy + optional hold progress when TDD declares interact UI |
| **Overlays** | Boot, win, lose, pause per TDD §9; fade with timed opacity |
| **Flash feedback** | Pickup/toast messages with timed fade when in registry |

Use UXML + USS under `Assets/UI/`. See `unity-ui-uitk-skill.md`.

---

## Atmosphere & post-processing (URP)

When TDD names fog, grade, vignette, grain, hum, or palette:

1. Director script owns Volume profile + fog — **lerp** intensities on state changes.
2. Run **`/urp-postprocessing`** or **`/lighting-setup`** — verify HDR, `renderPostProcessing`, volume layer mask.
3. Grain / vignette visible in Game View smoke when TDD declares them.

**TDD-driven tone only** — §1, §8, §9, atmosphere appendix, §11.5. Silent channel → note gap; do not invent genre defaults.

---

## Juice without VFX Graph

When TDD or feedback registry mentions pulse, flash, scale pop, or telegraph:

- Code-driven **scale/opacity/emission flash** on primitives is required.
- Prefer matte URP material pulses over particles for graybox production.

---

## Verification (G-FEEL)

Run **only rows that match the §11.5 profile**. Mark others **n/a**.

| Gate | When (§11.5 / TDD) | Check |
|------|-------------------|--------|
| **G-FEEL-CAM-01** | Lean / first-person look declared | Lean roll + lateral offset ease ~0.1–0.2 s — not instant snap |
| **G-FEEL-CAM-02** | Sprint FOV or headbob declared | FOV and/or bob amplitude change smoothly — not instant |
| **G-FEEL-CAM-03** | Follow / smooth damp / ortho follow declared | Camera follow eases at TDD rate; respects max drop/chase speed if quantified |
| **G-FEEL-CAM-04** | Bank roll / steer tilt declared | Roll eases toward TDD target — not instant on steer |
| **G-FEEL-HUD-01** | §9 HUD in run scope | Rim/meter opacity and overlays fade per TDD metrics |
| **G-FEEL-ATM-01** | Atmosphere / fog / grade in TDD | Volume/fog tweaks visible across TDD-defined state bands |

**Overall PASS** when every **applicable** row is PASS and no instant-snap anti-patterns on declared channels.

Document profile + results in `Docs/V57/reports/game-feel-report-{slug}.md`.

---

*Last updated: 2026-08-26*
