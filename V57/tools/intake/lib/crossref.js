'use strict';
// ID consistency across TDD / ADD / disk, with "did you mean" suggestions.
const { matchId, proseTypos, norm } = require('./fuzzy');

function canon(tdd) {
  const mechanics = tdd.mechanics.map((m) => m.name);
  const support = tdd.support.map((s) => s.id);
  const acs = [];
  tdd.specs.forEach((s) => s.acs.forEach((a) => acs.push(a.id)));
  return {
    mechanics,
    systems: [...mechanics, ...support],
    screens: tdd.screens.map((s) => s.id),
    scenes: tdd.scenes.map((s) => s.id),
    actions: tdd.inputs.map((a) => a.action),
    acs,
  };
}

/** Resolve an id against canonical candidates; logs case/typo as fixable. */
function makeResolver(issues) {
  return function resolve(id, candidates, { what, where, area = 'cross' }) {
    const r = matchId(id, candidates);
    if (r.status === 'case' || r.status === 'typo') {
      issues.add({ code: 'ID_TYPO', level: 'fixable', area, where,
        message: `${what} "${id}" does not match any canonical id — did you mean "${r.match}"?`,
        fix: `normalize to "${r.match}" in Docs/Generated (provider doc untouched)`, refs: [id, r.match] });
    }
    return r;
  };
}

