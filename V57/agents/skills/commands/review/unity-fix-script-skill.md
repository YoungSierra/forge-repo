# Unity Fix Script Skill

Use this skill when the user invokes **`/fix-<ScriptName>`** to auto-correct a Unity C# script to satisfy project standards.

| Item | Detail |
|------|--------|
| **Authoritative** | `V57/docs/standards/STANDARDS_CANONICAL.md` |
| **Outputs** | Patched script + `Docs/V57/reports/code-review/CODE_FIX_*.md` + `Docs/V57/reports/code-review/CODE_REVIEW_*_AFTER_FIX.md` |

---

## Invocation

```bash
/fix-<ScriptName>
```

Examples:

```bash
/fix-PlayerController
/fix-BrokenHoverController
```

---

## Target resolution

| Step | Action |
|------|--------|
| 1 | Parse `ScriptName` from the command |
| 2 | Search paths in order: `Assets/_Game/Scripts/**/ScriptName.cs`, then `V57/tools/lint/test/fixtures/ScriptName.cs` |
| 3 | Multiple matches → ask user to pick path |
| 4 | No match → stop with resolution failure |

---

## Required references (read before fix)

Use `review_standards.read_in_order` from `V57/agents/agents.yaml`:

| Order | File |
|-------|------|
| 1 | `V57/docs/standards/STANDARDS_CANONICAL.md` |
| 2 | `V57/agents/skills/shared/checks/unity-validation-skill.md` |
| 3 | `V57/agents/skills/shared/references/unity-naming-skill.md` |
| 4 | `V57/docs/standards/CODING_STANDARDS.md` |

---

## Fix process

### Step 1 — Analyze violations

Validate against **`STANDARDS_CANONICAL.md` §2–§10** and **`V57/agents/skills/shared/checks/unity-validation-skill.md`**: naming, structure, Unity patterns, SOs, diagnostics, performance.

### Step 2 — Apply corrections

Minimal, behavior-preserving edits:

| Area | Action |
|------|--------|
| Namespace | Add/repair `Company.Project.<Module>` style per project |
| One type per file | Enforce when required; avoid splitting unless necessary |
| Naming | `_camelCase` private, `PascalCase` members, `IPascalCase` interfaces |
| Fields | Prefer `[SerializeField] private` + accessors over public mutable fields |
| Regions | Per §5 for files **> 50 lines** |
| Async | `async Task` except Unity callbacks that must stay `void` |
| Update | Remove per-frame `GetComponent`; cache in `Awake`/`OnEnable`/`Start` |
| Diagnostics | Warnings/errors/asserts + null checks |
| Types | Explicit types — **no `var`** |
| Size | Prefer **≤ 200 lines** |

### Step 2.5 — Interface usage guard (mandatory)

| Rule | Detail |
|------|--------|
| 1 | Search for real usages (`: I…`, parameters, fields, generics, returns) |
| 2 | Unused interface declarations → **do not add** |
| 3 | Remove temporary interfaces that ended unused |
| 4 | Never keep an interface **only** to satisfy naming |

### Step 3 — Re-validate

Full checklist again until **critical** clear and verdict acceptable.

### Step 3.5 — Orphan interface cleanup (mandatory)

After validation: if an interface file has **zero** concrete references outside its declaration → delete and re-validate.

### Step 4 — Persist outputs

| Output | Path |
|--------|------|
| Corrected script | Original target (overwrite) |
| Fix report | `Docs/V57/reports/code-review/CODE_FIX_{ScriptName}.md` |
| Post-fix review | `Docs/V57/reports/code-review/CODE_REVIEW_{ScriptName}_AFTER_FIX.md` (template = `V57/agents/skills/commands/review/unity-code-reviewer-skill.md`) |

---

## Fix report format

```markdown
# Code Fix: {ScriptName}.cs

## Summary
**Status:** {SUCCESS / PARTIAL}
**Critical Issues Before:** X
**Critical Issues After:** X
**Warnings After:** X

## Changes Applied
- ...

## Files Updated
- `{target_script_path}`
- `Docs/V57/reports/code-review/CODE_FIX_{ScriptName}.md`
- `Docs/V57/reports/code-review/CODE_REVIEW_{ScriptName}_AFTER_FIX.md`

## Remaining Issues
- ...

## Verdict
{APPROVED / NEEDS FIXES}
```

---

## Rules

1. Never skip reading reference files.  
2. Never claim success without re-validation.  
3. Preserve gameplay intent unless a standard **forces** a behavior-visible change — then ask if ambiguous.  
4. **Minimal diffs** over large refactors.  
5. **No orphan interfaces.**  

---

## Apply this skill

1. Parse `/fix-<ScriptName>` and **resolve path**.  
2. Read references in order.  
3. Analyze → fix → re-validate → orphan cleanup.  
4. Write script + both **`Docs/V57/reports/code-review/`** reports.  
5. Verdict must reflect remaining critical issues honestly.
