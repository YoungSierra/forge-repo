# Unity Intake Skill (`/intake`) — stages I0, I1, I2

Turns a provider delivery (docs + raw art/audio, **no Unity files**) into an open, configured Unity project with imported assets, materials, Visual prefabs and level scenes. Called by `/vertical-slice` and `/game-setup`; can run standalone.

| Item | Detail |
|------|--------|
| **Stages** | I0 INTAKE → I1 PROJECT → I2 ASSEMBLY |
| **Intake CLI** | `node V57/tools/intake/index.js --repo . [--out Docs/Generated] [--json]` (Node 22+, run `npm install` in `V57/tools/intake` once) |
| **Unity package** | `com.v57.assembly` = `file:../V57/tools/unity-assembly` (Editor asmdef `V57.Assembly.Editor`, runtime `V57.GoldPath`) |
| **Assembly entry** | `unity command eval "V57.Assembly.AssemblyRunner.RunAll()"` (warm Editor) or `-executeMethod V57.Assembly.AssemblyRunner.RunAllBatch` (batch) |
| **Outputs** | `Docs/Generated/*.yaml` + `Docs/Generated/json/*.json` (never hand-edited), `Docs/V57/INTAKE_REPORT.md`, `Docs/V57/reports/I1-project-<slug>.md`, `Docs/V57/reports/assembly-report.json` |
| **Knowledge** | `V57/knowledge/asset-intake.md`, `camera.md`, `unity-cli.md` |

---

## Invocation

```bash
/intake                 # I0 → I1 → I2, chained
/intake --only I0       # re-run intake after an absorption (mapping or import rule change)
/intake --only I2 --step BuildVisualPrefabs   # re-run one AssemblyRunner step
```

---

## I0 — INTAKE (gate A0)

1. On branch `v57/setup` (create from the provider's default branch if missing).
2. `node V57/tools/intake/index.js --repo .` — capture exit code.
3. Read `Docs/V57/INTAKE_REPORT.md`, then `Docs/Generated/json/package.json` (slug, perspective, physics, platform, slice scenes, engine pin).
4. Classify issues:

| Level | Meaning | Action |
|---|---|---|
| `blocking` | Cannot build a slice (no TDD, no §B mechanics, no slice scene, unreadable ADD…) | **Stop** — exit code 2. Write nothing else. |
| `fixable` | V57 can absorb it without touching provider files (naming, folder, import scale, texture naming) | Apply the **absorption policy** below |
| `missing` | Asset/doc absent | Continue; listed in `Docs/V57/MISSING_ASSETS.md` and TODO; the slot stays empty (no placeholder) |
| `conflict` | TDD vs ADD disagree | Continue; TDD wins; ADD fills gaps; logged `D-###` |

5. Write **`Docs/V57/MISSING_ASSETS.md`** — the single list of resources the slice needs but the delivery lacks. Sources: intake `missing` issues, `Docs/DELIVERY.md` / provider notes, TDD §13.1 inventory vs files on disk (meshes, LODs, textures, UI sprites per §9.1 screen, audio stems/SFX/stingers/VO), ADD `asset_briefs` without files. Table: asset · category · required by (TDD §, mechanic/screen) · expected path · impact (`blocks: gameplay | visual | audio | none`) · status (`missing | delivered`). Later stages append rows when they find a gap. Intake counts alone are not enough (they miss gaps the TDD inventory implies).
6. Gate **A0**: exit code `0` → continue. `2` → stop (`vertical-slice` §7).
7. Commit `V57 I0: intake` (Docs/Generated + INTAKE_REPORT + MISSING_ASSETS).

### Absorption policy (`fixable` only)

| Rule | Detail |
|---|---|
| Principle | V57 **never renames, moves, deletes or edits** provider files (`Assets/_Game/Art/**`, `Assets/_Game/Audio/**`, `Docs/Design/**`, `Docs/ArtDirection/**`) and never writes to `Source/`. Fixable issues are **absorbed**, not repaired in place |
| Log | one `D-###` per absorbed issue in `Docs/V57/DECISIONS.md` + row in INTAKE_REPORT "Absorbed issues"; commit the V57-side change `V57 absorb <issue-id>: <what> [D-###]` |
| How issues are absorbed | **Naming / wrong folder / UUID textures / L-R mismatch:** the `asset_manifest` mapping (intake resolves the real file path per `asset_id`; `issues[]` records the deviation) — V57 references files by their delivered path. **Scale, axis, Read/Write, normal-map convention:** V57 import rules (`.meta` importer settings, recorded in the rule table). **Junk** (ComfyUI temps, `copy`, `temp`, candidates): excluded from the manifest and ignored by import rules/material building (never referenced). |
| Not allowed | `git mv`/rename/move/delete of provider files; writing into `Source/`; editing mesh/texture content; re-exporting FBX; editing `Docs/Design/**` or ADD text |
| Never | Patch an asset problem in gameplay code (hiding renderers, runtime scale/rotation, runtime material swaps) — see `V57/knowledge/asset-intake.md` |
| Verify | Re-run `/intake --only I0` and I2: the issue stays listed as absorbed (mapping/import rule applied) or downgrades to `missing` (listed in `MISSING_ASSETS.md`) |

Anything that would need content editing or a provider-side rename is reported to the owner via INTAKE_REPORT (provider fix list) and handled as `missing` (listed in `MISSING_ASSETS.md`, slot left empty) or absorbed; never fixed by V57 in place and never faked with a placeholder.

---

## I1 — PROJECT (script-checked)

Open the repo as a Unity project with the pinned editor (`package.json → engine.unity_pinned`, currently **6000.6.2f1**).

| # | Baseline item | How |
|---|---|---|
| 1 | `ProjectSettings/ProjectVersion.txt` = pinned version | write if absent; never downgrade |
| 2 | First open / import | `Unity -batchmode -quit -projectPath . -logFile Logs/v57-i1.log`, then open the warm Editor; `unity pipeline install` (adds `com.unity.pipeline`) |
| 3 | Packages | `Packages/manifest.json`: `"com.v57.assembly": "file:../V57/tools/unity-assembly"`, URP, Input System, Test Framework (let Package Manager resolve versions for the pinned editor via `/packages`); add `"testables": ["com.v57.assembly"]` so GoldPath tests run |
| 4 | Render pipeline | URP asset + renderer at `Assets/_Game/Settings/Rendering/`; assigned in Graphics + all Quality levels |
| 5 | Color space | **Linear** |
| 6 | Asset serialization | **Force Text** |
| 7 | Version control | **Visible Meta Files** |
| 8 | Input handling | **Input System Package (New)** only |
| 9 | Tags / layers | **Owned by V57**: base set (`Player`, `Environment`, `Interactable`, `Hazard`, `Trigger`, `Projectile`, `UIWorld`) + per-game additions from `entities.json`; documented in `Docs/V57/CONTEXT.md`. Provider never defines them |
| 10 | Platform | orientation + reference resolution + target FPS from `package.json → platform`; Game View resolution set to `reference_resolution` for screenshots |
| 11 | Physics | 2D vs 3D from `package.json → physics` (never mix; see `V57/knowledge/verification.md`) |
| 12 | Folders | create V57-owned folders from brief §2 (`Assets/_Game/{Prefabs/Visual,Prefabs/Gameplay,Scenes,Scripts,Data,Settings,Tests/EditMode,Tests/PlayMode}`) |

**Gate (script check)** — one eval, all must be true, and 0 console errors:

```bash
unity command eval "return string.Join(';', new[]{
  \"linear=\" + (UnityEditor.PlayerSettings.colorSpace == UnityEngine.ColorSpace.Linear),
  \"forceText=\" + (UnityEditor.EditorSettings.serializationMode == UnityEditor.SerializationMode.ForceText),
  \"visibleMeta=\" + (UnityEditor.VersionControlSettings.mode == \"Visible Meta Files\"),
  \"urp=\" + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null),
  \"v57asm=\" + (System.Type.GetType(\"V57.Assembly.AssemblyRunner, V57.Assembly.Editor\") != null),
  \"compileFailed=\" + UnityEditor.EditorUtility.scriptCompilationFailed });"
```

Input handling and tags/layers are read back from `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 1`) and `TagManager.asset`. Write `Docs/V57/reports/I1-project-<slug>.md` (table: item, expected, actual, result). Commit `V57 I1: project baseline`.

