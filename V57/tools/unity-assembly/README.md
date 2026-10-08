# com.v57.assembly — V57 Unity assembly package

Local UPM package used by the V57 pipeline (design brief §4). Two assemblies:

| Assembly | Platforms | Contents |
|---|---|---|
| `V57.Assembly.Editor` (`Editor/`) | Editor | Import rules (`V57AssetPostprocessor`), `AssemblyRunner` (materials, skybox, UI atlases, animator controllers, visual prefabs, level scenes, hierarchy diff, report), JSON DTOs for `Docs/Generated/json/*.json` |
| `V57.GoldPath` (`Runtime/`) | All | `GoldPathDriver`, `IGoldPathProbe`, `GoldPathProbeRegistry`, step model/parser, simulated input, `V57Marker` |
| `V57.GoldPath.Tests` (`Tests/PlayMode/`) | PlayMode tests | `GoldPath_Passes` |
| `V57.GoldPath.Tests.Editor` (`Tests/Editor/`) | EditMode tests | step parsing, JSON preprocessing, conditions, naming |
| `V57.Assembly.Tests.Editor` (`Tests/AssemblyEditor/`) | EditMode tests | DCC texture suffixes, default animator state, layouts JSON |

Target: Unity 6000.6 (pinned 6000.6.2f1; `package.json` declares the `6000.0` minimum), URP 17+, Input System 1.x (project-wide actions need 1.8+), C# 9.

## Install

`Packages/manifest.json` (the repo root holds `V57/`, so the path is relative to `Packages/`):

```json
{
  "dependencies": {
    "com.v57.assembly": "file:../V57/tools/unity-assembly"
  },
  "testables": ["com.v57.assembly"]
}
```

`testables` is required, or the package tests are not visible to the Test Runner or the CLI. The test asmdefs also need `com.unity.test-framework` in the project; I1 adds it, so the package itself declares only `com.unity.inputsystem`. URP is not a hard dependency: materials are created through `Shader.Find("Universal Render Pipeline/Lit")`, and if that shader is missing, `BuildMaterials` reports an error.

## Entry points (`V57.Assembly.AssemblyRunner`)

| Method | Does | Output |
|---|---|---|
| `RunAll()` | Runs `ApplyImportRules`, `BuildMaterials`, `BuildSkybox`, `BuildUiAtlases`, `BuildAnimators`, `BuildVisualPrefabs`, `BuildLevelData`, `BuildLevelScenes` and `WriteReport`, in that order | `Docs/V57/reports/assembly-report.json` |
| `RunAllForce()` | Same as `RunAll`, but it also rebuilds existing visual prefabs, animator controllers and level scenes (same path and GUID) | same |
| `RunAllBatch()` | For `-executeMethod`: runs `RunAll`, then exits with **0** when the report has `pass: true`, otherwise **1** | same |
| `ApplyImportRules()` | Force-reimports `Assets/_Game/Art` and `Assets/_Game/Audio` so the import rules apply, then links each `ANIM_` file to its `SK_` avatar | import log |
| `BuildMaterials()` | Builds `…/<Asset>/Materials/MAT_<Asset>.mat` (URP Lit) from `T_<Asset>_*` or DCC-suffixed textures (`*_albedo`, `_normal`, `_MetallicSmoothness`, `_metallic`, `_roughness`, `_ao`, `_emission`; asset = folder name) and remaps the model's main embedded material to it; then applies the LevelMaps manifest material values (flat colour for untextured assets, metallic/smoothness when no map, double-sided, alpha clip) | materials, `T_<Asset>_MSO.png`, `T_<Asset>_MS.png` |
| `BuildSkybox()` | `Art/Environment/Sky/<Name>.(png|jpg|exr|hdr)` → `Sky/Materials/MAT_<Name>.mat` (`Skybox/Panoramic`, lat-long 360°) | sky materials |
| `BuildUiAtlases()` | Builds one atlas per `Art/UI/Sprites/<Id>/` folder at `Art/UI/Atlases/ATL_<Id>.spriteatlas` (`.spriteatlasv2` when the packer mode is V2) | atlases |
| `BuildAnimators()` | `Data/Animation/AC_<Asset>.controller` per `SK_<Asset>` with `ANIM_<Asset>_*` clips: one state per clip, default = looping idle → first looping → first. No transitions/parameters (gameplay owns them) | controllers |
| `BuildVisualPrefabs()` | Builds `Prefabs/Visual/<Category>/PRF_<Asset>_Visual.prefab` for manifest assets of type `model`, `character` or `blockout`; rigged models get an Animator (`AC_<Asset>`, model avatar, root motion off). Then nests each Visual into an existing gameplay prefab `PRF_<Asset>` that lacks it (system authored before its art; variants already inherit it) | prefabs, `gameplay_linked` |
| `BuildLevelData()` | Copies each `Docs/Design/LevelData/<LevelId>.json` (index `level_data.json`) verbatim to `Assets/_Game/Data/Levels/<LevelId>.json` (TextAsset for gameplay code); rewritten only when it changed | level data files |
| `BuildLevelScenes()` | Builds `Assets/_Game/Scenes/SCN_<id>.unity` for each slice scene and each LevelMaps layout scene | scenes, Build Settings |
| `RebuildLevelContent()` | **New or changed map:** in each existing level scene, replaces only the level content (`_Environment` + `_Gameplay/Level`) from the current layout or blockout; player, systems, UI, cameras and lighting are kept. Not part of `RunAll` | scenes |
| `CaptureHierarchyDiff()` | M2 gate: compares the Edit Mode and Play Mode hierarchies (see below) | `hierarchy-edit.json`, `hierarchy-play.json`, `hierarchy-diff.json` |
| `WriteReport()` | Scans the created prefabs and scenes for missing references and writes the report | `assembly-report.json` |

