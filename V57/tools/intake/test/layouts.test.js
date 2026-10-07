'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { runIntake } = require('../lib/run');
const { englishLayer, splitInstanceName, instanceNames } = require('../lib/layouts');
const { makeRepo, touch, readJson, STANDARD_FILES } = require('./helpers');

const scene = (objects) => JSON.stringify({ contrato: 'unity_scene/1.1', level_id: 'x', convencion_ejes: 'Unity_Y_up_metros', objetos: objects });
const obj = (nombre, assetId, capa) => ({ nombre, asset_id: assetId, capa, position: [1, 2, 3], rotation: [0, 0.7071, 0, 0.7071], scale: [1, 1, 1] });

test('layer names become English PascalCase groups', () => {
  assert.deepEqual(englishLayer('Oceano'), { name: 'Ocean', renamed: true });
  assert.deepEqual(englishLayer('Vestido'), { name: 'Dressing', renamed: true });
  assert.deepEqual(englishLayer('Gameplay'), { name: 'Gameplay', renamed: false });
  assert.deepEqual(englishLayer('zona de carga'), { name: 'ZonaDeCarga', renamed: true });
});

test('instance names drop DCC suffixes and become <Base>_NN', () => {
  assert.deepEqual(splitInstanceName('Jellyfish.003'), { base: 'Jellyfish', index: 3 });
  assert.deepEqual(splitInstanceName('Jellyfish (2)'), { base: 'Jellyfish', index: 2 });
  const names = instanceNames([
    { nombre: 'Pipe.001', asset_id: 'Pipe', _layer: 'Dressing' },
    { nombre: 'Pipe.002', asset_id: 'Pipe', _layer: 'Dressing' },
    { nombre: 'Pipe.002', asset_id: 'Pipe', _layer: 'Dressing' },
    { nombre: 'Crate', asset_id: 'Crate', _layer: 'Dressing' },
  ]);
  assert.deepEqual(names, ['Pipe_01', 'Pipe_02', 'Pipe_03', 'Crate_01']);
});

test('LevelMaps layout: attached to the matching TDD scene, extra level added as layout scene', () => {
  const repo = makeRepo({ files: STANDARD_FILES });
  touch(repo, 'Docs/Design/LevelMaps/WoodlandPond/unity_scene.json', scene([
    obj('SM_Koala.001', 'SM_Koala', 'Vestido'), obj('Koala.002', 'Koala', 'Vestido'), obj('Ghost.001', 'Ghost', 'Oceano')]));
  touch(repo, 'Docs/Design/LevelMaps/WoodlandPond/manifest.json', JSON.stringify({ assets: [
    { asset_id: 'SM_Koala', materials: [{ name: 'Koala', albedo: 'Textures/T_Koala_BC.png', base_color: [1, 1, 1, 1], metallic: 0, smoothness: 0.4, double_sided: true }] }] }));
  touch(repo, 'Docs/Design/LevelMaps/Annex/unity_scene.json', scene([obj('Koala.001', 'Koala', 'Props')]));
  const r = runIntake({ repo });
  assert.equal(r.exitCode, 0);
  const layouts = readJson(repo, 'layouts').layouts;
  const wp = layouts.find((l) => l.level_id === 'WoodlandPond');
  assert.deepEqual(wp.objects.map((o) => `${o.layer}/${o.name}`), ['Dressing/SM_Koala_01', 'Dressing/Koala_02', 'Ocean/Ghost_01']);
  assert.equal(wp.objects[1].model, 'Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx');
  assert.equal(wp.objects[2].model, null);
  assert.equal(wp.materials[0].albedo, 'Assets/_Game/Art/Props/Bumpers/Koala/Textures/T_Koala_BC.png');
  const scenes = readJson(repo, 'scenes').scenes;
  assert.equal(scenes.find((s) => s.id === 'SCN_WoodlandPond_Gameplay').layout, 'WoodlandPond');
  assert.equal(scenes.find((s) => s.id === 'SCN_Annex').layout, 'Annex');
  const codes = r.issues.map((i) => i.code);
  ['LAYOUT_LAYER_RENAMED', 'LAYOUT_ASSET_MISSING', 'LAYOUT_SCENE_ADDED'].forEach((c) => assert.ok(codes.includes(c), c));
  assert.ok(!r.issues.some((i) => i.code === 'DOCS_UNEXPECTED' && /LevelMaps/.test(i.where)));
});
