'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { runIntake } = require('../lib/run');
const { matcher } = require('../lib/asset-rules');
const { makeRepo, touch, readJson, STANDARD_FILES } = require('./helpers');

test('asset rule patterns ignore case, hyphens and underscores; * is a wildcard', () => {
  assert.equal(matcher('AST-PROP-*')('ASTPROPRAILING001'), true);
  assert.equal(matcher('ast_prop_railing-001')('AST-PROP-RAILING-001'), true);
  assert.equal(matcher('AST-PROP-*')('ASTENVPIPE001'), false);
  assert.equal(matcher('*Koala')('SM_Koala'), true);
});

test('Docs/V57/asset_rules.json: later rules win, fields reach the manifest, unused rules and bad values are reported', () => {
  const repo = makeRepo({ files: [...STANDARD_FILES, 'Assets/_Game/Prefabs/VFX/VFX_Sparkle.prefab'] });
  touch(repo, 'Docs/V57/asset_rules.json', JSON.stringify({ rules: [
    { match: '*', shadows: 'on' },
    { match: 'Koala', collision: 'mesh', shadows: 'two_sided', shader: 'Game/Lit Wobble',
      attach: [{ prefab: 'Assets/_Game/Prefabs/VFX/VFX_Sparkle.prefab', offset: [0, 0.5, 0] }] },
    { match: 'SM_BiolumeBloom_*', collision: 'none' },
    { match: 'Carton', cull_below: 0.02 },
    { match: 'Carton', cull_below: 3 },
    { match: 'Carton', collision: 'bouncy' },
    { match: 'Nothing*', collision: 'none' },
  ] }));
  const r = runIntake({ repo });
  const assets = readJson(repo, 'asset_manifest').assets;
  const koala = assets.find((a) => /koala/i.test(a.asset_name));
  assert.equal(koala.collision, 'mesh');
  assert.equal(koala.ruled, true);
  assert.equal(koala.collision_ruled, true);
  const carton = assets.find((a) => /carton/i.test(a.asset_name));
  assert.equal(carton.cull_below, 0.02);
  assert.notEqual(carton.collision_ruled, true);
  assert.equal(koala.shadows, 'two_sided');
  assert.equal(koala.shader, 'Game/Lit Wobble');
  assert.deepEqual(koala.attach, [{ prefab: 'Assets/_Game/Prefabs/VFX/VFX_Sparkle.prefab', offset: [0, 0.5, 0], align: 'world' }]);
  const bloom = assets.find((a) => a.files && /BiolumeBloom/.test(a.files.mesh || ''));
  assert.equal(bloom.collision, 'none');
  assert.equal(bloom.shadows, 'on');
  const codes = r.issues.map((i) => i.code);
  assert.ok(codes.includes('ASSET_RULE_UNUSED'));
  assert.ok(codes.includes('ASSET_RULE_INVALID'));
  assert.ok(!codes.includes('ASSET_RULE_PREFAB_MISSING'));
});
