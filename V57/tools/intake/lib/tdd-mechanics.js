'use strict';
// §B "## Mechanic: <Name>" blocks and §C companion spec yaml blocks.
const YAML = require('yaml');
const { splitSections, parseTables, cell, stripMd, extractFences } = require('./md');

function subsection(sections, re) {
  return sections.find((s) => re.test(s.title));
}

function metaValue(body, labelRe) {
  for (const line of body.split(/\r?\n/)) {
    const m = line.match(/^\s*[-*]\s*\*\*(.+?):\*\*\s*(.*)$/);
    if (m && labelRe.test(m[1])) return stripMd(m[2]);
  }
  return '';
}

function backticked(s) {
  return [...String(s).matchAll(/`([^`]+)`/g)].map((m) => m[1].trim());
}

/** "chargeTimeMin [0.3 s]" -> {param, raw, value, unit} */
function parseLever(param, raw) {
  const r = raw === undefined ? '' : raw.trim();
  const lever = { param, raw: r, value: null, unit: null };
  if (!r) return lever;
  if (/^".*"$/.test(r)) { lever.value = r.slice(1, -1); lever.unit = 'text'; return lever; }
  if (/^[-\d.]+(\s*,\s*[-\d.]+)+$/.test(r)) { lever.value = r.split(/\s*,\s*/).map(Number); return lever; }
  const m = r.match(/^(-?\d+(?:\.\d+)?)\s*(.*)$/);
  if (m) {
    lever.value = Number(m[1]);
    let u = m[2].trim();
    if (u === '°') u = 'deg';
    if (u === '×' || u === 'x') u = 'x';
    lever.unit = u || null;
  }
  return lever;
}

function parseLevers(body) {
  const line = body.split(/\r?\n/).find((l) => /tuning levers/i.test(l));
  if (!line) return [];
  const text = line.replace(/^.*?tuning levers:\*\*\s*/i, '');
  const out = [];
  const re = /`([A-Za-z_]\w*)(?:\s*\[([^\]]*)\])?`(?:\s*\[([^\]]*)\])?/g;
  let m;
  while ((m = re.exec(text))) out.push(parseLever(m[1], m[2] !== undefined ? m[2] : m[3]));
  return out;
}

function parseAcs(body, baseLine) {
  const acs = [];
  body.split(/\r?\n/).forEach((l, i) => {
    const m = l.match(/^\s*[-*]\s*(?:\[[ xX]\]\s*)?\*\*(AC[-\w]*?\d+)\s*\((EditMode|PlayMode)\):?\*\*:?\s*(.*)$/);
    if (!m) return;
    const scene = (m[3].match(/Scene:\s*`?(SCN_\w+)/) || [])[1] || null;
    acs.push({ local: m[1], mode: m[2], text: stripMd(m[3]), scene, line: baseLine + i });
  });
  return acs;
}

function parseMechanicBlock(section, issues, file) {
  const subs = splitSections(section.body).map((s) => ({ ...s, line: s.line + section.line }));
  const meta = subsection(subs, /spec metadata/i);
  const metaBody = meta ? meta.body : section.body;
  const title = section.key.replace(/^mechanic:/, '');
  const name = backticked(metaValue(metaBody, /^name/i))[0] || stripMd(metaValue(metaBody, /^name/i)) || title;
  const io = subsection(subs, /inputs and outputs/i);
  const ioBody = io ? io.body : '';
  const playerLine = ioBody.split(/\r?\n/).find((l) => /player inputs/i.test(l)) || '';
  const playerInputs = backticked(playerLine.replace(/^.*?player inputs:\*\*/i, ''))
    .filter((x) => /^[A-Z][A-Za-z0-9]+$/.test(x));
  const depSec = subsection(subs, /dependencies/i);
  const deps = depSec ? (parseTables(depSec.body)[0] || { rows: [] }).rows
    .map((r) => ({ kind: cell(r, 'kind'), id: cell(r, 'id') })).filter((d) => d.id) : [];
  const acSec = subsection(subs, /acceptance criteria/i);
  const acs = acSec ? parseAcs(acSec.body, acSec.line + 1) : [];
  const player = subsection(subs, /player-facing/i);
  if (name !== title) {
    issues.add({ code: 'TDD_MECHANIC_NAME_MISMATCH', level: 'fixable', area: 'tdd', where: `${file}:${section.line}`,
      message: `§B heading "${title}" differs from metadata name "${name}"`, fix: `use metadata name "${name}"`, refs: [name] });
  }
  return {
    name,
    type: stripMd(metaValue(metaBody, /^type/i)) || null,
    status: stripMd(metaValue(metaBody, /^status/i)) || null,
    version: stripMd(metaValue(metaBody, /^version/i)) || null,
    description: metaValue(metaBody, /description/i) || null,
    playerInputs,
    uiRefs: [...new Set((section.body.match(/\bUI_[A-Za-z0-9_]+/g) || []))],
    sceneRefs: [...new Set((section.body.match(/\bSCN_[A-Za-z0-9_]+/g) || []))],
    levers: parseLevers(player ? player.body : section.body),
    dependencies: deps,
    acs,
    line: section.line,
  };
}

/** Quote unquoted flow values such as `{ path: Assets/X_[Name].asset }` and retry. */
function repairYaml(text) {
  const fixed = text
    .replace(/(\{\s*[A-Za-z_]\w*:\s*)([^{}"'\n]*?[[\]][^{}"'\n]*?)(\s*\})/g, (a, pre, v, post) => `${pre}"${v.trim()}"${post}`)
    .split('\n').map((line) => {
      const m = line.match(/^(\s*[\w-]+:\s*)\[(.*)\]\s*$/);
      if (!m || m[2].includes('{') || !/[[\]]/.test(m[2])) return line;
      return `${m[1]}[${m[2].split(/\s*,\s*/).map((x) => `"${x.trim().replace(/^"|"$/g, '')}"`).join(', ')}]`;
    }).join('\n');
  if (fixed === text) return null;
  try { return YAML.parse(fixed); } catch (_) { return null; }
}

function parseSpecs(section, issues, file) {
  if (!section) return [];
  const specs = [];
  for (const f of extractFences(section.body, section.bodyLine)) {
    if (f.lang && !/^ya?ml$/.test(f.lang)) continue;
    const head = f.content.match(/#\s*[─━—-]+\s*(C-\d+)\s+([A-Za-z_]\w*)/);
    let data = null;
    try {
      data = YAML.parse(f.content);
    } catch (e) {
      const label = head ? `${head[1]} ${head[2]}` : `at line ${f.line}`;
      data = repairYaml(f.content);
      issues.add({ code: 'TDD_SPEC_YAML_INVALID', level: 'fixable', area: 'tdd', where: `${file}:${f.line}`,
        message: `§C yaml block ${label} does not parse: ${e.message.split('\n')[0]}`,
        fix: data ? 'parsed after auto-quoting flow scalars (generated copy only; TDD untouched)'
          : 'keep raw spec text; acceptance criteria derived from §B block', refs: head ? [head[2]] : [] });
    }
    const name = (data && data.name) || (head && head[2]) || null;
    if (!name) continue;
    const acs = (data && Array.isArray(data.acceptanceCriteria) ? data.acceptanceCriteria : [])
      .filter((a) => a && a.id)
      .map((a) => ({ id: String(a.id), text: String(a.description || a.text || ''), mode: String(a.verification || a.mode || '') }));
    specs.push({ index: head ? head[1] : null, name, type: data ? data.type || null : null, raw: f.content, data, acs, line: f.line });
  }
  return specs;
}

module.exports = { parseMechanicBlock, parseSpecs, parseLever };
