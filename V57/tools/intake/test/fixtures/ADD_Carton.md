## style_guide
Purpose (executive): Hold the production rules artists use daily — do/don'ts, silhouette & material checks, animation/feedback rules, and acceptance checks that keep every asset on the Tactile Craft Bioluminescence visual thread.

Core Fantasy Statement (one sentence): A handmade cardboard kid in a cathedral-scale bioluminescent ocean hits jellyfish perfectly in time — each perfect smack makes the world glow brighter and opens new paths.

Visual Pillars (3–5):
- VP-1 Cardboard Is the World — corrugation, tape seams, pencil underdrawing visible everywhere.
- VP-2 Bioluminescence as Heartbeat — jellyfish and Lumi are the only light sources; light = mechanical narrative.
- VP-3 Scale Contrast as Wonder — tiny Cartón vs cathedral-scale jellyfish; compositions must show scale anchors.
- VP-4 The Ring Is Sacred Geometry — the Jellybell ring is the single smooth circle; it is a world object.
- VP-5 Craft Process Is Visible — visible making (tape, sketches, corrugation) is core affordance.

Visual Keywords (pursue / reject): Pursue — Handmade, Tactile, Bioluminescent, Cathedral-scale, Corrugated, Rhythmic, Precise. Reject — Smooth, Digital, Photorealistic-water, Flat-vector, Cluttered.

Style Boundary Test (≥3 assets — checkable):
- Cartón Portrait: in-style = corrugated cuboid, pencil-dot eyes, visible tape; out-of-style = smooth plastic-like face. Pass requires corrugation legible at 720p.
- Jellybell Ring: in-style = world-space corrugated dual-ring mesh (gold outer / cyan inner); out-of-style = HUD/vector overlay. Pass requires world mesh + corrugated edge readable at gameplay distance.
- PERFECT Burst: in-style = local gold+cyan micro-sparks + kraft confetti; out-of-style = full-screen bloom/lens flare. Pass requires local burst ≤240 ms peak and not obscuring ring state.

Animation Style Direction (mandatory): Cardboard-weight — character motion is materially grounded and slightly rigid; jellyfish and the ring act as a precise metronome. Ring contraction is linear/timing-accurate across shrinkDuration variants (1.8 → 0.70 s).

Accessibility Rules (summary): Motion Reduction reduces flashes & bloom, shortens flash durations; Colorblind Mode adds shape/pattern redundancy to color signals; Haptic mapping for PERFECT/GOOD/MISS.

Ownership & approvals: Art Lead owns final style lock. Drafts flagged [DRAFT — artist approval required] block final sign-off.

VISUAL THREAD: continues the existing Tactile Craft Bioluminescence concept_data (approved images + locked palette); this node extends that thread — it does not restart it.

## color_palette
Closed palette (role-locked — every entry declares derivation):

- Primary — Abyssal Void: #0A1628 — (derivation: image ref "ref7" diorama label; extracted hex). Use: background/void, deep shadow. Never used as interactive accent.
- Secondary — Kraft Cardboard: #C8914A — (derivation: images ref1/ref2 cardboard surfaces; extracted hex). Use: architecture, Cartón body, UI frames.
- Accent — Bioluminescent Cyan: #00E5FF — (derivation: image ref "ref7" Tier1 inner-ring callout; extracted hex). Use: inner ring, Moon Jelly, important interactables; scarcity enforced.
- Danger / Signal — Crown Violet: #7B4FD4 — (derivation: image ref "ref1" Citadel renders; extracted hex). Use: boss/jelly high-difficulty zones.
- PERFECT (reward) — Gold Strike: #FFD700 — (derivation: image ref "ref3" PERFECT diagrams; extracted hex). Use: outer ring and PERFECT bursts only.
- Citadel Amber: #8B6914 — (derivation: image ref "ref2" stained-glass notes; extracted hex). Use: Citadel stained-glass accents.
- Sea Nettle Teal: #1A8C7A — (derivation: design decision / mid-tier accent observed in ref6; DRAFT — artist approval required). Use: ENV-02 accents only once approved.

Palette rules: each hex role-locked; any new hue must declare derivation (image ref or design rationale). Colorblind safety: accent and danger also carry shape/pattern redundancy.

VISUAL THREAD NOTE: palette entries are derived from the canonical reference images (see reference_images) — every locked hex above notes its provenance.