/** "PlungerLaunchSystem + BallPhysicsDrainSystem (populates ...)" -> ids */
function ownerIds(s) {
  return String(s || '').replace(/\(.*$/s, '').split(/\s*(?:\+|,|&|\band\b)\s*/).map((x) => x.trim()).filter((x) => /^[A-Za-z]\w+$/.test(x));
}

/** "VG-M01-3" -> "AC-M01-3" when only the prefix differs from a §C id. */
function acByPrefix(id, acs) {
  const m = String(id).match(/^[A-Za-z]+-(M\d+-\d+)$/);
  if (!m) return null;
  return acs.find((a) => a.endsWith(`-${m[1]}`) && a !== id) || null;
}

function tddChecks(tdd, c, issues, resolve) {
  const f = tdd.file;
  const sys = [...c.systems, 'standalone'];
  const unknownSys = (id, where, what) => {
    const r = resolve(id, sys, { what, where, area: 'tdd' });
    if (r.status === 'unknown') {
      issues.add({ code: 'TDD_UNKNOWN_ID', level: 'conflict', area: 'tdd', where, message: `${what} "${id}" is not a §B mechanic or §B-S system`,
        fix: 'kept as-is (TDD authority); flagged for owner review', refs: [id] });
    }
  };
  tdd.screens.forEach((s) => s.consumed_by.forEach((id) => unknownSys(id, `${f}:${s.line}`, `§9.1 ${s.id} consumer`)));
  tdd.inputs.forEach((a) => a.consumed_by.forEach((id) => unknownSys(id, `${f}:${a.line}`, `§11.3 ${a.action} consumer`)));
  tdd.scenes.forEach((s) => {
    s.systems.forEach((id) => unknownSys(id, `${f}:${s.line}`, `§13.2 ${s.id} system`));
    ownerIds(s.world_owner).filter((x) => x !== 'standalone').forEach((id) => unknownSys(id, `${f}:${s.line}`, `§13.2 ${s.id} world owner`));
  });
  tdd.mechanics.forEach((m) => {
    m.uiRefs.forEach((u) => {
      const r = resolve(u, c.screens, { what: `§B ${m.name} UI reference`, where: `${f}:${m.line}`, area: 'tdd' });
      if (r.status === 'unknown') {
        issues.add({ code: 'TDD_UI_NOT_REGISTERED', level: 'missing', area: 'tdd', where: `${f}:${m.line}`,
          message: `§B ${m.name} references ${u}, absent from §9.1`, fix: 'placeholder screen entry added to ui.yaml', refs: [u] });
      }
    });
    m.sceneRefs.forEach((s) => {
      const r = resolve(s, c.scenes, { what: `§B ${m.name} scene reference`, where: `${f}:${m.line}`, area: 'tdd' });
      if (r.status === 'unknown') {
        issues.add({ code: 'TDD_SCENE_NOT_REGISTERED', level: 'conflict', area: 'tdd', where: `${f}:${m.line}`,
          message: `§B ${m.name} references ${s}, absent from §13.2`, fix: 'reference ignored; §13.2 is the scene authority', refs: [s] });
      }
    });
    m.playerInputs.forEach((a) => {
      const r = resolve(a, c.actions, { what: `§B ${m.name} player input`, where: `${f}:${m.line}`, area: 'tdd' });
      if (r.status === 'unknown') {
        issues.add({ code: 'TDD_INPUT_NOT_MAPPED', level: 'missing', area: 'tdd', where: `${f}:${m.line}`,
          message: `§B ${m.name} player input ${a} has no §11.3 row`, fix: 'placeholder action added to input_map (no binding)', refs: [a] });
      }
    });
    if (!tdd.specs.some((s) => s.name === m.name)) {
      issues.add({ code: 'TDD_SPEC_MISSING', level: 'missing', area: 'tdd', where: `${f}:${m.line}`,
        message: `§B ${m.name} has no §C companion spec`, fix: 'spec_yaml left empty; acceptance derived from §B ACs', refs: [m.name] });
    }
  });
  tdd.specs.filter((s) => !c.mechanics.includes(s.name)).forEach((s) => issues.add({ code: 'TDD_SPEC_ORPHAN', level: 'conflict', area: 'tdd',
    where: `${f}:${s.line}`, message: `§C spec ${s.name} has no §B block`, fix: 'spec ignored (§B is the mechanic authority)', refs: [s.name] }));
}

/** Normalize AC ids referenced by scenes; returns canonical list. */
function normalizeAcs(ids, c, { where, area, owner }, issues, resolve) {
  const out = [];
  const swapped = [];
  for (const id of ids) {
    if (c.acs.includes(id)) { out.push(id); continue; }
    const p = acByPrefix(id, c.acs);
    if (p) { swapped.push(`${id}→${p}`); out.push(p); continue; }
    const r = resolve(id, c.acs, { what: `${owner} AC`, where, area });
    if (r.status !== 'unknown') { out.push(r.match); continue; }
    issues.add({ code: 'AC_UNKNOWN', level: 'conflict', area, where, message: `${owner} lists AC "${id}" that no §C spec defines`,
      fix: 'dropped from generated scene ACs (§C is the AC authority)', refs: [id] });
  }
  if (swapped.length) {
    issues.add({ code: 'AC_ID_PREFIX', level: 'fixable', area, where,
      message: `${owner} uses ${swapped.length} AC ids with a different prefix than §C (e.g. ${swapped[0]})`,
      fix: 'mapped by mechanic/number to the §C AC ids', refs: [owner] });
  }
  return [...new Set(out)];
}

/** Scene level-name typos vs environment asset names / blockout folders (SCN_BiollumeBloom vs Biolume_Bloom). */
function levelNameChecks(sceneIds, envNames, where, issues) {
  const envs = [...new Set(envNames.filter(Boolean))];
  if (!envs.length) return;
  for (const id of sceneIds) {
    const m = String(id).match(/^SCN_([A-Za-z0-9]+)/);
    if (!m) continue;
    const level = m[1];
    if (envs.some((e) => norm(e) === norm(level) || norm(e).includes(norm(level)) || norm(level).includes(norm(e)))) continue;
    const r = matchId(level, envs);
    if (r.status === 'typo') {
      issues.add({ code: 'ID_TYPO', level: 'fixable', area: 'cross', where,
        message: `scene ${id}: level name "${level}" does not match any environment asset — did you mean "${r.match}"?`,
        fix: `treat level as "${r.match}" when resolving blockout/kit`, refs: [id, r.match] });
    }
  }
}

function proseChecks(docs, issues) {
  for (const t of proseTypos(docs)) {
    issues.add({ code: 'TEXT_TYPO', level: 'fixable', area: 'cross', where: `${t.doc}:${t.line}`,
      message: `"${t.token}" looks like a typo — did you mean "${t.suggestion}"?`,
      fix: `read as "${t.suggestion}" (provider doc untouched)`, refs: [t.token, t.suggestion] });
  }
}

/** Heuristic: ADD ids overlap nothing in TDD and never names the TDD title -> ADD likely belongs to another game. */
function addMismatch(tdd, add, c, issues) {
  const ids = [
    ...add.ui_screens.filter((s) => !s.consumed_by.includes('standalone')).map((s) => [s.id, c.screens]),
    ...add.scenes.map((s) => [s.id, c.scenes]),
    ...add.scenes.flatMap((s) => s.systems.map((x) => [x, c.systems])),
  ];
  if (ids.length < 5) return false;
  const hits = ids.filter(([id, list]) => matchId(id, list).status !== 'unknown').length;
  const title = tdd.identity.title || '';
  const mentions = title && new RegExp(title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i').test(add.text);
  if (hits / ids.length < 0.1 && !mentions) {
    issues.add({ code: 'ADD_TDD_MISMATCH', level: 'conflict', area: 'cross', where: add.file,
      message: `ADD ids overlap TDD in ${hits}/${ids.length} references and never names "${title}" — the ADD appears to describe a different game`,
      fix: 'TDD authority: only ADD style/palette/targets are used; ADD registries (ui_screens, scene_manifest, briefs.serves) are not merged', refs: ['ADD'] });
    return true;
  }
  return false;
}

module.exports = { canon, makeResolver, tddChecks, normalizeAcs, levelNameChecks, proseChecks, addMismatch, ownerIds };
