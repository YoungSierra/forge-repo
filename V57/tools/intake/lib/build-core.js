'use strict';
// Generated: package, mechanics, tuning, acceptance, entities.
const { matchId } = require('./fuzzy');

const pad = (n) => String(n).padStart(2, '0');

function buildMechanics(tdd) {
  const mechanics = tdd.mechanics.map((m, i) => {
    const spec = tdd.specs.find((s) => s.name === m.name);
    const num = spec && spec.index ? spec.index.replace(/^C-/, '') : pad(i + 1);
    let acs = spec ? spec.acs.map((a) => ({ id: a.id, mode: normMode(a.mode), text: a.text })) : [];
    if (!acs.length) {
      acs = m.acs.map((a) => ({ id: `AC-M${num}-${a.local.replace(/\D/g, '')}`, mode: normMode(a.mode), text: a.text }));
    }
    return {
      name: m.name, type: m.type || (spec && spec.type) || null, version: m.version, status: m.status,
      dependencies: m.dependencies.map((d) => d.id), spec_yaml: spec ? spec.raw : '', acs,
    };
  });
  return { mechanics };
}

function normMode(m) {
  return /edit/i.test(m) ? 'EditMode' : 'PlayMode';
}

function numericLeaves(obj, prefix, out) {
  if (!obj || typeof obj !== 'object') return out;
  for (const [k, v] of Object.entries(obj)) {
    if (['specVersion', 'version', 'acceptanceCriteria', 'validationGates', 'touches', 'components', 'publicAPI', 'dependencies'].includes(k)) continue;
    const key = prefix ? `${prefix}.${k}` : k;
    if (typeof v === 'number') out.push([key, v]);
    else if (v && typeof v === 'object' && !Array.isArray(v)) numericLeaves(v, key, out);
  }
  return out;
}

function buildTuning(tdd, issues) {
  const values = [];
  for (const m of tdd.mechanics) {
    for (const l of m.levers) {
      values.push({ mechanic: m.name, param: l.param, value: l.value, unit: l.unit, min: null, max: null, source: '§B levers' });
      if (l.value === null) {
        issues.add({ code: 'TUNING_NO_VALUE', level: 'missing', area: 'tdd', where: `${tdd.file}:${m.line}`,
          message: `${m.name}.${l.param} has no numeric value in §B levers`, fix: 'config field created with default 0; set in SO asset', refs: [`${m.name}.${l.param}`] });
      }
    }
    const spec = tdd.specs.find((s) => s.name === m.name);
    if (spec && spec.data) {
      for (const [param, value] of numericLeaves(spec.data, '', [])) {
        if (!values.some((v) => v.mechanic === m.name && v.param === param)) values.push({ mechanic: m.name, param, value, unit: null, min: null, max: null, source: '§C' });
      }
    }
  }
  // pair xMin/xMax levers into min/max of the base param when both exist
  for (const v of values) {
    const base = v.param.replace(/(Min|Max)$/, '');
    if (base === v.param) continue;
    const lo = values.find((x) => x.mechanic === v.mechanic && x.param === `${base}Min`);
    const hi = values.find((x) => x.mechanic === v.mechanic && x.param === `${base}Max`);
    if (lo && hi && typeof lo.value === 'number' && typeof hi.value === 'number') { v.min = lo.value; v.max = hi.value; }
  }
  return { values };
}

/**
 * Draft gold path from the §3 core-loop table, in the GoldPathDriver step contract:
 * input  {id, do: press|hold|release, action: "Map/Action", seconds?}
 * check  {id, expect: "<probe key>", timeout_s}
 */
