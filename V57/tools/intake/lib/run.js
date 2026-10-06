'use strict';
// Orchestrates I0 INTAKE: inventory -> parse -> cross-check -> build -> write -> report -> decisions.
const fs = require('fs');
const path = require('path');
const { Issues } = require('./issues');
const { inventory } = require('./inventory');
const { parseTdd } = require('./tdd');
const { parseAdd } = require('./add');
const xr = require('./crossref');
const { buildUi, buildScenes, buildInput, reportAddUnknown } = require('./build-world');
const { buildMechanics, buildTuning, buildAcceptance, buildEntities } = require('./build-core');
const { buildPackage, buildCamera, buildRendering, buildAudio, buildLocalization } = require('./build-meta');
const { buildAssets } = require('./build-assets');
const { writeGenerated, writeFileIfChanged } = require('./writer');
const { renderReport } = require('./report');
const { appendDecisions } = require('./decisions');

const VERSION = require('../package.json').version;
const DEFAULT_UNITY_PIN = '6000.6.2f1';

function unityPin(root, explicit) {
  if (explicit) return explicit;
  if (process.env.V57_UNITY_PIN) return process.env.V57_UNITY_PIN;
  for (const rel of ['V57/templates/ProjectSettings/ProjectVersion.txt', 'V57/ProjectVersion.txt', 'ProjectSettings/ProjectVersion.txt']) {
    try { const m = fs.readFileSync(path.join(root, rel), 'utf8').match(/m_EditorVersion:\s*(\S+)/); if (m) return m[1]; } catch (_) { /* next */ }
  }
  return DEFAULT_UNITY_PIN;
}

function locate(root, expected, dir, re, issues, code, label) {
  const want = path.join(root, expected);
  if (fs.existsSync(want)) return want;
  let alt = null;
  try { alt = fs.readdirSync(path.join(root, dir)).find((f) => re.test(f)); } catch (_) { /* none */ }
  if (alt) {
    issues.add({ code, level: 'fixable', area: 'repo', where: `${dir}/${alt}`, message: `${label} found at ${dir}/${alt} instead of ${expected}`, fix: `read from ${dir}/${alt} (provider should rename)`, refs: [label] });
    return path.join(root, dir, alt);
  }
  return null;
}

function markerIssues(doc, label, issues) {
  if (!doc) return;
  const bySection = new Map();
  for (const m of [...doc.markers.pending, ...doc.markers.draft]) {
    const k = m.marker.startsWith('[PENDING') ? 'PENDING' : 'DRAFT';
    const key = `${k}`;
    if (!bySection.has(key)) bySection.set(key, []);
    bySection.get(key).push(m);
  }
  for (const [k, list] of bySection) {
    issues.add({ code: `MARKER_${k}`, level: 'missing', area: label.toLowerCase(), where: `${doc.file}:${list.map((m) => m.line).join(',')}`,
      message: `${list.length} [${k}] marker(s) in ${label} (e.g. "${list[0].text.slice(0, 90)}")`, fix: 'content used as provisional; listed for owner review in M4', refs: list.map((m) => `L${m.line}`) });
  }
  (doc.pending || []).forEach((p) => issues.add({ code: 'TDD_PENDING_ROW', level: 'missing', area: 'tdd', where: `${doc.file}:${p.line}`,
    message: `§14.2 pending: ${p.location} — ${p.what} (owner ${p.owner}, ${p.resolve_by})`, fix: 'out of slice unless referenced by a slice scene', refs: [p.location] }));
  (doc.gate || []).filter((g) => g.status && !/pass/i.test(g.status)).forEach((g) => issues.add({ code: 'TDD_GATE_NOT_PASS', level: 'missing', area: 'tdd',
    where: `${doc.file}:${g.line}`, message: `§0.2 ${g.id} ${g.item} is "${g.status}"`, fix: 'intake re-checks independently; provider gate not trusted', refs: [g.id] }));
}

function sectionIssues(doc, label, required, issues) {
  for (const k of required) {
    if (!doc.found[k]) issues.add({ code: `${label}_SECTION_MISSING`, level: 'missing', area: label.toLowerCase(), where: doc.file, message: `${label} section "${k}" not found`, fix: 'defaults/placeholders used for dependent generated data', refs: [k] });
  }
}

