'use strict';
// Docs/V57/INTAKE_REPORT.md
const { LEVELS, LEVEL_MEANING } = require('./issues');

const esc = (s) => String(s === undefined || s === null ? '' : s).replace(/\|/g, '\\|').replace(/\r?\n/g, ' ');
const st = (ok, partial) => (ok ? 'OK' : partial ? 'PARTIAL' : 'MISSING');

function table(headers, rows) {
  return [`| ${headers.join(' | ')} |`, `|${headers.map(() => '---').join('|')}|`, ...rows.map((r) => `| ${r.map(esc).join(' | ')} |`)].join('\n');
}

function tddRows(tdd) {
  if (!tdd) return [['TDD', 'MISSING', '-', 'Docs/Design/TDD.md not found']];
  const f = tdd.found;
  const gatePass = tdd.gate.filter((g) => /pass/i.test(g.status)).length;
  const specOk = tdd.specs.filter((s) => s.data).length;
  return [
    ['§A Project Identity', st(f.identity && tdd.identity.engine, f.identity), tdd.identity.title || '-', `engine ${tdd.identity.engine || '?'}, ${tdd.identity.dimension || '?'}`],
    ['§0.2 Completeness gate', st(f.gate && gatePass === tdd.gate.length, f.gate), `${gatePass}/${tdd.gate.length} PASS`, 'provider self-report (not trusted as a gate)'],
    ['§3 Core-loop traceability', st(tdd.coreLoop.length > 0), tdd.coreLoop.length, 'source of draft gold path'],
    ['§B Mechanics', st(tdd.mechanics.length > 0), tdd.mechanics.length, tdd.mechanics.map((m) => m.name).join(', ')],
    ['§B-S Support systems', st(tdd.support.length > 0, f.support), tdd.support.length, tdd.support.map((s) => s.id).join(', ')],
    ['§C Companion specs', st(tdd.specs.length === tdd.mechanics.length && specOk === tdd.specs.length, tdd.specs.length > 0), `${tdd.specs.length} (${specOk} parse)`, ''],
    ['§8 Art / palette', st(tdd.art.palette.length > 0, f.art), tdd.art.palette.length, tdd.art.palette.map((p) => p.hex).join(' ')],
    ['§9.1 Screen registry', st(tdd.screens.length > 0), tdd.screens.length, ''],
    ['§10 Audio', st(f.audio), f.audio ? 'yes' : '-', ''],
    ['§11.1 Engine & rendering', st(Object.keys(tdd.engine).length > 0), Object.keys(tdd.engine).length, tdd.engine.render_pipeline ? tdd.engine.render_pipeline.slice(0, 40) : ''],
    ['§11.3 Input map', st(tdd.inputs.length > 0), tdd.inputs.length, tdd.inputs.map((a) => a.action).join(', ')],
    ['§11.5 Movement & camera', st(!!tdd.movement.in_play_camera, f.movement), Object.keys(tdd.movement).length, tdd.movement.control_mode ? `control: ${tdd.movement.control_mode.slice(0, 30)}` : ''],
    ['§11.6 Performance budgets', st(tdd.perf.length > 0), tdd.perf.length, ''],
    ['§13.1 Content inventory', st(tdd.content.length > 0), tdd.content.length, ''],
    ['§13.2 Scene manifest', st(tdd.scenes.length > 0), tdd.scenes.length, tdd.scenes.map((s) => s.id).join(', ')],
    ['§14.2 Pending registry', tdd.pending.length ? 'PARTIAL' : 'OK', tdd.pending.length, `${tdd.markers.pending.length} [PENDING] / ${tdd.markers.draft.length} [DRAFT] markers, ${tdd.markers.tofill} [TO-FILL:eng]`],
  ];
}

function addRows(add, model) {
  if (!add) return [['ADD', 'MISSING', '-', 'Docs/ArtDirection/ArtDirectionDocument.md not found']];
  const f = add.found;
  const v57 = add.briefs.filter((b) => b.asset_name && b.files).length;
  const tddScreens = new Set(model.ui ? model.ui.screens.map((s) => s.id) : []);
  const tddScenes = new Set(model.scenes ? model.scenes.scenes.map((s) => s.id) : []);
  const refOk = model.asset_manifest ? model.asset_manifest.assets.filter((a) => a.type === 'reference' && a.files.reference).length : 0;
  const fmt = (k) => (add.formats[k] ? ` (${add.formats[k]})` : '');
  return [
    ['asset_briefs', st(add.briefs.length && v57 === add.briefs.length, add.briefs.length), add.briefs.length, `${v57}/${add.briefs.length} with V57 fields${fmt('asset_briefs')}`],
    ['ui_screens', st(f.ui_screens && add.ui_screens.every((s) => tddScreens.has(s.id)), f.ui_screens), add.ui_screens.length, `${add.ui_screens.filter((s) => tddScreens.has(s.id)).length} merged into ui${fmt('ui_screens')}`],
    ['screen_layouts', st(add.screen_layouts.length > 0), add.screen_layouts.length, ''],
    ['scene_manifest', st(f.scene_manifest && add.scenes.every((s) => tddScenes.has(s.id)), f.scene_manifest), add.scenes.length, `${add.scenes.filter((s) => tddScenes.has(s.id)).length} matched TDD §13.2, ${add.scenes.filter((s) => s.slice).length} slice${fmt('scene_manifest')}`],
    ['color_palette', st(add.palette.length > 0), add.palette.length, `${add.palette.filter((p) => p.status === 'draft').length} draft`],
    ['visual_targets', st(!!add.visual_targets), add.visual_targets ? 'yes' : '-', ''],
    ['style_guide', st(!!add.style_guide), add.style_guide ? 'yes' : '-', ''],
    ['reference_images', st(add.reference_images.length > 0 && refOk === add.reference_images.length, add.reference_images.length), add.reference_images.length, `${refOk} files on disk`],
    ['camera_views', st(add.camera_views.length > 0), add.camera_views.length, 'optional'],
    ['markers', add.markers.draft.length + add.markers.pending.length ? 'PARTIAL' : 'OK', add.markers.draft.length + add.markers.pending.length, `${add.markers.draft.length} [DRAFT], ${add.markers.pending.length} [PENDING]`],
  ];
}

