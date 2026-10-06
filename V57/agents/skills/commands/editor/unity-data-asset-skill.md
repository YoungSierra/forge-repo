# Unity Data Asset Skill (`/data-asset`)

Instantiate **ScriptableObject** (and similar serialized) assets from spec types, populate fields from TDD/spec, verify via MCP read-back.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **OVR helper** | `V57/agents/skills/shared/helpers/unity-editor-verification-skill.md` |
| **Master doc** | `V57/docs/guides/EDITOR_WORKFLOW.md` |

---

## Invocation

```bash
/data-asset --type RoundConfig --path Assets/Settings/RoundConfig.asset --spec @V57/specs/<slug>/systems/win_lose_system.yaml
/data-asset --path Assets/Settings/CoinSpawnerConfig.asset --fields totalCoins=5,roundTime=60
/data-asset verify --path Assets/Settings/RoundConfig.asset
/data-asset --dry-run
```

| Flag | Behavior |
|------|----------|
| `--type <ClassName>` | SO type (must compile in project) |
| `--path` | Asset path under `Assets/` |
| `--spec` | Read `designerAssetSuggestedPath` + field values from spec |
| `--fields k=v,...` | Inline field overrides |
| `verify` | Read-back only |
| `--dry-run` | Plan values + invariants |

---

## Read order

1. MCP pre-flight
2. This skill + OVR helper
3. Spec YAML — `components[]` with `designerAssetSuggestedPath`, default values in description or `sceneSetup`
4. Generated `.cs` for the SO type must **compile** before Operate

---

## Workflow

### 1. Resolve type and values

Priority for field values:

1. `--fields` CLI
2. Spec explicit defaults (table in spec body or `dataAsset:` block when present)
3. TDD excerpt referenced by spec
4. Ask user if ambiguous — do not guess gameplay balance

### 2. Operate

```csharp
var asset = ScriptableObject.CreateInstance<RoundConfig>();
asset.totalCoins = 5;
asset.roundTimeSeconds = 60f;
asset.fallThreshold = -5f;
AssetDatabase.CreateAsset(asset, "Assets/Settings/RoundConfig.asset");
AssetDatabase.SaveAssets();
result.Log("Created RoundConfig");
```

For existing assets: load, modify, `EditorUtility.SetDirty`, save.

### 3. Verify (read-back)

```csharp
var asset = AssetDatabase.LoadAssetAtPath<RoundConfig>("Assets/Settings/RoundConfig.asset");
// Assert each spec field; log PASS/FAIL per field
```

Include nested references (prefab/SO) when spec requires them.

### 4. Console + report

`Docs/V57/reports/data-asset-report-{slug}.md`

Wire into scene/prefab **after** data asset PASS (order: SO → MonoBehaviour refs).

---

## Integration with `/spec implementa`

Implement workflow step 3 (`unity-spec-skill.md`):

1. Write `.cs` for SO type
2. Compile gate
3. **`/data-asset`** (or inline OVR) for each `designerAssetSuggestedPath`
4. Wire references in scene/prefab via `/scene-setup` or implement MCP block
5. **Editor OVR** section in implement report

Gate: `verify_editor_assets_via_readback: true` in `agents.yaml`.

---

## Prohibited

- Hand-editing `.asset` YAML on disk as primary workflow
- Creating asset before SO `.cs` compiles
- Skipping field read-back

---

*Last updated: 2026-06-10*
