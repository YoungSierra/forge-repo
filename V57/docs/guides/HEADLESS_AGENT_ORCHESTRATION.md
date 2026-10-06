# Headless agent orchestration — executive standard

*Last updated: 2026-09-28 — pipeline v2 (I0 → M4, autonomous), Unity CLI primary, `unity mcp` optional, provider repo layout (`Docs/Design`).*

What is **required** to run V57 pipelines (`/vertical-slice`, `/game-setup`) headless through the GameForge sidecar (`Tools/gameforge-agent`, Cursor SDK by default), without depending on a particular UI client.

---

## 1. Goal

1. **Autonomous runs:** one start request runs the whole pipeline I0 → M4. The agent chains stages, decides and logs (`Docs/V57/DECISIONS.md`) instead of asking, and ends only at M4 or on a stop condition (blocking intake, destructive action, owner stop token `.v57/ALLOW_STOP`). There is no per-stage `continue`.
2. **State on disk:** progress lives in the game repo (`Docs/V57/STATUS.json`, `PLAN.md`, `TODO.md`, `DECISIONS.md`, `DEVLOG.md`), so any session can resume and any UI can show progress without parsing chat.
3. **Unity through the CLI:** `unity command` / `eval` / `test` against the Editor open on the game repo; `unity mcp` is an optional stdio binding to the same Pipeline.
4. **Stable HTTP surface** on `http://127.0.0.1:3857` for health, run control, events and reports.

---

## 2. Architecture

```
[UI client] --HTTP/SSE--> [Sidecar (Node, :3857)] --AgentProvider--> Agent session (Cursor SDK | other)
                               |                                         |
                               |                                  shell: unity command / eval / test
                               |                                  node V57/tools/intake, runtime-lint
                               |                                  (optional) unity mcp stdio
                               |                                         v
                               +---- reads Docs/V57/* ------>  [Unity Editor 6000.6.2f1 + com.unity.pipeline + com.v57.assembly]
                                                                 open on the provider game repo (branch v57/setup)
```

| Layer | Responsibility |
|------|-----------------|
| **UI client** | Health, start/cancel/resume, live events, STATUS.json view, report list. No API keys, no repo writes. |
| **Sidecar** | Env/secrets, preflight, sessions, prompt building, provider adapter, report/evidence listing, HTTP API. |
| **Game repo** | Provider delivery + `V57/` pack + V57 outputs (`Docs/Generated`, `Docs/V57`, `Assets/_Game/...`). |
| **Editor** | Pinned 6000.6.2f1, `com.unity.pipeline` (via `unity pipeline install`), `com.v57.assembly` (added at I1). |

---

## 3. Runtime prerequisites

| Requirement | Notes |
|-----------|--------|
| Node.js 22+ | Sidecar and `V57/tools/intake`, `V57/tools/lint` |
| Unity CLI on PATH (or `UNITY_CLI_PATH`) | `unity pipeline list` shows the Editor for this project, not Safe Mode |
| Unity Editor 6000.6.2f1 open on the game repo | Needed from I1 on (I0 intake is Node-only) |
| Provider key | `CURSOR_API_KEY` (default provider) or the key of the selected provider, in `%USERPROFILE%\.v57\gameforge.env` / `~/.v57/gameforge.env` or project `.env.local` |
| Optional | `unity mcp configure cursor --local` when an IDE binding is wanted; legacy relay only with `GAMEFORGE_UNITY_MCP_MODE=legacy` (until end 2026) |

---

## 4. Configuration

