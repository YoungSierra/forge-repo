# Unity Editor Verification (OVR helper)

Shared contract for **Operate → Verify → Report** (OVR) on every editor operation. Operate and Verify use the **Unity CLI Pipeline** (`unity command`, `eval`, `get_scene_hierarchy`, `find_gameobjects`) or `com.v57.assembly` (`AssemblyRunner`). `unity mcp` is an optional stdio binding to the same Pipeline.

| Item | Detail |
|------|--------|
| **CLI policy** | `V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md` |
| **Write** | `AssemblyRunner.*` (assets, prefabs, level scenes) or `unity command` (`open_scene`, `create_gameobject`, `batch`, `set_serialized_field`, `eval`, `save_all`) |
| **Verify** | `unity command eval` + `get_scene_hierarchy` / `find_gameobjects`; `AssemblyRunner.CaptureHierarchyDiff()` for Edit vs Play |
| **Forbid** | Product `Scripts/Editor/*Authoring` MenuItem scene builders; runtime scene builders; human Hierarchy checklists as primary path |
| **Lessons** | `V57/knowledge/verification.md`, `V57/knowledge/unity-cli.md` |

---

## When to apply

- Any skill that creates or modifies scenes, prefabs, ScriptableObject instances, UI assets, audio mixers, or build settings
- `/spec implement` when the spec lists `touches.scenes`, `touches.prefabs`, or `sceneSetup`
- `/scene-setup`, `/prefab`, `/data-asset`, and every craft that writes to the Editor

---

## OVR pipeline (mandatory)

| Step | Action | Tool | FAIL behavior |
|------|--------|------|---------------|
| 1. Pre-flight | Editor reachable; not Safe Mode; compile complete | `unity pipeline list --format json`; `recompile_status` (poll until complete) | Fix and retry (2×), then alternate route (batch `-executeMethod`) |
| 2. Operate | Create/modify asset or scene | `AssemblyRunner.*` / `unity command` | Poll compile/import; retry once after a fix |
| 3. Verify | Read-back invariants (tables below) | `unity command eval` / `get_scene_hierarchy` | **Not done** — fix and repeat step 2 |
| 4. Edit vs Play | Hierarchy diff (scenes only) | `AssemblyRunner.CaptureHierarchyDiff()` | **Not done** if runtime created/patched structure |
| 5. Console | No new compile/import errors since step 1 | CLI console read / eval | **Not done** |
| 6. Evidence | Screenshots (visual changes) + report | end-of-frame Game View capture | N/A |

"The command succeeded" is never verification.

---

## Standard invariants (read-back)

### Scene

| Invariant | Read-back check |
|-----------|-----------------|
| Scene saved | `EditorSceneManager.GetActiveScene().path == target` and `!isDirty` |
| Object present | `get_scene_hierarchy` / `find_gameobjects` by path (Edit Mode tooling; never used in runtime code) |
| Component present | component list on that object |
| Serialized reference | field non-null (log field names on FAIL) |
| Tag / layer | equals `Docs/V57/CONTEXT.md` table |
| Missing refs | 0 (scene + referenced prefabs) |

### Scene — production layout (`SCENE_PRODUCTION_STANDARDS.md`)

| Invariant | Read-back check |
|-----------|-----------------|
| Root containers | `_Environment`, `_Gameplay`, `_Systems`, `_UI`, `_Cameras`, `_Lighting` at scene root |
| No loose gameplay roots | nothing else at scene root |
| Gameplay prefabs | run-scope actors are instances of `Assets/_Game/Prefabs/Gameplay/PRF_*`, each a variant of `PRF_*_Visual` |

### Edit vs Play hierarchy diff

Run `unity command eval "V57.Assembly.AssemblyRunner.CaptureHierarchyDiff()"` on each in-scope scene (it snapshots Edit Mode, enters Play for a few seconds, snapshots again, and writes `Docs/V57/reports/hierarchy-diff.json`; the gate is `pass: true`).

| Difference in Play | Verdict |
|---|---|
| New prefab instances under `_Gameplay/Spawned` (or a declared spawn root) | allowed |
| New non-prefab GameObjects, `CreatePrimitive` objects | **FAIL** |
| Reparented objects, added/removed components on scene objects | **FAIL** |
| Renderers enabled/disabled on provider art, transform changes on `_Environment` | **FAIL** |
| Camera transform/projection different from `camera.json` at frame 1 | **FAIL** (camera is authored in Edit Mode) |
| State changes on gameplay components (values, animator states) | allowed |

### Prefab

| Invariant | Read-back check |
|-----------|-----------------|
| Asset exists | `AssetDatabase.LoadAssetAtPath<GameObject>(path) != null` (Editor eval) |
| Variant base | `PrefabUtility.GetCorrespondingObjectFromSource` → `PRF_<Asset>_Visual` for Gameplay prefabs |
| Component set | required components on root/children; Visual prefabs have **no** gameplay scripts |

### ScriptableObject

| Invariant | Read-back check |
|-----------|-----------------|
| Asset exists + type | load at `Assets/_Game/Data/SO_<Name>.asset` |
| Fields | values equal spec / `tuning.json` |

### UI Toolkit

| Invariant | Read-back check |
|-----------|-----------------|
| UXML/USS on disk | under `Assets/_Game/` |
| UIDocument | `panelSettings` and `visualTreeAsset` non-null |
| Visual tree | in Play: `rootVisualElement.Q("<name>") != null` for each named element |

---

## Screenshots (evidence rules)

| Rule | Detail |
|---|---|
| Mode | **Play Mode** only (Edit Mode captures do not show runtime UI state) |
| Capture | End-of-frame Game View: in a coroutine `yield return new WaitForEndOfFrame();` then `ScreenCapture.CaptureScreenshotAsTexture()` — or `unity command screenshot` when it captures the composited Game View |
| Must include | UI Toolkit overlay. Camera-only render-texture captures miss UITK overlays and are **invalid** for UI/HUD evidence |
| Resolution | Game View at `package.platform.reference_resolution` |
| Location | `Docs/V57/evidence/<stage>/<UTC>/` — fresh folder per run; never reuse |
| Pairing | Every screenshot is paired with numbers (camera params, read-back values) in the same folder |
| Verdict | Visual verdicts come from `/independent-review`, not from the agent that made the change |

---

## Report format

Save to `Docs/V57/reports/<stage>-<operation>-<slug>.md`.

```markdown
# Editor OVR report: {Operation}
## Pre-flight   | Check | Result |
## Operations   | Step | Command | Result |
## Verification | Invariant | Expected | Actual | Result |
## Edit vs Play diff | Scene | Allowed diffs | Violations |
## Console      | Compile errors | Import errors |
## Evidence     | screenshots + numbers paths |
## Overall      **OVR:** PASS / FAIL — reason
```

---

## Integration with implement gates

When `implementation_done_gates.on_spec_implement.verify_editor_assets_via_readback: true`:
1. After implement, run OVR for every asset/scene row in the active spec.
2. Append an **Editor OVR** section to `Docs/V57/reports/F-<spec>-<slug>.md`.
3. Block sign-off on FAIL.

---

## Prohibited

- Marking done after Operate without Verify
- Runtime code that builds or patches the scene to make read-back pass
- Manual Inspector steps as the primary workflow when CLI is up
- Scene-view or camera-only screenshots as UI evidence
- Legacy HTTP bridges (`127.0.0.1:7890`) or vendored `Tools/unity-mcp-server`

---

*Last updated: 2026-09-28*
