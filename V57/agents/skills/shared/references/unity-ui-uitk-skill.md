# Unity UI Toolkit reference (V57)

**Source:** Adapted from [Unity-Technologies/skills — ui-uitk](https://github.com/Unity-Technologies/skills/tree/main/skills/ui-uitk) (MIT). Trimmed for GameForge agent use; full upstream includes Painter2D, manipulators, and binding refs.

**Used by:** `/game-ui`, `/ui-setup`, `unity-ui-patterns-skill.md`, Production **G-UI**.

---

## Stack policy

- **Runtime HUD / menus:** UI Toolkit (`UIDocument` + UXML + USS + PanelSettings) — mandatory unless TDD explicitly names uGUI.
- **Editor tools:** UITK `CreateGUI()` preferred over IMGUI for new windows.

---

## Folder layout (V57)

```text
Assets/UI/
├── Settings/PanelSettings.asset   # or {Prefix}PanelSettings.asset
├── Screens/{Screen}.uxml
└── Styles/{Screen}.uss
```

UXML links USS:

```xml
<Style src="project://database/Assets/UI/Styles/GameplayHud.uss" />
```

---

## Naming

| Item | Convention |
|------|------------|
| UXML `name` | camelCase (`scoreLabel`, `healthLabel`) |
| USS class | kebab-case (`.hud-rim`, `.stamina-bar`) |
| Screen files | PascalCase (`GameplayHud.uxml`, `{Screen}.uxml`) |

---

## USS restrictions (non-negotiable)

Unity USS is **not** full CSS. **Never use:**

| Forbidden | Use instead |
|-----------|-------------|
| `border` shorthand | `border-width`, `border-color` separately |
| `gap` | margin on children |
| `z-index` | DOM order / nesting |
| `pointer-events` | `picking-mode` on element |
| `box-shadow`, `filter`, `outline` | nested elements / Painter2D |
| `:nth-child`, attribute selectors | explicit classes |
| `linear-gradient()` in USS | Painter2D custom element |
| Inline `style="..."` in UXML | USS only |

External `url()` only as `project://database/Assets/...`.

---

## Production HUD quality (beyond “it compiles”)

When TDD §9 defines screens (HUD, overlays, prompts):

1. **State / mood rim** — full-screen border or overlay; opacity driven by TDD §9 metrics (not a debug `%` label alone).
2. **Meters** — track + fill; show/hide based on state per TDD.
3. **Interact prompt** — centered copy + optional hold progress bar (`width: N%` on fill child).
4. **Full-screen states** — win/lose/boot/pause panels from TDD §9; fade overlay with `MoveTowards` opacity in controller.
5. **Flash toasts** — short pickup messages with timed fade.
6. **Readability** — contrast for primary copy; secondary copy opacity per TDD (e.g. low-opacity metric hint).

When spec lists `Assets/UI/*.uxml`, generate **UXML + USS + controller** — not bare `Label` spam in one giant `BuildTree()` unless Stage F explicitly defers to G-UI asset pass.

---

## Validation

1. Write complete UXML/USS files (no partial files).
2. MCP read-back: `UIDocument.panelSettings` and `visualTreeAsset` **non-null**.
3. Play smoke: HUD visible; opacity/fill changes when state changes.

See `V57/docs/standards/GAME_FEEL_STANDARDS.md` § HUD.

---

## Generation scope

| Request | Output |
|---------|--------|
| USS only | `.uss` |
| Screen / HUD | `.uxml` + `.uss` |
| “with logic” / “functional” | + `.cs` controller |

“Proper buttons” = styled `Button` in UXML — **not** automatic C#.

---

*Upstream: Unity-Technologies/skills — ui, ui-uitk. Omitted: ui-ugui (legacy Canvas), optimize-web, monetization skills.*