Each single-step method first clears that step's earlier warnings, errors and result (so a fixed step can flip the report back to `pass: true`), then reloads the JSON, runs the step and rewrites the report, so you can re-run one failed step on its own. The same methods are also in the **Tools > V57 > Assembly** menu.

**No placeholders:** there is no placeholder step. An asset without a model gets no prefab, a scene without blockout or layout gets no level art, and both are listed in `Docs/V57/MISSING_ASSETS.md`.

### CLI

```bash
# Warm Editor (Unity CLI Pipeline). A blocking eval times out after 5 s on the main thread, so schedule long steps
# and poll the report file:
unity command eval "EditorApplication.delayCall += () => V57.Assembly.AssemblyRunner.RunAll(); return 1;"
unity command eval "V57.Assembly.AssemblyRunner.BuildVisualPrefabs()"   # re-run one step
unity command eval "V57.Assembly.AssemblyRunner.CaptureHierarchyDiff()" # async: poll Docs/V57/reports/hierarchy-diff.json

# Batch mode (needs a GPU for ORM repacking; do not pass -nographics)
Unity -batchmode -quit -projectPath . -executeMethod V57.Assembly.AssemblyRunner.RunAllBatch -logFile Logs/v57-assembly.log
echo $?   # 0 = assembly-report pass

# Gold path (PlayMode test)
V57_GOLDPATH_EVIDENCE_DIR=Docs/V57/evidence/goldpath/$(date -u +%Y%m%dT%H%M%SZ) \
  unity test . --mode PlayMode --filter V57.GoldPath
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter V57.GoldPath -testResults Logs/goldpath.xml
```

## Import rules (`V57AssetPostprocessor`, only under `Assets/_Game/Art/` and `Assets/_Game/Audio/`)

Every decision is written as one JSON line to `Library/V57/import-log.jsonl`, and the lines are embedded in the report as `import_log`. The log is a file, not a static, because postprocessors can run in import worker processes. The rules are authoritative: they are re-applied on every import. Bump `RulesVersion` whenever a rule changes.

