# Unity Build Skill (`/build-setup`)

Configure **Player Settings**, **Editor Build Settings** (scenes in build), and optionally run a **BuildPipeline** build — **only via Unity MCP**, with mandatory read-back verification.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` — see `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md` |
| **OVR helper** | `V57/agents/skills/shared/helpers/unity-editor-verification-skill.md` |
| **Platform matrix** | `V57/docs/performance/PLATFORM_MATRIX.md` |
| **Perf budgets** | `CONTEXT.md` → `targetPlatforms`, `performanceBudgets` (set via `/perf-budget`) |
| **Tool names** | Official Unity MCP only (`Unity.RunCommand`, `Unity.ManageEditor`, `Unity.ReadConsole`, …) — see `UNITY_MCP_REQUIREMENT.md` |

---

## Invocation

```bash
/build-setup --target StandaloneWindows64
/build-setup --target Android --scene Assets/_Game/Scenes/Main.unity
/build-setup --target WebGL --configure-only
/build-setup --target StandaloneWindows64 --output Builds/Win64/Game.exe
/build-setup --dry-run
```

| Flag | Behavior |
|------|----------|
| `--target <BuildTarget>` | Required unless `--dry-run`. Enum: `StandaloneWindows64`, `StandaloneOSX`, `StandaloneLinux64`, `Android`, `iOS`, `WebGL` |
| `--scene <path>` | Primary play scene to enable in build settings (repeatable; first = index 0) |
| `--output <path>` | Build output path for `BuildPipeline.BuildPlayer` via `Unity.RunCommand` (required when building) |
| `--configure-only` | PlayerSettings + build scenes only; skip the player build |
| `--development` | Pass `BuildOptions.Development` (development build) |
| `--dry-run` | Plan + invariant table only; no MCP writes |

Natural language: "configure Windows build", "set up Android build pipeline", "build for WebGL".

---

## Read order

1. `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md` — pre-flight
2. This skill
3. `CONTEXT.md` — `project_name`, `targetPlatforms`, `performanceBudgets`, main play scene path
4. `V57/docs/performance/PLATFORM_MATRIX.md` — platform id → `BuildTarget` mapping
5. `V57/docs/performance/PERFORMANCE_BUDGETS.md` — pre-build checklist (mobile/WebGL stricter)
6. Optional: latest `Docs/V57/reports/perf-report-*.md` — `/perf-audit` recommended before first build

---

## Platform resolution

Map `CONTEXT.md` `targetPlatforms[].id` to `--target` when the user omits it:

| Platform id | BuildTarget |
|-------------|-------------|
| `desktop_windows` | `StandaloneWindows64` |
| `desktop_mac` | `StandaloneOSX` |
| `mobile_android` | `Android` |
| `mobile_ios` | `iOS` |
| `webgl` | `WebGL` |
| `console_like` | Ask user — platform-specific |

Use `performanceBudgets.primaryPlatform` when multiple targets are declared.

---

## Workflow

### 1. Pre-flight MCP

Same as `UNITY_MCP_REQUIREMENT.md`. Minimum: `Unity MCP bridge Running`, `Unity.ReadConsole (Errors)` (zero errors). FAIL → **STOP**.

### 2. Plan (always, even without `--dry-run`)

Output table before Operate:

**A. Build configuration**

| Setting | Current (via `Unity.RunCommand` / `Unity.ManageEditor` read-back) | Planned |
|---------|------------------------------------------------------------------|---------|
| Product name | | From `CONTEXT.md` / `project_name` |
| Company name | | From `CONTEXT.md` or existing |
| Bundle version | | e.g. `0.1.0` — confirm with user if unset |
| Active build target | | `--target` |
| Scenes in build | | `--scene` or existing enabled scenes |
| Scripting backend | | IL2CPP on mobile (recommended); Mono/WebGL per matrix |
| Color space | | Linear if URP/HDRP |

**B. Pre-build gates** (from `PLATFORM_MATRIX.md`)

| Check | Required for target |
|-------|---------------------|
| `/perf-audit` recent PASS/WARN | Mobile, WebGL — **required**; desktop — recommended |
| Input maps cover platform | All |
| Texture presets (`/asset-pipeline`) | Mobile, WebGL |
| Playable scene exists and saved | All |

List **verification invariants** (section 4) derived from plan.

### 3. Operate (MCP writes)

Typical sequence (adjust per project):

1. **`Unity.ManageEditor` / `Unity.RunCommand`** — `productName`, `companyName`, `bundleVersion`, `runInBackground` as needed
2. **`Unity.RunCommand`** — platform-specific `PlayerSettings` not covered by ManageEditor:
   - `EditorUserBuildSettings.SwitchActiveBuildTarget`
   - `PlayerSettings.SetScriptingBackend` / API compatibility for target group
   - Android/iOS bundle id via `PlayerSettings.applicationIdentifier`
   - WebGL memory size, compression
3. **`Unity.RunCommand`** — `EditorBuildSettings.scenes`:
   - Enable `--scene` path(s) at index 0+
   - Disable or remove stale scenes not in plan
4. **`Unity.RunCommand`** — `AssetDatabase.SaveAssets` (+ refresh if settings scripts changed)
5. **Unless `--configure-only`:** **`Unity.RunCommand`** invoking `BuildPipeline.BuildPlayer` with `target`, `outputPath` (`--output`), enabled scenes, and optional `BuildOptions.Development`

Do **not** call legacy AnkleBreaker tools (`unity_build`, `unity_project_info`, `unity_settings_player`).

**Prohibited:** manual "File → Build Settings" checklists as the primary path when MCP is up.

### 4. Verify (read-back — mandatory)

Second pass — **read-only**:

| Invariant | How to verify |
|-----------|---------------|
| Active build target | `Unity.RunCommand` → `EditorUserBuildSettings.activeBuildTarget` matches `--target` |
| Scenes in build | `Unity.RunCommand` → `EditorBuildSettings.scenes` — expected paths enabled, index 0 = boot scene |
| Player settings | `Unity.ManageEditor` / `Unity.RunCommand` — productName, bundleVersion match plan |
| Compilation clean | `Unity.ReadConsole (Errors)` — zero errors |
| Build artifact (if built) | `BuildPipeline.BuildPlayer` result has no fatal errors; output exists at `outputPath` |
| Build size budget | If `performanceBudgets.maxBuildSizeMb` set in CONTEXT — compare reported build size |

Re-run Operate on any FAIL, then Verify again.

### 5. Console gate

`Unity.ReadConsole` — zero new errors after configure/build. FAIL → fix and repeat.

### 6. Report

Save `Docs/V57/reports/build-report-{target-slug}.md`:

```markdown
## Build setup report — {target}

