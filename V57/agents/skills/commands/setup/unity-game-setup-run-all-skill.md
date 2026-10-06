# Unity Game Setup Run-All Skill (`/game-setup-run-all`) — alias

**Deprecated alias.** Autonomous chaining is now the default of `/game-setup` and `/vertical-slice`: there is no INIT confirmation and no `continue` between stages.

`/game-setup-run-all` behaves exactly like `/game-setup` (`commands/setup/unity-game-setup-skill.md`). Arguments are passed through (`--from`, `--status`, `--scope slice`).

| Old behavior | Now |
|---|---|
| INIT confirm table | Removed — variables are resolved from `Docs/Generated/json/package.json` and written to `Docs/V57/PLAN.md` |
| Chain stages after INIT | Default for every run |
| Staged `/game-setup` with `continue` | Opt-in only: `/game-setup --staged` |
| Hard STOP on gate FAIL | Replaced by the driver rules: fix / revert / cut + `D-###`; stop only on blocking intake, destructive action, or `.v57/ALLOW_STOP` |
| Write roots `Assets/_Game/Scripts`, `Assets/_Isolated/<slug>` | `Assets/_Game/...` + `Docs/V57/...` (see `unity-vertical-slice-skill.md`) |

---

*Last updated: 2026-09-28*
