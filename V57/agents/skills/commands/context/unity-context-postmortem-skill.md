# Unity Context Post-Mortem Skill (`/context-postmortem`)

Single **post-run** entry after `/vertical-slice`, `/game-setup` or `/prototype`. It does three things, in order:

1. **Lessons** — write what this run taught into `V57/knowledge/` (the pack), so the next game does not repeat it.
2. **Readiness** — Context Intelligence Phase 2 check and branch (index / drift).
3. **Graph** — always generate the interactive HTML graph.

| Item | Detail |
|------|--------|
| **Invocation** | `/context-postmortem` · `/context-postmortem --force` · `/context-postmortem --no-open` · `/context-postmortem --lessons-only` |
| **When** | After M4 (or after a stopped run), or anytime |
| **Inputs** | `Docs/V57/{DECISIONS.md, DEVLOG.md, STATUS.json, INTAKE_REPORT.md}`, `Docs/V57/reports/*`, `review.json` files, revert stashes listed in DECISIONS |
| **Outputs** | `V57/knowledge/<topic>.md` edits; readiness report; graph viewer HTML |
| **Guide** | `V57/docs/context/CONTEXT_INDEX.md` |
| **Not for** | Initial CONTEXT bootstrap (that is `/tdd-to-context --context Docs/V57/CONTEXT.md` at M1) |

---

## 1. Lessons → `V57/knowledge/`

Harvest from the run's evidence, not from memory:

| Source | Look for |
|---|---|
| `DECISIONS.md` | decisions that fixed a recurring problem; reverts; cuts; timeboxes hit |
| `INTAKE_REPORT.md` | `fixable` issues that had to be absorbed (import rules / mapping); new provider mistakes |
| `assembly-report.json` | import rules added or missing |
| `review.json` (all rounds) | defects that repeated across rounds |
| gold path runs | probe bugs, flaky input simulation, CLI races |
| DEVLOG | stuck > 45 min entries |

Rules:
1. **One topic per file**, existing topics first: `asset-intake.md`, `camera.md`, `verification.md`, `runtime-bootstrap.md`, `unity-cli.md`, `process.md`. New file only for a genuinely new topic (kebab-case name), and add it to `V57/knowledge/README.md`.
2. Each lesson = **symptom → cause → rule → how it is enforced** (tool, lint rule, gate, or skill line), with the source (`<slug>`, report path or `D-###`).
3. Concrete and short (≤ 6 lines per lesson). No game-specific names in the rule itself — the game goes in the source line.
4. Merge with an existing lesson when it is the same cause; do not duplicate.
5. If a lesson needs a skill or tool change to be enforced, add a line to the game's `Docs/V57/TODO.md` under "Pack follow-ups" (the pack is changed in its own repo, not from inside a game run).
6. Keep each knowledge file under ~4 KB; condense older lessons instead of appending forever.

---

## 2. Readiness → Phase 2 work

```powershell
./V57/tools/check-context-readiness.ps1 -WriteReport
```

| Status | Phase 2 work |
|--------|--------------|
| **Green** | Light index attempt (or minimal fallback index). No CONTEXT auto-patch. |
| **Yellow** | Drift / touches alignment: `./V57/tools/index-project.ps1 -Drift -SkipUnityCheck` |
| **Red** (or `--force`) | `./V57/tools/index-project.ps1 -ProposeContext -Drift -SkipUnityCheck` (+ Unity asset export when the Editor is available). Show `context-auto-patch.md`; merge into `Docs/V57/CONTEXT.md` only on owner `yes` |

---

## 3. Graph (always, last)

```powershell
./V57/tools/export-context-graph.ps1 -SkipIndex -NoOpen -SkipUnityCheck
```

If `project-index.json` is missing or empty, build a minimal fallback index from `.cs` under `Assets/_Game/Scripts` (plus legacy `Assets/Prototypes`, `Assets/VerticalSlice`, `Assets/_Game/Scripts` when present), then export.

| Output | Path |
|--------|------|
| Viewer | `Docs/V57/reports/context-graph-viewer/index.html` (pack-only runs: `Docs/V57/reports/context-graph-viewer/`) |
| Sidecar URL | `GET http://127.0.0.1:3857/api/context/graph.html` |

---

## Progress output

```text
## Context post-mortem
**Lessons:** N added, M merged → V57/knowledge/{files}
**Readiness:** Green | Yellow | Red
**Phase 2 work:** none | drift-only | full (index+graph)
**Graph HTML:** path + sidecar URL
**CONTEXT patch:** none | awaiting yes
**Pack follow-ups:** N (Docs/V57/TODO.md)
```

---

## Rules

1. Lessons first; readiness second; graph always last.
2. Never block `/vertical-slice`, `/game-setup` or `/prototype`.
3. Never auto-merge `context-auto-patch.md`.
4. English only.
5. Do not auto-run `/context-query` / `/context-path`.

---

*Last updated: 2026-09-28*