| Variable | Default | Use |
|----------|---------|-----|
| `GAMEFORGE_PROJECT_ROOT` | two levels above the sidecar | **Game repo root** (provider repo). All paths resolve from here |
| `GAMEFORGE_PORT` / `PORT` | **3857** | Sidecar HTTP port |
| `GAMEFORGE_HOST` / `HOST` | `127.0.0.1` | Bind address (keep local) |
| `GAMEFORGE_TOKEN` | — | Optional shared token required on every `/api/*` except `/api/health` |
| `CURSOR_API_KEY` / other provider key | — | Active provider auth |
| `GAMEFORGE_UNITY_CLI` | `1` | `0` disables sidecar CLI helpers |
| `GAMEFORGE_UNITY_TEST_VIA_CLI` | `1` | Tests via `unity test` (gates expect this) |
| `UNITY_CLI_PATH`, `UNITY_PROJECT_PATH` | — | CLI binary; disambiguate multiple Editors |

Secrets load on health, start and resume (not only at boot). Never expose keys to the UI. Gitignore `.env*`, `node_modules/`, `sessions/`.

---

## 5. Game inputs (provider repo)

| Input | Path |
|---|---|
| TDD | `Docs/Design/TDD.md` (was `Docs/tdds/<slug>/…` in v1 — no longer used for new games) |
| ADD + references | `Docs/ArtDirection/ArtDirectionDocument.md`, `Reference/`, `UIMockups/`, `Camera/` |
| Art / audio | `Assets/_Game/Art/**`, `Assets/_Game/Audio/**` |
| Slug | `Docs/Generated/json/package.json → slug` after I0; before I0 the repo folder name |

The sidecar does not pick among several TDDs: one repo = one game = one TDD.

---

## 6. Preflight gate

No session may start while preflight fails.

| Check | Condition |
|------|-----------|
| `v57` | `V57/agents/agents.yaml` exists in the game repo |
| `tdd` | `Docs/Design/TDD.md` exists |
| `node` | Node ≥ 22; `V57/tools/intake/node_modules` present (or `npm install` runs as bootstrap) |
| `unityCli` | `unity` resolvable; `GET /api/unity-cli/preflight` ok (Editor reachable, eval probe) — **required from I1**; a run may start I0 without it and waits/retries per timeboxes |
| `provider` | key for the selected provider present |
| `mcp` | informational only (optional binding) |

`GET /api/health` returns the flags plus `blockers[] { code, message, remedy }`. The UI disables Start while a required flag is false.

---

## 7. Run model (autonomous)

1. UI → start (Production or Vertical Slice). Sidecar builds the prompt: invoke `/vertical-slice` (or `/game-setup`), game repo root, mode, and the rule "no continue; decide + log; stop only on the three stop conditions".
2. Agent reads `Docs/V57/STATUS.json` (+ PLAN/TODO/DEVLOG) and resumes at the recorded stage, or starts at I0.
3. Agent runs stages, commits at each green gate, appends `GF-PROGRESS` lines to DEVLOG and the event stream.
4. Terminal states: `finished` (M4 report written), `stopped` (stop condition; Stop block emitted), `cancelled` (user cancel; resumable from checkpoint), `error` (transport/provider failure; resumable).
5. Resume = new or continued session with "resume `/vertical-slice`"; the agent re-reads state files. Chat history is not required.
6. **Independent review (M3/M4):** the sidecar starts a **separate provider session** with only the review pack (`Docs/V57/evidence/M3/<UTC>/review-pack/`) and the reviewer instructions from `unity-independent-review-skill.md`; the reviewer's output is written to `review.json` + report. The implementing session never receives write access to the verdict files for that round.

---

## 8. HTTP API (current sidecar)

| Method | Route | Function |
|--------|------|---------|
| `GET` | `/api/health` | Preflight flags, port, project root, active/busy sessions |
| `GET` | `/api/unity-cli/preflight` | CLI + Pipeline + Editor + eval probe |
| `POST` | `/api/unity-cli/eval` · `/command` · `/test` | Direct CLI helpers (`{ code }`, `{ name, args }`, `{ mode, filter }`) |
| `POST` | `/api/mcp/ping` · `/api/mcp/reset` | Optional MCP binding status |
| `POST` | `/api/sessions/generate-final` | Start an autonomous run (SSE stream of events); body `{ slug, forgeMode: "VerticalSlice" \| "Production" }` |
| `POST` | `/api/sessions/chat` | Free chat / single command in the game repo |
| `POST` | `/api/sessions/:id/continue` | Resume from the last checkpoint after cancel/error (not a per-stage gate) |
| `POST` | `/api/sessions/:id/cancel` · `/api/sessions/cancel` | Cancel one / all sessions |
| `GET` | `/api/sessions/:id/events` · `/events/poll` · `/checkpoint` · `/activity` | Live events, polling fallback, checkpoint info |
| `GET` | `/api/context/graph.html` | Context graph viewer |

