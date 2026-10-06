# Unity Editor Smooth Ops Skill (`/editor-smooth`)

Keep the Unity Editor pipeline **smooth**: save dirty scenes, wait for compile, auto-accept **safe** reload/save prompts via MCP so the user is never left staring at an Engine popup without guidance.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` (Play / reload fallbacks) |
| **prefer_unity_cli** | `save_all`, `recompile_status` — `UNITY_CLI_INTEGRATIONS.md` |
| **on_unavailable** | `stop` |
| **Task subagent** | Optional; usually inline before Play / scene reload |
| **Unsafe dialogs** | License, destructive API Updater, package remove — **never** auto-accept |

---

## Invocation

```bash
/editor-smooth
/editor-smooth --before-play
/editor-smooth --reload-scene Assets/_Game/Scenes/SCN_<Level>.unity
```

---

## Workflow

### 1. Save dirty scenes

**Prefer CLI** (avoids save dialogs blocking MCP):

```bash
unity command save_all --project-path <repo-root>
```

Or `POST /api/unity-cli/command` `{ "name": "save_all" }`.

**Fallback — MCP eval:**

```csharp
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
```

Prefer unconditional save when the pipeline requested it (avoids “Save changes?” blocking MCP).

### 2. Wait for compile

**Prefer CLI:** poll `unity command recompile_status` until `completed` (after `recompile` if needed). Timeout ~120s → STOP.

**Fallback:** poll `EditorApplication.isCompiling` via MCP eval until false.

If compile errors → hand off to `/compile-heal`; **do not** force Play.

### 3. Domain / script reload

After AssetDatabase.Refresh / script writes:

1. Wait compile idle
2. If bridge timed out on a reload dialog, retry once after save + compile wait
3. Pipeline-requested scene reload → `unity_scene_open` / open path **without** asking the user

### 4. Before Play (`--before-play`)

1. Steps 1–3
2. Clear console optional (`unity_console_clear`) only if caller requested
3. Then caller may `unity_play_mode` play

### 5. Unsafe dialog policy

| Dialog type | Action |
|-------------|--------|
| Save scene / reload scene (pipeline-owned) | Auto-accept via save + explicit open |
| Script compile errors blocking Play | Do **not** enter Play; run `/compile-heal` |
| License / account / irreversible API Updater | **STOP** — tell user exact window + button |

Always surface: “Popup in Engine: {what}. Click {button}.” when auto-accept is impossible.

### 6. Report (chat or short file)

```markdown
## Editor smooth
- **Dirty scenes saved:** yes | no | n/a
- **Compile idle:** yes | FAIL
- **Reload:** done | skipped | STOP (unsafe: …)
```

---

## Integration

Call before every Play Mode entry from `/playability-cert`, `/perf-audit`, prototype H-05, `/gold-path`, and `/qa` (M4), and before pipeline scene reloads in `/scene-setup`.

---

*Last updated: 2026-07-30*