function draftGoldPath(tdd, c, scene) {
  if (!tdd.coreLoop.length) return null;
  const steps = [];
  const id = () => `S${String(steps.length + 1).padStart(2, '0')}`;
  for (const row of tdd.coreLoop) {
    const tokens = row.trigger.replace(/\(.*?\)/g, '').split(/\s*(?:\+|\/|,|\bor\b)\s*/).map((t) => t.trim()).filter(Boolean);
    for (const t of tokens) {
      const input = tdd.inputs.find((a) => a.action === t);
      if (input) {
        const verb = /^\s*hold/i.test(input.binding) ? 'hold' : /^\s*release/i.test(input.binding) ? 'release' : 'press';
        steps.push({ id: id(), do: verb, action: `${input.map}/${input.action}`, ...(verb === 'hold' ? { seconds: 1 } : {}), mechanic: row.mechanic });
      } else if (/^[A-Z]\w+Event$/.test(t)) {
        steps.push({ id: id(), expect: t, timeout_s: 10, mechanic: row.mechanic });
      }
    }
  }
  return steps.length ? { status: 'draft', source: 'TDD §3 core-loop traceability (review in M1)', scene, steps } : null;
}

function buildAcceptance(tdd, add, mech, c, issues, scene = null) {
  const criteria = [];
  mech.mechanics.forEach((m) => m.acs.forEach((a) => criteria.push({ id: a.id, mechanic: m.name, mode: a.mode, text: a.text })));
  let gold = null;
  if (tdd.goldPath) gold = { status: 'provided', source: 'TDD', ...(Array.isArray(tdd.goldPath) ? { steps: tdd.goldPath } : tdd.goldPath) };
  else if (add.gold_path) gold = { status: 'provided', source: 'ADD', ...add.gold_path };
  else {
    gold = draftGoldPath(tdd, c, scene);
    issues.add({ code: 'GOLD_PATH_MISSING', level: 'missing', area: 'tdd', where: tdd.file,
      message: 'no gold_path section in TDD or ADD', fix: gold ? `acceptance.gold_path is a draft derived from §3 (${gold.steps.length} steps, status: draft). M1: author Docs/V57/gold_path.json seeded from this draft (GoldPathSource prefers it) and log a D-###`
        : 'M1: author Docs/V57/gold_path.json (GoldPathSource prefers it) and log a D-###', refs: ['gold_path'] });
  }
  const noAc = mech.mechanics.filter((m) => !m.acs.length).map((m) => m.name);
  if (noAc.length) {
    issues.add({ code: 'AC_MISSING', level: 'missing', area: 'tdd', where: tdd.file, message: `mechanics without acceptance criteria: ${noAc.join(', ')}`,
      fix: 'placeholder AC "mechanic runs without errors in slice scene" generated in F', refs: noAc });
  }
  return { criteria, gold_path: gold };
}

const KIND_BY_CATEGORY = [
  [/boss|character|companion|npc|enemy|creature/i, 'character'],
  [/vfx|effect|particle/i, 'vfx'],
  [/ui|hud|frame|screen/i, 'ui'],
  [/environment|tileset|level|kit|decor/i, 'environment'],
  [/audio|sfx|music/i, 'audio'],
  [/prop|item|pickup/i, 'prop'],
];

function kindOf(category) {
  const k = KIND_BY_CATEGORY.find(([re]) => re.test(category || ''));
  return k ? k[1] : 'prop';
}

function buildEntities(tdd, assets, c) {
  const entities = [];
  const served = new Set();
  for (const a of assets.assets.filter((x) => x.source === 'brief')) {
    const mechs = a.serves.filter((s) => c.systems.includes(s));
    mechs.forEach((m) => served.add(m));
    entities.push({ id: a.asset_name || a.asset_id, kind: kindOf(a.category), mechanics: mechs, asset_id: a.asset_id });
  }
  tdd.mechanics.forEach((m) => { if (!served.has(m.name)) entities.push({ id: m.name, kind: 'mechanic', mechanics: [m.name], asset_id: null }); });
  tdd.support.forEach((s) => { if (!served.has(s.id)) entities.push({ id: s.id, kind: 'service', mechanics: [], asset_id: null }); });
  return { entities };
}

module.exports = { buildMechanics, buildTuning, buildAcceptance, buildEntities, kindOf };
