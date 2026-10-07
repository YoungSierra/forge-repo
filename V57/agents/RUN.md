# V57 run entry — `/vertical-slice` and `/game-setup`

Single entry for any agent (Cursor, Claude Code, Codex, the GameForge sidecar, any LLM with a shell). The chat commands in
`.cursor/commands/` and `.claude/commands/` only point here. Run everything below **without asking**; stop only on the
conditions in the skill's §7 (blocking intake, destructive action, owner stop token `.v57/ALLOW_STOP`).

| Command | Skill (the stage driver you execute) |
|---|---|
| `/vertical-slice [--from <stage>] [--status] [--staged]` | `V57/agents/skills/commands/setup/unity-vertical-slice-skill.md` |
| `/game-setup [--run-all] [--from <stage>] [--status]` | `V57/agents/skills/commands/setup/unity-game-setup-skill.md` |

## 1. Read (once per session)
`AGENTS.md`, `V57/SYSTEM_PROMPT.md`, the skill above, `V57/agents/skills/commands/setup/unity-intake-skill.md`, every
`V57/knowledge/*.md`. Never read `.env.local` or `gameforge.env`.

## 2. Preflight (fix what you can, then continue)

| # | Check | If it fails |
|---|---|---|
| 1 | `git status` at the repo root; branch `v57/setup` | create `v57/setup` from the current branch; commit uncommitted provider files as `P delivery` first |
| 2 | `Docs/Design/TDD.md` exists | **stop**: blocking intake (`TDD_MISSING`); report to the owner |
| 3 | `node --version` ≥ 22 | stop and report (cannot install Node) |
| 4 | `V57/tools/intake/node_modules` and `V57/tools/lint/node_modules` exist | `npm ci` (or `npm install`) in each folder |
| 5 | `unity --version`; `unity status --json` lists an Editor **6000.6.2f1** in state `ready` whose project is this repo root | `unity open .` (pinned version from `ProjectSettings/ProjectVersion.txt`), then poll `unity status --json` every 10 s until `ready` (first import can take minutes) |
| 6 | `unity command recompile_status` → idle, no compile errors | fix C# on disk (Safe Mode) before anything else |

Every `unity command …` call targets this project: add `--project-path <repo root>` when more than one Editor is open.
Long Editor work (`AssemblyRunner.RunAll`, big imports) must not block the CLI call: schedule it with
`unity command eval "EditorApplication.delayCall += () => V57.Assembly.AssemblyRunner.RunAll(); return 1;"` and poll its
output file (`Docs/V57/reports/assembly-report.json`) instead of waiting on the eval.

## 3. Run
Execute the skill from its §0 (session start / resume) to the end, chaining stages automatically. State lives in
`Docs/V57/STATUS.json`; `--status` prints it, `--from <stage>` resumes. Decisions go to `Docs/V57/DECISIONS.md` as `D-###`.

## 4. Finish
Reply with: final stage and status words, gate results (console errors, `assembly-report.json` missing refs, gold path
`checks.json`, EditMode/PlayMode totals), `Docs/V57/MISSING_ASSETS.md` summary, the `D-###` list, and open defects.
No player build unless the owner asked for one in the same request.