function repoRows(inv, model) {
  const n = (t) => inv.art.filter((f) => f.type === t).length;
  const area = (re) => inv.art.filter((f) => re.test(f.rel)).length;
  return [
    ['Characters', st(area(/\/Art\/Characters\//) > 0), area(/\/Art\/Characters\//), ''],
    ['Props', st(area(/\/Art\/Props\//) > 0), area(/\/Art\/Props\//), ''],
    ['Environment kits/decoration', st(area(/\/Environment\/(Kits|Decoration)\//) > 0), area(/\/Environment\/(Kits|Decoration)\//), ''],
    ['Blockouts (BLK_)', st(n('blockout') > 0), n('blockout'), 'one per slice scene expected'],
    ['UI sprites / icons / fonts', st(n('sprite') + n('font') > 0), n('sprite') + n('font'), ''],
    ['VFX', st(n('vfx') > 0), n('vfx'), ''],
    ['Audio', st(inv.audio.length > 0), inv.audio.length, ''],
    ['Docs files', st(inv.docs.length > 0), inv.docs.length, ''],
    ['Orphans (no brief)', model.asset_manifest && model.asset_manifest.orphans.length ? 'PARTIAL' : 'OK', model.asset_manifest ? model.asset_manifest.orphans.length : 0, ''],
    ['Forbidden files', inv.forbidden.length ? 'PARTIAL' : 'OK', inv.forbidden.length, inv.v57Managed ? 'repo is V57-managed (Unity folders tolerated)' : 'provider delivery'],
  ];
}

function renderReport({ tdd, add, inv, model, issues, exitCode, outputs, version }) {
  const counts = Object.fromEntries(LEVELS.map((l) => [l, issues.filter((i) => i.level === l).length]));
  const verdict = exitCode === 2 ? 'BLOCKED — the run must stop (exit 2)' : 'READY — run continues (exit 0)';
  const pkg = model.package || {};
  const lines = [
    '# V57 Intake Report',
    '',
    `> Generated by v57-intake ${version}. Do not edit by hand — re-run \`node V57/tools/intake/index.js --repo .\`.`,
    '',
    `**Verdict:** ${verdict}`,
    '',
    `**Game:** ${pkg.title || '?'} (\`${pkg.slug || '?'}\`) · perspective ${pkg.perspective || '?'} · physics ${pkg.physics || '?'} · Unity pinned ${pkg.engine ? pkg.engine.unity_pinned : '?'} (TDD declares ${pkg.engine ? pkg.engine.tdd_declared : '?'})`,
    `**Slice scenes:** ${pkg.slice && pkg.slice.scenes.length ? pkg.slice.scenes.join(', ') : 'none'}`,
    '',
    '## Summary',
    '',
    table(['Level', 'Count', 'Meaning'], LEVELS.map((l) => [l, counts[l], LEVEL_MEANING[l]])),
    '',
    '## Completeness — TDD',
    '',
    table(['Section', 'Status', 'Count', 'Notes'], tddRows(tdd)),
    '',
    '## Completeness — ADD',
    '',
    table(['Section', 'Status', 'Count', 'Notes'], addRows(add, model)),
    '',
    '## Completeness — assets on disk',
    '',
    table(['Module', 'Status', 'Files', 'Notes'], repoRows(inv, model)),
    '',
  ];
  for (const l of LEVELS) {
    const list = issues.filter((i) => i.level === l);
    lines.push(`## Issues — ${l} (${list.length})`, '');
    if (!list.length) { lines.push('_None._', ''); continue; }
    lines.push(table(['ID', 'Code', 'Where', 'Issue', 'Suggested auto-fix / handling'],
      list.map((i) => [i.id, i.code, `\`${i.where}\``, `${i.message}${i.count > 1 ? ` (×${i.count})` : ''}`, i.fix])), '');
  }
  lines.push('## Generated files', '', ...outputs.map((o) => `- \`${o}\``), '');
  return lines.join('\n');
}

module.exports = { renderReport };
