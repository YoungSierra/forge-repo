# Unity Localization Skill (`/localization`)

String tables and locale switching via Unity Localization (or project-equivalent) when TDD requires multi-locale content; otherwise **silent skip** (no report).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-L10N applies |
| **Pairs with** | `/game-ui` |

---

## Invocation

```bash
/localization
/localization --locales en,es
/localization --table UI
/localization verify
```

---

## Workflow

1. Confirm Localization package if TDD requires it
2. Create Locales; String Table Collection for UI/dialogue keys from TDD/`narrativeRef`
3. Bind UI Toolkit / TextMeshPro / Labels via LocalizedString or project helper
4. Smoke: switch locale → visible string changes
5. Do not hardcode player-facing strings in C# when localization is in scope
6. Report missing keys

### Report

`Docs/V57/reports/localization-report-{slug}.md`

---

## Integration

`/game-setup` **G-L10N** when multi-locale / string tables required. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
