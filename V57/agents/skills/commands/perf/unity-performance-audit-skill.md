# Unity Performance Audit Skill (`/perf-audit`)

Two-layer performance audit: **static code scan** + **dynamic metrics via Unity MCP** (Play Mode / Editor stats).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` for dynamic layer; static layer works without MCP but full audit requires MCP |
| **on_unavailable** | `stop` for dynamic layer — report static-only with WARN banner |
| **Budgets** | `V57/docs/performance/PERFORMANCE_BUDGETS.md` + `CONTEXT.md` → `performanceBudgets` |
| **Fixture** | `V57/tools/lint/test/fixtures/BrokenCoinCollector.cs` — pack anti-pattern demo fixture (filename historical; content is generic bad practices) |

---

## Invocation

```bash
/perf-audit
/perf-audit --scope Assets/_Game/Scripts/Features/
/perf-audit --scene Assets/_Game/Scenes/Main.unity --playmode-seconds 5
/perf-audit --static-only
/perf-budget   # define budgets first (optional)
```

| Flag | Behavior |
|------|----------|
| `--scope <path>` | Limit static scan (default: `Assets/_Game/Scripts/`) |
| `--scene` | Scene to open for dynamic sampling |
| `--playmode-seconds N` | Enter Play Mode N seconds for frame/GC sampling (default 3) |
| `--static-only` | Skip MCP dynamic layer |

---

## Read order

1. `CONTEXT.md` — `performanceBudgets`, `targetPlatforms`
2. `V57/docs/performance/PERFORMANCE_BUDGETS.md`
3. This skill
4. MCP pre-flight (unless `--static-only`)

---

## Layer A — Static scan (always)

Scan `.cs` under scope for anti-patterns. Emit findings with severity.

| Pattern | Severity | Notes |
|---------|----------|-------|
| `GameObject.Find(` / `FindObjectOfType` in Update/FixedUpdate/LateUpdate | **Critical** | Also in `agents.yaml` prohibited for Find |
| `Camera.main` in hot paths (Update loops) | **Critical** | Cache in Awake/Start |
| String concat/`$""` in Update | **High** | GC pressure |
| LINQ (`.Where`, `.Select`, etc.) in Update/FixedUpdate | **High** |
| `new` collection/class in Update | **High** | Allocs per frame |
| Missing pooling for frequent spawn/destroy | **Medium** | Compare with spec spawn rates |
| `SendMessage` / `BroadcastMessage` | **Medium** | Reflection cost |
| Public mutable fields on MonoBehaviour | **Critical** | Standards violation |

**Gate integration:** when `run_perf_static_scan: true`, run on every `/spec implementa` touched files. **BLOCK** on Critical.

**Demo:** scanning `V57/tools/lint/test/fixtures/BrokenCoinCollector.cs` must report ≥1 **Critical** (e.g. public mutable fields) and ≥1 **High** (e.g. `Physics.OverlapSphere` alloc in `Update`).

---

## Layer B — Dynamic audit (MCP required)

Pre-flight PASS → `Unity.RunCommand`:

### B1. Editor stats (scene loaded)

Sample via script:

```csharp
result.Log($"drawCalls={UnityStats.drawCalls}");
result.Log($"setPassCalls={UnityStats.setPassCalls}");
result.Log($"triangles={UnityStats.triangles}");
result.Log($"batches={UnityStats.batches}");
```

### B2. Memory

```csharp
result.Log($"totalAllocated={Profiler.GetTotalAllocatedMemoryLong()}");
result.Log($"totalReserved={Profiler.GetTotalReservedMemoryLong()}");
```

### B3. Play Mode frame sampling (optional `--playmode-seconds`)

Controlled Play Mode entry:

```csharp
EditorApplication.isPlaying = true;
// wait N seconds via EditorApplication.update or delay
// sample Time.deltaTime, GC.GetTotalMemory
EditorApplication.isPlaying = false;
```

Use `UnityEditor.Profiling.ProfilerRecorder` when available for CPU/GC markers (project-version dependent).

### B4. Compare to budget

Read `CONTEXT.md` → `performanceBudgets` or defaults from `PERFORMANCE_BUDGETS.md`:

| Metric | Budget source | Semaphore |
|--------|---------------|-----------|
| Frame time (ms) | `targetFrameTimeMs` | 🟢 ≤ budget · 🟡 ≤ 1.25× · 🔴 > 1.25× |
| Draw calls | `maxDrawCalls` | same |
| SetPass calls | `maxSetPassCalls` | same |
| GC alloc/frame (gameplay) | `maxGcAllocPerFrameBytes` (0 ideal) | same |

---

## Report

Save `Docs/V57/reports/perf-report-{slug}.md`:

```markdown
# Performance audit

## Static findings
| File | Line | Pattern | Severity |
|------|------|---------|----------|

## Dynamic metrics (MCP)
| Metric | Value | Budget | Status |
|--------|-------|--------|--------|

## Overall
**Static:** PASS/FAIL
**Dynamic:** PASS/FAIL / SKIPPED (--static-only)
**Recommendations:** ...
```

---

## Integration

- `/game-setup` Stage **G-PERF**: a red budget miss sets `g_perf_pass: false` and **blocks CHECKLIST**. Yellow is recorded, not a block.
- After `/scene-setup` on consumer projects — optional advisory run
- Before milestone sign-off / `/build-setup` (future) — recommended
- `/code-reviewer` may reference perf findings but does not replace this skill

---

## Prohibited

- Claiming perf PASS without MCP dynamic layer when `--static-only` not set and `Assets/` exists
- Optimizing without measured baseline in report

---

*Last updated: 2026-06-10*
