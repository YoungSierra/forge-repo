# Unity Code Reviewer Skill

Use this skill when the user invokes **`/code-reviewer`** with a script path for a full standards pass. The report is **saved to disk** under `Docs/V57/reports/code-review/`.

| Item | Detail |
|------|--------|
| **Authoritative** | `V57/docs/standards/STANDARDS_CANONICAL.md` — load first; all PASS/FAIL trace here |
| **Output** | `Docs/V57/reports/code-review/CODE_REVIEW_{filename}.md` |

---

## Invocation

```bash
/code-reviewer <script-path>
```

Examples:

```bash
/code-reviewer Assets/_Game/Scripts/Features/Player/PlayerController.cs
/code-reviewer Assets/_Game/Scripts/Core/Managers/GameManager.cs
```

---

## Review process

### Step 1 — Read the target script

Read the full file at the provided path.

### Step 2 — Load references (order matters)

Use `review_standards.read_in_order` from `V57/agents/agents.yaml`:

| Order | File |
|-------|------|
| 1 | `V57/docs/standards/STANDARDS_CANONICAL.md` — **single source of truth** (strict, §10) |
| 2 | `V57/agents/skills/shared/checks/unity-validation-skill.md` — checklist mapped to sections |
| 3 | `V57/agents/skills/shared/references/unity-naming-skill.md` — naming summary |
| 4 | `V57/docs/standards/CODING_STANDARDS.md` — patterns (canonical wins on conflict) |

Do **not** invent stricter rules than the canonical doc.

### Step 3 — Perform validation

Evaluate against **`STANDARDS_CANONICAL.md` §2–§10** (global rules, naming, namespaces, regions, encapsulation, async/coroutines, diagnostics, ScriptableObjects, compliance). Use **`V57/agents/skills/shared/checks/unity-validation-skill.md`** as the checklist you walk through.

**Critical vs warning** — definitions from canonical **§10** only.

---

## Output format (saved report)

**Save to:** `Docs/V57/reports/code-review/CODE_REVIEW_{filename}.md`

Use this structure (emoji in generated reports improve scanability in PRs and chat):

```markdown
# Code Review: {filename}

## Summary
**Lines:** X | **Issues:** X critical, X warnings

**Standards:** Evaluated against `V57/docs/standards/STANDARDS_CANONICAL.md` (strict).

## ✅ Passed Checks
- Naming conventions: PASS/FAIL
- Code structure: PASS/FAIL
- Unity patterns: PASS/FAIL
- Error handling: PASS/FAIL
- Performance: PASS/FAIL

---

## 🔴 Critical Issues

| Line | Issue | Fix |
|------|-------|-----|
| XX   | Description | Recommended fix |

## 🟡 Warnings

| Line | Warning | Suggestion |
|------|---------|------------|
| XX   | Description | Suggestion |

---

## 📋 Full Validation Checklist

### Naming Conventions
- [ ] Class: PascalCase - {PASS/FAIL}
- [ ] Interface: IPascalCase - {PASS/FAIL}
- [ ] Private fields: _camelCase - {PASS/FAIL}
- [ ] No public mutable fields - {PASS/FAIL}
- [ ] Constants: PascalCase - {PASS/FAIL}
- [ ] Methods: PascalCase - {PASS/FAIL}
- [ ] Properties: PascalCase - {PASS/FAIL}
- [ ] Events: OnPascalCase - {PASS/FAIL}
- [ ] Coroutines: CoPascalCase - {PASS/FAIL}
- [ ] Filename matches class name - {PASS/FAIL}

### Code Structure
- [ ] Namespace correct - {PASS/FAIL}
- [ ] Namespace follows Company.Project.Module - {PASS/FAIL}
- [ ] #region Unity Lifecycle - {PASS/FAIL}
- [ ] #region Public/Private Methods - {PASS/FAIL}
- [ ] No regions for files under 50 lines - {PASS/FAIL}
- [ ] File under 200 lines - {PASS/FAIL}
- [ ] Explicit types (no var) - {PASS/FAIL}

### Unity Patterns
- [ ] [SerializeField] usage - {PASS/FAIL}
- [ ] [Header] grouping - {PASS/FAIL}
- [ ] [Tooltip] documentation - {PASS/FAIL}
- [ ] [Range] for numeric ranges - {PASS/FAIL or N/A}
- [ ] Coroutines return IEnumerator - {PASS/FAIL or N/A}
- [ ] async methods return Task - {PASS/FAIL or N/A}
- [ ] Singleton Instance property - {PASS/FAIL or N/A}
- [ ] Singleton null check - {PASS/FAIL or N/A}

### ScriptableObjects
- [ ] [CreateAssetMenu] attribute - {PASS/FAIL or N/A}
- [ ] Menu path format - {PASS/FAIL or N/A}
- [ ] Fields are [SerializeField] private - {PASS/FAIL or N/A}
- [ ] Access via properties - {PASS/FAIL or N/A}

### Error Handling
- [ ] Debug.LogWarning usage - {PASS/FAIL}
- [ ] Debug.LogError usage - {PASS/FAIL}
- [ ] Assert.IsNotNull for required refs - {PASS/FAIL}
- [ ] Null checks before component access - {PASS/FAIL}

### Performance
- [ ] Caches component references in Awake/Start - {PASS/FAIL}
- [ ] No GetComponent in Update - {PASS/FAIL}
- [ ] Object pooling for frequent create/destroy - {PASS/FAIL or N/A}
- [ ] Appropriate collection types - {PASS/FAIL}

---

## Verdict

{APPROVED / NEEDS FIXES / REJECTED}

{If rejected or fixes needed, write **plain-language bullets**: what is wrong and why it blocks approval—do not defer to “see §10” or doc section numbers; the reader may not have the doc open.}
```

---

## Critical issues (block approval)

Align with **`V57/docs/standards/STANDARDS_CANONICAL.md` §10**, including at minimum:

| # | Issue |
|---|--------|
| 1 | Missing or wrong namespace where required |
| 2 | Interface without `I` prefix |
| 3 | Undocumented public mutable fields |
| 4 | `var` in production code |
| 5 | `GetComponent` (or equivalent) in `Update` / `FixedUpdate` / `LateUpdate` |
| 6 | `async void` without justified Unity callback pattern |
| 7 | Singleton missing `Instance` + duplicate guard when claiming singleton |
| 8 | File over 200 lines without split plan |
| 9 | Type/file naming mismatch or compile-breaking naming |

---

## Warnings (fix when practical)

Per canonical §10 and **`V57/agents/skills/shared/checks/unity-validation-skill.md`** — e.g. missing `[Header]`/`[Tooltip]`, `#region` layout for files **> 50 lines**, `GetComponent` in `Start` when `Awake`/`OnEnable` is better, missing null checks.

---

## Rules

1. Always write the report to **`Docs/V57/reports/code-review/CODE_REVIEW_{filename}.md`**.  
2. **APPROVED** only if zero critical issues (canonical §10).  
3. Never contradict the canonical document.  

---

## Apply this skill

1. Wait for **`/code-reviewer <path>`**.  
2. Read the script + references in Step 2 order.  
3. Validate fully against the canonical doc.  
4. Emit the structured report and **save** it.  
5. Set verdict per rules above.
