# DECISIONS — `<slug>`

Append-only log of choices V57 made instead of asking. Ids are sequential (`D-001`, `D-002`, …) and referenced from commits (`[D-###]`), STATUS.json, TODO.md and reports. The owner reviews asynchronously; to reverse a decision, add a new entry that supersedes it.

Authority order used for every decision: **TDD > ADD > V57 defaults**.

## Entry template

```markdown
### D-001 — <short title>
- **When:** <UTC> · **Stage:** <I0..M4> · **Commit:** <sha or pending>
- **Context:** <what was ambiguous / failing; evidence path>
- **Options:** A) … B) … C) …
- **Choice:** <A/B/C> — <why, citing TDD/ADD section or V57 rule>
- **Reversal cost:** low | medium | high — <what would need redoing>
- **Type:** absorption | conflict | missing | cut | timebox | revert | tolerance | other
```

---

<!-- entries below, newest last -->
