# Runtime bootstrap lessons

## The 1,239-line bootstrap
- **Symptom:** `HappyHabitatPlayBootstrap.cs` grew to 1,239 lines: it found objects by name (`GameObject.Find`), created and reparented objects, set private fields by reflection, hardcoded world coordinates, fell back to `ScriptableObject.CreateInstance` configs, and used `#if UNITY_EDITOR AssetDatabase` inside runtime code. Controllers also built flipper bats at runtime.
- **Cause:** "runtime director/builder" was an allowed authoring path; every craft stage patched the scene from code instead of fixing the saved scene.
- **Consequence:** Edit Mode never showed the real game; each fix broke another stage; player builds differed from the Editor (AssetDatabase paths); ~150 one-off repair scripts accumulated.

## Rule
The saved scene is the game. A runtime bootstrap may only:
1. hold `[SerializeField]` references to scene objects, prefabs and `SO_*` configs (set in Edit Mode);
2. call `Init(...)` on systems in a fixed order;
3. instantiate **prefabs** referenced by serialized fields under `_Gameplay/Spawned` (gameplay spawns such as balls, projectiles).

Banned in runtime code: `GameObject.Find*`, `FindObjectOfType/FindObjectsByType/FindFirstObjectByType/FindAnyObjectByType`, reflection `SetValue`/`BindingFlags.NonPublic`, `ScriptableObject.CreateInstance` config fallbacks, `Resources.Load` config fallbacks, `AssetDatabase` (also inside `#if UNITY_EDITOR`), `new GameObject`/`AddComponent`/`CreatePrimitive` for level structure, hardcoded layout coordinates, renderer toggles on provider art, `Camera.main`, files > 200 lines.

Missing reference at runtime → log an error and disable the component. Never search for a substitute.

## Where the work goes instead
| Need | Do it here |
|---|---|
| Materials, prefabs, level scenes | `com.v57.assembly` (`AssemblyRunner`) at I2 |
| Behaviour on an asset | Gameplay prefab variant (`/prefab`) |
| Placing actors | Edit Mode at `Marker_*` (`/scene-setup`) |
| Config values | `SO_*` assets from `tuning.json` (`/data-asset`) |

**Enforced by:** `runtime-lint.js` at M2; `AssemblyRunner.CaptureHierarchyDiff()`; `agents.yaml → scene_authoring`. **Source:** HH bootstrap + implement reports, `_listings/NOTES.txt`.
