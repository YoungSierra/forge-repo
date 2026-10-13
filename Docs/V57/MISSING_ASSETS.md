# MISSING_ASSETS — Professor Wort & Sprat

Single list of resources the game needs but the delivery lacks (TDD 1.0.0 §6, §9.1, §10, §13.1 vs files on disk, intake 2026-10-07). No placeholders are generated; every slot below stays empty until delivered. Rule: `V57/knowledge/assets-collision-builds.md`.

| Asset | Category | Required by | Expected path | Impact | Status |
|---|---|---|---|---|---|
| Level contract markers (spawn, zones, exits, kill, camera zone, patrols, doors, keys, flies) | Level | TDD §6 level contract | `Docs/Design/LevelMaps/Level_01/unity_scene.json` (`marcadores`) | — | delivered (Level_01 V10, D-270) |
| `MechanicalCrab` instances in the level | Level | TDD §6 (CrabEncounter) | `marcadores` Patrol + `trayectorias` | — | conflict (delivered: 11 patrols in V10; Z2 has 8 crabs vs LR-04 max 4 per zone) |
| Zone doors | Level | TDD §6 (KeyMaterialisation) | `marcadores` Door + LABDOOR leaves | — | delivered (V10: the B-17 lab double doors are the zone doors, D-270) |
| Level_01 V06 gap / side-room questions | Level | TDD §6 | — | — | superseded by the V10 layout |
| Second shark path (2 sharks placed, one loop `LVL01_MK_SHARK_LOOP`) | Level | TDD §6 AmbientCreatures | `unity_scene.json` `trayectorias` | the second shark shares the single loop | missing |
| Layout ids of the rigged characters (`AST-CHAR-NEONFLY-001` → `Fly`, `AST-CHAR-JELLYFISH-001` → `Jellyfish`, `AST-CHAR-RADIOACTIVESHARK-001` → `Shark`) | Level | TDD §6 (FlyCollection, AmbientCreatures) | `unity_scene.json` re-export | — (absorbed: `Docs/V57/layout_aliases.json`, D-252) | absorbed |
| Art Direction Document | Doc | lighting, camera framing references, post, UI layouts | `Docs/ArtDirection/ArtDirectionDocument.md` | blocks: visual review references | missing |
| UI mockups (`UI_HUDCounter`, `UI_MainMenu`, `UI_PauseMenu`, `UI_Loading`, `UI_LevelEnd`) | UI | TDD §9.1 | `Docs/ArtDirection/UIMockups/<ScreenId>.png` | blocks: visual (UI built from UXML/USS + palette) | missing |
| UI sprites per screen, icons `ICO_Fly`, `ICO_AccessKey` | UI | TDD §9.1, §13.1 | `Assets/_Game/Art/UI/Sprites/<ScreenId>/`, `Art/UI/Icons/` | — | delivered (owner UI sheet v02, sliced, D-268) |
| Rounded sans font with tabular numerals | UI | TDD §9.1, §13.1 | `Assets/_Game/Art/UI/Fonts/` | visual: UI uses the default font (no bold weight) | missing |
| Music: level base cue | Audio | TDD §10 | `Assets/_Game/Audio/Music/` | — | delivered (`MUS_PuffAndRebellion.wav`, D-258) |
| Music: level intensity layer (synchronised with the base cue), menu cue, key motif (1.2 s), door-unlock cadence, level-end sting | Audio | TDD §10, §13.1 | `Assets/_Game/Audio/Music/` | blocks: audio | missing |
| SFX (≈ 30: footsteps, jump/land, stomp, crab patter/contact/defeat, fly pop, key, door, UI) — full list + ElevenLabs prompts in `Docs/V57/SFX_PROMPTS_ELEVENLABS.md` | Audio | TDD §10, §B feedback | `Assets/_Game/Audio/SFX/<Category>/` | blocks: audio | missing (owner generating) |
| Ambience beds (3 zone themes) | Audio | TDD §10 | `Assets/_Game/Audio/Ambience/` | blocks: audio | missing |
| VO (≈ 22 non-verbal: Professor efforts, Sprat tonals) | Audio | TDD §10 | `Assets/_Game/Audio/Voice/` | blocks: audio | missing |
| VFX textures (land/footstep splash, stomp impact, crab sparks, fly pop, zone ring, key materialise, door unlock, respawn fade) | VFX | TDD §13.1, §B feedback | `Assets/_Game/Art/VFX/<EffectId>/` | blocks: visual (effects stay empty) | missing |
| Normal / ORM maps (environment kits) | Textures | URP Lit detail | `<asset>/Textures/` | — | delivered (Level_01 V06: albedo, normal, metallic-smoothness per asset) |
| Licenses | Doc | delivery checklist | `Docs/LICENSES.md` | blocks: release | missing |
| Characters + animations (Professor, Sprat, MechanicalCrab, Fly, Shark, Jellyfish, Fish) | Characters | TDD §13.1 | `Assets/_Game/Art/Characters/<Id>/` | — | delivered |
| AccessKey, ZoneDoor props | Props | TDD §13.1 | `Assets/_Game/Art/Props/Gameplay/` | — | delivered (`AST-PROP-ACCESSKEY-001`, `AST-PROP-ZONEDOOR-001`) |
| Sky | Environment | §8 | `Assets/_Game/Art/Environment/Sky/` | — | delivered (`SkyBox_1.png`) |
| Algae vertex colours (R bend, G phase, B flutter) for per-strand sway | Environment | owner request (D-263) | `AST-ENV-VEGETATION-001`, `AST_ENV_OCEANLEAVES-001` FBX | sway from height (whole clump in phase) | missing (meshes delivered separated in V10) |
| Door consoles (`Switch` markers) — intended mechanic? | Level | not in the TDD | design answer | markers only; doors open with the key | question |
| Hyphen naming for `AST_ENV_OCEANBOAT/OCEANROCKS/OCEANSKELETON/OCEANLEAVES/OCEANROCKLEAVES-001` | Naming | asset naming | provider re-export | — (absorbed by `layout_aliases.json`) | fixable |
