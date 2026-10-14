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

test('unity_scene 1.1: marcadores, trayectorias, props, marker actors and static:false aliases', () => {
  const repo = makeRepo({ files: STANDARD_FILES });
  const fish = { ...obj('FISH_01', 'Koala', 'Eventos/Peces'), props: { trayectoria: 'PATH_Fish_01', fase: 0.25, offset_path_m: [1, 0, 2] } };
  touch(repo, 'Docs/Design/LevelMaps/WoodlandPond/unity_scene.json', JSON.stringify({
    contrato: 'unity_scene/1.1', level_id: 'WoodlandPond', convencion_ejes: 'Unity_Y_up_metros',
    objetos: [fish, obj('Kelp.001', 'SM_Koala', 'Vestido')],
    marcadores: [
      { nombre: 'Marker_Zone_Z1', tipo: 'Zone', position: [0, 1, 0], rotation: [0, 0, 0, 1], scale: [10, 2, 5], volumen: { forma: 'BOX', tamano: [2, 2, 2] } },
      { nombre: 'MarkerPatrol_CrabA', tipo: 'Patrol', position: [0, 0, 1], rotation: [0, 0, 0, 1], scale: [1, 1, 1], props: { path: 'PATH_Crab_A' } },
      { nombre: 'LVL_MK_DOOR1', tipo: 'Door', id: 'door_1', position: [5, 0, 0], rotation: [0, 0, 0, 1], scale: [1, 1, 1] },
      { nombre: 'LVL_MK_SHARK', tipo: 'Event', id: 'shark', position: [0, 0, 0], rotation: [0, 0, 0, 1], scale: [18, 1, 9], volumen: { forma: 'CIRCLE_XZ', radio: 1 } }],
    trayectorias: [
      { nombre: 'PATH_Crab_A', tipo: 'cangrejo', actor: 'MarkerPatrol_CrabA', ciclo: false, modo: 'ida_vuelta', velocidad_mps: 1.2, puntos: [[0, 0, 0], [0, 0, 2]] },
      { nombre: 'PATH_Fish_01', tipo: 'pez', actor: 'Koala', ciclo: true, velocidad_mps: 0.8, puntos: [[0, 1, 0], [1, 1, 0], [1, 1, 1]], instancias: [{ nombre: 'FISH_01' }], cantidad: 1 }],
  }));
  touch(repo, 'Docs/V57/layout_aliases.json', JSON.stringify({
    aliases: { SM_Koala: { static: false } },
    marker_actors: { Patrol: { prefab: 'Assets/_Game/Prefabs/Gameplay/PRF_Crab.prefab', layer: 'Enemies', anchor: 'path' }, Door: { prefab: 'Assets/_Game/Prefabs/Gameplay/PRF_Door.prefab' } },
  }));
  const r = runIntake({ repo });
  assert.equal(r.exitCode, 0);
  const wp = readJson(repo, 'layouts').layouts.find((l) => l.level_id === 'WoodlandPond');
  const byName = Object.fromEntries(wp.objects.map((o) => [o.name, o]));
  // markers: names, box volume = tamano x scale, patrol marker moved to the route end, non-box shapes kept as props
  assert.deepEqual(byName.Marker_Zone_Z1.scale, [20, 4, 10]);
  assert.equal(byName.Marker_Zone_Z1.shape, 'box');
  assert.deepEqual(byName.Marker_Patrol_CrabA.position, [0, 0, 2]);
  assert.ok(byName.Marker_Door_door_1);
  assert.equal(byName.Marker_Event_shark.shape, 'point');
  assert.deepEqual(byName.Marker_Event_shark.scale, [18, 1, 9]);
  assert.ok(byName.Marker_Event_shark.props.some((p) => p.key === 'volume_shape' && p.value === 'CIRCLE_XZ'));
  // marker actors: crab named after its marker id, at the route start facing +X to the end; door at its marker
  assert.equal(byName.CrabA.prefab, 'Assets/_Game/Prefabs/Gameplay/PRF_Crab.prefab');
  assert.equal(byName.CrabA.layer, 'Enemies');
  assert.deepEqual(byName.CrabA.position, [0, 0, 0]);
  const [, qy, , qw] = byName.CrabA.rotation;
  assert.ok(Math.abs(Math.abs(qy) - Math.SQRT1_2) < 1e-6 && Math.abs(Math.abs(qw) - Math.SQRT1_2) < 1e-6);
  assert.equal(byName.door_1.prefab, 'Assets/_Game/Prefabs/Gameplay/PRF_Door.prefab');
  assert.equal(byName.door_1.dynamic, true);
  // props flattened to strings; static:false alias keeps the model and marks it dynamic
  const fishProps = Object.fromEntries(byName.FISH_01.props.map((p) => [p.key, p.value]));
  assert.deepEqual(fishProps, { trayectoria: 'PATH_Fish_01', fase: '0.25', offset_path_m: '1,0,2' });
  assert.equal(byName.Kelp_01.dynamic, true);
  assert.equal(byName.Kelp_01.model, 'Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx');
  // paths: flattened points, instances by name, extra keys as props
  const fishPath = wp.paths.find((p) => p.name === 'PATH_Fish_01');
  assert.deepEqual(fishPath.points, [0, 1, 0, 1, 1, 0, 1, 1, 1]);
  assert.deepEqual(fishPath.instances, ['FISH_01']);
  assert.equal(fishPath.loop, true);
  assert.ok(fishPath.props.some((p) => p.key === 'cantidad' && p.value === '1'));
});

test('layout extras: Docs/V57/layout_extras/<LevelId>.json objects are appended to the provider map', () => {
  const repo = makeRepo({ files: STANDARD_FILES });
  touch(repo, 'Docs/Design/LevelMaps/WoodlandPond/unity_scene.json', scene([obj('Koala.001', 'Koala', 'Vestido')]));
  touch(repo, 'Docs/V57/layout_extras/WoodlandPond.json', JSON.stringify({ objetos: [obj('Koala.002', 'Koala', 'Vestido'), obj('Koala.003', 'Koala', 'Vestido')] }));
  const r = runIntake({ repo });
  const layout = readJson(repo, 'layouts').layouts.find((l) => l.level_id === 'WoodlandPond');
  assert.equal(layout.objects.length, 3);
  assert.ok(r.issues.some((i) => i.code === 'LAYOUT_EXTRAS'));
});
