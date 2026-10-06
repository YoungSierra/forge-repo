# Unity URP Post-Processing Skill (`/urp-postprocessing`)

Configure URP **Volume** post-processing (bloom, tonemapping, vignette, film grain, color grade) for atmosphere and juice. Uses **Unity MCP** (`Unity.RunCommand` / component property writes) — not hand-editing Volume assets as YAML.

**Source:** Adapted from [Unity-Technologies/skills — urp-postprocessing](https://github.com/Unity-Technologies/skills/tree/main/skills/urp-postprocessing) (MIT).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-POST or atmosphere craft applies |
| **Pairs with** | `/lighting-setup`, `/render-setup`, `AtmosphereDirector`-style runtime owners |

---

## Invocation

```bash
/urp-postprocessing --scene Assets/_Game/Scenes/Main.unity
/urp-postprocessing --recipe horror
/urp-postprocessing verify --scene {SCENE}
```

| Recipe | Use when |
|--------|----------|
| `horror` | TDD or CONTEXT declares dark / liminal / horror mood |
| `cinematic` | Neutral film look |
| `clean` | Mobile-safe minimal bloom |

---

## Pre-flight (MCP — block on FAIL)

1. Active pipeline is **URP** (`UniversalRenderPipeline.asset` non-null).
2. **HDR** enabled on URP asset (tonemapping / bloom).
3. Main camera: `UniversalAdditionalCameraData.renderPostProcessing = true`.
4. Global **Volume** on scene layer included in camera `volumeLayerMask`.
5. Profile has overrides with **`overrideState = true`** on every set parameter.

---

## Volume API (anti-hallucination)

| Wrong | Correct |
|-------|---------|
| `PostProcessVolume` | `Volume` |
| `PostProcessLayer` | `UniversalAdditionalCameraData.renderPostProcessing` |
| `profile.GetSetting<T>()` | `profile.TryGet<T>(out var t)` |
| Modify `sharedProfile` at runtime | Use `volume.profile` (instance) for Play tweaks |

Every `VolumeParameter` needs `overrideState = true` before assigning `value`.

---

## Recipes (starting points — tune to TDD literals)

**Horror / liminal** (starting point — tune to TDD atmosphere appendix):

- ColorAdjustments: postExposure −0.3…−0.5, saturation −20…−30
- Vignette: intensity ~0.22, smoothness ~0.4
- FilmGrain: Medium/Large, intensity 0.2–0.4
- Optional ChromaticAberration: lerp intensity with TDD-driven stress/state metric

**Cinematic:**

- Tonemapping ACES + Bloom threshold 0.9 intensity 0.5 + Vignette 0.3 + FilmGrain 0.2

**Clean / mobile:**

- Neutral tonemap + subtle bloom only; skip grain/motion blur.

---

## Runtime atmosphere (game state)

When a director script owns the Volume:

- **Lerp** grain/vignette/fog/end distance on state changes (`Time.deltaTime * 3f` typical).
- Do not snap binary on/off unless TDD requires it.

Hook camera once in `Awake`/`OnEnable`; cache `UniversalAdditionalCameraData`.

---

## Report

`Docs/V57/reports/urp-postprocessing-report-{slug}.md` — pre-flight table, recipe applied, Game View capture note, Overall PASS/FAIL.

---

## Integration

| Pipeline | Stage |
|----------|-------|
| `/game-setup` | **G-POST** when TDD names fog/grade/vignette/grain/atmosphere appendix |
| `/lighting-setup` | May delegate volume grade to this skill |

If TDD has atmosphere systems but this craft is skipped → **G-FEEL FAIL**.

---

*Omitted upstream skills: optimize-web, LevelPlay/IAP, Vivox, build-live-game — not required for V57 graybox/production feel.*
