# Unity Cinematics Skill (`/cinematics`)

Timeline / Playable Director cutscenes: tracks, bindings, skip, and **handoff back to gameplay** (unlock input). Invoke when TDD/spec has cutscenes; otherwise **silent skip** (no report).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-CIN applies |
| **Pairs with** | `/camera-setup`, `/animation-setup`, `/audio-setup`, `/game-ui` (subtitles) |

---

## Invocation

```bash
/cinematics --scene Assets/_Game/Scenes/SCN_<Level>.unity
/cinematics --timeline Assets/Cinematics/Intro.playable
/cinematics --spec @V57/specs/<slug>/features/intro_cutscene.yaml
/cinematics verify --scene {SCENE}
```

---

## Workflow

1. Confirm Timeline package; create `.playable` under `Assets/Cinematics/`
2. Add PlayableDirector on a GO under `_Systems` or `_Cinematics`
3. Tracks as needed: Animation, Activation, Audio, Signal, Control, optional Subtitle via UI
4. Bind tracks to scene objects (never leave null bindings for required tracks)
5. **Gameplay handoff:**
   - On play: disable player input map / set cinematic flag
   - On stop / skip: restore input; re-enable player
   - Wire Skip action (UI button or Input) → `director.Stop()` + handoff
6. Optional QTE: Signal receivers → gameplay callbacks (only if TDD asks)
7. Verify in Play: Director plays; skip restores control; no softlock (`/playability-cert` cinematics branch; `/qa` GQ-09 at M4 — always enabled)
8. Save scene + assets

### Report

`Docs/V57/reports/M3-cinematics-{slug}.md`

---

## Integration

`/game-setup` **G-CIN** when TDD/spec has `cinematics:` / Timeline / cutscene. `/playability-cert` and `/qa` (M4, always enabled) must cover play/skip/handoff when in run scope. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
