# Unity Game Feel Skill (`/game-feel`)

Polish **player-facing feel** for production scenes: smoothed camera/locomotion (per **TDD §11.5 profile**), HUD motion, atmosphere lerps, and code-driven feedback — without a full VFX pipeline.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-FEEL applies |
| **Canonical doc** | `V57/docs/standards/GAME_FEEL_STANDARDS.md` |
| **Presentation source** | Active TDD only — §1, §8, §9, atmosphere appendix, §11.5 |

---

## Invocation

```bash
/game-feel --scene Assets/_Game/Scenes/Main.unity
/game-feel --spec @V57/specs/<slug>/features/wanderer_locomotion.yaml
/game-feel verify --scene {SCENE}
```

---

## When to run

| Trigger | Action |
|---------|--------|
| `/game-setup` **G-FEEL** | **Mandatory** when run scope includes §11.5 camera motion, §9 HUD, atmosphere, or quantified locomotion presentation |
| After **G-CAM** / **G-UI** | Reconcile camera + HUD with feel rules |
| Spec implement touched locomotion/HUD | Run targeted verify before marking spec done |

---

## Workflow

1. Read TDD **§11.5 In-play camera**, control mode, locomotion §B blocks, §9 UI, §8, atmosphere appendix.
2. **Classify camera profile** (`GAME_FEEL_STANDARDS.md` Step 0): FPS look / follow-vertical / fixed / 2D / auto.
3. Implement **only** what the TDD quantifies — no web search, genre templates, or FPS checks on non-FPS profiles.
4. Locate production scripts under `Assets/_Game/Scripts/**` (locomotion, camera follow, HUD, atmosphere).
5. **Camera / locomotion audit (profile branch):**
   - **FPS profile:** lean roll/offset, crouch eye height, headbob, sprint FOV — all lerped; **FAIL** on instant snap.
   - **Follow / vertical / side profile:** smooth damp or lerp on follow axes, ortho/framing, bank roll on steer — **FAIL** on teleport follow; **do not** require Q/E lean.
   - **Fixed / auto:** only TDD-quantified motion (shake, blend).
6. **HUD audit** (`/game-ui` wires structure — this stage verifies *motion*):
   - Rim/meter opacity, overlays, fades per TDD §9.
7. **Atmosphere:** lerp grade/grain/fog per TDD — not binary snap.
8. **Code juice:** telegraphs (ring, scale pulse, emission) while interaction windows open.
9. MCP Play smoke ≥15 s: exercise **declared** channels only (e.g. steer bank for jumper; lean Q/E for FPS).
10. Write report with profile + gate table.

### Report

`Docs/V57/reports/game-feel-report-{slug}.md`:

```markdown
# Game feel report
- Scene: {SCENE}
- §11.5 profile: FPS | follow-vertical | fixed | 2D | auto | mixed
- TDD sections applied: §8 / §9 / §11.5 / atmosphere (list)
- TDD gaps noted (if any): …
- G-FEEL-CAM-01: PASS | FAIL | n/a
- G-FEEL-CAM-02: PASS | FAIL | n/a
- G-FEEL-CAM-03: PASS | FAIL | n/a
- G-FEEL-CAM-04: PASS | FAIL | n/a
- G-FEEL-HUD-01: PASS | FAIL | n/a
- G-FEEL-ATM-01: PASS | FAIL | n/a
- Overall: PASS | FAIL
```

---

## Integration

`/game-setup` **G-FEEL** runs **after** applicable G-CAM / G-UI crafts, **before** G-FUNC. FAIL blocks CHECKLIST (max 2 fix cycles).

Pairs with `/camera-setup`, `/game-ui`, `/lighting-setup`, `/urp-postprocessing`, `/audio-setup`.

---

*Complements Unity-Technologies/skills urp-postprocessing (adapted in `unity-urp-postprocessing-skill.md`).*
