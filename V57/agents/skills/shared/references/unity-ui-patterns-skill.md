# Unity UI Patterns (reference)

Conventions for UI Toolkit in V57 consumer projects. **Genre- and title-agnostic** — replace `{Screen}` / `{Prefix}` from CONTEXT/TDD.

| Item | Detail |
|------|--------|
| **Skill** | `/ui-setup` |
| **Folders** | `Assets/UI/{Screens,Styles,Settings}/` |

---

## Naming

| Asset | Pattern | Example |
|-------|---------|---------|
| UXML | `{Screen}.uxml` PascalCase screen | `GameplayHud.uxml`, `MainMenu.uxml` |
| USS | `{Screen}.uss` or `Theme.uss` | `GameplayHud.uss` |
| PanelSettings | `PanelSettings.asset` or `{Prefix}PanelSettings.asset` | `PanelSettings.asset` |
| Scene GO | Screen name | `HUD`, `MainMenu` |
| Element `name` | kebab-case | `score-label`, `pause-panel` |
| USS class | kebab-case | `hud-label`, `top-bar` |

---

## Structure

```text
Assets/UI/
├── Settings/
│   └── PanelSettings.asset
├── Screens/
│   └── GameplayHud.uxml
└── Styles/
    └── GameplayHud.uss
```

UXML references USS:

```xml
<Style src="project://database/Assets/UI/Styles/GameplayHud.uss" />
```

---

## Responsive / safe area

- Use USS flex + `%` / `vw`/`vh` where supported
- Safe area: controller reads `Screen.safeArea` and applies padding to root in `OnEnable`
- Test target aspects in Game view before sign-off

---

## Spec block (`ui:`)

See `feature_spec_template.yaml`. Each screen lists elements, events, and verification queries for OVR.

**Production polish:** see `unity-ui-uitk-skill.md` — HUD must meet TDD §9 presentation (rim, meters, overlays), not debug labels only.

---

*Last updated: 2026-07-31*
