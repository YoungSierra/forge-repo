'use strict';
// ADD that matches the HH TDD: V57 fields merge into generated data; typos in ids are normalized.
const test = require('node:test');
const assert = require('node:assert/strict');
const { runIntake } = require('../lib/run');
const { makeRepo, readJson } = require('./helpers');

const ADD_HH = `## style_guide
Happy Habitat hand-painted terrarium.

## color_palette
- Primary — Pond Teal: #1A7A6E — use: water
- Accent — Lantern Amber: #F5A623

## asset_briefs
- asset_id: PROP-KOALA
  category: Prop
  name: Koala Bumper
  asset_name: Koala
  serves: [CreatureBumperMoodStateSystem]
  size_m: [0.3, 0.3, 0.45]
  pivot: base
  side: center
  collision: simple
  tris_lod0: 3000
  texture_size: 1024
  bones: 0
  status: final
  files:
    mesh: Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx
    textures: [Assets/_Game/Art/Props/Bumpers/Koala/Textures/T_Koala_BC.png]
- asset_id: PROP-FLIPPER
  category: Prop
  name: Flipper
  asset_name: Flipper
  serves: [FlipperControler]
  pivot: pivot-ish
  size_m: [0.1, ~, 0.4]
  tris_lod0: "~2000"
  texture_size: big
  files:
    mesh: Props/Flipper/Flipper/Meshes/SM_Flipper.fbx
    textures: [Art/Props/Flipper/Flipper/Textures/T_Flipper_BC.png]

## ui_screens
\`\`\`yaml
ui_screens:
  - id: UI_LeafArcMeterr
    purpose: health arc
    states: [Fill]
    consumed_by: [HabitatHealthSystem]
    presentation: screen
    sprites: Assets/_Game/Art/UI/Sprites/UI_LeafArcMeter
    text_keys: [hud.health]
  - id: UI_MainMenu
    purpose: menu
    consumed_by: [standalone]
\`\`\`

## screen_layouts
- id: HUD
  mockup: Docs/ArtDirection/UIMockups/HUD.png
  contains: [UI_LeafArcMeter, UI_ScoreReadout]

## scene_manifest
- id: SCN_WoodlandPond_Gameplay
  purpose: gameplay
  systems: [PlungerLaunchSystem, FlipperController]
  slice: true
  acs_covered: [AC-M01-3]
  markers: [Marker_Spawn_Player, Marker_Bounds]
  camera_ref: CAM_Main

## reference_images
- **id:** ref1
- **prompt:** table
`;

const FILES = [
  'Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx',
  'Assets/_Game/Art/Props/Bumpers/Koala/Textures/T_Koala_BC.png',
  'Assets/_Game/Art/UI/Sprites/UI_LeafArcMeter/SPR_UI_LeafArcMeter_Fill.png',
  'Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx',
  'Docs/ArtDirection/UIMockups/HUD.png',
  'Docs/ArtDirection/Camera/CAM_Main.png',
  'Docs/ArtDirection/Reference/ref1_Table.png',
  'Assets/_Game/Art/Props/Flipper/Flipper/Meshes/SM_Flipper.fbx',
  'Assets/_Game/Art/Props/Flipper/Flipper/Textures/T_Flipper_BC.png',
];

const repo = makeRepo({ add: ADD_HH, files: FILES });
const r = runIntake({ repo });
const has = (code) => r.issues.filter((i) => i.code === code);

test('matching ADD: no mismatch conflict, slice from ADD', () => {
  assert.equal(r.exitCode, 0);
  assert.equal(has('ADD_TDD_MISMATCH').length, 0);
  assert.equal(has('SLICE_INFERRED').length, 0);
  const s = readJson(repo, 'scenes').scenes.find((x) => x.id === 'SCN_WoodlandPond_Gameplay');
  assert.equal(s.slice, true);
  assert.deepEqual(s.markers, ['Marker_Spawn_Player', 'Marker_Bounds']);
  assert.equal(s.camera_ref, 'CAM_Main');
});

test('ui id typo merged into canonical §9.1 id with ADD V57 fields', () => {
  assert.ok(has('ID_TYPO').some((i) => /UI_LeafArcMeterr.*UI_LeafArcMeter/.test(i.message)));
  const ui = readJson(repo, 'ui');
  const leaf = ui.screens.find((s) => s.id === 'UI_LeafArcMeter');
  assert.equal(leaf.sprites, 'Assets/_Game/Art/UI/Sprites/UI_LeafArcMeter');
  assert.deepEqual(leaf.text_keys, ['hud.health']);
  assert.ok(ui.screens.some((s) => s.id === 'UI_MainMenu' && s.consumed_by[0] === 'standalone'));
  assert.deepEqual(ui.layouts, [{ id: 'HUD', mockup: 'Docs/ArtDirection/UIMockups/HUD.png', contains: ['UI_LeafArcMeter', 'UI_ScoreReadout'] }]);
});

test('briefs: files bound, serves typo normalized, bad enum defaulted, entities linked', () => {
  const am = readJson(repo, 'asset_manifest');
  const koala = am.assets.find((a) => a.asset_id === 'PROP-KOALA');
  assert.equal(koala.status, 'final');
  assert.deepEqual(koala.size_m, [0.3, 0.3, 0.45]);
  assert.deepEqual(koala.serves, ['CreatureBumperMoodStateSystem']);
  assert.ok(!am.orphans.some((o) => /Koala|LeafArcMeter|BLK_/.test(o)));
  const flip = am.assets.find((a) => a.asset_id === 'PROP-FLIPPER');
  assert.deepEqual(flip.serves, ['FlipperController']);
  assert.equal(flip.pivot, 'base');
  assert.equal(flip.size_m, null, 'non-finite member -> null, never null inside the array');
  assert.equal(flip.tris_lod0, 2000);
  assert.equal(flip.texture_size, null);
  assert.equal(flip.files.mesh, 'Assets/_Game/Art/Props/Flipper/Flipper/Meshes/SM_Flipper.fbx');
  assert.deepEqual(flip.files.textures, ['Assets/_Game/Art/Props/Flipper/Flipper/Textures/T_Flipper_BC.png']);
  assert.ok(!am.orphans.some((o) => /Flipper/.test(o)), 'relative brief paths claim the files');
  const bad = has('BRIEF_BAD_VALUE');
  assert.ok(bad.some((i) => i.level === 'fixable' && i.refs.includes('pivot')));
  for (const f of ['size_m', 'tris_lod0', 'texture_size']) assert.ok(bad.some((i) => i.level === 'missing' && i.refs.includes(f)), f);
  const ent = readJson(repo, 'entities').entities.find((e) => e.asset_id === 'PROP-KOALA');
  assert.deepEqual(ent.mechanics, ['CreatureBumperMoodStateSystem']);
  const ref = am.assets.find((a) => a.asset_id === 'ref1');
  assert.equal(ref.files.reference, 'Docs/ArtDirection/Reference/ref1_Table.png');
});

test('palette merge and localization', () => {
  const pal = readJson(repo, 'rendering').palette;
  assert.equal(pal.filter((p) => p.source === 'tdd').length, 4);
  assert.equal(has('PALETTE_CONFLICT').length, 0, 'ADD hexes are a subset of TDD §8');
  const cam = readJson(repo, 'camera').views;
  assert.match(cam.find((v) => v.id === 'CAM_Main').notes, /reference: Docs\/ArtDirection\/Camera\/CAM_Main\.png/);
});