| Asset | Rule |
|---|---|
| Models | `globalScale 1`, `useFileScale`, no cameras or lights, `addCollider off`, materials `ImportViaMaterialDescription` kept **in the model** (not extracted). V57 builds `MAT_<Asset>` and remaps to it. `isReadable` is on only when the manifest says `collision: exact` |
| `SM_*`, `BLK_*` | `animationType None`, no animation import |
| `SK_*` | `Human` when the manifest says `rig: "humanoid"`, otherwise `Generic` |
| `ANIM_<Asset>_<Clip>` | Same type as `SK_<Asset>`. `preserveHierarchy` on, so curves keep the `<Rig>/` path of the `SK_` hierarchy (armature-only exports have a single root node). Its avatar is copied from `SK_<Asset>` by `ApplyImportRules`. `loopTime` comes from `animations[].loop`, else from the provider sidecar `Characters/<Asset>/<Asset>_export.json` (`clips[].file` / `loop`), else a name heuristic (idle/walk/run/loop/cycle). Events from `animations[].events` call `OnV57AnimationEvent(string name)` |
| `UCX_*` nodes | Convex `MeshCollider` from the node's own mesh; renderer disabled |
| `Marker_*`, `Socket_*` nodes | Kept, with renderers disabled |
| `T_*_N` | NormalMap |
| `T_*_ORM`, `_MS`, `_Mask`, `_MSO`; DCC `*_MetallicSmoothness`, `_metallic`, `_roughness`, `_ao` | Default, sRGB off |
| `T_*_BC`, `_E`; DCC `*_albedo`, `_basecolor`, `_diffuse`, `_emission` | Default, sRGB on, mipmaps on |
| DCC `*_normal` | NormalMap |
| `Environment/Sky/**` | Lat-long sky: sRGB, mipmaps off, wrap U repeat / V clamp, max size = source (≤ 8192) |
| `UI/Sprites/**`, `UI/Icons/**` | Sprite (Single), mipmaps off, clamp, alpha is transparency. `_9s-<px>` sets `spriteBorder` to (px, px, px, px) |
| `VFX_*` | sRGB with alpha. The `_Sheet_<c>x<r>` grid is logged so TextureSheetAnimation can match it |
| Audio `Music/`, `Ambience/` | Streaming, Vorbis q0.7, load in background |
| Audio `SFX/` | DecompressOnLoad |
| Audio `Voice/` | CompressedInMemory, Vorbis |
| Fonts | No changes (logged) |

## Materials and ORM (be aware)

Provider ORM textures are packed R=occlusion, G=roughness, B=metallic. URP Lit reads metallic from R and smoothness from A of `_MetallicGlossMap`, and occlusion from **G** of `_OcclusionMap`, so the ORM texture cannot be assigned directly. `OrmPacker` blits the ORM on the GPU, so Read/Write is not needed, and writes `Materials/T_<Asset>_MSO.png` (R=metallic, G=occlusion, B=0, A=1−roughness, imported linear). It then assigns that texture to both `_MetallicGlossMap` and `_OcclusionMap`, with the `_METALLICSPECGLOSSMAP` and `_OCCLUSIONMAP` keywords and `_Smoothness = 1` as a multiplier. This needs a graphics device. If packing fails, the material keeps URP defaults and the report gets a warning. `_Mask` textures are imported linear but not wired to anything, because their meaning is game-specific.

## Visual prefabs and scenes

- **Visual prefab:** a root `PRF_<Asset>_Visual` containing the model as a *nested prefab instance*, so reimports flow through and sockets/markers are kept. It also gets:
  - an `LODGroup` when the model has `*_LOD0…n` nodes;
  - colliders per the manifest `collision` value:
    - `exact`: the UCX colliders;
    - `simple`: a box, or a capsule for characters;
    - `none`: no collider;
    - blockouts without UCX: non-convex MeshColliders.

  Size is checked against `size_m` [x, y, z] with ±10% tolerance, and the pivot is checked (`feet`/`base`, `center`, `hinge` + `side`, `axle`). Mismatches are **warnings**. The prefab holds no gameplay scripts.
