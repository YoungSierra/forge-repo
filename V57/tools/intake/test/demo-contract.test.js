'use strict';
// Regressions found by the Happy Habitat demo delivery (provider-format pinball demo):
// blockout textures, `serves: []` for pure decoration, blockout named by asset_id, non-mesh briefs.
const test = require('node:test');
const assert = require('node:assert/strict');
const { runIntake } = require('../lib/run');
const { classify } = require('../lib/naming');
const { makeRepo, readJson } = require('./helpers');

const ADD = `## asset_briefs
- asset_id: ENV_WoodlandPond
  category: Level blockout
  name: Woodland Pond table
  asset_name: WoodlandPond
  serves: []
  size_m: [0.8, 0.02, 1.0]
  pivot: center
  side: center
  collision: exact
  tris_lod0: 100
  texture_size: 2048
  bones: 0
  status: provisional
  files:
    mesh: Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx
    textures: [Assets/_Game/Art/Environment/Blockout/WoodlandPond/Textures/T_WoodlandPond_BC.png]

- asset_id: ENV_Rail
  category: Environment kit
  name: Table rail
  asset_name: Rail
  serves: []
  size_m: [0.01, 0.05, 0.12]
  pivot: base
  side: center
  collision: simple
  tris_lod0: 100
  texture_size: 1024
  bones: 0
  status: final
  files:
    mesh: Assets/_Game/Art/Environment/Kits/Rail/Meshes/SM_Rail.fbx

- asset_id: TEX_CanopyShadow
  category: Shared texture
  type: texture
  name: Canopy shadow mask
  asset_name: CanopyShadow
  serves: []
  status: final
  files:
    textures: [Assets/_Game/Art/Shared/Textures/T_CanopyShadow_Mask.jpg]

## scene_manifest
- id: SCN_WoodlandPond_Gameplay
  purpose: gameplay
  systems: [FlipperController]
  slice: true
  blockout: ENV_WoodlandPond
`;

const repo = makeRepo({ add: ADD, files: [
  'Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx',
  'Assets/_Game/Art/Environment/Blockout/WoodlandPond/Textures/T_WoodlandPond_BC.png',
  'Assets/_Game/Art/Environment/Kits/Rail/Meshes/SM_Rail.fbx',
  'Assets/_Game/Art/Shared/Textures/T_CanopyShadow_Mask.jpg',
] });
const r = runIntake({ repo });
const codes = (code) => r.issues.filter((i) => i.code === code);

test('blockout Textures/ folder holds T_ textures, not blockouts', () => {
  const c = classify('Assets/_Game/Art/Environment/Blockout/WoodlandPond/Textures/T_WoodlandPond_BC.png');
  assert.equal(c.type, 'texture');
  assert.equal(c.asset, 'WoodlandPond');
  assert.equal(classify('Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx').type, 'blockout');
  assert.ok(!codes('NAME_CONVENTION').some((i) => /T_WoodlandPond_BC/.test(i.where)));
});

test('serves: [] is valid; texture briefs do not need mesh fields', () => {
  const lacks = codes('BRIEF_V57_FIELDS');
  assert.ok(!lacks.some((i) => i.refs.includes('ENV_Rail')), 'empty serves accepted');
  assert.ok(!lacks.some((i) => i.refs.includes('TEX_CanopyShadow')), 'texture brief complete without size/pivot/tris');
  const am = readJson(repo, 'asset_manifest');
  assert.deepEqual(am.assets.find((a) => a.asset_id === 'ENV_Rail').serves, []);
  assert.equal(am.assets.find((a) => a.asset_id === 'TEX_CanopyShadow').type, 'texture');
});

test('scene blockout given as asset_id resolves to the BLK_ file; BLK_ brief typed blockout', () => {
  assert.equal(codes('SLICE_NO_BLOCKOUT').length, 0);
  const s = readJson(repo, 'scenes').scenes.find((x) => x.id === 'SCN_WoodlandPond_Gameplay');
  assert.equal(s.blockout, 'Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx');
  const am = readJson(repo, 'asset_manifest');
  assert.equal(am.assets.find((a) => a.asset_id === 'ENV_WoodlandPond').type, 'blockout');
});
