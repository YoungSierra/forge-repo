'use strict';
// Provider TDD ("TDD Standard 2.x", V57/docs/tdd/TDD_Template.md) parser. Section-keyed, tolerant: every missing
// section is recorded in `found` and reported by the caller, never thrown.
const YAML = require('yaml');
const { splitSections, findSection, parseTables, cell, splitList, stripMd, extractFences, isEmptyValue } = require('./md');
const { parseMechanicBlock, parseSpecs } = require('./tdd-mechanics');
const { parsePalette } = require('./palette');

const SECTIONS = {
  identity: { keys: ['A'], re: [/project identity/i] },
  control: { keys: ['0.1'], re: [/document control/i] },
  gate: { keys: ['0.2'], re: [/completeness gate/i] },
  coreLoop: { keys: ['3'], re: [/core gameplay/i] },
  overview: { keys: ['2'], re: [/game overview/i] },
  art: { keys: ['8'], re: [/art direction/i] },
  ui: { keys: ['9'], re: [/^ui\b/i] },
  screens: { keys: ['9.1'], re: [/screen registry/i] },
  audio: { keys: ['10'], re: [/audio direction/i] },
  engine: { keys: ['11.1'], re: [/engine & rendering/i] },
  input: { keys: ['11.3'], re: [/input map/i] },
  persistence: { keys: ['11.4'], re: [/persistence/i] },
  movement: { keys: ['11.5'], re: [/movement/i] },
  perf: { keys: ['11.6'], re: [/performance budgets/i] },
  content: { keys: ['13.1'], re: [/content scope/i] },
  scenes: { keys: ['13.2'], re: [/scene manifest/i] },
  pending: { keys: ['14.2'], re: [/pending registry/i] },
  ledger: { keys: ['14.3'], re: [/consistency ledger/i] },
  support: { keys: ['B-S'], re: [/support systems/i] },
  specs: { keys: ['C'], re: [/companion specs/i] },
  goldPath: { keys: ['gold_path'], re: [/gold path/i] },
};

function firstTable(sec) {
  if (!sec) return [];
  const t = parseTables(sec.body, sec.bodyLine)[0];
  return t ? t.rows : [];
}

function kv(rows) {
  const out = {};
  rows.forEach((r) => {
    const k = stripMd(r._cells[0] || '').toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '');
    if (k) out[k] = stripMd(r._cells[1] || '');
  });
  return out;
}

