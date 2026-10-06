# Unity Independent Review Skill (`/independent-review`) — gate M3 and final

A **fresh-context reviewer** judges how the game looks and reads against the provider's references. The implementing agent prepares the evidence pack and receives the verdict — it **never writes, edits or overrides the verdict**.

| Item | Detail |
|------|--------|
| **Used at** | M3 CRAFT gate (after crafts), M4 final (visual part of acceptance) |
| **Reviewer** | A new session with no prior context: IDE Task subagent, or a separate provider session started by the GameForge sidecar (`Agent.create` with only the pack) |
| **Input (only)** | Screenshots + numbers + `Docs/ArtDirection/Reference/` + `UIMockups/` + `Camera/` + ADD style sections |
| **Output** | `Docs/V57/evidence/review/<UTC>/review.json` (the runner gate reads it there; must carry `reviewer` and `areas[]`) + `Docs/V57/reports/M3-review-<slug>.md` (M4: `M4-review-<slug>.md`), written by the reviewer |
| **Threshold** | `agents.yaml → gates.independent_review.min_score` (default **7**), every scored area |
| **Rounds** | max `gates.independent_review.max_rounds` (default **3**), fresh reviewer each round |

---

## Invocation

```bash
/independent-review            # M3
/independent-review --final    # M4
```

---

## 1. Build the evidence pack (implementing agent)

Folder: `Docs/V57/evidence/M3/<UTC>/review-pack/` (M4: `evidence/M4/...`). Fresh captures only.

| File | Content |
|---|---|
| `shots/<scene>_<view>_<n>.png` | Play Mode, end-of-frame Game View at `package.platform.reference_resolution` (UI Toolkit overlay included). Minimum: gold path checkpoints + one per `camera.json` view + one per `ui.json` screen state in slice |
| `numbers.json` | measured values: camera (type, fov/size, angle, distance) vs `camera.json`; avg/min FPS, draw calls, tris vs `rendering.budgets`; exposure/average luminance per shot; palette distance of dominant colors vs `rendering.palette`; gold path summary (`checks.json` pass count) |
| `style.md` | copied verbatim from ADD: `style_guide`, `color_palette`, `visual_targets` (+ `rendering.json` palette); no commentary from the implementer |
| `refs/` | copies (or path list) of `Docs/ArtDirection/Reference/*`, `UIMockups/*`, `Camera/*` |
| `areas.json` | list of areas to score (below) with the matching reference files |

The pack must **not** contain: implementer opinions, previous verdicts, code, DEVLOG, or "known issues" framing.

---

## 2. Areas (score 1–10 each)

| Area | Compared against |
|---|---|
| Camera & composition | `Camera/<ViewId>.png`, `camera.json`, TDD §11.5 |
| Lighting & exposure | Reference/, `visual_targets` |
| Materials & palette | `color_palette`, Reference/ |
| UI vs mockups | `UIMockups/<ScreenId>.png`, `ui.json` layouts |
| Gameplay readability | can a player see the ball/player/targets/hazards and their state? |
| VFX & feedback | `VFXReference/`, `visual_targets` (skip if none in slice) |
| Animation | `AnimationPreviews/` (skip if no animated actors in slice) |
| Overall style match | `style_guide`, Reference/ |

Skipped areas are listed as `n/a` with the reason; they are not scored.

---

## 3. Reviewer instructions (sent verbatim with the pack)

```text
You are an independent art/UX reviewer. You did not build this game. Use only the files in this pack.
For each area in areas.json: compare the screenshots and numbers with the references and style text.
Score 1–10 (10 = indistinguishable in intent from the references; 7 = clearly on-style with minor issues;
4 = recognisable but wrong in important ways; 1 = broken/black/pink/missing).
List the 3 worst defects overall, each with: screenshot file, area, what is wrong, what the reference shows.
Numbers that contradict the images (e.g. "camera matches" but the framing differs) are defects.
Do not give credit for intent, effort, or explanations. Output review.json exactly in the schema given.
```

`review.json` schema:

```json
{ "round": 1, "reviewer": "fresh-session-id", "min_score": 7,
  "areas": [ { "area": "Camera & composition", "score": 6, "evidence": ["shots/SCN_Main_top_01.png"], "notes": "..." } ],
  "worst_defects": [ { "rank": 1, "area": "UI vs mockups", "shot": "shots/...", "problem": "...", "reference": "UIMockups/HUD.png" } ],
  "verdict": "meets_threshold | below_threshold" }
```

---

## 4. Gate and loop

1. Gate passes when every scored area ≥ `min_score` **and** gold path is green on the same commit.
2. Below threshold → the implementing agent fixes the 3 worst defects (owning craft skill), re-captures a **new** pack, starts a **new** reviewer (never the same session).
3. After `max_rounds`: log `D-###` (areas still below, defects), set M3 `failed-timeboxed`, continue to M4; defects are listed as open in the final report. Status word for affected items stays `implemented`.
4. The implementing agent may add a `response.md` next to the report (what was changed) but may not edit `review.json` or the reviewer's report.

---

## 5. Prohibited

- Implementing agent scoring its own screenshots, or summarizing the verdict more favourably than `review.json`
- Reusing a reviewer session across rounds; passing prior verdicts into a new round
- Screenshots from Scene view, camera-only render textures (miss UI Toolkit), or Edit Mode
- Cropping/choosing shots that hide defects; all gold path checkpoints must be included
- Changing references, ADD text, thresholds or areas to pass

---

*Last updated: 2026-09-28*