**Overall:** PASS | FAIL
**Configure:** PASS | FAIL | SKIPPED (--configure-only)
**Build:** PASS | FAIL | SKIPPED

### Player settings (read-back)
| Field | Expected | Actual |
...

### Scenes in build
| Index | Path | Enabled |
...

### Build result (if run)
| Field | Value |
| success | |
| outputPath | |
| totalSize | |
| totalErrors | |

### Budget check
maxBuildSizeMb: {from CONTEXT or N/A}

### Notes
...
```

**DONE** only when report **Overall: PASS** and all listed invariants PASS.

### 7. CONTEXT.md update (optional, autonomous)

Update `performanceBudgets.maxBuildSizeMb` from actual build size — summary shown, then written in the same turn (`agents.yaml` `writes` policy).

---

## MCP tools reference

| Step | Tool |
|------|------|
| Pre-flight | Unity MCP bridge Running, `Unity.ReadConsole` (Errors) |
| Read project state | `Unity.RunCommand` / `Unity.ManageEditor` |
| Player settings write | `Unity.ManageEditor`, `Unity.RunCommand` |
| Build settings / switch target | `Unity.RunCommand` |
| Build | `Unity.RunCommand` → `BuildPipeline.BuildPlayer` |
| Console | `Unity.ReadConsole` |

---

## Relation to other commands

| Command | When |
|---------|------|
| `/perf-budget` | Before first `/build-setup` — declare `targetPlatforms` and budgets |
| `/perf-audit` | Recommended (required mobile/WebGL) before milestone build |
| `/scene-setup` | Play scene must exist and be wired before adding to build |
| `/game-setup` Stage BUILD | Delegates here after playable scene + optional perf audit |

---

## Prohibited

- Primary workflow as manual Editor build-settings steps when MCP is available
- DONE without read-back verification report
- Build with compilation errors or zero enabled scenes
- Skipping scene path confirmation when CONTEXT lists multiple candidates

---

*Last updated: 2026-07-22 — official Unity MCP (`Unity.RunCommand` / BuildPipeline).*
