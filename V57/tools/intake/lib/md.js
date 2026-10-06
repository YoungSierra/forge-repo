'use strict';
// Markdown helpers: heading-keyed sections, pipe tables, fenced blocks.
// Tolerant by design: provider docs are LLM-written and loosely formatted.

const HEADING = /^(#{1,6})\s+(.*?)\s*#*\s*$/;
const FENCE = /^\s*(`{3,}|~{3,})\s*([\w.+-]*)/;

function stripMd(s) {
  if (s === undefined || s === null) return '';
  return String(s)
    .replace(/\*\*/g, '')
    .replace(/__/g, '')
    .replace(/`/g, '')
    .replace(/^\*+|\*+$/g, '')
    .replace(/\s+$/g, '')
    .trim();
}

function slug(s) {
  return stripMd(s)
    .toLowerCase()
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
}

// "9.1 Screen registry" -> "9.1", "§A · Project Identity" -> "A", "§B-S · ..." -> "B-S",
// "Mechanic: Foo" -> "mechanic:Foo", "asset_briefs" -> "asset_briefs".
function sectionKey(title) {
  const t = stripMd(title).replace(/^§\s*/, '');
  let m = t.match(/^(\d+(?:\.\d+)*)(?=[\s·.:]|$)/);
  if (m) return m[1].replace(/\.$/, '');
  m = t.match(/^([A-Z](?:-[A-Z])?)\s*(?:·|$)/);
  if (m) return m[1];
  m = t.match(/^Mechanic:\s*`?([A-Za-z_][\w]*)`?/i);
  if (m) return 'mechanic:' + m[1];
  return slug(t);
}

/** Split markdown into sections. body = lines until next heading of same or higher level. */
function splitSections(text) {
  const lines = String(text || '').split(/\r?\n/);
  const heads = [];
  let fence = null;
  lines.forEach((line, i) => {
    const f = line.match(FENCE);
    if (f) {
      if (!fence) fence = f[1][0];
      else if (f[1][0] === fence && !f[2]) fence = null;
      return;
    }
    if (fence) return;
    const h = line.match(HEADING);
    if (h) heads.push({ level: h[1].length, title: stripMd(h[2]), index: i });
  });
  return heads.map((h, n) => {
    let end = lines.length;
    for (let k = n + 1; k < heads.length; k++) {
      if (heads[k].level <= h.level) { end = heads[k].index; break; }
    }
    return {
      level: h.level,
      title: h.title,
      key: sectionKey(h.title),
      line: h.index + 1,
      bodyLine: h.index + 2,
      body: lines.slice(h.index + 1, end).join('\n'),
    };
  });
}

/** Find the first section matching a key (exact) or a title regex. */
function findSection(sections, keys = [], titleRes = []) {
  for (const k of keys) {
    const s = sections.find((x) => x.key === k);
    if (s) return s;
  }
  for (const re of titleRes) {
    const s = sections.find((x) => re.test(x.title));
    if (s) return s;
  }
  return null;
}

function splitRow(line) {
  let s = line.trim();
  if (s.startsWith('|')) s = s.slice(1);
  if (s.endsWith('|') && !s.endsWith('\\|')) s = s.slice(0, -1);
  return s.split(/(?<!\\)\|/).map((c) => c.replace(/\\\|/g, '|').trim());
}

function normalizeHeader(h) {
  return stripMd(h)
    .toLowerCase()
    .replace(/\(.*?\)/g, '')
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
}

/** Parse every pipe table in a body. Rows are objects keyed by normalized header, plus _line. */
function parseTables(body, baseLine = 1) {
  const lines = String(body || '').split(/\r?\n/);
  const tables = [];
  let i = 0;
  while (i < lines.length) {
    if (!lines[i].trim().startsWith('|')) { i++; continue; }
    const start = i;
    while (i < lines.length && lines[i].trim().startsWith('|')) i++;
    const group = lines.slice(start, i);
    if (group.length < 2 || !/^\|?\s*:?-{2,}/.test(group[1].trim())) continue;
    const headers = splitRow(group[0]);
    const keys = headers.map(normalizeHeader);
    const rows = group.slice(2).map((l, n) => {
      const cells = splitRow(l);
      const row = { _line: baseLine + start + 2 + n, _cells: cells };
      keys.forEach((k, c) => { row[k || `col${c}`] = cells[c] !== undefined ? cells[c] : ''; });
      return row;
    });
    tables.push({ headers, keys, rows, line: baseLine + start });
  }
  return tables;
}

/** Return the value of the first alias present in a table row, cleaned. */
function cell(row, ...aliases) {
  for (const a of aliases) {
    if (row && row[a] !== undefined && row[a] !== '') return stripMd(row[a]);
  }
  for (const a of aliases) {
    const k = Object.keys(row || {}).find((x) => x.startsWith(a));
    if (k && row[k] !== '') return stripMd(row[k]);
  }
  return '';
}

const EMPTY = new Set(['', '-', '—', '–', 'none', 'n/a', 'na', 'null', 'tbd']);

function isEmptyValue(v) {
  return EMPTY.has(stripMd(v).toLowerCase());
}

/** "A, B; C" -> [A,B,C]; strips markdown and empty markers. */
function splitList(s) {
  return stripMd(s)
    .split(/\s*[,;]\s*/)
    .map((x) => x.trim())
    .filter((x) => !isEmptyValue(x));
}

function extractFences(body, baseLine = 1) {
  const lines = String(body || '').split(/\r?\n/);
  const out = [];
  let cur = null;
  lines.forEach((line, i) => {
    const f = line.match(FENCE);
    if (f && !cur) { cur = { lang: (f[2] || '').toLowerCase(), marker: f[1][0], start: i, buf: [] }; return; }
    if (f && cur && f[1][0] === cur.marker && !f[2]) {
      out.push({ lang: cur.lang, content: cur.buf.join('\n'), line: baseLine + cur.start });
      cur = null;
      return;
    }
    if (cur) cur.buf.push(line);
  });
  return out;
}

module.exports = {
  stripMd, slug, sectionKey, splitSections, findSection, parseTables, cell,
  splitList, isEmptyValue, extractFences, normalizeHeader,
};
