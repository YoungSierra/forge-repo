# Unity Asset Pipeline Skill (`/asset-pipeline`)

Thin wrapper over **`com.v57.assembly`**. Import rules, materials, skybox, UI atlases, animator controllers, Visual prefabs and level scenes are produced by `V57.Assembly.AssemblyRunner`, not by hand-written per-game scripts or MCP snippets. This skill runs, audits and re-runs those steps.

| Item | Detail |
|------|--------|
| **Stage** | I2 (full run via `/intake`); re-run of single steps at any later stage |
| **requires** | Unity CLI Pipeline reachable (`unity pipeline list`); `com.v57.assembly` in `Packages/manifest.json` |
| **Inputs** | `Docs/Generated/json/*.json` (asset_manifest, ui, scenes, layouts, camera, rendering) |
| **Output** | `Docs/V57/reports/assembly-report.json` |
| **Rules** | `shared/references/unity-import-skill.md`; lessons `V57/knowledge/asset-intake.md` |

---

## Invocation

```bash
/asset-pipeline run                       # AssemblyRunner.RunAll()
/asset-pipeline run --step BuildMaterials # one step
/asset-pipeline audit                     # read assembly-report.json + re-check import settings
```

| Step | Entry point |
|---|---|
| all | `unity command eval "V57.Assembly.AssemblyRunner.RunAll()"` |
| all (batch fallback) | `Unity -batchmode -quit -projectPath . -executeMethod V57.Assembly.AssemblyRunner.RunAllBatch -logFile Logs/v57-assembly.log` |
| import rules | `V57.Assembly.AssemblyRunner.ApplyImportRules()` |
| materials (textures + layout manifest values) | `V57.Assembly.AssemblyRunner.BuildMaterials()` |
| skybox | `V57.Assembly.AssemblyRunner.BuildSkybox()` |
| UI atlases | `V57.Assembly.AssemblyRunner.BuildUiAtlases()` |
| animator controllers | `V57.Assembly.AssemblyRunner.BuildAnimators()` |
| Visual prefabs | `V57.Assembly.AssemblyRunner.BuildVisualPrefabs()` |
| level scenes (blockout or LevelMaps layout) | `V57.Assembly.AssemblyRunner.BuildLevelScenes()` |
| hierarchy diff (M2) | `V57.Assembly.AssemblyRunner.CaptureHierarchyDiff()` |
| report | `V57.Assembly.AssemblyRunner.WriteReport()` |

---

## Workflow

1. Pre-flight: CLI reachable, not Safe Mode, `recompile_status` complete.
2. Run the step(s); poll import/compile completion before reading results.
3. Read `assembly-report.json`: missing refs, errors, per-asset issues.
4. Fix causes, in this order: `asset_manifest` mapping (`/intake`, logged `D-###`) → import rule → re-run step. Max 3 attempts per asset, then leave the slot empty, list it in `Docs/V57/MISSING_ASSETS.md` + TODO line.
5. Verify visually: Play Mode end-of-frame screenshots of each level scene into `Docs/V57/evidence/I2/<UTC>/` (informational; visual verdicts come from `/independent-review` at M3).

---

## Rules

- Do not write per-game import/material/prefab scripts; extend `com.v57.assembly` if a rule is missing (and note it in `V57/knowledge/asset-intake.md`).
- Do not edit provider meshes, textures or audio; do not fix asset problems in gameplay code.
- Do not hand-assign materials in scenes; materials live on Visual prefabs.
- Missing art → no prefab, the slot stays empty and is listed in `Docs/V57/MISSING_ASSETS.md`; never a placeholder, never invisible colliders.
- No synthesized audio (`AudioClip.Create`).

---

## Integration

`/intake` I2 runs this skill end-to-end. M2/M3 crafts may re-run single steps after an import-rule or mapping change. `/perf-audit` uses `assembly-report.json` for texture memory and tris vs budgets.

---

*Last updated: 2026-09-28*
