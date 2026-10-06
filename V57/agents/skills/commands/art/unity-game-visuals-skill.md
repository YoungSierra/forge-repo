# Unity Game Visuals Skill (`/game-visuals`)

Integration pass so the playable content **renders correctly** for the declared dimension: cameras see content, sprites/meshes visible, materials non-null, optional reference-scene parity.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-VIS applies |
| **Pairs with** | `/render-setup`, `/lighting-setup`, `/shader-setup`, `/camera-setup` |

---

## Invocation

```bash
/game-visuals --scene Assets/_Game/Scenes/Main.unity
/game-visuals --reference Assets/Prototypes/Scenes/PT.unity
/game-visuals verify --scene {SCENE}
```

---

## Knowledge by dimension

### 2D

- Ortho camera frames playfield
- SpriteRenderer / Tilemap / Grid present and enabled
- Sorting Layers / Order consistent; no z-fighting with UI
- Sprite Atlas / PPU consistency (delegate import fixes to `/asset-pipeline`)
- No pink missing sprites

### 2.5D / hybrid

- Perspective or ortho as TDD states
- Sprite-in-3D / sorting groups / **parallax layers if in TDD**
- Frustum includes all playable layers
- **Full-bleed backdrop:** parallax/background layers must cover the Game View (no large letterbox bands from UV crop < ~full frame). FAIL if Play capture shows empty side/top bands where backdrop should be.

### 3D

- MeshRenderers with pipeline-valid materials
- Critical LineRenderer / Trail materials assigned
- Opaque/transparent queue sane
- Optional clone of lighting/visual stack from `--reference` playable scene

### Any

- Capture Game View; FAIL if empty/magenta dominant
- Player (or POV) visible in bounds
- When TDD lists Background/Midground parallax: verify instance exists under `_Environment` **and** fills the view

### Report

`Docs/V57/reports/game-visuals-report-{slug}.md`

---

## Integration

`/game-setup` **G-VIS** when playable content must render. `/playability-cert` G-FUNC-02 depends on this pass.

---

*Last updated: 2026-07-31*
