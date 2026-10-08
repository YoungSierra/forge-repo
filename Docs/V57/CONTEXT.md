# CONTEXT — Professor Wort & Sprat (game architecture)

Source of truth: `Docs/Design/TDD.md` 1.0.0 (TDD Standard 2.1.0). This file describes how the implementation is organised; V57 specs live in `V57/specs/ProfessorWortSprat/features/`.

## Assemblies (`Assets/_Game/Scripts/`)

| Assembly | Folder | Contents |
|---|---|---|
| `ProfessorSprat.Core` | `Core/` | `EventBus` (typed, synchronous) + the 20 TDD events (`PlayerEvents.cs`, `WorldEvents.cs`) |
| `ProfessorSprat.Gameplay` | `Gameplay/` | `Config/` (9 ScriptableObject configs), `Player/` (Locomotion, Jump, Stomp + models, DropShadow), `Animation/`, `Flies/`, `AccessKey/`, `Crab/`, `Sprat/`, `Flow/` (ZoneFlow, LevelContent, Respawn, Save), `CameraSystem/` (CameraRig), `Input/` (InputHandler), `Services/` (Audio, VFX, AmbientCreature), `UI/` |
| `ProfessorSprat.GoldPath` | `GoldPath/` | `ProfessorWortSpratGoldPathProbe` (read-only) |

## Scene contract (`SCN_HydroStation_Gameplay`)

- `_Environment` + `_Environment/_Markers` + `_Gameplay/Level` = **level content** (rebuilt by `AssemblyRunner.RebuildLevelContent()` when a new map arrives). Gameplay prefabs placed by the layout land in `_Gameplay/Level`.
- Everything else is the gameplay setup and is never touched by a rebuild: `_Gameplay/Professor`, `_Gameplay/Sprat`, `_Systems/GameSystems` (ZoneFlow, ZoneFlyTracker, SaveService, RespawnService, InputHandler, CameraRig, AudioService, VFXService), `_Systems/GoldPathProbe`, `_Cameras` (Main Camera + CinemachineBrain, `CM_Follow`, `CM_KeyShot`, `KeyShotGroup`), `_UI` (HUD, PauseMenu, LevelEnd, Loading — one UIDocument each, `PS_PanelSettings`).
- **ZoneFlow** references only the two level containers and reads the §6 level contract at Init (`LevelContent.Scan`): markers (`Spawn_Player`, `Zone_<Id>`, `Exit_<Id>`, `Kill_*`, `CameraZone_*`, `Patrol_<Crab>`) and actors inside each zone volume (flies, crabs, key, door). No references to individual level objects, no global lookups.

## Init order (ZoneFlow, execution order −200)

Save load → `LevelContent.Scan` (contract problems logged as warnings) → Professor teleported to `Marker_Spawn_Player` → CameraRig bind (default yaw from `Marker_CameraZone_Default`) → Sprat bind → Respawn bind (kill volumes) → zone 1 (fly registration closes, crab patrols, key armed with its door) → input on.

## Gameplay prefabs (`Assets/_Game/Prefabs/Gameplay/`)

Variants of the generated Visual prefabs: `PRF_Professor` (CharacterController r 0.35 h 1.09, Locomotion/Jump/Stomp/AnimationDriver, DropShadow), `PRF_Sprat`, `PRF_Fly`, `PRF_MechanicalCrab`, `PRF_Shark` (AmbientCreature), `PRF_AST-PROP-ACCESSKEY-001` (AmberLight child), `PRF_AST-PROP-ZONEDOOR-001` (BoxCollider blocker), walkable kit pieces `PRF_AST-ENV-{PLATFORMMODULE,LABFLOOR,B17FLOORPLATE,GRATEPLATFORM}-001` (BoxCollider from mesh bounds, collider on the piece's own prefab).

## Input, tags and layers

- Input: `Assets/_Game/Settings/Input/PS_Input.inputactions` (maps `Gameplay`: Move, Jump, Stomp, Pause; `UI`: Navigate, Submit, Cancel). Move is camera-relative (CameraRig active yaw, deferred basis switch).
- Tags: Player, Environment, Interactable, Hazard, Trigger, Projectile, UIWorld, Enemy, Collectible. Layers: Player 8, Environment 9, Interactable 10, Hazard 11, Trigger 12, Projectile 13, UIWorld 14, Enemy 15, Collectible 16.

## Config assets (`Assets/_Game/Data/Config/`)

`LocomotionConfig`, `JumpConfig`, `StompConfig`, `FlyConfig`, `AccessKeyConfig`, `CrabConfig`, `SpratConfig`, `CameraConfig`, `RespawnConfig` — TDD values as defaults, read-only at runtime.
