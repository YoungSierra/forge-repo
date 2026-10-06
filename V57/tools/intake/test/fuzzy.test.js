'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { osa, matchId, proseTypos } = require('../lib/fuzzy');
const { checkFile } = require('../lib/naming');

test('OSA distance counts a transposition as 1', () => {
  assert.equal(osa('prefect', 'perfect'), 1);
  assert.equal(osa('biollumebloom', 'biolumebloom'), 1);
  assert.equal(osa('abc', 'abc'), 0);
});

test('matchId: exact, case, typo, confusable, unknown', () => {
  assert.equal(matchId('UI_LeafArcMeter', ['UI_LeafArcMeter']).status, 'exact');
  assert.equal(matchId('ui_leafarcmeter', ['UI_LeafArcMeter']).status, 'case');
  assert.deepEqual(matchId('BiollumeBloom', ['Biolume_Bloom', 'Drifting_Garden']).match, 'Biolume_Bloom');
  assert.equal(matchId('G0OD', ['GOOD']).status, 'typo');
  assert.equal(matchId('ScoreService', ['ScoreSystem']).status, 'unknown');
  assert.equal(matchId('JellybellRing', ['FlipperController', 'WiltSystem']).status, 'unknown');
});

test('proseTypos: PREFECT and G0OD vs frequent tokens', () => {
  const text = 'PERFECT window. PERFECT burst. UI_RESULT_PERFECT. PREFECT flare. GOOD GOOD GOOD then G0OD/MISS';
  const found = proseTypos([{ name: 'ADD.md', text }]);
  const m = Object.fromEntries(found.map((f) => [f.token, f.suggestion]));
  assert.equal(m.PREFECT, 'PERFECT');
  assert.equal(m.G0OD, 'GOOD');
  assert.equal(m.MISS, undefined);
});

test('naming: good names pass, bad names get suggestions', () => {
  assert.deepEqual(checkFile('Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx').problems, []);
  assert.deepEqual(checkFile('Assets/_Game/Art/UI/Sprites/UI_LeafArcMeter/SPR_UI_LeafArcMeter_Fill_9s-12.png').problems, []);
  const tex = checkFile('Assets/_Game/Art/Characters/Carton/Textures/carton_albedo.png').problems;
  assert.equal(tex[0].suggestion, 'T_Carton_BC.png');
  const blk = checkFile('Assets/_Game/Art/Environment/Blockout/WoodlandPond/blk_woodland.fbx').problems;
  assert.equal(blk[0].suggestion, 'BLK_WoodlandPond.fbx');
  assert.equal(checkFile('Assets/_Game/Art/VFX/Burst/New Folder/VFX_Burst.png').problems[0].kind, 'forbidden');
  assert.equal(checkFile('Assets/_Game/Art/Props/A/B/Meshes/SM_B copy.fbx').problems[0].kind, 'forbidden');
  assert.equal(checkFile('Assets/_Game/Art/Misc/x.png').problems[0].kind, 'folder');
});