- **Category folder** comes from the declared `files.mesh` path, then the asset `type`/`category`. A re-delivered model therefore keeps the same prefab path and GUID.
- **2D physics** (`package.physics: "2d"`): colliders use the 2D types — zone triggers are `BoxCollider2D`, and `simple` colliders are `BoxCollider2D` (`CapsuleCollider2D` for characters). UCX `exact` stays a 3D convex MeshCollider with a warning, because no PolygonCollider2D is generated. 2D blockouts without UCX get no colliders (warning).
- **Existing prefabs, controllers and scenes are not overwritten** unless you use `RunAllForce`, so gameplay edits made after I2 are safe.
- **Rigged models:** the model instance carries an `Animator` with `AC_<Asset>` and the `SK_` avatar (root motion off, culling `CullUpdateTransforms`).
- **Level scene:**
  - The six root containers `_Environment` (with `_Markers`), `_Gameplay` (with `Spawned`), `_Systems`, `_UI`, `_Cameras` and `_Lighting`.
  - **LevelMaps layout scene** (`layout` set in `scenes.json`): one Visual prefab instance per `layouts.json` object under `_Environment/<Layer>` (English PascalCase group), named `<Name>_NN`, with the exported position/rotation/scale as-is; non-animated instances are static; objects without model/prefab are skipped and counted in `layout_missing`. **One prefab per thing:** when the asset has a gameplay prefab `Prefabs/Gameplay/**/PRF_<Asset>.prefab`, that is placed instead, under `_Gameplay/Level/<Layer>` (counted in `layout_gameplay`), so a new map gets working collectibles/enemies/doors with no manual work. Layout objects of kind `marker` (`Marker_<Type>_<Id>`) become `V57Marker`s under `_Environment/_Markers` exactly like blockout markers (volume = 1 m cube × the exported scale).
  - **Blockout scene:** the blockout instance under `_Environment`. The visual prefab is used if one exists, otherwise the model.
  - One `V57Marker` per `Marker_*` node, under `_Environment/_Markers` (typed kind/id and local mesh bounds). A marker is a **volume** — trigger `BoxCollider` sized from the marker mesh (layout markers: 1 m cube × scale) — when its kind is `Zone`, `Bounds`, `Exit`, `Kill` or `CameraZone`, when the layout object says `shape: box`, or when a blockout marker of an unknown type has a mesh. Other kinds (`Spawn`, `Camera`, `Checkpoint`, `Patrol`, `Other`) are points. Gameplay code binds markers by name; V57 does not know what a game uses them for.
  - Static dressing under `_Environment/_Dressing`: every `Marker_Spawn_<AssetName>_NN` whose asset has no `serves` (environment / decoration, not a gameplay entity) gets the asset's Visual prefab with the marker's position, rotation and scale. Spawn markers of gameplay entities are left for gameplay code (M1).
  - Cameras under `_Cameras`: one per `Marker_Camera_<ViewId>`, with projection values from `camera.json`. The main camera is the one matching `camera_ref`, otherwise the first; the others are disabled. With no camera markers, a default camera is placed using `camera.json` angle and distance.
  - One directional light; `RenderSettings.skybox` = the first delivered sky material (exposure/rotation/fog are M3 craft).
  - A scene with **no blockout and no layout** keeps no level art (warning). A declared blockout or layout that can't be found is an error.
  - The scene is added to Build Settings.
  - The step refuses to run in Play Mode or while an open scene has unsaved changes.

  `_Markers` sits under `_Environment`, not at the scene root, because SCENE_PRODUCTION_STANDARDS allows only the six containers at the root.

## Hierarchy diff (M2)

1. In Edit Mode, `CaptureHierarchyDiff()` snapshots the active scene. Each node records its path, component types, `activeSelf` and renderer state.
2. It enters Play Mode. After 3 s, a hook (SessionState flag, so it survives domain reload) snapshots the scene again, including DontDestroyOnLoad objects, writes `hierarchy-diff.json` and exits Play Mode.
3. The report **fails** on any of:
   - an object created outside `_Gameplay/Spawned`;
   - a destroyed object;
   - an added or removed component;
   - a renderer toggled outside `_UI`.
4. Activation changes are reported as information only.
5. DontDestroyOnLoad roots whose names start with `_Systems` (persistent services), and their children, are allowed and listed under `created_allowed`.
6. URP's auto-added `UniversalAdditionalCameraData`/`LightData`, `[Debug Updater]` and the test runner object are ignored and listed.
7. Called in Play Mode, it compares against the last edit snapshot immediately.

## JSON contract consumed (Docs/Generated/json)

Files are read with `JsonUtility` after `JsonPreprocessor` strips `"key": null` members. Unknown keys are ignored. Fields with uncertain JSON types are deliberately **not** mapped: `bones`, `tris_lod0`, `texture_size`, `issues`, `players`.

