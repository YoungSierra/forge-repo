# Unity Render Setup Skill (`/render-setup`)

Lock and verify **one** render pipeline (URP / HDRP / Built-in) from `CONTEXT.md` / TDD §A. Prevent mixed pipelines and pink materials.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | Required from `/game-setup` when Assets/ exists |
| **OVR** | `unity-editor-verification-skill.md` |

---

## Invocation

```bash
/render-setup
/render-setup --verify-only
/render-setup --pipeline urp
```

---

## Workflow

1. Read `render_pipeline` and `dimension` from CONTEXT/TDD (`urp` | `hdrp` | `built-in`; `2d` | `2.5d` | `3d`)
2. Verify package present (`com.unity.render-pipelines.universal` / `high-definition` or Built-in only) — if missing URP and CONTEXT locks URP: `/packages ensure --for urp`
3. Verify Graphics Settings / Quality use the matching Pipeline Asset
4. If `dimension` is 2D and URP: prefer URP **2D Renderer** when TDD expects 2D lights/sprites
5. Scan open scene / project sample for magenta/error shaders → list offenders; hand to `/shader-setup` if needed
6. **FAIL** if both URP and HDRP assets are active or materials mix pipelines

### Report

`Docs/V57/reports/render-setup-report-{slug}.md` — Overall PASS/FAIL; pipeline locked; pink list.

---

## Integration

`/game-setup` **G-RENDER** always when `Assets/` exists (pipeline lock). Silent skip only for template-only pack with no Unity project — no report in that case.

---

*Last updated: 2026-07-30*
