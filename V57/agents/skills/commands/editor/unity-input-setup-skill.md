# Unity Input Setup Skill (`/input-setup`)

Configure Unity Input System (or documented legacy): action maps, bindings, PlayerInput, and verify primary actions fire in Play.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-INPUT applies |

---

## Invocation

```bash
/input-setup
/input-setup --asset Assets/_Game/Settings/Input/<Slug>Input.inputactions
/input-setup --spec @V57/specs/<slug>/features/player_movement.yaml
/input-setup verify
```

Input Action asset extension: `.inputactions`. Actions and bindings come from `Docs/Generated/json/input_map.json` (TDD §11.3) — do not invent action names; the gold path drives these exact names.

---

## Workflow

1. Read `input_system` from CONTEXT (`new` | `old` | `hybrid`)
2. For **new**: create/update Input Action asset; maps (Player, UI, Dialogue); actions from TDD
3. Generate C# wrapper if project convention uses it
4. Wire `PlayerInput` or injected `InputAction` references on player/UI
5. Rebind hooks only if TDD/accessibility requires (coordinate `/accessibility`)
6. Verify: Play Mode → simulated device press, then release on a **later frame** → observable effect (evidence for the gold path, `/playability-cert` G-FUNC-01 and `/qa` GQ-02 at M4). Restore real devices in `finally` — see `V57/knowledge/unity-cli.md`
7. FAIL if primary actions unbound or listening to wrong map

### Report

`Docs/V57/reports/M2-input-setup-{slug}.md`

---

## Integration

Pipeline stage **I1** (baseline: Input System active) and **M1** (actions needed by the gold path); craft polish at **M2** when Input System is `new` and run scope has player/UI actions. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-09-28*
