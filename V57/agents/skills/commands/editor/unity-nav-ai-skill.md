# Unity Nav AI Skill (`/nav-ai`)

NavMesh surfaces/agents and simple AI wiring (patrol / chase hooks) from spec — beyond bake-only. Invoke when TDD needs pathfinding or AI agents; otherwise **silent skip** (no report).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-NAV applies |
| **Bake may also** | Be started from `/level-design` |

---

## Invocation

```bash
/nav-ai --scene Assets/_Game/Scenes/Main.unity
/nav-ai --bake
/nav-ai --spec @V57/specs/<slug>/features/enemy_ai.yaml
/nav-ai verify --scene {SCENE}
```

---

## Workflow

1. **Package:** `/packages ensure --for nav` (`com.unity.ai.navigation`). Do not invent versions — see `unity-package-management-skill.md`.
2. **Pre-flight assess:** list existing `NavMeshSurface` / `NavMeshAgent` / `NavMeshObstacle` / `NavMeshLink` / modifiers (CLI hierarchy or scene search) before changing.
3. Mark walkable geometry; create **NavMeshSurface**; bake (agent type matches Navigation Agents tab).
4. Add **NavMeshAgent** on AI actors; speed / stopping distance / area mask from spec — agent type must match surface.
5. Wire AI controller states (Idle/Patrol/Chase) to agent `SetDestination` (or click-to-move if TDD says so).
6. Optional: **NavMeshObstacle** (carve for dynamic blockers), **NavMeshLink** between disconnected regions, modifiers/costs.
7. If Rigidbody + agent: Rigidbody **Is Kinematic**.
8. **Verify:** Play → valid path to sample destination; agent not permanently off-mesh; bake visible; CLI OVR after save.
9. Save scene

### Report

`Docs/V57/reports/nav-ai-report-{slug}.md`

---

## Integration

`/game-setup` **G-NAV** when TDD needs pathfinding or AI agents. Behavior-tree depth stays in feature implement; this skill owns scene navigation wiring. Patterns adapted from unity-agent-plugin `initialize-ai-navigation`. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-09-10*
