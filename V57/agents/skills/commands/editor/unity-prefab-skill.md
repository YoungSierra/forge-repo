# Unity Prefab Skill (`/prefab`)

Create or update **Gameplay prefabs** as **prefab variants of Visual prefabs**, via the Unity CLI Pipeline with OVR verification.

| Item | Detail |
|------|--------|
| **requires** | Unity CLI Pipeline; `unity mcp` optional |
| **OVR helper** | `shared/helpers/unity-editor-verification-skill.md` |
| **Visual prefabs** | built by `AssemblyRunner.BuildVisualPrefabs()` / `BuildPlaceholders()` at I2 — this skill does not hand-build them |

---

## The Visual → Gameplay rule

| Layer | Path | Owner | Contains |
|---|---|---|---|
| **Visual** | `Assets/_Game/Prefabs/Visual/{Characters,Props,Environment,VFX}/PRF_<Asset>_Visual.prefab` | `com.v57.assembly` | provider mesh/sprite, `MAT_<Asset>`, colliders from `UCX_*`, sockets, animator (if skinned). **No gameplay scripts.** |
| **Gameplay** | `Assets/_Game/Prefabs/Gameplay/PRF_<Asset>.prefab` | this skill | **prefab variant** of the Visual prefab + behaviour components, Rigidbody/trigger setup, serialized config (`SO_*`) |

Why: art updates (new mesh, placeholder → final) re-run I2 and flow into gameplay without touching behaviour; behaviour never patches art at runtime.

Rules:
1. Never add gameplay components to a Visual prefab; never duplicate a Visual prefab to add behaviour.
2. Override only what gameplay needs (components, layer, tag, collider trigger flags); document overrides in the report.
3. If the Visual prefab is wrong (scale, pivot, mirrored side), fix it at import (import rule or manifest mapping, logged `D-###`) — not with variant transform overrides or runtime code.
4. One Gameplay prefab per `asset_manifest` asset that has behaviour (`entities.json → asset_id`).

---

## Invocation

```bash
/prefab variant --base Assets/_Game/Prefabs/Visual/Props/PRF_Flipper_Visual.prefab --path Assets/_Game/Prefabs/Gameplay/PRF_Flipper.prefab --spec @V57/specs/<slug>/features/flipper_controller.yaml
/prefab update --path Assets/_Game/Prefabs/Gameplay/PRF_Flipper.prefab --spec @V57/specs/<slug>/features/flipper_controller.yaml
/prefab verify --path Assets/_Game/Prefabs/Gameplay/PRF_Flipper.prefab
/prefab --dry-run
```

---

## Workflow

### 1. Plan

| Field | Value |
|-------|-------|
| Base (Visual) | `PRF_<Asset>_Visual` |
| Variant path | `Assets/_Game/Prefabs/Gameplay/PRF_<Asset>.prefab` |
| Added components | from spec |
| Overrides | layer/tag/collider flags only |
| Serialized refs | `SO_*` configs, child transforms, sockets |

### 2. Operate (Editor `eval`)

```csharp
var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Visual/Props/PRF_Flipper_Visual.prefab");
var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
instance.AddComponent<FlipperController>();            // behaviour lives on the variant only
PrefabUtility.SaveAsPrefabAsset(instance, "Assets/_Game/Prefabs/Gameplay/PRF_Flipper.prefab"); // saved as a variant of the base
Object.DestroyImmediate(instance);
AssetDatabase.SaveAssets();
```

Then set serialized fields with `unity command set_serialized_field` (or `SerializedObject` in `eval`).

### 3. Verify (read-back)

- Asset exists; `PrefabUtility.GetPrefabAssetType == Variant`; source = the Visual prefab
- Required components present; Visual prefab still has no gameplay scripts
- Serialized refs non-null
- 0 console errors

### 4. Report

`Docs/V57/reports/M2-prefabs-<slug>.md` — table per prefab: base, added components, overrides, refs, result.

---

## Prohibited

- Gameplay scripts on Visual prefabs; copying a Visual prefab instead of making a variant
- Transform/scale overrides on the variant to compensate for import problems
- Editing prefab YAML on disk while CLI is reachable
- Variants without documented overrides

---

*Last updated: 2026-09-28*
