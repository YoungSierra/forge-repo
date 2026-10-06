# Unity Camera Setup Skill (`/camera-setup`)

Main and overlay cameras: ortho vs perspective, follow/Cinemachine, pixel-perfect 2D, framing playable bounds.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-CAM applies |

---

## Invocation

```bash
/camera-setup --scene Assets/_Game/Scenes/Main.unity
/camera-setup --follow Player
/camera-setup verify --scene {SCENE}
```

---

## Workflow

1. Read `dimension` and camera notes from TDD **§11.5 In-play camera**
2. Place camera under `_Cameras` or `Player/CameraPivot` per TDD
3. **2D:** orthographic size frames playfield; optional Pixel Perfect Camera component if package present
4. **3D:** FOV, near/far clip; clear flags match skybox/solid
5. Follow / Cinemachine Virtual Camera when slice needs tracking — bind Follow/LookAt
6. **Game feel handoff:** framing only — **motion polish** (FPS lean/FOV/headbob, follow smooth damp, bank roll, etc.) belongs in locomotion/camera scripts + **`/game-feel`** per **§11.5 profile** (`GAME_FEEL_STANDARDS.md`). Do not implement instant snap on declared channels here.
7. Culling masks include gameplay layers; exclude unused
8. Verify playable bounds in frustum during OVR (move target to edge if needed). From `/game-setup`, read a Game View capture: subject framed per TDD §11.5. Miss = FAIL and re-run (max 2).
9. Save scene

### Report

`Docs/V57/reports/camera-setup-report-{slug}.md`

---

## Integration

`/game-setup` **G-CAM** when camera/follow/framing is in run scope. Cinematics camera shots also use `/cinematics` (Timeline). If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