Route names follow `Tools/gameforge-agent/index.js`; if the sidecar renames routes, update this table in the same change.

---

## 9. Progress (UI-agnostic)

Preferred: poll `Docs/V57/STATUS.json` (stage, gates, goldpath, status_word, blockers). Secondary: parse event text for

- `GF-PROGRESS stage=<id> … gate=<pass|fail|running> status_word=<…>` — many per turn in an autonomous run; process all matches.
- `## V57 — STOPPED at Stage {id}` → `stopped`; show Reason / Evidence / Resume.
- M4 final report path `Docs/V57/reports/M4-final-<slug>.md` → finished.

Status words shown to users: `implemented`, `agent-verified`, `pending owner review`. The UI never shows "accepted".

---

## 10. Reports and evidence

| Kind | Path (game repo) |
|---|---|
| Stage reports | `Docs/V57/reports/<stage>-<slug>.md` |
| Assembly | `Docs/V57/reports/assembly-report.json` |
| Evidence | `Docs/V57/evidence/<stage>/<UTC>/`, gold path `Docs/V57/evidence/goldpath/<UTC>/checks.json` |
| State | `Docs/V57/{STATUS.json, PLAN.md, TODO.md, DECISIONS.md, DEVLOG.md, INTAKE_REPORT.md}` |

The UI lists and reads; it never writes into the repo. Reports are not auto-opened. `Docs/V57/reports/` is legacy history.

---

## 11. Agent providers

UI → Sidecar API → `AgentProvider` adapter → model + shell tools (Unity CLI, Node) [+ optional MCP] → game repo.

```ts
interface AgentProvider {
  start(prompt: string, opts: { cwd: string; tools: ToolConfig }): Promise<AgentTurnResult>;
  resume(providerSessionId: string, message: string): Promise<AgentTurnResult>;
}
```

| Layer | Changes when swapping provider? |
|------|-------------------------|
| HTTP API, preflight, sessions, prompt builder, report listing, STATUS polling | No |
| Auth key, create/resume/stream | Yes (adapter) |
| Tool surface | Shell (Unity CLI, Node) is the same; MCP binding is provider-specific and optional |

The independent reviewer may use a different provider/model than the implementer.

---

## 12. Standardization checklist

- [ ] Sidecar resolves the game repo root; TDD path `Docs/Design/TDD.md`
- [ ] Port 3857; optional token; local bind
- [ ] Preflight includes Node/intake deps and Unity CLI (MCP informational)
- [ ] Prompt builder: autonomous `/vertical-slice` or `/game-setup`; no stage-gated continue; `--staged` only on explicit owner request
- [ ] Resume re-reads `Docs/V57/*` state files
- [ ] Separate reviewer session for `/independent-review`
- [ ] UI progress from STATUS.json + `GF-PROGRESS`
- [ ] Gitignore for secrets and sessions

---

## Pack references

| Doc / skill | Role |
|-------------|-----|
| `V57/agents/skills/commands/setup/unity-vertical-slice-skill.md` | Stage driver |
| `V57/agents/skills/commands/setup/unity-game-setup-skill.md` | Production scope |
| `V57/agents/skills/commands/review/unity-independent-review-skill.md` | Reviewer contract |
| `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md` | CLI policy and sidecar CLI routes |
| `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md` | Optional MCP binding |
