# Unity Animation Skill (`/animation-setup`)

Create **AnimatorController** assets, states, transitions, and animation event wiring via Unity MCP with OVR. Supports **3D Animator** and **2D** sprite flipbook / 2D Animation workflows.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **OVR helper** | `unity-editor-verification-skill.md` |
| **Task subagent** | When G-ANIM applies from `/game-setup` / `/prototype` |

---

## Invocation

```bash
/animation-setup --controller Assets/Animation/Player/Player.controller
/animation-setup --spec @V57/specs/<slug>/features/player_movement.yaml
/animation-setup --mode 2d --controller Assets/Animation/Player/Player2D.controller
/animation-setup verify --controller Assets/Animation/Player/Player.controller
```

---

## Workflow

### 1. Plan from spec `animation:` block

Parameters (float/bool/trigger), states, transitions, blend trees, clip assignments (when clips exist). Read `dimension` from CONTEXT.

### 2. Operate — 3D

```csharp
var controller = AnimatorController.CreateAnimatorControllerAtPath(
    "Assets/Animation/Player/Player.controller");
// Add parameters, states, transitions via AnimatorController APIs
AssetDatabase.SaveAssets();
```

Wire `Animator` on prefab/scene; assign controller. Avatar masks when layered. Animation Events → public methods on spec MonoBehaviour (name must match).

### 3. Operate — 2D

- SpriteRenderer + Animator with sprite-swap / 2D Animation package when present
- Flipbook: one state per clip; transitions on MoveX/Speed/triggers from TDD
- Face-flip via scale.x or SpriteRenderer.flipX from movement code — document convention
- FAIL if animated actor has null `runtimeAnimatorController`

### 4. Verify

- Controller asset loads
- Parameter names/types match spec
- Required states exist; no orphan states without transitions (warn)
- Default state correct
- Prefab/scene Animator assigned — **FAIL if player/actor unwired**
- Optional Play Mode: trigger → state name changes

Report: `Docs/V57/reports/animation-setup-report-{slug}.md`

---

## Scope

V57 **does not create** raw animation clips from scratch. Uses only existing `.anim`/FBX/sprite clips from `/asset-pipeline`. Configures controller + wiring only.

---

## Integration

`/game-setup` **G-ANIM** when TDD/spec has `animation:` or animated actors. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
