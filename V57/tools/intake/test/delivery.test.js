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

  const from = tmpDir('v57-delivery-');
  touch(from, 'Models/AST_ENV_PIPE-001.fbx', 'new pipe');
  touch(from, 'Models/SM_Crate.fbx', 'new crate');
  touch(from, 'Models/AST-ENV-ROCK-002.fbx', 'rock');
  touch(from, 'Textures/AST-ENV-ROCK-002_Albedo.jpg', 'rock albedo');
  touch(from, 'unity_scene.json', '{"v":2}');
  touch(from, 'Captures/top.png', 'png');

  const plan = planDelivery({ from, repo, newRoot: 'Assets/_Game/Art/Environment/Decoration' });
  assert.deepEqual(plan.replace.map((p) => p.to).sort(), [`${art}/Crate/Meshes/SM_Crate.fbx`, 'Docs/Design/LevelMaps/L1/unity_scene.json']);
  assert.deepEqual(plan.renamedMatch, [{ from: 'Models/AST_ENV_PIPE-001.fbx', to: `${art}/AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx` }]);
  assert.deepEqual(plan.add.map((p) => p.to).sort(), [
    'Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCK-002/Meshes/AST-ENV-ROCK-002.fbx',
    'Assets/_Game/Art/Environment/Decoration/AST-ENV-ROCK-002/Textures/AST-ENV-ROCK-002_Albedo.jpg']);
  assert.deepEqual(plan.unplaced, ['Captures/top.png']);
  assert.ok(plan.keptNotDelivered.includes(`${art}/AST-ENV-PIPE-001/Textures/AST-ENV-PIPE-001_Normal.png`));

  const before = fs.readdirSync(path.join(repo, art, 'AST-ENV-PIPE-001/Textures'));
  assert.equal(applyDelivery(plan, { from, repo }), 5);
  assert.equal(fs.readFileSync(path.join(repo, art, 'AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx'), 'utf8'), 'new pipe');
  assert.equal(fs.readFileSync(path.join(repo, art, 'AST-ENV-PIPE-001/Meshes/AST-ENV-PIPE-001.fbx.meta'), 'utf8'), 'guid: keep');
  assert.deepEqual(fs.readdirSync(path.join(repo, art, 'AST-ENV-PIPE-001/Textures')), before);
});
