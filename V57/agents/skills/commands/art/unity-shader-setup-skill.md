# Unity Shader Setup Skill (`/shader-setup`)

Create or assign Shader Graph / materials matching the **active** SRP. Replace pink/error shaders on critical renderers (Mesh, Sprite, LineRenderer, UI).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-SHADER applies |
| **Depends on** | `/render-setup` |

---

## Invocation

```bash
/shader-setup --scene Assets/_Game/Scenes/Main.unity
/shader-setup --fix-pink
/shader-setup --material Assets/Materials/AimArc.mat --shader URP/Lit
```

---

## Workflow

1. Confirm locked pipeline from CONTEXT
2. Scan scene/prefabs for missing/error shaders (magenta)
3. Assign pipeline-valid defaults:
   - URP: Lit / Unlit / Sprite-Lit-Default / Particles
   - HDRP: Lit / Unlit equivalents
   - Built-in: Standard / Sprites/Default
4. Shader Graph only when TDD needs custom look (toon, outline) — create under `Assets/Shaders/`
5. LineRenderer / Trail / UI critical paths must have non-null sharedMaterial
6. Stylized/noir: prefer materials consistent with `/lighting-setup` grade
7. **Custom URP Render Graph features:** if the project has `ScriptableRendererFeature` subclasses, run the checklist in `shared/references/unity-urp-render-graph-validate-skill.md` before PASS (material bind, PassData, descriptors, blit helpers).
8. Verify no pink in Game View capture
9. Save assets + scene

### Report

`Docs/V57/reports/shader-setup-report-{slug}.md`

---

## Integration

`/game-setup` **G-SHADER** when pink mats / custom look / critical renderers need attention (often after G-VIS). If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-09-10*
