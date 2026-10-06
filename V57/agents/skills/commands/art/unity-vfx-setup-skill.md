# Unity VFX Setup Skill (`/vfx-setup`)

Wire gameplay juice: Particle System and/or VFX Graph with **SRP-compatible** shaders; pool vs one-shot; event triggers.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-VFX applies |

---

## Invocation

```bash
/vfx-setup --scene Assets/_Game/Scenes/SCN_<Level>.unity
/vfx-setup --spec @V57/specs/<slug>/features/coin_collectible.yaml
/vfx-setup verify --scene {SCENE}
```

---

## Workflow

1. Read spec `vfx:` / TDD juice rows
2. Prefer Particle System for simple bursts; VFX Graph when TDD/package requires GPU effects
3. Materials/shaders must match `/render-setup` pipeline (no HDRP graph on URP)
4. Visual prefab `Assets/_Game/Prefabs/Visual/VFX/PRF_<Asset>_Visual.prefab` (built by `AssemblyRunner` from provider `Assets/_Game/Art/VFX/<Asset>/`); pool if high frequency
5. Trigger from gameplay events (collect, hit, pulse) — no orphan systems
6. Verify: play trigger → renderer enabled / particle count &gt; 0 within timeout
7. FAIL if missing shader / pink particles

### Report

`Docs/V57/reports/M3-vfx-setup-{slug}.md`

---

## Integration

`/game-setup` **G-VFX** only when TDD/spec requires Particle System / VFX Graph / `vfx:` blocks. If not required: **silent skip** — do not invoke, do not write a report, do not emit progress.

---

*Last updated: 2026-07-30*
