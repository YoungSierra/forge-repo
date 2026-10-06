'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const Ajv = require('ajv/dist/2020');
const { runIntake } = require('../lib/run');
const { GENERATED } = require('../lib/writer');
const { makeRepo, tmpDir, runCli, readJson, STANDARD_FILES } = require('./helpers');

const SCHEMA_DIR = path.join(__dirname, '..', '..', 'schema');
const codes = (r, code) => r.issues.filter((i) => i.code === code);

const repo = makeRepo({ files: STANDARD_FILES });
const r1 = runIntake({ repo });

test('HH TDD + Cartón ADD fixture: exit 0, all generated files with fixed top-level keys', () => {
  assert.equal(r1.exitCode, 0);
  const keys = {
    package: ['slug', 'title', 'version', 'genre', 'modules', 'perspective', 'physics', 'players', 'engine', 'platform', 'world', 'slice', 'sources'],
    asset_manifest: ['assets', 'orphans'], entities: ['entities'], mechanics: ['mechanics'], ui: ['screens', 'layouts'], scenes: ['scenes'],
    input_map: ['actions'], camera: ['views'], rendering: ['pipeline', 'palette', 'budgets', 'post'], audio: ['events', 'middleware', 'notes'],
    tuning: ['values'], acceptance: ['criteria', 'gold_path'],
  };
  for (const n of GENERATED) {
    assert.ok(fs.existsSync(path.join(repo, 'Docs/Generated', `${n}.yaml`)), `${n}.yaml`);
    assert.deepEqual(Object.keys(readJson(repo, n)), keys[n], `${n} top-level keys`);
  }
  assert.ok(fs.existsSync(path.join(repo, 'Docs/Generated/localization.csv')));
  assert.ok(fs.existsSync(path.join(repo, 'Docs/V57/INTAKE_REPORT.md')));
});

test('generated json validates against V57/tools/schema', () => {
  const ajv = new Ajv({ allErrors: true, allowUnionTypes: true });
  for (const n of GENERATED) {
    const validate = ajv.compile(JSON.parse(fs.readFileSync(path.join(SCHEMA_DIR, `${n}.schema.json`), 'utf8')));
    const ok = validate(readJson(repo, n));
    assert.ok(ok, `${n}: ${JSON.stringify(validate.errors)}`);
  }
});

test('counts carried into generated data', () => {
  assert.equal(readJson(repo, 'mechanics').mechanics.length, 10);
  assert.equal(readJson(repo, 'input_map').actions.length, 8);
  assert.equal(readJson(repo, 'scenes').scenes.length, 4);
  const ui = readJson(repo, 'ui');
  assert.equal(ui.screens.filter((s) => s.source === 'tdd').length, 33);
  const pkg = readJson(repo, 'package');
  assert.equal(pkg.physics, '2d');
  assert.equal(pkg.engine.unity_pinned, '6000.6.2f1');
  assert.deepEqual(pkg.slice.scenes, ['SCN_WoodlandPond_Gameplay']);
  const act = readJson(repo, 'input_map').actions.find((a) => a.action === 'ActivateLeftFlipper');
  assert.equal(act.bindings.keyboard, '<Keyboard>/z');
  assert.equal(act.bindings.gamepad, '<Gamepad>/leftShoulder');
  const sc = readJson(repo, 'scenes').scenes[0];
  assert.ok(sc.acs.includes('AC-M01-3') && !sc.acs.some((a) => a.startsWith('VG-')));
  assert.equal(sc.blockout, 'Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx');
});

test('typo detection with did-you-mean', () => {
  const typo = [...codes(r1, 'ID_TYPO'), ...codes(r1, 'TEXT_TYPO')].map((i) => i.message).join('\n');
  assert.match(typo, /BiollumeBloom.*did you mean "Biolume_?Bloom"/);
  assert.match(typo, /"PREFECT".*"PERFECT"/);
  assert.match(typo, /"G0OD".*"GOOD"/);
  assert.ok(codes(r1, 'ID_TYPO').every((i) => i.level === 'fixable'));
});

