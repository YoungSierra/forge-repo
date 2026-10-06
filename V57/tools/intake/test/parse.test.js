'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const { Issues } = require('../lib/issues');
const { parseTdd } = require('../lib/tdd');
const { parseAdd } = require('../lib/add');
const { parseItems } = require('../lib/loose');
const { parseTables, splitSections, sectionKey } = require('../lib/md');
const { TDD, ADD } = require('./helpers');

const tddText = fs.readFileSync(TDD, 'utf8');
const addText = fs.readFileSync(ADD, 'utf8');

test('section keys', () => {
  assert.equal(sectionKey('9.1 Screen registry *(gate G-11)*'), '9.1');
  assert.equal(sectionKey('§A · Project Identity `[REQUIRED]`'), 'A');
  assert.equal(sectionKey('§B-S · Support Systems Registry'), 'B-S');
  assert.equal(sectionKey('0.2 · Completeness Gate'), '0.2');
  assert.equal(sectionKey('Mechanic: PlungerLaunchSystem'), 'mechanic:PlungerLaunchSystem');
  assert.equal(sectionKey('asset_briefs'), 'asset_briefs');
});

test('HH TDD: registry counts', () => {
  const issues = new Issues();
  const t = parseTdd(tddText, 'Docs/Design/TDD.md', issues);
  assert.equal(t.identity.title, 'Happy Habitat');
  assert.equal(t.identity.engine, 'Unity 6000.0.40f1');
  assert.equal(t.screens.length, 33, '§9.1 screens');
  assert.equal(t.inputs.length, 8, '§11.3 actions');
  assert.equal(t.scenes.length, 4, '§13.2 scenes');
  assert.equal(t.mechanics.length, 10, '§B mechanics');
  assert.equal(t.specs.length, 10, '§C blocks');
  assert.equal(t.support.length, 6, '§B-S');
  assert.equal(t.gate.length, 18);
  assert.equal(t.perf.length, 6);
  assert.equal(t.coreLoop.length, 8);
  assert.deepEqual(t.mechanics.map((m) => m.name), t.specs.map((s) => s.name));
  assert.equal(t.pending.length, 0, '§14.2 placeholder row is not a pending item');
  assert.equal(t.markers.pending.length, 0, 'legend mentions of [PENDING] are not markers');
});

test('HH TDD: mechanic block details and §C repair', () => {
  const issues = new Issues();
  const t = parseTdd(tddText, 'TDD.md', issues);
  const p = t.mechanics[0];
  assert.deepEqual(p.playerInputs, ['ChargePlunger', 'ReleasePlunger']);
  assert.equal(p.acs.length, 4);
  assert.deepEqual(p.levers.find((l) => l.param === 'launchSpeedMax'), { param: 'launchSpeedMax', raw: '14.0 m/s', value: 14, unit: 'm/s' });
  const c03 = t.specs[2];
  assert.equal(c03.name, 'CreatureBumperMoodStateSystem');
  assert.ok(c03.data, 'C-03 parsed after repair');
  assert.equal(c03.acs.length, 5);
  assert.ok(issues.list.some((i) => i.code === 'TDD_SPEC_YAML_INVALID' && i.level === 'fixable'));
  const wood = t.scenes[0];
  assert.equal(wood.id, 'SCN_WoodlandPond_Gameplay');
  assert.equal(wood.acs.length, 22);
  assert.equal(t.inputs[0].map, 'Launcher');
});

test('Cartón ADD: loose sections', () => {
  const issues = new Issues();
  const a = parseAdd(addText, 'ADD.md', issues);
  assert.equal(a.briefs.length, 10);
  assert.equal(a.briefs[0].asset_id, 'CHAR-01');
  assert.equal(a.briefs[0].name, 'Cartón');
  assert.match(a.briefs[0].technical_constraints, /rig ≤22 bones/);
  assert.equal(a.ui_screens.filter((s) => !s._prose).length, 14);
  assert.deepEqual(a.ui_screens.filter((s) => s._prose).map((s) => s.id), ['UI_MainMenu', 'UI_PauseMenu', 'UI_Options', 'UI_Loading']);
  assert.deepEqual(a.ui_screens[1].states, ['Idle', 'Active', 'Contracting', 'Flash']);
  assert.equal(a.scenes.length, 7);
  assert.equal(a.scenes.filter((s) => s.slice).length, 4);
  assert.equal(a.reference_images.length, 7);
  assert.equal(a.reference_images[6].id, 'ref7');
  assert.equal(a.palette.length, 7);
  assert.equal(a.palette.find((p) => p.hex === '#1A8C7A').status, 'draft');
  assert.ok(a.markers.draft.length >= 5);
  assert.equal(a.formats.asset_briefs, 'loose');
});

test('ADD: fenced yaml + nested V57 fields', () => {
  const md = [
    '## asset_briefs', '```yaml', 'asset_briefs:', '  - asset_id: PROP-01', '    asset_name: Koala', '    serves: [CreatureBumperMoodStateSystem]',
    '    size_m: [0.3, 0.3, 0.4]', '    files: { mesh: Props/Bumpers/Koala/Meshes/SM_Koala.fbx, textures: [a.png] }', '```',
    '## scene_manifest', '- id: SCN_X  ', '  slice: true  ', '  files:', '    mesh: BLK_X.fbx', '  markers: [Marker_Spawn_Player]',
  ].join('\n');
  const a = parseAdd(md, 'ADD.md', new Issues());
  assert.equal(a.formats.asset_briefs, 'yaml');
  assert.deepEqual(a.briefs[0].size_m, [0.3, 0.3, 0.4]);
  assert.equal(a.briefs[0].files.mesh, 'Props/Bumpers/Koala/Meshes/SM_Koala.fbx');
  assert.deepEqual(a.briefs[0].serves, ['CreatureBumperMoodStateSystem']);
  assert.equal(a.scenes[0].slice, true);
  assert.deepEqual(a.scenes[0].markers, ['Marker_Spawn_Player']);
  const it = parseItems('- id: A\n  files:\n    mesh: m.fbx\n    textures: [t1, t2]\n- id: B\n', 1).items;
  assert.deepEqual(it.map((x) => x.id), ['A', 'B']);
  assert.deepEqual(it[0].files, { mesh: 'm.fbx', textures: ['t1', 't2'] });
});

test('tolerates missing/garbage sections', () => {
  const issues = new Issues();
  const t = parseTdd('# Some doc\n\nno tables here\n```yaml\n: : bad\n```\n', 'TDD.md', issues);
  assert.equal(t.mechanics.length, 0);
  assert.equal(t.found.screens, false);
  const a = parseAdd('', 'ADD.md', issues);
  assert.equal(a.briefs.length, 0);
});

test('tables: escaped pipes and aliases', () => {
  const rows = parseTables('| Screen id | Purpose |\n|---|---|\n| `UI_A` | a \\| b |\n')[0].rows;
  assert.equal(rows[0].screen_id, '`UI_A`');
  assert.equal(rows[0].purpose, 'a | b');
  assert.equal(splitSections('# A\n## B\ntext\n# C\n').length, 3);
});
