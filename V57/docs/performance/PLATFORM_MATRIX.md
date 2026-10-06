# Platform matrix — V57 reference

Target platform matrix for `/build-setup`, `/perf-budget`, and content validation.

| Item | Detail |
|------|--------|
| **Build skill** | `/build-setup` |
| **Perf budgets** | `PERFORMANCE_BUDGETS.md` |
| **Context** | `CONTEXT.md` → `targetPlatforms` |

---

## Summary matrix

| Platform id | Input | Typical resolution | Perf doc section | Build target |
|-------------|-------|-------------------|------------------|--------------|
| `desktop_windows` | KBM, gamepad | 1920×1080 | Desktop | `StandaloneWindows64` |
| `desktop_mac` | KBM, gamepad | 1920×1080 | Desktop | `StandaloneOSX` |
| `mobile_android` | Touch | Flexible / safe area | Mobile | `Android` |
| `mobile_ios` | Touch | Flexible / safe area | Mobile | `iOS` |
| `webgl` | KBM, touch | Browser canvas | WebGL | `WebGL` |
| `console_like` | Gamepad | 4K/HDR optional | Console-like | platform-specific |

---

## Per-platform checklist (pre-build)

| Check | Desktop | Mobile | WebGL |
|-------|---------|--------|-------|
| Input maps cover platform | ✓ | Touch bindings | KBM + touch |
| Quality tier assigned | ✓ | ✓ | Low default |
| Texture presets (`/asset-pipeline`) | BC | ASTC/ETC2 | DXT/limited |
| IL2CPP | Optional | Recommended | N/A |
| `/perf-audit` dynamic | Recommended | **Required** | **Required** |
| Max build size budget | Optional | Store limits | Hosting limits |

---

## Platform specifics

### Mobile
- Thermal throttling — target 60 or 30 FPS explicit in CONTEXT
- Battery — reduce overdraw; `/perf-audit` draw calls strict

### WebGL
- Memory ceiling low — asset audit mandatory
- Threading limited — no blocking loads on main thread

### Desktop
- Higher draw/triangle budgets
- Optional dedicated server build documented separately if multiplayer

---

## Declaration in CONTEXT.md

Use `/perf-budget` to write `targetPlatforms` and `performanceBudgets.primaryPlatform`. `/build-setup` reads both before configure.

---

*Last updated: 2026-06-10*
