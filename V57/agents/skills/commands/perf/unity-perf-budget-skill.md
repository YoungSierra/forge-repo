# Unity Performance Budget Skill (`/perf-budget`)

Define or update **performance budgets** in `CONTEXT.md` per target platform. Budgets are consumed by `/perf-audit` and future `/build-setup`.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `false` (writes CONTEXT.md only) |
| **Writes** | Autonomous — summary shown, then written in the same turn (`agents.yaml` `writes.require_user_confirm_before_context_md: false`) |
| **Reference** | `V57/docs/performance/PERFORMANCE_BUDGETS.md` |

---

## Invocation

```bash
/perf-budget
/perf-budget --platform mobile
/perf-budget --from-tdd @Docs/Design/TDD.md
/perf-budget show
```

| Flag | Behavior |
|------|----------|
| `--platform mobile\|desktop\|console-like\|webgl` | Seed from reference table |
| `--from-tdd` | Infer targets from TDD platform section |
| `show` | Print current budgets from CONTEXT.md |

---

## Workflow

1. Read `CONTEXT.md` — existing `targetPlatforms`, `performanceBudgets`
2. If missing, propose block from `PERFORMANCE_BUDGETS.md` defaults for selected platform
3. Show diff to user — **wait for confirm**
4. Write `CONTEXT.md` sections (YAML blocks below)
5. Suggest `/perf-audit` to validate against new budgets

---

## CONTEXT.md blocks to add/update

```yaml
targetPlatforms:
  - id: desktop_windows
    input: [keyboard_mouse, gamepad]
    resolution: 1920x1080
  - id: mobile_android
    input: [touch]
    resolution: flexible

performanceBudgets:
  primaryPlatform: desktop_windows
  targetFrameTimeMs: 16.67        # 60 FPS
  maxDrawCalls: 300
  maxSetPassCalls: 150
  maxTrianglesVisible: 500000
  maxGcAllocPerFrameBytes: 0      # gameplay loops
  maxTotalMemoryMb: 512           # advisory
  maxBuildSizeMb: null            # set at /build-setup
```

Adjust per platform when multi-target; document overrides in prose under the YAML.

---

## Rules

- Budgets must be **numeric and testable** — no vague "should be fast"
- Primary platform drives `/perf-audit` semaphore when multiple targets
- Changing budgets after audit → re-run `/perf-audit`

---

*Last updated: 2026-06-10*
