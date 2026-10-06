# Unity Vertical Slice Skill (`/vertical-slice`) — stage driver

The **autonomous stage driver** that turns a provider-delivered repository into a playable vertical slice (VS). It runs **I0 → M4** in one go, decides and logs instead of asking, and stops only on the conditions in §7. `/game-setup` (Production) uses this same driver with a wider scope (see `unity-game-setup-skill.md`).

| Item | Detail |
|------|--------|
| **Input** | Provider repo (TDD `Docs/Design/TDD.md`, ADD `Docs/ArtDirection/ArtDirectionDocument.md`, art/audio under `Assets/_Game/Art`, `Assets/_Game/Audio`) |
| **Branch** | Work directly in the provider repo on branch `v57/setup` (never on the provider's default branch) |
| **Authority** | TDD wins over ADD; ADD fills what TDD leaves open; `Docs/Generated/*` is derived (never hand-edited) |
| **Engine** | Pinned by V57 (template `ProjectVersion` **6000.6.2f1**); TDD engine value is informational |
| **Write roots** | `Assets/_Game/{Prefabs,Scenes,Scripts,Data,Settings,Tests}`, `Assets/_Game/Art/**/Materials/`, `Docs/V57/**`, `Docs/Generated/**` (intake only), `V57/specs/<slug>/`, `Packages/manifest.json`, `ProjectSettings/` |
| **Never write** | `Docs/Design/**`, `Docs/ArtDirection/**`, provider files in `Assets/_Game/Art/**` and `Assets/_Game/Audio/**` — never renamed, moved or edited; `fixable` issues are absorbed (see `/intake`); `Source/` is never written |
| **Scope (VS)** | Scenes with `slice: true` in `Docs/Generated/json/scenes.json` + mechanics whose ACs they cover + their §D dependency closure |
| **Knowledge** | Read `V57/knowledge/*.md` once per session before I1 (short, one topic each) |

---

## Invocation

```bash
/vertical-slice                 # start or resume; autonomous chaining I0 → M4
/vertical-slice --from M2       # resume at a stage (earlier gates must be recorded as passed in STATUS.json)
/vertical-slice --staged        # opt-in manual mode: stop after each stage (owner debugging only)
/vertical-slice --status        # print STATUS.json summary + next action; no writes
```

There is **no `continue`** between stages in the default mode.

---

## 0. Session start / resume protocol (every session, every resume)

1. `git status` — on `v57/setup`, working tree clean (commit leftovers with a DEVLOG note).
2. If `.v57/ALLOW_STOP` exists → finish nothing new; write DEVLOG entry, commit, stop (§7).
3. Read, in order: `Docs/V57/STATUS.json`, `Docs/V57/PLAN.md`, `Docs/V57/TODO.md`, the last 3 entries of `Docs/V57/DEVLOG.md`, the last 10 `D-###` in `Docs/V57/DECISIONS.md`. Missing → copy templates from `V57/templates/game-state/`.
4. Read `V57/knowledge/*.md` (first session only, or when STATUS says a lesson was added).
5. Resume at `STATUS.stage`; re-run that stage's gate before new work if the previous session ended mid-stage.

---

## 1. Stage table

| Stage | Skill | What | Gate (never self-graded) | Evidence | Default timebox |
|---|---|---|---|---|---|
| **I0 INTAKE** | `/intake` | `node V57/tools/intake/index.js --repo .` → `Docs/Generated/` + `Docs/V57/INTAKE_REPORT.md` + `Docs/V57/MISSING_ASSETS.md` | **A0:** exit code 0 (2 = blocking → stop) | INTAKE_REPORT | 15 min |
| **I1 PROJECT** | `/intake` | Open with Unity 6000.6.2f1, baseline settings, add `com.v57.assembly`, 0 console errors | script check (see `/intake` I1) | `Docs/V57/reports/I1-project-<slug>.md` | 30 min |
| **I2 ASSEMBLY** | `/intake` | `unity command eval "V57.Assembly.AssemblyRunner.RunAll()"` | `Docs/V57/reports/assembly-report.json`: 0 missing refs, 0 errors | assembly-report.json | 45 min |
| **M1 GREYBOX GOLD PATH** | `/gold-path` | Plan + specs + core-loop gameplay on level scenes (Visual prefabs; missing art left empty and listed in MISSING_ASSETS.md) | `checks.json` all pass, 0 errors | `Docs/V57/evidence/goldpath/<UTC>/` | 4 h |
| **F SPECS** | `/spec` + `/compile-heal` + `/gold-path` | One §C spec at a time, real tests | per spec: EditMode + PlayMode green **and** gold path still green | `Docs/V57/reports/F-<spec>-<slug>.md` | 90 min / spec |
| **M2 SCENE = GAME** | `/scene-setup`, `/prefab`, `/game-ui`, `/audio-setup`, `/vfx-setup`, `/input-setup`, `/level-design` | Gameplay prefab variants, UI from mockups, audio/VFX wiring | runtime lint clean, Edit vs Play hierarchy diff clean, collision-with-mesh check, gold path green | `Docs/V57/reports/M2-*.md`, lint JSON, diff | 4 h |
| **M3 CRAFT** | camera/lighting/post/vfx/audio/game-feel crafts + `/independent-review` | Polish vs STYLE (ADD) | independent reviewer: every area ≥ `min_score`; gold path green | `Docs/V57/reports/M3-review-<slug>.md` + `review.json` | 4 h |
| **M4 ACCEPTANCE** | `/playability-cert`, `/qa` | All P0 acceptance, final report (no player build unless the owner asks) | playability cert + QA from evidence; status words only (§8) | `Docs/V57/reports/M4-final-<slug>.md` | 2 h |

Timebox values are defaults; the source of truth is `agents.yaml → pipeline.timeboxes`. From **M1 onward the game must stay playable**: every commit after M1 must keep the gold path green.

---

## 2. Stage details

### I0 – I2
Run `/intake` (`commands/setup/unity-intake-skill.md`). It owns intake CLI, baseline, assembly and the absorption policy.

### M1 — plan, specs, greybox gold path
1. **Plan:** write `Docs/V57/PLAN.md` from `package.json` (slice scenes, gold path), `mechanics.json`, §D order. Fill `TODO.md` (one line per spec + per M2/M3 area).
2. **Context:** `/tdd-to-context Docs/Design/TDD.md --bootstrap --context Docs/V57/CONTEXT.md` → **`Docs/V57/CONTEXT.md`** (never a root `CONTEXT.md`).
3. **Specs:** adopt each §C companion spec (`mechanics.json → spec_yaml`) into `V57/specs/<slug>/{features,systems}/<specId>.yaml` via `/tdd-to-spec Docs/Design/TDD.md --all --out-dir V57/specs/<slug>/features --map "MECH_<SystemId>:system"` (systems land in `V57/specs/<slug>/systems/`). Specs are V57-owned; the TDD is not.
4. **Greybox core loop:** minimal gameplay code for the §3 core loop in `Assets/_Game/Scripts/<Area>/` on the I2 level scenes, using Visual prefabs; a missing mesh leaves the slot empty (functional collider/behaviour only) and is listed in `Docs/V57/MISSING_ASSETS.md` — never a placeholder. Every collider sits on the prefab of the mesh it represents (`V57/knowledge/assets-collision-builds.md`). Camera values come from `camera.json` — locked here, not re-decided later (`V57/knowledge/camera.md`).
5. `/gold-path` → author steps, implement `IGoldPathProbe`, run `GoldPathDriver`. Gate = `checks.json` all pass.
6. Commit `V57 M1: gold path green` → `STATUS.last_green_commit`.

### F — specs (queue from PLAN.md, §D order)
Per spec: `/spec "implement @V57/specs/<slug>/…/<spec>.yaml"` → `/compile-heal` → EditMode + PlayMode tests via `unity test` → `/gold-path --run`. PlayMode acceptance tests use real physics and real input (see §9). Green → commit `V57 F:<spec>`; TODO/STATUS updated.

### M2 — scene = game
1. Gameplay prefabs: `/prefab variant` from each `PRF_<Asset>_Visual` → `Assets/_Game/Prefabs/Gameplay/PRF_<Asset>.prefab`, including static kit pieces that collide (rails, walls, deflectors): the collider lives on that prefab, next to its mesh; replace greybox instances. No detached collider objects.
2. UI from `Docs/ArtDirection/UIMockups/<ScreenId>.png` + `ui.json` (UI Toolkit; sprites from `Assets/_Game/Art/UI/Sprites/<ScreenId>/`).
3. Audio (`audio.json`) and VFX (`Assets/_Game/Art/VFX/`) wiring.
4. Gates: `node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests` exit 0; `AssemblyRunner.CaptureHierarchyDiff()` → `Docs/V57/reports/hierarchy-diff.json` with `pass: true`; every Collider/Collider2D in the level scene belongs to a prefab instance that also carries the mesh it represents; gold path green. No player build (only on explicit owner request).

### M3 — craft
Route crafts by need (camera, lighting, post, VFX, audio, game feel; see `unity-game-setup-skill.md` craft routing). Each craft ends with fresh evidence (screenshots **with** UI Toolkit overlay + numbers). Then `/independent-review` — the implementing agent never writes the verdict.

### M4 — acceptance
`/playability-cert` (evidence = gold path `checks.json` + independent screenshot review), `/qa` (always enabled at M4), final report (links `Docs/V57/MISSING_ASSETS.md`; no player build unless the owner asked for one) `Docs/V57/reports/M4-final-<slug>.md` with per-item status words (§8), open defects, and the full `D-###` list.

---

## 3. Autonomy rules

1. **No `continue` between stages.** Chain automatically; `--staged` is the only exception.
2. **Decide + log instead of asking.** Any ambiguity → pick the option most consistent with TDD > ADD > V57 defaults, append `D-###` to `Docs/V57/DECISIONS.md` (context, options, choice, reversal cost), continue.
3. **Never edit provider files** (`Docs/Design`, `Docs/ArtDirection`, `Assets/_Game/Art`, `Assets/_Game/Audio`) — no renames, moves or edits; `fixable` issues are absorbed by import rules + manifest mapping (`/intake`, logged `D-###`). **Editing the TDD is a hard stop** — the TDD is never changed to make a gate pass.
4. **Never patch assets through gameplay code** (disabling provider renderers, scaling/rotating in `Awake`, runtime material swaps to hide import issues). Fix at import (import rule / manifest mapping) or in the Visual prefab.
5. **Never self-grade** a subjective gate. Numbers come from tools; visuals come from `/independent-review`.
6. **State on disk, not in chat:** STATUS.json after every gate, DEVLOG entry per work block, TODO ticked as you go.
7. **English only** in all reports, logs and decisions.

---

## 4. Timeboxes and retries

| Rule | Value |
|---|---|
| Stuck on one problem | **45 min** → apply fallback (simpler implementation, cut; never a placeholder asset) and log `D-###` |
| Tool / command failure | retry **twice**, then use an alternate route (CLI ↔ `eval`, batch `-executeMethod` ↔ warm Editor) |
| Per asset per stage | max **3 attempts**; then leave the slot empty, list it in `MISSING_ASSETS.md` + `issues[]` note in the report |
| Per spec | 90 min default; red for **> 20 min** after a previously green state → revert rule (§5) |
| Independent review | max **3 rounds** at M3 (fresh reviewer each round) |

---

## 5. Revert rule

If tests or the gold path are red for more than 20 minutes after a green commit:
1. Record the broken attempt in DEVLOG (files touched, failing check) — do **not** stash.
2. Restore V57-owned game content only:
   `git checkout <STATUS.last_green_commit> -- Assets/_Game ':!Assets/_Game/Art' ':!Assets/_Game/Audio' Packages ProjectSettings`
   (plus `V57/specs/<slug>` if the spec itself was changed). Provider folders and **`Docs/V57/**` are never reverted** — state, decisions and evidence stay append-only.
3. `/compile-heal`, then verify the gold path is green again.
4. Log `D-###` (what broke, what was restored), increment `STATUS.specs[i].attempts`, commit.
5. Retry with a smaller change. After 3 attempts → mark the spec `cut`, log, continue with the queue.

---

## 6. Gate fail handling

| Gate | On FAIL |
|---|---|
| A0 blocking | Stop (§7). Write the blocking list at the top of INTAKE_REPORT. |
| A0 fixable / missing / conflict | Continue: absorb via import rule / manifest mapping (fixable), list in `MISSING_ASSETS.md` and leave empty (missing), authority rule (conflict); all logged. |
| I1 | Fix settings/packages; Safe Mode → fix C# on disk, restart Editor; 2 retries then alternate route (batch mode). |
| I2 | Read `assembly-report.json` issues; fix import rule / manifest mapping; re-run the single `AssemblyRunner` step; 3 attempts per asset, then list it in `MISSING_ASSETS.md` and leave it empty. |
| M1 / F gold path | Read failing check + screenshot; fix gameplay (never the check, never the TDD). Revert rule applies. |
| M2 lint / diff | Move runtime construction into Edit-Mode prefabs/scenes; delete banned calls. Never add lint suppressions. |
| M3 review | Fix the 3 worst defects, new evidence, new fresh reviewer. After 3 rounds: log `D-###`, carry defects to M4 as open. |
| M4 | Fix + re-run the failing check; unresolved items stay open in the final report (status word stays `implemented`). |

---

## 7. Stop conditions (the only ones)

1. **Blocking intake** (`index.js` exit code 2).
2. **Irreversible or destructive action** needed (deleting provider files, force-push, history rewrite, editing the TDD).
3. **Owner stop token** `.v57/ALLOW_STOP` present at a check point (session start, after each gate).

On stop:
1. Commit.
2. Update `Docs/V57/STATUS.json`: `status: "blocked"`, `hard_stop: "<reason>"`, `stage_state: "stopped"`, `blockers[]`, and the current gate as `{ "state": "blocked", "evidence": "<path>", "at_utc": "<UTC>" }`.
3. Print exactly one line `HARD-STOP: <reason>` (the runner keys on it), then the Stop block (§10).
4. Write a DEVLOG entry.

Everything else is decided, logged and continued (`status: "running"`; `status: "done"` only after M4 passed).

**Gate record shape (everywhere):** `gates.<id> = { "state": "pending|running|passed|failed|failed-timeboxed|blocked|n/a", "evidence": "<path or null>", "at_utc": "<UTC or null>" }` (M3 also `review_round`, `min_area_score`). Do not add other keys such as `reason` — the runner rejects `{ "state": "failed", "reason": … }`; put the reason in the evidence file / DEVLOG and the `D-###`.

---

## 8. Status vocabulary

| Word | Meaning | Who sets it |
|---|---|---|
| `implemented` | Code/assets exist; not yet verified by automated evidence | implementing agent |
| `agent-verified` | Automated evidence green (tests, gold path checks, lint, independent review ≥ threshold) | pipeline from evidence files |
| `pending owner review` | Final state after M4 for every delivered item | M4 report |

Never write "accepted", "done", "PASS (visual)" or "complete" for subjective quality. Only the owner accepts.

---

## 9. Verification rules (apply in every stage)

- PlayMode acceptance tests and the gold path must not use `Physics.Simulate`, `detectCollisions = false`, manual position/velocity clamps or teleports, direct calls to gameplay methods in place of input, or `[Ignore]`d checks (`V57/knowledge/verification.md`).
- Input simulation: press and release on **separate frames**; restore real devices in `finally` (`V57/knowledge/unity-cli.md`).
- Screenshots: Play Mode, end-of-frame Game View capture (includes UI Toolkit overlay). Evidence = screenshots **and** numbers.
- After any C# write: poll `recompile_status` until complete — never blind-retry during a domain reload.

---

## 10. Output lines

Progress (one line per gate; also appended to `Docs/V57/DEVLOG.md`):

```text
GF-PROGRESS stage=<id> spec=<spec|none> done=<n> total=<m> gate=<pass|fail|running> status_word=<implemented|agent-verified> action="<short>"
```

Stop block:

```markdown
## V57 — STOPPED at Stage {id}
- **Reason:** blocking intake | destructive action required | owner stop token
- **Evidence:** {paths}
- **Resume:** fix the cause (or remove `.v57/ALLOW_STOP`), then `/vertical-slice`
```

Preceded by the single line `HARD-STOP: <reason>`.

---

## Sub-skills

| Stage | Skill |
|---|---|
| I0–I2 | `commands/setup/unity-intake-skill.md` |
| M1, F, M2+ regression | `commands/setup/unity-gold-path-skill.md` |
| M1 context/specs | `commands/tdd/unity-tdd-to-context-skill.md`, `commands/tdd/unity-tdd-to-spec-skill.md` |
| F | `commands/spec/unity-spec-skill.md`, `commands/compilation/unity-compile-heal-skill.md` |
| M2 | `commands/editor/unity-scene-setup-skill.md`, `unity-prefab-skill.md`, `commands/ui/unity-game-ui-skill.md`, audio/vfx/input/level skills |
| M3 | craft skills + `commands/review/unity-independent-review-skill.md` |
| M4 | `commands/setup/unity-playability-cert-skill.md`, `commands/review/unity-qa-skill.md`, `commands/compilation/unity-build-skill.md` |

---

*Last updated: 2026-09-28*