- `asset_manifest.assets[]`: `asset_id, asset_name, category, type, serves[], files{mesh, textures[], reference}, size_m[3], pivot, side, collision, rig (optional: "humanoid"|"generic"), status, source, animations[{clip, file, loop (bool), events[{name, frame (int)}]}]`. **Intake:** emit `rig` for skinned characters, or everything imports as Generic.
- `scenes.scenes[]`: `id, purpose, world_owner, systems[], acs[], slice (bool), blockout (asset_id | asset_name | path), layout (LevelMaps level id | null), markers[], camera_ref (view id, Marker_Camera_<Id> or Camera/<Id>.png)`.
- `layouts.layouts[]` (optional file): `level_id, source, manifest, objects[{name, kind (model | marker), shape (box | point | null), source_name, asset_id, model (asset path | null), layer, position[3], rotation[4] (x,y,z,w), scale[3]}], materials[{asset_id, name, albedo, normal, metallic_smoothness, base_color[4], metallic, smoothness, alpha_mode, alpha_cutoff, double_sided}]`.
- `level_data.levels[]` (optional file): `level_id, source (Docs/Design/LevelData/<LevelId>.json), target (Assets/_Game/Data/Levels/<LevelId>.json), contract, keys[]` — the game-specific payload stays in the source file.
- `package`: `slug, title, version, perspective, physics, platform{targets[], orientation, reference_resolution[], target_fps}, world{…}, slice{scenes[]}`.
- `ui.screens[]` / `ui.layouts[]`: as in brief §2. `sprites` is a folder name or path under `Art/UI/Sprites`.
- `camera.views[]`: `id, scene, type (orthographic|perspective), angle_deg, fov_or_size, distance_m, follow (string), notes`.

### Gold path step contract (`acceptance.json → gold_path` or `Docs/V57/gold_path.json`)

```json
{ "scene": "SCN_Level01", "status": "final",
  "steps": [
    { "id": "S01", "expect": "session.state", "op": "==", "value": "Playing", "timeout_s": 5, "screenshot": true },
    { "id": "S02", "do": "hold", "action": "Launcher/ChargePlunger", "seconds": 0.8 },
    { "id": "S03", "do": "release", "action": "Launcher/ChargePlunger" },
    { "id": "S04", "do": "move", "action": "Player/Move", "value": "0,1", "seconds": 1.5 },
    { "id": "S05", "do": "press", "control": "<Keyboard>/space" },
    { "id": "S06", "expect": "score.value", "op": ">", "value": 0, "timeout_s": 20, "screenshot": true },
    { "id": "S07", "do": "capture", "name": "end_screen" }
  ] }
```

**Step fields**

| Field | Meaning |
|---|---|
| `id` | Step id (optional) |
| `do` | Input verb: `press` (or `tap`), `hold`, `release`, `move`, `wait`, `wait_until`, `capture` |
| `action` | `Map/Action`. It is looked up among enabled actions, then in project-wide `InputSystem.actions`, and its bindings are mapped onto simulated devices |
| `control` | A raw control path, e.g. `<Keyboard>/space`, `<Gamepad>/buttonSouth` or `<Touchscreen>`. `space` alone means `<Keyboard>/space` |
| `value` | For `move`: `"x,y"`. For touch: a normalized `"x,y"` screen position (default center). For a button: its value (default 1). Bare numbers and booleans are quoted automatically |
| `seconds`, `frames` | Duration of the input or wait |
| `expect` + `op` + `value` | A probe condition. `op` is one of `== != > >= < <=`. Without `op`, the value is truthy or `!key` |
| `condition` | The compact form, e.g. `"score.value >= 3"` |
| `timeout_s` | With a value above 0, the condition is polled until the timeout (WaitUntil). Without it, the condition is checked once |
| `screenshot` | Capture after the step |
| `name` | Capture name |

**How steps run**

- A step with both `do` and `expect` runs as two steps: the input first, then the check.
- `press` goes down on frame N and up on frame N+1 or later.
- `hold` without `seconds` stays down until a matching `release`.
- `move` without `seconds` keeps the stick at `value` until a matching `release` (a later `move` on the same action changes the direction). Pair it with `wait_until` steps on probe positions for closed-loop routes (walk to a ledge, jump, land) instead of timed moves.
- Every wait uses unscaled wall-clock time.

**Built-in probe keys:** `scene.active` (string), `scene.<Name>` (bool), `marker.<Kind>.<Id>` (bool), `time.level`, `time.unscaled`, `frame`.

## Gold path driver

