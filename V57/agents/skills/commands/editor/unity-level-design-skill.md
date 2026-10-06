# Unity Level Design Skill (`/level-design`)

Greybox and run-scope layout: blockers, spawns, triggers/volumes, checkpoints, hazards, collision layers, and NavMesh bake when needed so win/lose paths are **reachable**. **Production bar:** every gameplay-relevant volume is player-visible — greybox meshes/materials are fine, invisible bare colliders are not.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-LEVEL applies |
| **Production** | Gameplay prefab variants under `Assets/_Game/Prefabs/Gameplay/`; containers per `SCENE_PRODUCTION_STANDARDS.md`. Level geometry comes from the provider blockout `BLK_<Level>.fbx` placed by `AssemblyRunner.BuildLevelScenes()` — do not re-place it |

---

## Invocation

```bash
/level-design --scene Assets/_Game/Scenes/SCN_<Level>.unity
/level-design --spec @V57/specs/<slug>/features/run_loop.yaml
/level-design verify --scene {SCENE}
```

---

## Workflow

1. Read TDD run-scope content requirements (interactables, hazards, exits)
2. Place under `_Environment` / `_Gameplay` as appropriate
3. Spawn points come from blockout `Marker_Spawn_*` / `Marker_Checkpoint_NN` empties; gameplay components reference them via serialized fields (no name lookups at runtime)
4. Triggers for zones, fail volumes, win exits
5. **Visible renderers on gameplay volumes** — hazards/pickups/exits get a mesh or sprite (greybox or VFX proxy OK, swappable prefab); invisible bare colliders FAIL G-FUNC-04. **MCP read-back required:** for each hazard volume, log renderer type + enabled; collider-only = FAIL this stage (do not rely on later cert alone).
6. Physics layers / collision matrix for player vs hazard vs ground (document layer names). When TDD lists `Player ↔ X (trigger for Y detection)`, implement the trigger **and** the detection gate in gameplay code (not EventBus-only global windows).
7. NavMesh bake only when `/nav-ai` or agents need it (or bake here and hand off)
8. Min content gate: enough interactables/hazards for G-FUNC-03/04
9. Verify distances: player can reach key points in Play smoke
10. Save scene — **always write** `Docs/V57/reports/M2-level-design-{slug}.md` when this stage runs (missing report = incomplete stage)

### Report

`Docs/V57/reports/M2-level-design-{slug}.md` — include a table of each hazard/interactable with `visible_renderer: yes|no`.

---

## Integration

Pipeline stage **M2** (`/vertical-slice`, `/game-setup`). Reachability/softlock coverage is enforced by the gold path (M1+), `/playability-cert` G-FUNC-03/04 and `/qa` GQ-07 at **M4** (always enabled).

---

*Last updated: 2026-09-28*