function runIntake({ repo, out, unityPin: pin }) {
  const issues = new Issues();
  const root = path.resolve(repo);
  const outDir = path.resolve(root, out || 'Docs/Generated');
  if (!fs.existsSync(root) || !fs.statSync(root).isDirectory()) {
    issues.add({ code: 'REPO_NOT_FOUND', level: 'blocking', area: 'repo', where: root, message: 'repository path does not exist', fix: 'pass --repo <provider repo root>' });
    return { exitCode: 2, issues: issues.finalize(), outputs: [], model: {} };
  }
  const inv = inventory(root, issues);
  if (!inv.hasDocs && !inv.hasGame) {
    issues.add({ code: 'STRUCTURE_UNRECOGNIZABLE', level: 'blocking', area: 'repo', where: '.', message: 'neither Docs/ nor Assets/_Game/ exists — not a V57 provider repo', fix: 'provider must deliver the contract tree (brief §1)' });
  }
  const tddPath = locate(root, 'Docs/Design/TDD.md', 'Docs/Design', /(tdd|technical_design).*\.md$/i, issues, 'TDD_LOCATION', 'TDD');
  const addPath = locate(root, 'Docs/ArtDirection/ArtDirectionDocument.md', 'Docs/ArtDirection', /(art_?direction|^add).*\.md$/i, issues, 'ADD_LOCATION', 'ADD');
  const tddText = tddPath ? fs.readFileSync(tddPath, 'utf8') : '';
  const addText = addPath ? fs.readFileSync(addPath, 'utf8') : '';
  const rel = (p) => path.relative(root, p).replace(/\\/g, '/');
  const tdd = tddPath ? parseTdd(tddText, rel(tddPath), issues) : null;
  const add = parseAdd(addText, addPath ? rel(addPath) : 'Docs/ArtDirection/ArtDirectionDocument.md', issues);
  if (!tdd) issues.add({ code: 'TDD_MISSING', level: 'blocking', area: 'tdd', where: 'Docs/Design/TDD.md', message: 'provider TDD not found', fix: 'provider must deliver Docs/Design/TDD.md' });
  else if (!tdd.mechanics.length && !tdd.found.identity) issues.add({ code: 'TDD_UNRECOGNIZABLE', level: 'blocking', area: 'tdd', where: tdd.file, message: 'TDD has neither §A identity nor §B mechanics', fix: 'provider must deliver a TDD Standard 2.0.0 document' });
  if (!addPath) issues.add({ code: 'ADD_MISSING', level: 'missing', area: 'add', where: 'Docs/ArtDirection/ArtDirectionDocument.md', message: 'ADD not found', fix: 'style from TDD §8 only; all assets placeholders unless on disk' });
  if (!add.briefs.length && !inv.art.length) {
    issues.add({ code: 'NO_ASSETS', level: 'blocking', area: 'cross', where: 'Assets/_Game/Art/', message: 'ADD has no asset_briefs and no art assets are on disk', fix: 'provider must deliver asset briefs or assets' });
  }
  const model = {};
  let outputs = [];
  if (tdd && (tdd.mechanics.length || tdd.found.identity)) {
    sectionIssues(tdd, 'TDD', ['identity', 'screens', 'input', 'movement', 'scenes', 'specs', 'support'], issues);
    if (addPath) sectionIssues(add, 'ADD', ['asset_briefs', 'ui_screens', 'scene_manifest', 'color_palette', 'reference_images'], issues);
    markerIssues(tdd, 'TDD', issues);
    markerIssues(addPath ? add : null, 'ADD', issues);
    const c = xr.canon(tdd);
    const resolve = xr.makeResolver(issues);
    const ctx = { tdd, add, inv, issues, resolve, c, opts: { unityPin: unityPin(root, pin) }, tddPath, addPath, tddText, addText };
    xr.tddChecks(tdd, c, issues, resolve);
    const mismatch = addPath ? xr.addMismatch(tdd, add, c, issues) : false;
    xr.proseChecks([{ name: tdd.file, text: tddText }, { name: add.file, text: addText }], issues);
    const envNames = [...add.briefs.filter((b) => /environment|level|tileset/i.test(b.category || '')).map((b) => b.asset_name || b.name),
      ...inv.art.filter((f) => f.type === 'blockout').map((f) => f.asset)];
    xr.levelNameChecks([...tdd.scenes.map((s) => s.id), ...add.scenes.map((s) => s.id)], envNames, add.file, issues);
    model.ui = buildUi(ctx);
    model.scenes = buildScenes(ctx);
    model.input_map = buildInput(ctx);
    model.mechanics = buildMechanics(tdd);
    model.tuning = buildTuning(tdd, issues);
    const sliceScene = (model.scenes.scenes.find((s) => s.slice) || {}).id || null;
    model.acceptance = buildAcceptance(tdd, add, model.mechanics, c, issues, sliceScene);
    model.asset_manifest = buildAssets(ctx, model.ui, model.scenes.scenes);
    model.entities = buildEntities(tdd, model.asset_manifest, c);
    model.camera = buildCamera(ctx, model.scenes.scenes);
    model.rendering = buildRendering(ctx, mismatch);
    model.audio = buildAudio(ctx);
    reportAddUnknown(ctx, mismatch);
    const slice = model.scenes.scenes.filter((s) => s.slice).map((s) => s.id);
    if (!slice.length) issues.add({ code: 'NO_SLICE_SCENE', level: 'blocking', area: 'cross', where: `${tdd.file}:§13.2`, message: 'no slice scene can be determined (ADD scene_manifest and TDD §13.2)', fix: 'provider must mark one scene slice: true' });
    model.package = buildPackage(ctx, slice, model.acceptance.gold_path);
    model.localization = buildLocalization(tdd, model.ui);
  }
  const list = issues.finalize();
  const ref = (i) => `${i.id} ${i.code}`;
  if (model.asset_manifest) model.asset_manifest.assets.forEach((a) => { a.issues = [...new Set(a.issues.map(ref))]; });
  const exitCode = issues.has('blocking') ? 2 : 0;
  if (model.package) outputs = writeGenerated(outDir, model, model.localization || [], VERSION).map(rel);
  const reportPath = path.join(root, 'Docs/V57/INTAKE_REPORT.md');
  const decisions = appendDecisions(root, list);
  outputs.push(rel(reportPath), rel(decisions.file));
  writeFileIfChanged(reportPath, renderReport({ tdd, add: addPath ? add : null, inv, model, issues: list, exitCode, outputs, version: VERSION }));
  return { exitCode, issues: list, outputs, model, decisions, counts: issues.counts() };
}

module.exports = { runIntake, VERSION, DEFAULT_UNITY_PIN };
