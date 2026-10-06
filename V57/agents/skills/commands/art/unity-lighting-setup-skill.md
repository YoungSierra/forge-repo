# Unity Lighting Setup Skill (`/lighting-setup`)

Configure scene lighting for the locked render pipeline: lights, Light2D, Volumes, fog, skybox, and style grades (noir / stylized / realistic).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-LIGHT applies |
| **Depends on** | `/render-setup` preferred first |

---

## Invocation

```bash
/lighting-setup --scene Assets/_Game/Scenes/Main.unity
/lighting-setup --style noir
/lighting-setup verify --scene {SCENE}
```

---

## Workflow

1. Read `render_pipeline`, `dimension`, optional style from TDD/CONTEXT
2. Place lights under `_Lighting` container
3. **URP 3D:** Light + `UniversalAdditionalLightData` when required; URP Volume (exposure, bloom, color)
4. **HDRP:** Light + `HDAdditionalLightData`; HDRP Volume profile (exposure mandatory for readable scene)
5. **Built-in:** directional + ambient; Legacy post only if project uses it
6. **2D / URP 2D:** Light2D (global + optional point); ensure sprites receive light if expected
7. Skybox / HDRI / solid background per TDD § art
8. Style checklists:
   - **Noir:** low-key contrast, limited palette via Volume color grading
   - **Stylized:** flat/soft shadows, avoid photoreal over-bright HDR
9. Verify Game View not black / not blown-out white. From `/game-setup`, read the capture against the TDD (exposure, fog, grade). Visual miss = FAIL and re-run this craft (max 2).
10. Save scene

### Report

`Docs/V57/reports/lighting-setup-report-{slug}.md`

---

## Integration

`/game-setup` **G-LIGHT** when visuals need lights. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-07-30*
