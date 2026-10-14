'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { planDelivery, applyDelivery } = require('../lib/delivery');
const { tmpDir, touch } = require('./helpers');

test('delivery import: replaces in place (also under a looser name), adds new assets with their textures, never deletes', () => {
  const repo = tmpDir();
  const art = 'Assets/_Game/Art/Environment/Kits';
  touch(repo, `${art}/AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx`, 'old');
  touch(repo, `${art}/AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx.meta`, 'guid: keep');
  touch(repo, `${art}/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Normal.png`, 'old normal');
  touch(repo, `${art}/Crate/Meshes/SM_Crate.fbx`, 'old');
  touch(repo, 'Docs/Design/LevelMaps/L1/unity_scene.json', '{}');
  touch(repo, 'Assets/_Game/Art/Characters/Shark/Animations/ANIM_Shark_Swim.fbx', 'swim');

  const from = tmpDir('v57-delivery-');
  touch(from, 'Models/AST_ENV_PIPE-001.fbx', 'new pipe');
  touch(from, 'Models/SM_Crate.fbx', 'new crate');
  touch(from, 'Models/AST-ENV-ROCK-002.fbx', 'rock');
  touch(from, 'Textures/AST-ENV-ROCK-002_Albedo.jpg', 'rock albedo');
  touch(from, 'unity_scene.json', '{"v":2}');
  touch(from, 'Captures/top.png', 'png');
  touch(from, 'ANIM_Shark_Attack.fbx', 'attack');

  const plan = planDelivery({ from, repo, newRoot: 'Assets/_Game/Art/Environment/Decoration' });
  assert.deepEqual(plan.replace.map((p) => p.to).sort(), [`${art}/Crate/Meshes/SM_Crate.fbx`, 'Docs/Design/LevelMaps/L1/unity_scene.json']);
  assert.deepEqual(plan.renamedMatch, [{ from: 'Models/AST_ENV_PIPE-001.fbx', to: `${art}/AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx` }]);
  assert.deepEqual(plan.add.map((p) => p.to).sort(), [
    'Assets/_Game/Art/Characters/Shark/Animations/ANIM_Shark_Attack.fbx',
    'Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCK-002/Meshes/AST-ENV-ROCK-002.fbx',
    'Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCK-002/Textures/AST-ENV-ROCK-002_Albedo.jpg']);
  assert.deepEqual(plan.unplaced, ['Captures/top.png']);
  assert.ok(plan.keptNotDelivered.includes(`${art}/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Normal.png`));

  const before = fs.readdirSync(path.join(repo, art, 'AST-ENV-PIPE-001/Textures'));
  assert.equal(applyDelivery(plan, { from, repo }), 6);
  assert.equal(fs.readFileSync(path.join(repo, art, 'AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx'), 'utf8'), 'new pipe');
  assert.equal(fs.readFileSync(path.join(repo, art, 'AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx.meta'), 'utf8'), 'guid: keep');
  assert.deepEqual(fs.readdirSync(path.join(repo, art, 'AST-ENV-PIPE-001/Textures')), before);
});

test('delivery import: a new convention-named asset is grouped (SK_ + LOD files, ANIM_, export sidecars, loose textures)', () => {
  const repo = tmpDir();
  touch(repo, 'Assets/_Game/Art/Environment/Decoration/Rock/Meshes/SM_Rock.fbx', 'rock');
  const from = tmpDir('v57-delivery-');
  for (const f of ['SK_Dome.fbx', 'SK_DomeLOD01.fbx', 'SK_Dome_LOD2.fbx', 'ANIM_Dome_Pulse.fbx', 'Dome_export.json', 'DomeLOD01_export.json', 'Textures/S Prop_normal.jpg']) touch(from, f, f);
  const plan = planDelivery({ from, repo, newRoot: 'Assets/_Game/Art/Environment/Decoration' });
  const to = Object.fromEntries(plan.add.map((p) => [p.from, p.to]));
  const base = 'Assets/_Game/Art/Environment/Decoration/Dome';
  assert.equal(to['SK_Dome.fbx'], `${base}/Meshes/SK_Dome.fbx`);
  assert.equal(to['SK_DomeLOD01.fbx'], `${base}/Meshes/SK_DomeLOD01.fbx`);
  assert.equal(to['SK_Dome_LOD2.fbx'], `${base}/Meshes/SK_Dome_LOD2.fbx`);
  assert.equal(to['ANIM_Dome_Pulse.fbx'], `${base}/Animations/ANIM_Dome_Pulse.fbx`);
  assert.equal(to['Dome_export.json'], `${base}/Dome_export.json`);
  assert.equal(to['DomeLOD01_export.json'], `${base}/DomeLOD01_export.json`);
  assert.equal(to['Textures/S Prop_normal.jpg'], `${base}/Textures/S Prop_normal.jpg`);
  assert.deepEqual(plan.unplaced, []);
});
