# Unity Physics Setup Skill (`/physics-setup`)

Wire **3D PhysX** or **2D Box2D** (per `package.json → physics` — never both) collision/trigger layers from TDD/spec: Layer Collision Matrix, Rigidbody/Collider setup on Gameplay prefabs, and diagnostics when callbacks miss. Adapted from [Unity-Technologies/unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) `physics-3d-collision` (Companion License) for V57 OVR + CLI.

| Item | Detail |
|------|--------|
| **Stage** | M1 (minimum for the core loop), M2 (full) |
| **requires** | Unity CLI Pipeline (writes + verify); `unity mcp` optional |
| **OVR helper** | `unity-editor-verification-skill.md` |
| **Reference** | `shared/references/unity-physics-3d-reference.md` |
| **Lessons** | `V57/knowledge/verification.md` |

---

## Invocation

```bash
/physics-setup --scene Assets/_Game/Scenes/SCN_<Level>.unity
/physics-setup --spec @V57/specs/<slug>/features/<spec>.yaml
/physics-setup diagnose "OnTriggerEnter not firing"
/physics-setup verify --scene {SCENE}
```

---

## When to run

TDD/spec lists collision matrix, layers, triggers, Rigidbody locomotion, CharacterController, or proximity detection. Otherwise skip silently.

---

## Workflow (setup)

1. **Pre-flight** — CLI reachable, compile complete.
2. **Read** TDD physics tables, `package.json → physics` (2d|3d), layers in `Docs/V57/CONTEXT.md` (layers are V57-owned, set at I1).
3. **Operate (Edit Mode):**
   - Layer Collision Matrix: only intended pairs collide/trigger.
   - Colliders: from Visual prefab `UCX_*` (convex) or primitive colliders sized from the brief `size_m`; exact mesh colliders only when `collision: exact` (import rule sets Read/Write).
   - Rigidbody/CharacterController on the **Gameplay prefab variant** — never both CharacterController and non-kinematic Rigidbody on one mover.
   - Triggers: `isTrigger = true` + a Rigidbody on at least one side.
   - Kinematic movers: `MovePosition`/`MoveRotation`; do not assign `velocity` on kinematic bodies.
   - Fast bodies: continuous collision detection per TDD.
4. **Verify:** read-back of colliders/rigidbodies/layers; matrix flags for named pairs; 0 new console errors.
5. **Prove behaviour with real physics:** PlayMode tests with real `FixedUpdate` stepping and real input (see Proof rules). Gold path re-run.
6. **Report:** `Docs/V57/reports/M2-physics-setup-<slug>.md`.

---

## Proof rules (physics must be real)

| Banned | Use instead |
|---|---|
| `Physics.Simulate` / `Physics2D.Simulate` to prove callbacks or trajectories (Edit or Play) | Play Mode, `yield return new WaitForFixedUpdate()` loops, real time |
| `detectCollisions = false`, disabling colliders, moving objects to other layers during a test | Keep the production setup; assert on contacts/events |
| Clamping/teleporting positions or velocities to "keep the ball on the table" | Fix colliders, CCD, materials, or the Visual prefab |
| Bounds-box colliders replacing provider meshes because they were not Read/Write | Fix the import rule (Read/Write) or use `UCX_*` from the provider |
| Hand-tuned heuristics that duplicate trigger zones in code (e.g. drain Z threshold) | Use the trigger volume from the blockout marker |

---

## Diagnose mode

Match symptoms against **CRITICAL FACTS** in `shared/references/unity-physics-3d-reference.md` (kinematic × kinematic triggers do fire `OnTriggerEnter`; overlapping colliders explode ragdolls; sleep/`WakeUp`; CharacterController → `OnControllerColliderHit`; raycasts need `QueryTriggerInteraction`). Apply the fix in Edit Mode or script; re-verify in Play Mode. Max 5 investigative tool calls, then answer with the best diagnosis.

---

## Integration

`/vertical-slice` and `/game-setup` M1/M2 when TDD/spec needs collision setup. `/fix-*` and `/qa` may invoke diagnose mode.

---

*Last updated: 2026-09-28*
