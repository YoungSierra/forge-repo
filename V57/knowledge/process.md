# Process lessons

## No `continue`
- **Symptom:** ~30 stages each paused for a human `continue`; the owner became the scheduler; runs took days of wall time with idle gaps.
- **Rule:** stages chain automatically. Stop only on blocking intake, destructive/irreversible action, or `.v57/ALLOW_STOP`. `--staged` is an owner debugging option, not the default.
- **Source:** HH, WoO.

## Decide and log
- **Rule:** when unsure, choose (TDD > ADD > V57 defaults), write `D-###` in `Docs/V57/DECISIONS.md` (context, options, choice, reversal cost), continue. The owner reviews decisions asynchronously and can reverse them.
- **Source:** WoO.

## Timeboxes
| Situation | Limit | Then |
|---|---|---|
| Stuck on one problem | 45 min | fallback / cut + `D-###` (no placeholder assets) |
| Tool failure | 2 retries | alternate route |
| One asset in one stage | 3 attempts | list in `MISSING_ASSETS.md` + TODO |
| Red after green | 20 min | stash + restore last green commit |
- **Source:** WoO; HH (flipper bats repaired 3+ times across stages).

## Playable from M1
- **Rule:** get the core loop playable on greybox first (gold path), then improve. Every later commit keeps the gold path green. Polish before playability wastes work when gameplay changes.
- **Source:** WoO.

## State on disk
- **Rule:** `Docs/V57/{STATUS.json, PLAN.md, TODO.md, DECISIONS.md, DEVLOG.md}` are the memory. Re-read them at every session start/resume; never rely on chat history.
- **Source:** WoO, HH (`game-setup-progress.md` overwritten per stage lost history).

## One game, one repo, one context
- **Rule:** per-game context lives in the game repo (`Docs/V57/CONTEXT.md`), specs in `V57/specs/<slug>/`, reports in `Docs/V57/reports/`. Never swap a shared root `CONTEXT.md` between games.
- **Source:** HH (`CONTEXT.*.bak.md` swaps).

## No one-off scripts
- **Symptom:** ~150 throwaway editor scripts (`Probe*`, `Fix*`, `Repair*2/3`) and `_tmp_*.cs` in reports folders.
- **Rule:** use `unity command eval` for one-offs (nothing written to disk) or extend `com.v57.assembly`. Scratch files go to `Temp/` or are deleted in the same work block.
- **Source:** HH `_listings/NOTES.txt`.

## Commits as checkpoints
- **Rule:** commit at every green gate with `V57 <stage>: <summary> [D-###]`; record `last_green_commit` in STATUS.json. One absorbed intake issue = one `D-###`.
