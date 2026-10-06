# Unity Accessibility Skill (`/accessibility`)

Text scale, contrast, colorblind-safe cues, and input remapping hooks when TDD requires a11y. Otherwise N/A.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-A11Y applies |
| **Pairs with** | `/game-ui`, `/input-setup` |

---

## Invocation

```bash
/accessibility
/accessibility --spec @V57/specs/<slug>/features/settings.yaml
/accessibility verify --scene {SCENE}
```

---

## Workflow

1. Read a11y requirements from TDD/settings spec
2. UI text scale setting (Toolkit/USS or TMP size multiplier)
3. Contrast: ensure HUD critical text readable on backgrounds (capture check)
4. Colorblind: do not rely on color alone for fail/success — add icon/shape/text
5. Remapping: expose rebind UI hooks via `/input-setup` when required
6. Subtitles toggle if cinematics/dialogue in slice
7. Verify settings persist if `/save-meta` in scope

### Report

`Docs/V57/reports/accessibility-report-{slug}.md`

---

## Integration

`/game-setup` **G-A11Y** when TDD lists a11y requirements. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
