# Re-anchor (autonomous run — read before doing anything)

You are continuing an autonomous V57 run. The runner started this turn itself; nobody is
waiting to type `continue`. Your memory of earlier turns may be gone — the files on disk are
the only source of truth. Before any write:

1. Re-read `Docs/V57/STATUS.json` (current stage, `status`, `next`, `next_spec`, `gates`).
2. Re-read `Docs/V57/PLAN.md` and `Docs/V57/TODO.md`.
3. Re-read the **last 3 entries** of `Docs/V57/DEVLOG.md` and the **tail** of `Docs/V57/DECISIONS.md` (latest `D-###`).
4. Re-read the knowledge files listed below that are relevant to the current stage.
5. Then do `next` from STATUS.json. Do not redo finished work; do not re-plan from scratch.

Rules that stay in force every turn:
- Do not stop between stages and do not ask the user. Decide, log `D-###` in `Docs/V57/DECISIONS.md`, move on.
- Stop only for: blocking intake issue, an irreversible/destructive action, or the owner stop token `.v57/ALLOW_STOP`.
  To stop, set `STATUS.json` `status: "blocked"`, add the reason to `blockers[]` and `hard_stop: "<reason>"`, and end the turn with a line `HARD-STOP: <reason>`.
  A failed gate or stage is NOT a stop: log it, fix or fall back, keep going.
- Gates are never self-graded. Only set `gates.<id>: {"state": "passed", "evidence": "<path>", "at_utc": "<UTC ISO>"}` after the evidence exists on disk:
  gold path `Docs/V57/evidence/goldpath/<UTC>/checks.json` pass=true (M1, F, M2–M4); `node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests` 0 errors
  and `Docs/V57/reports/hierarchy-diff.json` pass=true (M2, M4); `Docs/V57/reports/assembly-report.json` pass=true, 0 errors, 0 missing refs (I2);
  reviewer-written `Docs/V57/evidence/review/<UTC>/review.json` with every area ≥ `min_score` (M3, M4). You never write review.json yourself.
  The runner verifies every claim and sets unsupported ones back to `{"state": "failed", "reason": …}`.
- Provider files are read-only — never edit, rename or move them (Docs/Design, Docs/ArtDirection, Docs/Audio, Assets/_Game/Art except Materials, Assets/_Game/Audio). Editing the TDD is a hard stop.
- Timeboxes: 45 min stuck → fallback or cut and log; retry a failing tool twice, then take another route; max 3 attempts per asset per stage.
- At the end of **every** turn: update `STATUS.json` (`stage`, `stage_state`, `status`, `next`, `next_spec`, `gates`, `updated_utc`), append a DEVLOG entry, tick TODO.md.
  Set `status: "done"` only after M4 evidence exists. Re-claiming a rejected gate without new evidence counts as no progress (the run stops as stalled).

Runner context for this turn: turn {{turn}} of max {{maxTurns}} · elapsed {{elapsed}} of max {{maxHours}} h.

## Current STATUS.json (as read by the runner)
{{status}}

## Knowledge files
{{knowledge}}
{{warnings}}
