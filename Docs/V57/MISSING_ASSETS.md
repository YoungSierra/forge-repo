# MISSING_ASSETS — ProfesorSprat

Montage-only stage (D-001). No placeholders are generated; each gap below stays empty until the provider delivers it. Rule: `V57/knowledge/assets-collision-builds.md`.

| Asset | Category | Required by | Expected path | Impact | Status |
|---|---|---|---|---|---|
| TDD | Doc | I0 intake | `Docs/Design/TDD.md` | blocks: pipeline (I0) | missing |
| Art Direction Document | Doc | lighting, camera, post, materials | `Docs/ArtDirection/ArtDirectionDocument.md` | blocks: look (owner: arrives later) | missing |
| Licenses | Doc | delivery checklist | `Docs/LICENSES.md` | blocks: release | missing |
| Level blockout with `Marker_*` | Environment | level scene from markers | `Art/Environment/Blockout/<LevelId>/BLK_<LevelId>.fbx` | blocks: none now (JSON placement used) | missing |
| Albedo maps for NEONFLY, WATERSURFACE, ACCESSKEY, ZONEDOOR | Textures | materials | `<asset>/Textures/` | blocks: none (manifest flat colours used) | not delivered (by design?) |
| Normal / ORM maps (all 29 assets) | Textures | URP Lit detail | `<asset>/Textures/T_<Asset>_N`, `_ORM` | blocks: visual quality | missing |
| Animations | Characters | creature motion | `Art/Characters/<Id>/Animations/` | — | delivered for Professor, Shark, Sprat, Fish, Fly, Jellyfish, MechanicalCrab (D-007) |
| Sky / underwater background | Environment | backdrop | `Art/Environment/Sky/` | — | delivered (`SkyBox_1.png`, D-006); higher resolution (4096×2048+) recommended |
| Audio | Audio | — | `Assets/_Game/Audio/` | blocks: none now | missing |