test('orphans, forbidden files and naming', () => {
  const am = readJson(repo, 'asset_manifest');
  assert.ok(am.orphans.includes('Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx'), 'Koala has no brief');
  assert.ok(!am.orphans.some((o) => /BLK_WoodlandPond/.test(o)), 'blockout claimed by slice scene');
  assert.ok(!am.orphans.some((o) => /Carton\//.test(o)), 'Carton files claimed by brief CHAR-01');
  assert.ok(am.assets.some((a) => a.source === 'inventory' && a.asset_name === 'Koala'));
  const carton = am.assets.find((a) => a.asset_id === 'CHAR-01');
  assert.equal(carton.files.mesh, 'Assets/_Game/Art/Characters/Carton/Meshes/SK_Carton.fbx');
  assert.equal(carton.bones, 22);
  const forb = codes(r1, 'REPO_FORBIDDEN').map((i) => i.where);
  assert.ok(forb.some((w) => w.endsWith('.fbx.meta')));
  assert.ok(forb.some((w) => /temp\.png$/.test(w)));
  assert.ok(forb.some((w) => /3f2a9c1e/.test(w)));
  const naming = codes(r1, 'NAME_CONVENTION');
  assert.ok(naming.some((i) => /carton normal\.png$/.test(i.where) && /canonical name T_Carton_N\b/.test(i.fix) && !/rename to|git mv/.test(i.fix) && i.level === 'fixable'));
  assert.ok(naming.some((i) => /Música Pond\.wav$/.test(i.where)));
  assert.equal(codes(r1, 'REFERENCE_IMAGE_MISSING')[0].refs.length, 6, 'ref1 is on disk');
});

test('cross-doc conflicts: ADD for another game, VG-/AC- prefixes, slice inference', () => {
  assert.equal(codes(r1, 'ADD_TDD_MISMATCH').length, 1);
  assert.equal(codes(r1, 'AC_ID_PREFIX').length, 3);
  assert.equal(codes(r1, 'SLICE_INFERRED')[0].level, 'fixable');
  assert.match(codes(r1, 'GOLD_PATH_MISSING')[0].fix, /M1: author Docs\/V57\/gold_path\.json seeded from this draft \(GoldPathSource prefers it\) and log a D-###/);
  assert.equal(readJson(repo, 'acceptance').gold_path.status, 'draft');
  assert.ok(codes(r1, 'MARKER_DRAFT').length >= 1);
  assert.equal(r1.counts.blocking, 0);
});

test('DECISIONS.md is idempotent across re-runs', () => {
  const file = path.join(repo, 'Docs/V57/DECISIONS.md');
  const before = fs.readFileSync(file, 'utf8');
  const n = (before.match(/^## D-\d+/gm) || []).length;
  assert.equal(n, r1.issues.filter((i) => i.level === 'fixable' || i.level === 'conflict').length);
  const r2 = runIntake({ repo });
  assert.equal(r2.decisions.added, 0);
  assert.equal(fs.readFileSync(file, 'utf8'), before);
  fs.appendFileSync(file, '\n## D-900 · M1 · manual decision\n- kept\n');
  fs.writeFileSync(path.join(repo, 'Assets/_Game/Art/Props/Bumpers/Koala/Meshes/sm koala2.fbx'), 'x');
  const r3 = runIntake({ repo });
  assert.equal(r3.decisions.added, 1);
  assert.match(fs.readFileSync(file, 'utf8'), /## D-901 · I0 INTAKE · fixable · NAME_CONVENTION/);
});

test('CLI: empty repo is blocking (exit 2), --json summary', () => {
  const empty = tmpDir();
  const r = runCli(['--repo', empty, '--json']);
  assert.equal(r.code, 2, r.stderr);
  const out = JSON.parse(r.stdout);
  const c = out.issues.filter((i) => i.level === 'blocking').map((i) => i.code);
  assert.ok(c.includes('TDD_MISSING') && c.includes('STRUCTURE_UNRECOGNIZABLE') && c.includes('NO_ASSETS'));
  assert.ok(fs.existsSync(path.join(empty, 'Docs/V57/INTAKE_REPORT.md')));
});

test('CLI: template without TDD is blocking; bad args exit 1; fixture exits 0', () => {
  const noTdd = makeRepo({ tdd: false });
  assert.equal(runCli(['--repo', noTdd]).code, 2);
  assert.equal(runCli(['--bogus']).code, 1);
  assert.equal(runCli([]).code, 1);
  const ok = runCli(['--repo', repo]);
  assert.equal(ok.code, 0, ok.stderr);
  assert.match(ok.stdout, /V57 intake: OK/);
});

test('no ADD briefs and no assets -> blocking NO_ASSETS', () => {
  const r = runIntake({ repo: makeRepo({ add: false }) });
  assert.equal(r.exitCode, 2);
  assert.ok(r.issues.some((i) => i.code === 'NO_ASSETS' && i.level === 'blocking'));
});
