# Physics 3D reference (V57)

Condensed from Unity Agent Plugin `physics-3d-collision`. Primary scope: **3D PhysX MonoBehaviour**.

## Critical facts

1. **Two kinematic triggers DO fire `OnTriggerEnter`.** Trigger Matrix ≠ Collision Matrix. If both are kinematic+trigger and callbacks miss, check Layer Collision Matrix, script placement, signature `OnTriggerEnter(Collider)`, and that kinematic bodies actually move (`MovePosition` / transform — not `velocity`).
2. **Ragdoll explodes frame 1:** overlapping colliders → shrink so none overlap at rest pose. Physics Debugger highlights overlaps.
3. **CharacterController** never gets `OnCollisionEnter` — use `OnControllerColliderHit`. Do not pair with a dynamic Rigidbody.
4. **`AddForce` after settle:** body slept → `Rigidbody.WakeUp()` before force.
5. **All physics frozen, raycasts work:** `Time.timeScale == 0`.
6. **Raycast misses:** origin inside collider; inactive GO / disabled Collider; triggers need `QueryTriggerInteraction.Collide` (or Project Settings → Queries Hit Triggers).
7. Prefer **Layer Collision Matrix** over long-lived `Physics.IgnoreLayerCollision` for design-time pairs.

## MeshCollider

- Convex mesh for dynamic Rigidbodies; non-convex mesh only as static geometry.
- Prefer primitives for gameplay movers when possible.

## 2D / DOTS

Out of primary scope — answer best-effort with API disclaimer, or hand to a dedicated skill if present.

## Verify via CLI

```bash
unity command eval "return UnityEngine.Physics.GetIgnoreLayerCollision(a,b);" --project-path <repo>
unity command find_gameobjects --project-path <repo>   # when available
```

*Upstream: Unity-Technologies/unity-agent-plugin — Companion License.*
