# Performance budgets — V57 reference

Performance budgets per platform and static anti-patterns. Active project values live in **`CONTEXT.md`** (`performanceBudgets`), defined via `/perf-budget`.

| Item | Detail |
|------|--------|
| **Audit skill** | `/perf-audit` |
| **Budget skill** | `/perf-budget` |
| **MCP** | Dynamic metrics require Unity MCP — see `UNITY_MCP_REQUIREMENT.md` |

---

## Reference budgets

Guideline values for prototype / vertical slice. Adjust per genre and art.

### Desktop (Windows/Mac/Linux)

| Metric | Target | Notes |
|--------|--------|-------|
| Frame time | ≤ 16.67 ms (60 FPS) | 33.3 ms if target 30 FPS |
| Draw calls | ≤ 300 | Batching/URP/HDRP dependent |
| SetPass calls | ≤ 150 | Unique materials |
| Triangles (visible) | ≤ 500k | Typical gameplay scene |
| GC alloc/frame (gameplay) | **0 B** ideal | Spikes only on load/async |
| Total memory (managed+native advisory) | ≤ 512 MB | Raise for open world |

### Mobile (Android/iOS)

| Metric | Target | Notes |
|--------|--------|-------|
| Frame time | ≤ 16.67 ms (60) or ≤ 33.3 ms (30) | Declare in CONTEXT |
| Draw calls | ≤ 150 | Aggressive batching |
| SetPass calls | ≤ 80 | Shader variant cost |
| Triangles (visible) | ≤ 150k | LOD required |
| GC alloc/frame | **0 B** | Thermal throttling risk |
| Total memory | ≤ 256 MB | Low-end devices |

### Console-like / high-end

| Metric | Target |
|--------|--------|
| Frame time | ≤ 16.67 ms (60) or ≤ 8.33 ms (120) |
| Draw calls | ≤ 500 |
| GC alloc/frame | 0 B gameplay |

### WebGL

| Metric | Target | Notes |
|--------|--------|-------|
| Frame time | ≤ 33.3 ms | Browser overhead |
| Draw calls | ≤ 100 | |
| Total memory | ≤ 128–256 MB | Tab crash threshold |
| Threads | Main thread heavy | No blocking sync loads |

---

## How to measure via MCP

See `unity-performance-audit-skill.md` Layer B.

| Metric | API / MCP source |
|--------|------------------|
| Draw calls | `UnityStats.drawCalls` |
| SetPass | `UnityStats.setPassCalls` |
| Triangles | `UnityStats.triangles` |
| Batches | `UnityStats.batches` |
| Memory | `Profiler.GetTotalAllocatedMemoryLong()` |
| Frame time | `Time.deltaTime` sampled in Play Mode |
| GC | `GC.GetTotalMemory` delta between frames |

**Pre-flight required** before Play Mode sampling.

---

## Static anti-patterns (severity)

Used by `/perf-audit` Layer A and gate `run_perf_static_scan`.

| Anti-pattern | Severity | Action |
|--------------|----------|--------|
| `GameObject.Find` in Update/FixedUpdate | **Critical** | Cache ref; **BLOCK implement** |
| `Camera.main` in hot path | **Critical** | Serialize/cache camera |
| String concat in Update | High | StringBuilder / cached string |
| LINQ in gameplay loop | High | for-loop / pool |
| `new` in Update | High | Pool / reuse |
| `SendMessage` | Medium | Direct call / interface |
| No pooling on frequent spawn | Medium | Object pool per spec |
| Public mutable fields | **Critical** | Standards — property / `[SerializeField] private` |

Intentional fixture: `V57/tools/lint/test/fixtures/BrokenCoinCollector.cs`.

---

## Traffic light vs budget

| value/budget ratio | Status |
|--------------------|--------|
| ≤ 1.0 | 🟢 PASS |
| ≤ 1.25 | 🟡 WARN — plan fix before ship |
| > 1.25 | 🔴 FAIL — block milestone sign-off |

---

## Pipeline integration

1. `/perf-budget` — define budgets in CONTEXT (autonomous write; summary shown)
2. `/spec implementa` — automatic static scan (`run_perf_static_scan`)
3. After `/scene-setup` — `/perf-audit` recommended
4. Milestone / pre-build — dynamic `/perf-audit` mandatory (future `/build-setup`)

---

*Last updated: 2026-06-10*
