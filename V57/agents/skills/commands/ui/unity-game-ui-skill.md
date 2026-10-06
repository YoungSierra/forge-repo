# Unity Game UI Skill (`/game-ui`)

Game-facing UI specialist: HUD, menus, results, **visual-novel dialogue/choices**, overlays. **Stack policy: UI Toolkit always** (UXML + USS + PanelSettings + `UIDocument`); uGUI Canvas is allowed **only when the TDD explicitly specifies it** (declared uGUI/Canvas stack, or a TDD-required world-space UI that Toolkit cannot fulfill — quote the TDD line in the report). Must be **visible in Play**. **Production bar:** debug-only IMGUI is never the shipped HUD.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-UI applies |
| **Low-level Toolkit** | `unity-ui-patterns-skill.md` + **`unity-ui-uitk-skill.md`** (USS rules + production HUD polish) |
| **OVR** | `unity-editor-verification-skill.md` |

---

## Invocation

```bash
/game-ui --scene Assets/_Game/Scenes/Main.unity
/game-ui --spec @V57/specs/<slug>/features/game_hud.yaml
/game-ui --screen Hud
/game-ui verify --scene {SCENE}
```

---

## Workflow

1. Collect UI needs from TDD feedback table (§9-style), spec `ui:`, and run-scope mechanics
2. Choose stack (strict):
   - **UI Toolkit** — **mandatory** for all screen HUD/menus/overlays. Build UXML + USS under `Assets/UI/` per `unity-ui-uitk-skill.md` (state-driven rim, meters, overlay fades per TDD §9). PanelSettings + `UIDocument` in scene; controllers query elements and subscribe to EventBus.
   - **uGUI Canvas** — **only if the TDD explicitly specifies it** (uGUI/Canvas named in §A/§9/§11, or TDD-required world-space UI). Quote the exact TDD line in the report; without that quote, uGUI = FAIL.
   - **IMGUI** — debug/diagnostic overlays **only**; never the shipped HUD and never the way to pass playability-cert. If time is short, ship a minimal **UI Toolkit** HUD with placeholder styling (swappable) instead
3. **Registry coverage (production bar):** every TDD §9 / feedback-registry element consumed by an implemented mechanic must exist as a real **UI Toolkit** element **or world-anchored visual** (timing rings, aim arcs, trails, telegraph cues as scene renderers/VFX) — **no silent omissions and no “Deferred — cosmetic” PASS**. List any N/A only with a TDD quote proving it is out of run scope. Any run-scope registry id that telegraphs a timing/interaction window on a world object must have a **visible cue while that window is active**; missing cue = **FAIL**.
4. Implement screens: Hud, Pause, Results, Dialogue (speaker, body, advance, choices)
5. Wire controllers + EventBus / bindings per spec
6. Safe area / readable contrast
7. **Hard wiring OVR (required — no false PASS):** via MCP read-back on the scene `UIDocument` **after save**:
   - `panelSettings != null` (assigned asset path logged)
   - `visualTreeAsset` / `sourceAsset != null`
   - If either is null → **FAIL** the stage (do not write Overall PASS). Creating `PanelSettings.asset` on disk alone is **not** sufficient.
   - Never mark `PanelSettings assigned | PASS` unless the read-back of the **component field** is non-null.
8. **World timing / telegraph cues:** if the TDD feedback registry lists a world-anchored timing or telegraph cue for an interactable (ring, flash, scale pulse, highlight, etc.), verify in Edit/Play that a visible renderer appears while the interaction window is open. Absence = **FAIL**. Do not hard-code genre- or title-specific ids — match whatever ids the active TDD registry declares.
9. Verify element queries in Edit **and** in Play via `/playability-cert` (G-FUNC-01/G-FUNC-06 HUD visibility)
10. Never defer run-scope UI to Slice-Alpha — scope labels (run selection, legacy `sliceScope`) never lower quality

### VN / narrative

- Advance input mapped via `/input-setup`
- Choice buttons branch without softlock on last line
- Optional typewriter — not required unless TDD says so

### Report

`Docs/V57/reports/game-ui-report-{slug}.md` — must include the PanelSettings read-back row with the **actual** assigned path or `null` (FAIL). Registry rows may not say Deferred/PASS for run-scope items.

---

## Integration

`/game-setup` **G-UI**. Prefer this over raw `/ui-setup` for game HUD/feedback. `/ui-setup` remains Toolkit plumbing.

---

*Last updated: 2026-07-31*
