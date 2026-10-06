# Unity PR Reviewer Skill

Use this skill when the user invokes **`/pr-review`** with a script path. Output is **PR-ready Markdown in chat only** — **no files saved**.

| Item | Detail |
|------|--------|
| **Authoritative** | `V57/docs/standards/STANDARDS_CANONICAL.md` |
| **Output** | Chat / clipboard only (GitHub-flavored Markdown) |

---

## Invocation

```bash
/pr-review <script-path>
```

Examples:

```bash
/pr-review Assets/_Game/Scripts/Features/Player/PlayerController.cs
/pr-review Assets/_Game/Scripts/Player/PlayerController.cs
```

---

## Process

### Step 1 — Read the target script

Read the full file at the provided path.

### Step 2 — Load references (order matters)

Use `review_standards.read_in_order` from `V57/agents/agents.yaml`:

| Order | File |
|-------|------|
| 1 | `V57/docs/standards/STANDARDS_CANONICAL.md` |
| 2 | `V57/agents/skills/shared/checks/unity-validation-skill.md` |
| 3 | `V57/agents/skills/shared/references/unity-naming-skill.md` |
| 4 | `V57/docs/standards/CODING_STANDARDS.md` |

### Step 3 — Perform full validation

Evaluate **`STANDARDS_CANONICAL.md` §2–§10**. Critical/warning definitions = **§10** + alignment with **`V57/agents/skills/commands/review/unity-code-reviewer-skill.md`**.

---

## Output format (PR comment)

Paste-ready GitHub Markdown:

```markdown
## Code Review: `{filename}`

**Status:** {APPROVED | ⚠️ REQUEST CHANGES | ❌ REJECTED}
**Issues:** {X} critical | {Y} warnings
**Standards:** `V57/docs/standards/STANDARDS_CANONICAL.md` (strict)

---

### Summary

| Area | Status |
|------|--------|
| §2–§3 Naming & file rules | {PASS/FAIL} |
| §4–§5 Namespace & regions | {PASS/FAIL} |
| §6–§7 Unity / async | {PASS/FAIL} |
| §8 Diagnostics | {PASS/FAIL} |
| §9 ScriptableObjects | {PASS/FAIL or N/A} |

### {Critical Issues Count} critical issue(s)

{#each issue with line number and fix}

### {Warnings Count} warning(s)

{#each warning with suggestion}

---

**Reviewed by:** Unity PR Reviewer Skill
**Date:** {YYYY-MM-DD}
```

---

## Critical issues (block approval)

Same set as **`V57/docs/standards/STANDARDS_CANONICAL.md` §10** and **`V57/agents/skills/commands/review/unity-code-reviewer-skill.md`** (namespace, `I` prefix, public mutable fields, `var`, per-frame `GetComponent`, bad `async void`, singleton pattern, >200 lines, naming/compile issues, etc.).

---

## Verdict rules

| Verdict | Condition |
|---------|-----------|
| **APPROVED** | No critical issues; warnings acceptable per §10 |
| **REQUEST CHANGES** | Critical issues that are fixable |
| **REJECTED** | Severe or widespread §10 violations |

---

## Rules

1. **Do not save** any file — chat output only.  
2. Same bar as **`/code-reviewer`**, different delivery channel.  
3. Canonical document always wins.  

---

## Apply this skill

1. Wait for **`/pr-review <path>`**.  
2. Read script + references in Step 2 order.  
3. Validate fully.  
4. Emit PR Markdown to chat only.