1. `GoldPathDriver.CoRun()` loads the steps. **Source priority:** `Docs/V57/gold_path.json` whenever it exists. Otherwise `acceptance.json → gold_path`, unless its `status` is `"draft"` — intake always emits a draft, and a draft alone is reported as a parse error (the run fails) until V57 authors `gold_path.json`. The `scene` field may be a scene id or an asset path.
2. It swaps in a temporary copy of `InputSystem.settings` (IgnoreFocus; in the Editor, all input goes to the Game View), adds the simulated devices `V57GoldPathKeyboard`, `Gamepad`, `Mouse` and `Touchscreen`, and disables real keyboards, pointers and gamepads.
3. It runs the steps. Failures are logged as warnings, so they don't count as console errors.
4. On failure, and at the end, it captures `NN_<name>.png`.
5. In `finally` and in `OnDisable`, it removes the simulated devices, re-enables the real devices and restores the settings.
6. It writes `checks.json` to `Docs/V57/evidence/goldpath/<UTC>/`, or to `$V57_GOLDPATH_EVIDENCE_DIR` when that is set:

```json
{ "started": "…", "finished": "…", "source": "…/acceptance.json", "scene": "Assets/_Game/Scenes/SCN_Level01.unity",
  "evidence_dir": "…", "steps": [{ "index": 0, "id": "S01", "type": "WaitUntil", "arg": "session.state == Playing (timeout 5s)",
  "pass": true, "elapsed": 0.42, "detail": "session.state='Playing' == 'Playing'", "screenshot": "00_S01.png" }],
  "parse_errors": [], "console_errors": 0, "console_error_samples": [], "fps_avg": 59.8, "pass": true }
```

`pass` is true only when there is at least one step, every step passed, there are 0 parse errors and 0 console errors or exceptions (counted by `GoldPathConsoleWatcher` through `Application.logMessageReceivedThreaded`).

If a run dies halfway, the disabled device ids are also saved to `Application.temporaryCachePath/v57-goldpath-devices.txt`. `GoldPathEditorRecovery` (`[InitializeOnLoad]`) restores them, removes leftover simulated devices on returning to Edit Mode or on Editor load, and records the time in SessionState `V57.GoldPath.LastRecovery`.

The PlayMode test `GoldPath_Passes`:

1. Loads `gold_path.scene`, else the first scene in `package.slice.scenes`, else the first scene with `slice: true`, using `EditorSceneManager.LoadSceneAsyncInPlayMode`, so the scene does not need to be in Build Settings.
2. Waits up to 10 s for a game probe to register.
3. Runs the driver and asserts `pass`.

### Implementing a probe (game code)

```csharp
using UnityEngine;
using V57.GoldPath;

namespace Acme.Pinball.GoldPath
{
    /// <summary>Read-only gold path probe; lives on _Systems/GoldPathProbe in the slice scene.</summary>
    public sealed class PinballGoldPathProbe : MonoBehaviour, IGoldPathProbe
    {
        [SerializeField] private ScoreSystem _score;       // bound in Edit Mode, no Find/reflection
        [SerializeField] private BallSystem _ball;
        [SerializeField] private SessionController _session;

        private void OnEnable() => GoldPathProbeRegistry.Register(this);

        private void OnDisable() => GoldPathProbeRegistry.Unregister(this);

        public bool TryGetBool(string key, out bool value)
        {
            value = key == "ball.in_play" && _ball.InPlay;
            return key == "ball.in_play";
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = key == "score.value" ? _score.Current : 0f;
            return key == "score.value";
        }

        public bool TryGetString(string key, out string value)
        {
            value = key == "session.state" ? _session.State.ToString() : null;
            return value != null;
        }
    }
}
```

Return `false` for unknown keys; the check then fails as `unknown`, never true. Probes only observe; they never mutate gameplay.

## Standards exceptions

- JSON DTOs (`*Dto`, `GoldPathChecks`, `GoldPathStepResult`, `AssemblyReport`, `AssemblyCounts`, `AssemblyStepResult`, `MissingRefEntry`, `ImportLogEntry`, `Hierarchy*` report types) use **public snake_case fields**. `JsonUtility` maps field names to JSON keys and has no rename attribute. This is the documented exception to STANDARDS_CANONICAL §3 (public fields and naming), and these types carry no behaviour.
- `GoldPathStepDto.@do` is the C# escape for the JSON key `do`.
- Test files contain small nested DTOs and fakes (tests are outside the strict scope).
- Assembly "errors" go to the report and are logged with `Debug.LogWarning`, not `LogError`, so the I1/I2 zero-console-error checks don't double count them. The report is the gate.

## KNOWN LIMITATIONS (nothing here has been run in a real Unity Editor)