Safe Mode (compile errors block packages): fix C# on disk, restart Editor, retry — never hand-edit scenes meanwhile.

---

## I2 — ASSEMBLY

```bash
unity command eval "V57.Assembly.AssemblyRunner.RunAll()"
# batch fallback (after 2 failed warm attempts):
Unity -batchmode -quit -projectPath . -executeMethod V57.Assembly.AssemblyRunner.RunAllBatch -logFile Logs/v57-assembly.log
```

`RunAll` runs, in order: `ApplyImportRules()` → `BuildMaterials()` → `BuildUiAtlases()` → `BuildVisualPrefabs()` → `BuildPlaceholders()` → `BuildLevelScenes()` → `WriteReport()` (placeholders first, so a missing blockout still yields a scene). It reads `Docs/Generated/json/*.json` only. **Placeholders are legacy:** prefabs from `BuildPlaceholders()` are never instanced in gameplay scenes and every one of them must appear in `MISSING_ASSETS.md` (pack follow-up: remove the step).

1. Poll `recompile_status` / import completion before reading results (`V57/knowledge/unity-cli.md`).
2. Read `Docs/V57/reports/assembly-report.json`.
3. **Gate:** 0 missing references, 0 errors. Warnings are listed in TODO.
4. On failure: fix the cause (import rule or manifest mapping), re-run **only** the failing step (`AssemblyRunner.<Step>()`), max 3 attempts per asset, then list it in `MISSING_ASSETS.md` + log (no placeholder).
5. Spot-check with screenshots of each level scene (end-of-frame Game View) — informational at I2, not a verdict.
6. Commit `V57 I2: assembly` → set `STATUS.last_green_commit`.

What I2 must **not** do: write gameplay code, hand-place level geometry that the blockout already defines, or re-decide camera values (they come from `camera.json`).

---

## STATUS updates

| After | `STATUS.json` (gate records always `{ "state", "evidence", "at_utc" }` — no other keys) |
|---|---|
| I0 | `gates.A0 = { "state": "passed" \| "blocked", "evidence": "Docs/V57/INTAKE_REPORT.md", "at_utc": … }`, `intake.exit_code`, `intake.issue_counts`; on exit 2 also `status: "blocked"`, `hard_stop: "blocking intake"` and print `HARD-STOP: blocking intake` |
| I1 | `gates.I1 = { "state": "passed", "evidence": "Docs/V57/reports/I1-project-<slug>.md", "at_utc": … }` |
| I2 | `gates.I2 = { "state": "passed", "evidence": "Docs/V57/reports/assembly-report.json", "at_utc": … }`, `last_green_commit`; `stage: "M1"`, `next: "M1 gold path"` |

---

*Last updated: 2026-09-28*
