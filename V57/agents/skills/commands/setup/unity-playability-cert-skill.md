# Unity Playability Cert Skill (`/playability-cert`)

**Certification from evidence.** Decides whether the scene is playable using files produced by tools and by the independent reviewer — never by the implementing agent's own judgement. Used at **M4** by `/vertical-slice` and `/game-setup`; `/prototype` uses the prototype mode (smoke only).

| Item | Detail |
|------|--------|
| **Evidence (required)** | 1. latest gold path `Docs/V57/evidence/goldpath/<UTC>/checks.json` on the current commit; 2. independent review `review.json` (`/independent-review --final`) for screenshots |
| **Numbers** | console log of the gold path run, `assembly-report.json`, runtime lint JSON, hierarchy diff |
| **No self-PASS** | The agent collects and cross-checks evidence; it does not assign visual verdicts |
| **Preflight** | `/compile-heal` green + `/editor-smooth --before-play` |
| **Task subagent** | Required when invoked from a pipeline |

---

## Invocation

```bash
/playability-cert --scene Assets/_Game/Scenes/SCN_<Level>.unity                   # M4 (mode game-setup)
/playability-cert --scene Assets/Prototypes/Scenes/Spike.unity --mode prototype   # prototype smoke
```

---

## Gates (G-FUNC)

| Gate | Evidence | PASS when |
|------|----------|-----------|
| **G-FUNC-01 Boot + loop** | gold path `checks.json` | all checks pass on the current commit; 0 errors/exceptions in the run log; Build Settings index 0 = gold path scene |
| **G-FUNC-02 Rendering** | `review.json` + `numbers.json` | reviewer: no area ≤ 3 (black/pink/missing); 0 pink materials in read-back; exposure numbers inside ADD targets |
| **G-FUNC-03 End states** | gold path steps | the gold path reaches win/lose/exit (TDD §3); boot-only paths FAIL |
| **G-FUNC-04 Visible content** | read-back + screenshots | every run-scope hazard/interactable/pickup has an enabled renderer from a delivered Visual prefab; pieces whose art is missing are collider-only **and** listed in `Docs/V57/MISSING_ASSETS.md` (reported, not a V57 placeholder) |
| **G-FUNC-05 Tests** | `unity test` XML | EditMode + PlayMode green; ≥1 PlayMode test loads the gold path scene; no banned tricks (lint JSON) |
| **G-FUNC-06 UI** | read-back + `review.json` UI area | every in-scope `ui.json` screen: `UIDocument.panelSettings` and `visualTreeAsset` non-null; reviewer UI score ≥ threshold; uGUI only if TDD names it; IMGUI debug stubs FAIL |
| **G-FUNC-07 Camera** | `numbers.json` vs `camera.json` | camera values within tolerance of `camera.json`; smoothing channels measured when §11.5 declares them; `n/a` otherwise |

A gate with missing or stale evidence (older than the current commit) is **FAIL**, not WARN. WARN does not exist.

---

## Workflow

1. Confirm the gold path ran on `HEAD` (compare `STATUS.goldpath.last_run_utc` with the last commit time); if not, run `/gold-path --run`.
2. Confirm `/independent-review --final` ran on `HEAD`; if not, request it (the reviewer writes `review.json`).
3. Read-back via CLI `eval`: Build Settings, `UIDocument` fields, renderer presence, pink materials.
4. Cross-check: a passing check whose checkpoint screenshot the reviewer flagged as broken → FAIL G-FUNC-01 (probe bug) and re-run after fixing the probe.
5. Write the report. Status word per scene: `agent-verified` only when all gates PASS; otherwise `implemented`. Final state after M4 is `pending owner review`.

---

## Prototype mode

`--mode prototype`: G-FUNC-01 via a ≥10 s Play smoke (`unity command editor_play` + end-of-frame screenshot + console), G-FUNC-02/04 read-back only. No reviewer, no gold path required. Not valid for `/vertical-slice` or `/game-setup`.

---

## Report

`Docs/V57/reports/M4-playability-cert-<slug>.md`:

```markdown
# Playability cert — <slug>
- **Result:** all gates pass | failing: G-FUNC-xx
- **Commit:** <sha>
- **Gold path:** Docs/V57/evidence/goldpath/<UTC>/checks.json (N/N)
- **Independent review:** Docs/V57/reports/M4-review-<slug>.md (min area score: x)
- **G-FUNC-01..07:** table (gate, evidence path, result)
- **UIDocument read-back:** table
- **Status word:** agent-verified | implemented  → pending owner review
```

---

*Last updated: 2026-09-28*