This package was written without access to Unity. Its C# was syntax-checked and type-checked with `mcs` against hand-written API stubs. Those stubs encode *my* understanding of the Unity signatures, so the check proves internal consistency only, not that each API exists as used. The pure-logic parts (step parser, JSON preprocessor, conditions, naming) were run in a harness and pass. **First action in a real project:** open the Editor, fix any compile errors, run the EditMode tests, then run `RunAll` on the provider template plus one real asset per type.

APIs and behaviours I have not verified in 6000.6:

1. **Sprite Atlas V2 path:** `new SpriteAtlasAsset()`, `SpriteAtlasAsset.Save`, and the `SpriteAtlasImporter.packingSettings/textureSettings/includeInBuild` setters. The V1/V2 switch is detected with `EditorSettings.spritePackerMode.ToString().Contains("V2")` to stay compile-safe. Whether Unity 6 auto-migrates a V1 `.spriteatlas` created while V2 is enabled is untested.
2. **`TextureImporter.spriteBorder`** for Single sprites with the 2D Sprite package installed. It may be superseded by the `ISpriteEditorDataProvider` data.
3. **`ModelImporter.avatarSetup = CopyFromOther` + `sourceAvatar`** for Generic rigs (Human is the documented case).
4. **Animation events:** a clip event without an `OnV57AnimationEvent(string)` receiver logs *"AnimationEvent has no receiver"*, which is a console error that fails the gold path. Games must implement the receiver on animated objects, or the manifest must omit events.
5. **Convex MeshColliders on non-readable meshes in player builds:** `isReadable` is set only for `collision: exact`, per the brief. Blockout MeshColliders (non-convex) on non-readable meshes rely on build-time collision prebaking.
6. **The GPU ORM repack** (`Graphics.Blit` + `ReadPixels`) fails under `-nographics`. The result is a warning, and the material keeps URP defaults.
7. **URP Lit keywords set from script:** URP's material inspector revalidates keywords on edit, which should be consistent. URP 17 material upgrader interaction is unverified.
8. **Input System:**
   - `InputSettings.editorInputBehaviorInPlayMode` and `backgroundBehavior`, and replacing `InputSystem.settings` with a temporary instance;
   - `InputControlPath.TryFindControl(device, bindingPath)` for usage and wildcard paths;
   - `InputSystem.actions` (1.8+);
   - Touchscreen simulation through `QueueStateEvent(TouchState)`.

   `PlayerInput` with control-scheme auto-switching may not pair the simulated devices. Games using `PlayerInput` must allow auto-switch, or the gold path must use `control` paths that match the paired scheme.
9. **Coroutine exceptions:** if a step coroutine throws, Unity may stop the parent coroutine without running `finally`. The fallbacks are `OnDisable` teardown and Editor recovery, and `checks.json` may not be written in that case (the test then fails on the missing `LastPass`).
10. **Screenshots:** `ScreenCapture.CaptureScreenshotAsTexture` after `WaitForEndOfFrame` needs a rendering Game View. In `-batchmode`, `Camera.main` is rendered into a RenderTexture (`Camera.Render()` under URP), so overlay UI is missing. With `-nographics` no screenshots are possible.
11. **`SerializedProperty.objectReferenceInstanceIDValue`** (missing-reference detection) may be marked obsolete in later 6.x (EntityId migration). That would be a warning, not a compile error, as far as I know.
12. **Hierarchy diff:** `EditorApplication.EnterPlaymode()` from `unity command eval` is asynchronous; poll the output file. Batch mode is unsupported. The ignore lists (URP additional data, `[Debug Updater]`, test runner) will need extending for other engine-created objects. The diff **allow-lists every DontDestroyOnLoad root named `_Systems*`** and everything under it, so persistent services placed there are not checked for being prefab-based or for what they create beneath them.
13. **`LevelSceneBuilder` restoring the previous scene setup** is skipped if the previous setup contained an untitled scene.
14. **`ApplyImportRules`** force-reimports all art and audio, which is slow on big projects. It relies on `StartAssetEditing` batching.
15. **`JsonUtility` type mismatches** (e.g. `loop: "true"` as a string) are not coerced; such values keep their defaults. `JsonPreprocessor` does not handle keys containing escaped quotes.
16. **Heuristics:** marker `lossyScale` is copied as `localScale` (exact only without skew), and the LOD transition heights (0.5, 0.25, …, last ≤ 0.02) are heuristic defaults.