function parseIdentity(sec, control, text, issues, file) {
  let id = {};
  if (sec) {
    const f = extractFences(sec.body, sec.bodyLine).find((x) => !x.lang || /ya?ml/.test(x.lang));
    if (f) {
      try { id = YAML.parse(f.content) || {}; } catch (e) {
        issues.add({ code: 'TDD_IDENTITY_YAML_INVALID', level: 'fixable', area: 'tdd', where: `${file}:${f.line}`,
          message: `§A yaml does not parse: ${e.message.split('\n')[0]}`, fix: 'fall back to §0.1 Document Control table' });
      }
    }
  }
  const ctl = kv(firstTable(control));
  const h1 = (String(text).match(/^#\s+(.+?)\s+[—–-]\s+/m) || [])[1];
  return {
    title: id.project_name || ctl.game_title || (h1 ? stripMd(h1) : null),
    version: id.document_version ? String(id.document_version) : ctl.document_version || null,
    standard: ctl.template_standard || null,
    engine: id.engine || null,
    render_pipeline: id.render_pipeline || null,
    dimension: id.dimension || null,
    genre: id.genre || null,
    target_platform: Array.isArray(id.target_platform) ? id.target_platform.map(String) : (id.target_platform ? [String(id.target_platform)] : []),
    input_system: id.input_system || null,
    save_model: id.save_model || null,
    multiplayer_model: id.multiplayer_model || null,
    max_players: id.max_players || null,
    performance_targets: Array.isArray(id.performance_targets) ? id.performance_targets : [],
    raw: id,
  };
}

/** Real [PENDING]/[DRAFT] markers (legend/meta mentions in backticks without owner are ignored). */
function findMarkers(text) {
  const out = { pending: [], draft: [], tofill: 0 };
  String(text).split(/\r?\n/).forEach((line, i) => {
    // backticked markers are legend/meta mentions unless they carry a concrete owner=
    const noCode = line.replace(/`(\[(?:PENDING|DRAFT)[^\]`]*\])`/g, (all, m) => (/owner=[^<\s\]]+/.test(m) ? m : ''));
    const p = noCode.match(/\[PENDING[^\]]*\]/g);
    if (p) p.filter((x) => !/<who>/.test(x)).forEach((m) => out.pending.push({ line: i + 1, marker: m, text: stripMd(line).slice(0, 140) }));
    const d = noCode.match(/\[DRAFT[^\]]*\]/g);
    if (d) d.forEach((m) => out.draft.push({ line: i + 1, marker: m, text: stripMd(line).slice(0, 140) }));
    out.tofill += (line.match(/\[TO-FILL/g) || []).length;
  });
  return out;
}

function parseTdd(text, file, issues) {
  const sections = splitSections(text);
  const sec = {};
  const found = {};
  for (const [name, spec] of Object.entries(SECTIONS)) {
    sec[name] = findSection(sections, spec.keys, spec.re);
    found[name] = !!sec[name];
  }
  const identity = parseIdentity(sec.identity, sec.control, text, issues, file);
  const mechanics = sections.filter((s) => s.key.startsWith('mechanic:')).map((s) => parseMechanicBlock(s, issues, file));
  const support = firstTable(sec.support).map((r) => ({ id: cell(r, 'id'), purpose: cell(r, 'purpose'), line: r._line })).filter((s) => s.id);
  const specs = parseSpecs(sec.specs, issues, file);
  const screens = firstTable(sec.screens).map((r) => ({
    id: cell(r, 'screen_id', 'id', 'screen'),
    purpose: cell(r, 'purpose'),
    states: splitList(cell(r, 'key_states', 'states')),
    consumed_by: splitList(cell(r, 'consumed_by', 'consumer')),
    line: r._line,
  })).filter((s) => s.id);
  const inputs = firstTable(sec.input).map((r) => ({
    map: cell(r, 'action_map', 'map'),
    action: cell(r, 'action'),
    binding: cell(r, 'suggested_binding', 'binding', 'bindings'),
    consumed_by: splitList(cell(r, 'consumed_by')),
    line: r._line,
  })).filter((a) => a.action);
  const scenes = firstTable(sec.scenes).map((r) => ({
    id: cell(r, 'scene_id', 'id', 'scene'),
    purpose: cell(r, 'purpose'),
    world_owner: cell(r, 'world_owner'),
    systems: splitList(cell(r, 'systems_present', 'systems')),
    acs: splitList(cell(r, 'playmode_acs_covered', 'acs', 'playmode_acs')),
    line: r._line,
  })).filter((s) => s.id);
  const pending = firstTable(sec.pending).map((r) => ({
    location: cell(r, 'location'), what: cell(r, 'what_is_pending', 'what'), owner: cell(r, 'owner'),
    resolve_by: cell(r, 'resolve_by'), status: cell(r, 'status'), line: r._line,
  })).filter((p) => !isEmptyValue(p.location) || !isEmptyValue(p.what))
    .filter((p) => !/^\(?empty/i.test(p.what) && !/^\(?empty/i.test(p.location));
  const gate = firstTable(sec.gate).map((r) => ({ id: cell(r, 'col0', '') || stripMd(r._cells[0]), item: cell(r, 'item'), status: cell(r, 'status'), line: r._line }));
  const coreLoop = firstTable(sec.coreLoop).map((r) => ({
    step: cell(r, 'loop_step', 'step'), mechanic: cell(r, 'b_mechanic', 'mechanic'),
    trigger: cell(r, 'triggering_input', 'trigger'), feedback: cell(r, 'feedback_channel', 'feedback'), line: r._line,
  })).filter((r) => r.step);
  const engine = kv(firstTable(sec.engine));
  const movement = kv(firstTable(sec.movement));
  const perf = firstTable(sec.perf).map((r) => ({
    platform: cell(r, 'platform'), resolution: cell(r, 'resolution'), fps: Number(cell(r, 'fps_target', 'fps')) || null,
    frame_ms: Number(cell(r, 'frame_budget', 'frame_budget_ms')) || null, memory: cell(r, 'memory_ceiling', 'memory'), notes: cell(r, 'notes'),
  })).filter((p) => p.platform);
  const content = firstTable(sec.content).map((r) => ({ category: cell(r, 'category'), count: cell(r, 'first_pass_count', 'count'), notes: cell(r, 'notes') }));
  const overview = kv(firstTable(sec.overview));
  let goldPath = null;
  if (sec.goldPath) {
    const f = extractFences(sec.goldPath.body, sec.goldPath.bodyLine)[0];
    try { goldPath = f ? YAML.parse(f.content) : null; } catch (_) { goldPath = null; }
  }
  return {
    file, found, identity, mechanics, support, specs, screens, inputs, scenes, pending, gate, coreLoop,
    engine, movement, perf, content, overview, goldPath,
    ledger: firstTable(sec.ledger).length,
    art: { text: sec.art ? sec.art.body : '', palette: sec.art ? parsePalette(sec.art.body, 'tdd') : [] },
    audio: { text: sec.audio ? sec.audio.body : '' },
    uiText: sec.ui ? sec.ui.body : '',
    markers: findMarkers(text),
    sectionLines: Object.fromEntries(Object.entries(sec).filter(([, s]) => s).map(([k, s]) => [k, s.line])),
  };
}

module.exports = { parseTdd, findMarkers, SECTIONS };
