# Unity Package Management Skill (`/packages`)

Add / remove / verify **UPM** packages without hand-editing `Packages/manifest.json` when an Editor is available. Adapted from [unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) `unity-package-management`. Complements CLI (which does **not** manage UPM).

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | Preferred: `Unity.RunCommand` / PackageManager Client |
| **prefer_unity_cli** | Live Editor `eval` for `Client.Add` poll; headless installer only when no GUI |
| **on_unavailable** | STOP for required packages; do not invent versions |
| **Selection table** | `shared/references/unity-package-select-reference.md` |

---

## Invocation

```bash
/packages list
/packages add com.unity.ai.navigation
/packages add com.unity.cinemachine com.unity.timeline
/packages ensure --for nav|audio|localization|input|test
/packages search com.unity.ai.navigation
/packages --dry-run
```

---

## Rules

1. Install **only** what TDD/CONTEXT/craft needs — prefer template-provided packages.
2. **Never invent version strings.** Resolve via registry (`https://packages.unity.com/<id>`) or `Client.Search`, or copy a version already in `manifest.json` / sibling packages.
3. Prefer **live Editor** path: MCP/`eval` → `UnityEditor.PackageManager.Client.Add` / `AddAndRemove`, poll until complete (async — do not busy-wait without `EditorApplication.update`).
4. **Do not** use `unity run … -quit` for installs — `-quit` exits before UPM finishes. Headless: Editor binary `-batchmode` **without** `-quit`, script calls `EditorApplication.Exit`.
5. After add: wait compile (`/compile-heal` / CLI `recompile_status`), confirm id in `manifest.json`.
6. Monetization packages (IAP, LevelPlay, UGS live ops): install only if product asks; integration stays out of V57 default crafts.

---

## `ensure` presets (V57)

| Flag | Ensures (if missing) |
|------|----------------------|
| `nav` | `com.unity.ai.navigation` |
| `input` | `com.unity.inputsystem` |
| `test` | `com.unity.test-framework` |
| `localization` | `com.unity.localization` |
| `cinemachine` | `com.unity.cinemachine` |
| `urp` | `com.unity.render-pipelines.universal` (only if CONTEXT locks URP and missing) |

---

## Integration

Call before **G-NAV**, **G-L10N**, or any craft that fails with missing package. `/game-setup` SETUP may run `/packages ensure` for known CONTEXT needs. Report optional: `Docs/V57/reports/packages-report.md` only when packages changed.

---

*Last updated: 2026-09-10*
