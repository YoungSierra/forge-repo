# Unity Compile Heal Skill (`/compile-heal`)

After any C# or asmdef write, force Unity to compile and **heal** until zero compile errors — or FAIL with a clear blocker list. Genre-neutral. Used by `/game-setup`, `/prototype`, and `/spec "implement"` (and `/qa` Q0 at M4).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` (writes/heal fixes) |
| **prefer_unity_cli** | `recompile`, `recompile_status`, `eval` — see `UNITY_CLI_INTEGRATIONS.md` |
| **on_unavailable** | `stop` |
| **Task subagent** | When called from `/game-setup` or `/prototype`, launch as Task (`generalPurpose`) that reads this skill end-to-end |
| **Max fix loops** | 5 (configurable `--max-loops N`) |

---

## Invocation

```bash
/compile-heal
/compile-heal --max-loops 5
/compile-heal --dry-run
```

---

## Workflow

### 1. Refresh and wait

**Prefer Unity CLI** (no domain reload; safe during Forge Chat):

```bash
unity command recompile --project-path <repo-root>
# poll until completed:
unity command recompile_status --project-path <repo-root>
```

Or sidecar: `POST /api/unity-cli/command` with `{ "name": "recompile" }`.

**Fallback — MCP** (`Unity.RunCommand` / `Unity_RunCommand`):

1. `AssetDatabase.Refresh()`
2. Poll until `!EditorApplication.isCompiling` (timeout ~120s → FAIL)

**Collect errors — CLI eval first:**

```bash
unity command eval "<CompilationPipeline or EditorUtility.scriptCompilationFailed read-back>" --project-path <repo-root>
```

Or `POST /api/unity-cli/eval`. Then MCP `Unity_GetConsoleLogs` if needed.

### 2. Collect errors

- Compilation errors (CS####) with file + line
- Ignore Info; treat Error as blocking; Warning does not block heal unless `--strict-warnings`

### 3. Heal loop

While errors remain and loop &lt; max:

1. Group by file; open/read each file
2. Fix root causes (missing usings, wrong namespaces, asmdef refs, typos) — do **not** suppress with `#pragma` or empty stubs that hide bugs
3. Refresh + wait compile again
4. Re-read errors

If the same error persists 2 consecutive loops → STOP and report (need human design decision).

### 4. Gate

| Result | Meaning |
|--------|---------|
| **PASS** | 0 compile errors |
| **FAIL** | Errors remain after max loops or unrecoverable |

**Never enter Play Mode** while compile errors exist.

### 5. Report

Write `Docs/V57/reports/<stage>-compile-heal-{slug}.md` (pack-only runs without a game repo: `Docs/V57/reports/`):

```markdown
# Compile heal report
- **Overall:** PASS | FAIL
- **Loops:** N
- **Remaining errors:** …
- **Files touched:** …
```

---

## Integration

- `/game-setup` Stage F+: after every implement write
- `/prototype` Stage E+: after every implement write
- `/playability-cert` preflight must see PASS (or run heal first); same for `/qa` Q0 (M4)

---

*Last updated: 2026-07-30*
