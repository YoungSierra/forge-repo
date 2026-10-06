# Unity UI Toolkit Skill (`/ui-setup`)

Create and wire **UI Toolkit** screens (UXML, USS, PanelSettings, UIDocument) via Unity MCP with OVR verification.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **OVR helper** | `unity-editor-verification-skill.md` |
| **Patterns** | `unity-ui-patterns-skill.md` |
| **Master doc** | `EDITOR_WORKFLOW.md` |

---

## Invocation

```bash
/ui-setup --screen Hud --uxml Assets/UI/Hud.uxml --scene Assets/_Game/Scenes/Main.unity
/ui-setup --spec @V57/specs/<slug>/features/game_hud.yaml
/ui-setup verify --uxml Assets/UI/Hud.uxml --scene Assets/_Game/Scenes/Main.unity
/ui-setup --dry-run
```

| Flag | Behavior |
|------|----------|
| `--screen` | Logical screen name (HUD, MainMenu, Pause) |
| `--uxml` / `--uss` | Asset paths |
| `--scene` | Scene to place `UIDocument` |
| `--spec` | Read optional `ui:` block from spec |
| `verify` | Read-back only (VisualElement queries) |
| `--dry-run` | Plan + element inventory |

---

## Workflow

### 1. Pre-flight + read spec

From spec `ui:` block or TDD: screens, element names, events, data bindings.

### 2. Operate

1. Write UXML/USS to disk under `Assets/UI/` (workspace or MCP file write + `AssetDatabase.Refresh`)
2. Create `PanelSettings` asset if missing (`Create → UI Toolkit → Panel Settings`)
3. `Unity.RunCommand` / `unity_component_set_reference`: add `UIDocument` GO to scene, assign `sourceAsset` + `panelSettings`
4. Wire C# controller from spec (`GameHud`, etc.) — `[SerializeField] private UIDocument _uiDocument`
5. Save scene

### 3. Verify (read-back) — hard fail if unwired

```csharp
var doc = GameObject.Find("HUD")?.GetComponent<UIDocument>();
if (doc == null) result.Log("FAIL: UIDocument missing");
else if (doc.panelSettings == null) result.Log("FAIL: panelSettings null");
else if (doc.visualTreeAsset == null) result.Log("FAIL: visualTreeAsset null");
else result.Log("PASS: UIDocument wired " + doc.panelSettings.name);

var root = doc?.rootVisualElement;
if (root?.Q<Label>("coins-text") == null) result.Log("FAIL: coins-text missing");
else result.Log("PASS: coins-text");
```

**Overall PASS is forbidden** if `panelSettings` or `visualTreeAsset` is null after save. Asset-on-disk without the scene reference = FAIL.

Verify USS classes applied, hidden panels (`display: none` / `is-hidden`), safe area if specified.

### 4. Report

`Docs/V57/reports/ui-setup-report-{screen}.md` — OVR format.

---

## Binding pattern (required)

- **UXML:** layout + element `name=` only — no game logic
- **USS:** styles, themes, responsive breakpoints
- **C# controller:** one class per screen; subscribe in `OnEnable`, query elements once, update via public methods/events
- **No** business logic in UXML bindings beyond simple labels

---

## When uGUI still applies

**Only when the TDD explicitly specifies it** — uGUI/Canvas named in the TDD, or a TDD-required world-space UI / third-party package that Toolkit cannot fulfill. Quote the TDD line in the implement report. Everything else (screen HUD, menus, overlays): **UI Toolkit, no exceptions**.

---

## Game-facing UI

For HUD, results, VN dialogue, and run-scope feedback tables, prefer **`/game-ui`** (UI Toolkit stack policy + Play visibility). Use this `/ui-setup` skill for low-level UXML/USS/PanelSettings plumbing when `/game-ui` delegates here.

---

*Last updated: 2026-07-30*
