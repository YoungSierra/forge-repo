# Unity Scene Setup Skill (`/scene-setup`)

Turn an I2 level scene (built by `AssemblyRunner.BuildLevelScenes()`) into the **playable game scene** at M2: gameplay prefab instances, systems, UI, cameras, lighting — all authored in **Edit Mode** via the Unity CLI Pipeline, with mandatory OVR read-back and an Edit vs Play hierarchy diff.

| Item | Detail |
|------|--------|
| **Stage** | M1 (minimal: core-loop actors on the level scene), M2 (full) |
| **requires** | Unity CLI Pipeline (`unity pipeline list` reachable); `unity mcp` optional |
| **OVR helper** | `shared/helpers/unity-editor-verification-skill.md` |
| **Standards** | `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md` — mandatory |
| **Inputs** | `scenes.json`, `camera.json`, `entities.json`, `ui.json`, blockout `Marker_*` empties, specs `V57/specs/<slug>/**` |
| **Alias** | `/spec --setup-scene` delegates here |
| **Forbid** | Runtime scene builders; product `Scripts/Editor/*Authoring` + `MenuItem` scene builders |

---

## Invocation

```bash
/scene-setup --scene Assets/_Game/Scenes/SCN_<Level>.unity
/scene-setup --scene Assets/_Game/Scenes/SCN_<Level>.unity --spec @V57/specs/<slug>/features/<spec>.yaml
/scene-setup --dry-run
```

---

## Read order

1. `UNITY_CLI_INTEGRATIONS.md` (pre-flight), this skill, the OVR helper
2. `Docs/V57/CONTEXT.md` — tags/layers, modules, `sceneProduction`
3. `scenes.json` entry for the scene (systems, markers, camera_ref), `camera.json` view(s)
4. Active spec(s) — `sceneSetup`, `touches.scenes`, component wiring
5. `SCENE_PRODUCTION_STANDARDS.md`; `V57/knowledge/runtime-bootstrap.md`

---

## Workflow

### 1. Pre-flight
CLI reachable, not Safe Mode, `recompile_status` complete.

### 2. Plan (write it before Operate)

| Container | Content | Source |
|-----------|---------|--------|
| `_Environment` | `BLK_<Level>` (already placed by I2), kits, decoration, sky | Visual prefabs (I2) — do not re-place |
| `_Gameplay` | player/actors at `Marker_Spawn_*`, interactables, hazards, `Spawned/` | `Assets/_Game/Prefabs/Gameplay/PRF_<Asset>.prefab` (variants of Visual) |
| `_Systems` | scene managers + thin `GameBootstrap` (serialized refs, ordered Init) | game scripts |
| `_UI` | `UIDocument` per `ui.json` screen | UXML/USS + PanelSettings |
| `_Cameras` | Main Camera with `camera.json` values | — |
| `_Lighting` | lights, Global Volume | — |

Plus a wiring table: object → components → serialized refs → tags/layers.

### 3. Operate (Edit Mode, CLI)
1. Open the scene.
2. Instantiate Gameplay prefabs at the matching `Marker_*` transforms (read marker transforms in Edit Mode; copy the pose; do not parent under the blockout).
3. Add systems under `_Systems`; set every `[SerializeField]` reference with `set_serialized_field` (scene objects, prefabs, `SO_*` data from `/data-asset`).
4. UI under `_UI` (UI Toolkit; `/game-ui` owns layout).
5. Camera values from `camera.json` (type, fov/size, angle, distance, follow); log any tolerance adjustment as `D-###`.
6. `unity command save_all`.

### 4. Verify (read-back — mandatory)
Functional: objects, components, non-null refs, tags/layers. Production: six containers, no loose roots, Gameplay prefab variants. Console: 0 new errors.

### 5. Edit vs Play diff (mandatory at M2)
`unity command eval "V57.Assembly.AssemblyRunner.CaptureHierarchyDiff()"` → `Docs/V57/reports/hierarchy-diff.json` must have `pass: true` — only prefab instances under `_Gameplay/Spawned` (or declared spawn roots) may appear in Play. Anything else means runtime code is building/patching the scene → move it into Edit Mode authoring.

### 6. Evidence + report
Play Mode end-of-frame screenshot per `camera.json` view into `Docs/V57/evidence/M2/<UTC>/`; gold path re-run. Report `Docs/V57/reports/M2-scene-setup-<slug>.md` with **Functional invariants**, **Production invariants**, **Edit vs Play diff**.

Done only when all three sections pass and the gold path is green.

---

## Prohibited

- Runtime bootstraps that find, create, reparent or patch scene objects (`GameObject.Find*`, `FindObjectsByType`, `new GameObject`/`AddComponent` for structure, `CreatePrimitive`, hardcoded coordinates, reflection `SetValue`, `AssetDatabase` in runtime code)
- Hiding provider renderers and substituting primitives to make gameplay work
- Re-framing the camera away from `camera.json` without a `D-###`
- Asking the human to mount Hierarchy; blind `.unity` YAML edits while CLI is reachable
- Loose gameplay objects at scene root; scene-only duplicates without a prefab
- Done without read-back report and saved scene

---

*Last updated: 2026-09-28*