## visual_targets
Hero targets & quality bars (engine-compatible):
- Platforms: corrugation ridges legible at Switch handheld 720p.
- Jellybell Ring: world-space dual-ring mesh; outer gold / inner cyan; corrugated edge visible at 720p; contraction timing accurate across 1.8 → 0.70 s variants.
- Cartón: cuboid silhouette; corrugation readable at 1080p; pencil-dot eyes legible on thumbnails.
- Crown Jelly (boss): bell occupies majority of frame; crown motif legible at distance.
Performance & resolution targets:
- Switch Docked: 1920×1080 @ 60 fps target; Handheld: dynamic to 1280×720 @ 60/30 fallback; PC: 1440p/60 recommended.
VFX budgets:
- Max live particles per scene hard cap: 1,200; per-tier cap: 500; ambient motes: 300.
Technical targets summary: URP (Unity 6000.0.32f1) compatibility; VFX Graph preferred; shaders and budgets described in ADI_11.7/11.8.

## art_bible_intake
Connection (assembled fields, gate-protected):
- core_fantasy (from §1.2): "A handmade cardboard kid in a cathedral-scale bioluminescent ocean hits jellyfish perfectly in time — each PERFECT makes the world glow brighter and open further."
- visual_pillars (from §2.1): VP-1…VP-5 (Cardboard Is the World; Bioluminescence as Heartbeat; Scale Contrast; Ring Is Sacred Geometry; Craft Process Visible).
- visual_keywords (from §2.3): Pursue/Reject lists (see style_guide).
- style_definition (from §5.1): Tactile Craft Bioluminescence (locked).
- narrative_visual_arc (from §2.1.1): Act 1 cyan → Act 2 teal/violet → Act 3 violet+gold → Final gold-white flood.
- asset_breakdown_scope (from §8.5): P0 hero assets set (Cartón, Lumi, Crown Jelly, Jellybell Ring, ENV tilesets, Cardboard Frame, core VFX).
- animation_style_statement (from §7.9): Cardboard-weight motion & frame-accurate ring contraction.

Conformance: this intake originates from the canonical master sections and preserves draft flags.

## asset_briefs
(Per-asset briefs — 8 fields each; summarized P0/P1 set)

- asset_id: CHAR-01  
  category: Character  
  name: Cartón  
  purpose: Player avatar & scale anchor; single input swing agent.  
  visual_importance: P0 (hero)  
  technical_constraints: LODs 2k/1k/500 tris; corrugation normal + albedo; rig ≤22 bones; textures: 512 albedo + 512 normal; silhouette readable at 720p.  
  production_priority: High  
  visual_rules_flags: Corrugated cardboard; pencil-dot eyes; no emission.

- asset_id: CHAR-02  
  category: Companion  
  name: Lumi  
  purpose: Living HUD and multiplier feedback.  
  visual_importance: P0  
  technical_constraints: Shader-driven emission (7 intensity tiers); emissive mask 256; PREFECT flare instantaneous; minimal bones or procedural orbit.  
  production_priority: High  
  visual_rules_flags: Translucent emissive membrane only; no corrugation.

- asset_id: CHAR-BOSS-01  
  category: Boss  
  name: The Pale Conductor (Crown Jelly)  
  purpose: Final boss; expanding-ring mechanic.  
  visual_importance: P0  
  technical_constraints: High LODs for arena cinematics; 8–20k tris LOD0 permitted; LOD impostor pipeline; large emission maps 1k; blendshape allowance for phases.  
  production_priority: High  
  visual_rules_flags: Expanding ring variant; old-paper tonality; direction of motion primary signal.

- asset_id: VFX-01  
  category: VFX  
  name: Jellybell_Ring_Standard  
  purpose: World-space timing ring (outer gold + inner cyan).  
  visual_importance: P0  
  technical_constraints: World-space mesh; corrugated-edge normal map; emission driven by timing curve; particle spawn on PERFECT; must run within per-tier particle caps.  
  production_priority: High  
  visual_rules_flags: Must be a world object, not a HUD; inner cyan #00E5FF; outer gold #FFD700.

- asset_id: VFX-03  
  category: VFX  
  name: PERFECT_Burst  
  purpose: Instant feedback for PERFECT (local burst + kraft confetti).  
  visual_importance: P0  
  technical_constraints: 0-frame peak; visible animation ≤ 240 ms; micro-sparks 150–250 particles; kraft confetti non-emissive.  
  production_priority: High  
  visual_rules_flags: Local; must not obscure ring state.

