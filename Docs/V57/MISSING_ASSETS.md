# MISSING_ASSETS — Professor Wort & Sprat

Single list of resources the game needs but the delivery lacks (TDD 1.0.0 §6, §9.1, §10, §13.1 vs files on disk, intake 2026-10-07). No placeholders are generated; every slot below stays empty until delivered. Rule: `V57/knowledge/assets-collision-builds.md`.

| Asset | Category | Required by | Expected path | Impact | Status |
|---|---|---|---|---|---|
| Level contract markers: `Marker_Spawn_Player`, `Marker_Zone_<ZoneId>`, `Marker_Exit_<ZoneId>`, `Marker_Kill_<NN>`, `Marker_CameraZone_Default`, `Marker_Patrol_<Crab>` | Level | TDD §6 level contract (ZoneFlow, RespawnService, CameraRig, CrabEncounter) | `Docs/Design/LevelMaps/Level_01/unity_scene.json` (`asset_id: "Marker"`) | blocks: gameplay on the delivered map (Level_01 V06 has none; temporary contract D-253) | missing |
| `MechanicalCrab` instances in the level | Level | TDD §6 (CrabEncounter) | `unity_scene.json` objects with `asset_id: "MechanicalCrab"` | blocks: crab gameplay on the delivered map (temporary crab, D-253) | missing |
| `ZoneDoor` instance in the level (`AST-PROP-ZONEDOOR-001`), or confirmation that the `AST-ENV-LABDOOR-001` pair in the right-tunnel arch is the zone door | Level | TDD §6 (KeyMaterialisation: one key anchor + one ZoneDoor per zone) | `Docs/Design/LevelMaps/Level_01/unity_scene.json` | blocks: door on the delivered map (temporary door inside the right tunnel, D-253) | missing |
| Level_01 gap fix: platform x[−0.6, 2.4] → key platform x[6, 9] is 3.6 m with a 1.6 m rise | Level | TDD §6 LR-02 (gap ≤ 3.2 m) | `Docs/Design/LevelMaps/Level_01/unity_scene.json` | jump only possible diagonally at full speed | conflict |
| Level_01 side rooms (x ±36–53, floor y 9.3): intended reachable? no route reaches them | Level | TDD §6 | design answer | rooms are scenery until a route exists | question |
| Layout ids of the rigged characters (`AST-CHAR-NEONFLY-001` → `Fly`, `AST-CHAR-JELLYFISH-001` → `Jellyfish`, `AST-CHAR-RADIOACTIVESHARK-001` → `Shark`) | Level | TDD §6 (FlyCollection, AmbientCreatures) | `unity_scene.json` re-export | — (absorbed: `Docs/V57/layout_aliases.json`, D-252) | absorbed |
| Art Direction Document | Doc | lighting, camera framing references, post, UI layouts | `Docs/ArtDirection/ArtDirectionDocument.md` | blocks: visual review references | missing |
| UI mockups (`UI_HUDCounter`, `UI_MainMenu`, `UI_PauseMenu`, `UI_Loading`, `UI_LevelEnd`) | UI | TDD §9.1 | `Docs/ArtDirection/UIMockups/<ScreenId>.png` | blocks: visual (UI built from UXML/USS + palette) | missing |
| UI sprites per screen, icons `ICO_Fly`, `ICO_AccessKey`, rounded sans font | UI | TDD §9.1, §13.1 | `Assets/_Game/Art/UI/Sprites/<ScreenId>/`, `Art/UI/Icons/`, `Art/UI/Fonts/` | blocks: visual | missing |
| Music: level base cue | Audio | TDD §10 | `Assets/_Game/Audio/Music/` | — | delivered (`MUS_PuffAndRebellion.wav`, D-258) |
| Music: level intensity layer (synchronised with the base cue), menu cue, key motif (1.2 s), door-unlock cadence, level-end sting | Audio | TDD §10, §13.1 | `Assets/_Game/Audio/Music/` | blocks: audio | missing |
| SFX (≈ 30: footsteps, jump/land, stomp, crab patter/contact/defeat, fly pop, key, door, UI) | Audio | TDD §10, §B feedback | `Assets/_Game/Audio/SFX/<Category>/` | blocks: audio | missing |
| Ambience beds (3 zone themes) | Audio | TDD §10 | `Assets/_Game/Audio/Ambience/` | blocks: audio | missing |
| VO (≈ 22 non-verbal: Professor efforts, Sprat tonals) | Audio | TDD §10 | `Assets/_Game/Audio/Voice/` | blocks: audio | missing |
| VFX textures (land/footstep splash, stomp impact, crab sparks, fly pop, zone ring, key materialise, door unlock, respawn fade) | VFX | TDD §13.1, §B feedback | `Assets/_Game/Art/VFX/<EffectId>/` | blocks: visual (effects stay empty) | missing |
| Normal / ORM maps (environment kits) | Textures | URP Lit detail | `<asset>/Textures/` | — | delivered (Level_01 V06: albedo, normal, metallic-smoothness per asset) |
| Licenses | Doc | delivery checklist | `Docs/LICENSES.md` | blocks: release | missing |
| Characters + animations (Professor, Sprat, MechanicalCrab, Fly, Shark, Jellyfish, Fish) | Characters | TDD §13.1 | `Assets/_Game/Art/Characters/<Id>/` | — | delivered |
| AccessKey, ZoneDoor props | Props | TDD §13.1 | `Assets/_Game/Art/Props/Gameplay/` | — | delivered (`AST-PROP-ACCESSKEY-001`, `AST-PROP-ZONEDOOR-001`) |
| Sky | Environment | §8 | `Assets/_Game/Art/Environment/Sky/` | — | delivered (`SkyBox_1.png`) |