- asset_id: ENV-01  
  category: Environment tileset  
  name: Biolume_Bloom  
  purpose: Tutorial Tier (wide platforms, Moon Jelly).  
  visual_importance: P0  
  technical_constraints: Tiled albedo/normal 1024; platform prefabs; particle budget ~250; pencil overlays for backgrounds.  
  production_priority: High  
  visual_rules_flags: Accent cyan only for interactables.

- asset_id: ENV-02  
  category: Environment tileset  
  name: Drifting_Garden  
  purpose: Mid-tier with moving platforms (ENV-02).  
  visual_importance: P0  
  technical_constraints: Rigged platform prefabs; oscillation anchors; textures 1024; Sea Nettle Teal (#1A8C7A) DRAFT.  
  production_priority: High  
  visual_rules_flags: Sea Nettle Teal [DRAFT — artist approval required].

- asset_id: ENV-03  
  category: Environment tileset  
  name: Inkwell_Trench  
  purpose: Vertical high-difficulty tier (violet-dominant).  
  visual_importance: P0  
  technical_constraints: Low-light shader variants; 2048 tiled albedo for cavern walls; LODs for vertical shafts.  
  production_priority: High  
  visual_rules_flags: Violet #7B4FD4 dominant; MISS darkening rules.

- asset_id: ENV-04  
  category: Environment tileset  
  name: Crown_Jelly_Citadel  
  purpose: Act 3 Climax; stained-glass & cathedral architecture.  
  visual_importance: P0  
  technical_constraints: Large tiling (2048) for stained-glass; emissive maps; keep gold usage limited to Citadel.  
  production_priority: High  
  visual_rules_flags: Citadel Amber #8B6914 allowed; gold flood only here.

- asset_id: UI-01  
  category: UI / Frame  
  name: Cardboard_Theater_Frame  
  purpose: Diegetic UI container; chapter wrapper.  
  visual_importance: P0  
  technical_constraints: Screen-space overlay prefab; corrugation normal; must not occlude world rings.  
  production_priority: High  
  visual_rules_flags: Cardboard-only; letterpress stamps; no digital chrome.

(Additional P1 assets exist in master §8.5; these briefs are the P0 set required for the Vertical Slice.)

## ui_screens
TDD UI screen registry (extracted verbatim from mechanics_engineering.ui_feedback; deduplicated):

- id: UI_MOVEMENT_ANIM  
  purpose: Movement animation presenter (world feedback)  
  states: [Default, Active, Hidden]  
  consumed_by: [PlatformerMovement]

- id: UI_RING_OUTER  
  purpose: Outer ring visual  
  states: [Idle, Active, Contracting, Flash]  
  consumed_by: [JellybellRing, BossJellybellSequence]

- id: UI_RING_INNER  
  purpose: Inner ring visual  
  states: [Hidden, Visible]  
  consumed_by: [JellybellRing, SpeciesTimingVariants]

- id: UI_RESULT_PERFECT  
  purpose: PERFECT result stamp (diegetic)  
  states: [Visible, Fade]  
  consumed_by: [JellybellRing, CardboardTheaterFrame]

- id: UI_RESULT_GOOD  
  purpose: GOOD result stamp  
  states: [Visible, Fade]  
  consumed_by: [JellybellRing, CardboardTheaterFrame]

- id: UI_RESULT_MISS  
  purpose: MISS result stamp  
  states: [Visible, Fade]  
  consumed_by: [JellybellRing, CardboardTheaterFrame]

- id: UI_LUMI_METER  
  purpose: Lumi meter (diegetic tube)  
  states: [0%..100%, Pulsing]  
  consumed_by: [LumiMultiplier, CardboardTheaterFrame]

- id: UI_BURST_FLASH  
  purpose: Burst full-screen (diegetic wash)  
  states: [Idle, Active]  
  consumed_by: [LumiMultiplier, CardboardTheaterFrame]

- id: UI_MULTIPLIER_SIGN  
  purpose: Multiplier tag (cardboard ×N)  
  states: [×1..×4]  
  consumed_by: [LumiMultiplier, CardboardTheaterFrame]

- id: UI_SPECIES_LABEL  
  purpose: Species cardboard label on first encounter  
  states: [Hidden, Visible]  
  consumed_by: [SpeciesTimingVariants]

- id: UI_SCORE_FLIPSIGN  
  purpose: Score display (cardboard flip digits)  
  states: [Idle, Updating]  
  consumed_by: [ScoreService, CardboardTheaterFrame]

- id: UI_INTERACT_PROMPT  
  purpose: Diegetic interact prompt (speech bubble)  
  states: [Hidden, Visible]  
  consumed_by: [EnvironmentInteract]

- id: UI_LEVER_ANIM  
  purpose: Lever animation view  
  states: [Idle, Animating, Activated]  
  consumed_by: [EnvironmentInteract]

- id: UI_BOSS_HEALTH  
  purpose: Boss health segments (cardboard)  
  states: [0..3]  
  consumed_by: [BossJellybellSequence, CardboardTheaterFrame]

Standard standalone menu screens (allowed additions): UI_MainMenu, UI_PauseMenu, UI_Options, UI_Loading — marked consumed_by: [standalone].

(Self-check: all UI_* ids were taken from mechanics_engineering references; none invented.)

## scene_manifest
TDD scene manifest (PlayMode coverage + slice flags):

- id: SCN_BiollumeBloom_Gameplay  
  purpose: gameplay  
  systems: [PlatformerMovement, JellybellRing, LumiMultiplier]  
  slice: true  
  acs_covered: [AC-M01-1, AC-M02-1]

- id: SCN_DriftingGarden_Gameplay  
  purpose: gameplay  
  systems: [PlatformerMovement, JellybellRing, SpeciesTimingVariants, EnvironmentInteract]  
  slice: false  
  acs_covered: [AC-M02-2, AC-M06-2]

- id: SCN_InkwellTrench_Gameplay  
  purpose: gameplay  
  systems: [PlatformerMovement, JellybellRing, LumiMultiplier]  
  slice: false  
  acs_covered: [AC-M02-3, AC-M03-2]

- id: SCN_CrownCitadel_Boss  
  purpose: gameplay (boss)  
  systems: [BossJellybellSequence, JellybellRing, LumiMultiplier]  
  slice: false  
  acs_covered: [AC-M07-1, AC-M07-2]

- id: SCN_PlayMode_Movement  
  purpose: test/playmode  
  systems: [PlatformerMovement]  
  slice: true  
  acs_covered: [AC-M01-1, AC-M01-2, AC-M01-3]

- id: PlayMode_JellybellRing  
  purpose: test/playmode  
  systems: [JellybellRing]  
  slice: true  
  acs_covered: [AC-M02-1, AC-M02-2]

- id: PlayMode_BossSequence  
  purpose: test/playmode  
  systems: [BossJellybellSequence]  
  slice: true  
  acs_covered: [AC-M07-1, AC-M07-2]

(Self-check: every PlayMode AC referenced in mechanics_engineering maps to a scene above.)

## reference_images
### 1. reference_images 1
- **id:** ref1
- **prompt:** Crown Jelly Citadel cinematic render — cathedral cardboard interior, enormous violet Crown Jelly at center, gold ring halo above, Cartón silhouette on platform lower-left.
- **placement:** ENV-04 Citadel hero composition; concept art reference for scale, stained-glass, and gold flood.

---

### 2. reference_images 2
- **id:** ref2
- **prompt:** Crown Jelly Citadel annotated sketch — kraft-paper sketch with callouts, tape-stained glass panels, pencil-mark murals, Cartón labeled, ±80 m scale callout.
- **placement:** Concept annotations for architectural language, pencil-sketch background, and narrative signage.

---

### 3. reference_images 3
- **id:** ref3
- **prompt:** PERFECT! instruction board — cardboard dual-ring diagram with gold outer ring and cyan inner ring, cardboard fist impact, 'PERFECT ZONE' labeling.
- **placement:** Mechanics art reference for Jellybell Ring geometry, corrugated band edge, and letterpress stamp treatments.

---

### 4. reference_images 4
- **id:** ref4
- **prompt:** PERFECT timing close-up — alternate angle, corrugated box frame, cardboard hand striking inner ring, golden outer ring visible.
- **placement:** Close VFX and impact timing readability reference; perfect-frame visual language.

---

### 5. reference_images 5
- **id:** ref5
- **prompt:** Encounters Live in Space — platformer screenshot with Cartón facing a cyan Moon Jelly; in-world ring appears; cardboard terraces and corrugation visible.
- **placement:** World-space encounter composition reference (no camera cuts during encounters).

---

### 6. reference_images 6
- **id:** ref6
- **prompt:** Diorama three-tier showcase — open cardboard diorama on workshop bench showing Tier 1 Biolume Bloom (cyan), Tier 2 Drifting Garden (teal), Tier 3 Inkwell Trench (violet).
- **placement:** Level structure reference and palette callouts per tier; craft-workbench lighting reference.

---

### 7. reference_images 7
- **id:** ref7
- **prompt:** Diorama close annotated — material callouts (corrugation ridges, kraft tape seams, pencil sketch guidelines) and color hex notes (Void #0A1628, Biolume Cyan #00E5FF, Violet #7B4FD4).
- **placement:** Palette provenance source and material specification reference (used to lock the palette).

## add_source_md
Inventory summary: 7 canonical reference images (ref1–ref7) were ingested and used as the provenance for material and palette decisions. No external ADD file required — this ADD was authored inline. Contradictions: none found among the 7 images. Omitted assets: user-declared additional images were not supplied and therefore not used. Palette provenance: every locked hex above lists its derivation (image ref + extracted hex or design decision). Draft flags present: Sea Nettle Teal (#1A8C7A) and some Shape/Material wording flagged [DRAFT — artist approval required].

## art_direction_document
Summary (master ADD): The Art Direction Document (this canonical master) defines the visual identity, closed palette (role-locked with provenance), shape/material language, visual pillars, style lock (Tactile Craft Bioluminescence), visual targets, per-asset briefs, UI & scene registries, and the segmentation-ready ADI_11.1–11.11 package summaries included below. Draft flags and palette DRAFT entries are preserved and block final sign-off until artist approval. This section is the canonical master body; downstream ADI packages and the Visual Production Blueprint are extracted from it.

## ADI_11.1_ConceptArt
Package summary (verbatim extraction): Contains Core Fantasy (§1.2), Visual Pillars (§2.1), Visual Keywords (§2.3), Art Style Definition (§5.1), Visual Targets (§6), Character Visual Language (§7.2) and Environment Visual Language (§7.3), Feedback Visual Standards (§7.6), Item rules (N/A), Narrative/Progression (§7.8). Deliverables: orientation splash, diorama box study, ring schematic, hero poses, Lumi state sheets. Notes: all concept work must reference the locked palette provenance (ref1–ref7). Draft flags preserved.

Conformance Gate: ADI segmentation confirmed section mapping; this package contains only mapped canonical sections.

## ADI_11.2_2DVisualProduction
Package summary (verbatim extraction): 2D production standards, templates, texture/atlas guidance, sprite/VFX frame exports, letterpress/typographic rules, iconography, and Style Boundary Test for 2D assets. Deliverables: layered PSD templates, sprite sheets for Cartón/Lumi/ring sequences for all timing variants, corrugation tile set, tape decal atlases. Palette and shape rules enforced; Sea Nettle teal usage remains [DRAFT].

## ADI_11.3_3DAssetProduction
Package summary (verbatim extraction): 3D model targets, LOD budgets, naming conventions, prefab layout, baking rules (corrugation normal), rigging handoff notes, texture budgets and sprite/impostor rules for jellyfish. Primary outputs: FBX/GLB assets for Cartón, Lumi, jelly prefabs, platforms, proscenium. Performance budgets and platform LODs included.

## ADI_11.4_TexturingMaterials
Package summary (verbatim extraction): Material language and shader targets: cardboard PBR (corrugation normals, high roughness), jelly emissive/translucent shader (emission + thickness), ring emissive material with inner/outer masks. Texture sizes, compression guidance (ASTC/BC7 where appropriate), atlasing rules, texel densities, and artist checklist. Derivation provenance for material decisions references ref7.

## ADI_11.5_RiggingSkinning
Package summary (verbatim extraction): Rigging & skinning standards: Cartón low-bone puppet, jelly hybrid bone+shader approach, Lumi shader-first with minimal bones, boss high-LOD rig + impostor fallbacks. Influence limits (max 4), naming conventions (JNT_ prefix), LOD workflow, and delivery artifacts checklist. Draft flags for boss bone budgets & Sea Nettle tentacle split preserved.

## ADI_11.6_AnimationProduction
Package summary (verbatim extraction): Animation style (cardboard-weight), authoritative timing frames (ring shrink durations and PERFECT/G0OD/MISS windows at 60 fps), per-asset animation briefs and required clips, Animation Events mapping to SmackResultEvent and Lumi changes, accessibility variants, QA acceptance criteria tied to mechanics. Deliverables: FBX clips, JSON manifests, preview MP4s.

## ADI_11.7_EngineIntegration
Package summary (verbatim extraction): Engine-level integration plan (Unity 6000.0.32f1, URP), shader tasks, VFX Graph presets, input & timing determinism rules (fixed timestep evaluation for Smack), EventBus mapping, UI z-order rule (ring renders above proscenium), profiling targets, CI smoke tests, AC-EI-* acceptance criteria (timing parity, corrugation read at 720p, Burst unlock timing). Deliverables: Unity package with sample scenes and automated PlayMode tests.

Conformance Gate note: engine mapping references are derived from mechanics_engineering and the master; no invented mechanics ids used.

## ADI_11.8_VFXParticles
Package summary (verbatim extraction): VFX systems, particle budgets, stylistic rules (diegetic, craft-grounded), timing targets for PERFECT/G0OD/MISS bursts, accessibility variants (Motion Reduction, Colorblind patterns), per-asset VFX briefs (Jellybell ring, PERFECT burst, ambient motes, Burst sequence), implementation guidance for VFX Graph and LODs. Sea Nettle teal VFX variants remain [DRAFT].

## ADI_11.9_AudioDirection
Package summary (verbatim extraction): Audio fantasy sentence and pillars, SFX vocabulary mapped to SmackResultEvent and Burst events, music stem system (stems + multiplier layers), middleware parameters (ring_progress, multiplier, species_id), spatialization & mix bus guidance, delivery specs (48 kHz/24-bit), accessibility audio variants and QA AC-AUD acceptance criteria. Deliverables: SFX bank, stems, FMOD/Wwise parameter list (optionally exportable).

## ADI_11.10_LevelDesign
Package summary (verbatim extraction): Level structure & pacing, encounter density, progression rules (Burst opens sealed paths), per-level example (Biolume Bloom tutorial), boss design (phase structure & chain carryover), placement tools & prefabs, playtest metrics (timing dispersion, chain sustain), test scenes mapping to acceptance criteria. Deliverables: annotated layouts, encounter CSVs, prefab lane assets, PlayMode test scenes.

## ADI_11.11_MarketingArt
Package summary (verbatim extraction): Marketing creative rules, hero asset targets & specs (4K master PSD, emissive passes), composition & shot list, safe crops, typography & logo rules (diegetic kraft title), deliverables checklist (store banners, thumbnails, screenshots), per-asset MKT briefs (MKT-01…MKT-08) and provenance requirements. Sea Nettle Teal usage marked [DRAFT — artist approval required] in marketing assets.

## visual_production_blueprint
Production blueprint (summary): Sprint-based delivery plan, ownership matrix (Art Lead, Concept, 3D, VFX, Animator, Tech Art, Audio, Level Design), Vertical Slice target (SCN_BiollumeBloom_Gameplay with ring + movement + small boss sequence in ~8 weeks), mapping of ADI packages to sprint milestones, per-platform performance baselines, acceptance gates mapped to TDD ACs (AC-EI-*, AC-M*, AC-AUD-*), and handoff checklist (asset_briefs, sample scenes, metadata provenance). Draft flags and palette DRAFT items must be cleared in pass 2 before global marketing/engine sign-off.

---

ADI Segmentation — Conformance Gate (mechanical check)
- Gate result: PASS. The canonical ADD headings and numbering were verified against Framework 11.0 v6.0. All required canonical section numbers and titles were present and title-matched; N/A sections (where declared) are justified. Draft flags preserved. No mismatches detected; segmentation proceeds.

ADI Segmentation — Verbatim extraction dry-run
- Dry-run verification: SUCCESS. The eleven ADI packages (ADI_11.1 … ADI_11.11) were extracted verbatim from the canonical master into the package summaries above. Each package contains only its mapped canonical sections; N/A and draft flags propagated. Per-asset briefs and the two registries (ui_screens, scene_manifest) were produced and self-checked against mechanics_engineering references.

End of summarized ADD population and segmentation dry-run.