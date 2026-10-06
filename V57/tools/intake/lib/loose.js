'use strict';
// Parser for loose provider "pseudo-YAML" bullet lists, e.g.
//   - asset_id: CHAR-01␠␠
//     category: Character␠␠
// or bold-key bullets ("- **id:** ref1"), or fenced ```yaml blocks.
const YAML = require('yaml');
const { extractFences } = require('./md');

const BULLET = /^(\s*)[-*+]\s+(?:\*\*)?([A-Za-z_][\w-]*)(?::\*\*|\*\*:|:)(?:\s+(.*)|\s*)$/;
const KEYLINE = /^(\s+)(?:\*\*)?([A-Za-z_][\w-]*)(?::\*\*|\*\*:|:)(?:\s+(.*)|\s*)$/;

function snake(k) {
  return String(k).trim().replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '');
}

function unquote(s) {
  const m = s.match(/^(['"])(.*)\1$/);
  return m ? m[2] : s;
}

function parseScalar(raw) {
  if (raw === undefined || raw === null) return null;
  const v = String(raw).replace(/\s+$/g, '').trim();
  if (v === '' || v === '~' || /^null$/i.test(v)) return null;
  if (/^(true|yes)$/i.test(v)) return true;
  if (/^(false|no)$/i.test(v)) return false;
  if (/^-?\d+(\.\d+)?$/.test(v)) return Number(v);
  if (/^\[.*\]$/.test(v) || /^\{.*\}$/.test(v)) {
    try {
      const p = YAML.parse(v);
      if (p !== null && typeof p === 'object') return p;
    } catch (_) { /* fall through */ }
    if (v.startsWith('[')) {
      return v.slice(1, -1).split(',').map((x) => parseScalar(x)).filter((x) => x !== null);
    }
  }
  return unquote(v.replace(/^`|`$/g, ''));
}

function dedent(lines) {
  const ind = Math.min(...lines.filter((l) => l.trim()).map((l) => l.match(/^\s*/)[0].length));
  return lines.map((l) => l.slice(Number.isFinite(ind) ? ind : 0)).join('\n');
}

function normalizeKeys(obj) {
  if (Array.isArray(obj)) return obj.map(normalizeKeys);
  if (obj && typeof obj === 'object') {
    const out = {};
    for (const [k, v] of Object.entries(obj)) out[snake(k)] = normalizeKeys(v);
    return out;
  }
  return obj;
}

/** Items from fenced yaml blocks inside a section. */
function itemsFromFences(body, baseLine, sectionName, onError) {
  const items = [];
  for (const f of extractFences(body, baseLine)) {
    if (f.lang && !/^(ya?ml)$/.test(f.lang)) continue;
    let data;
    try { data = YAML.parse(f.content); } catch (e) {
      if (onError) onError(`yaml parse error at line ${f.line}: ${e.message.split('\n')[0]}`, f.line);
      continue;
    }
    if (data && !Array.isArray(data) && typeof data === 'object') {
      const keys = Object.keys(data);
      const want = keys.find((k) => snake(k) === snake(sectionName || '')) || (keys.length === 1 ? keys[0] : null);
      if (want && Array.isArray(data[want])) data = data[want];
      else if (keys.every((k) => data[k] && typeof data[k] === 'object' && !Array.isArray(data[k]))) {
        data = keys.map((k) => ({ id: k, ...data[k] }));
      } else data = [data];
    }
    if (Array.isArray(data)) {
      data.filter((x) => x && typeof x === 'object').forEach((x) => items.push({ ...normalizeKeys(x), _line: f.line }));
    }
  }
  return items;
}

/** Line-based loose parser. */
function itemsFromBullets(body, baseLine) {
  const lines = String(body || '').split(/\r?\n/);
  const items = [];
  let cur = null;
  let firstKey = null;
  let keyCol = 0;
  let lastKey = null;
  let nested = null; // { key, col, lines }
  let inFence = false;

  const flushNested = () => {
    if (!nested || !cur) { nested = null; return; }
    const text = nested.lines.filter((l) => l.trim());
    if (text.length) {
      try { cur[nested.key] = normalizeKeys(YAML.parse(dedent(text))); } catch (_) { cur[nested.key] = dedent(text).trim(); }
    }
    nested = null;
  };
  const flushItem = () => { flushNested(); if (cur) items.push(cur); cur = null; lastKey = null; };

  lines.forEach((raw, i) => {
    const line = raw.replace(/\s+$/g, '');
    if (/^\s*(`{3,}|~{3,})/.test(line)) { inFence = !inFence; return; }
    if (inFence) return;
    if (!line.trim()) return;
    const indent = line.match(/^\s*/)[0].length;
    if (nested && indent > nested.col) { nested.lines.push(line); return; }
    if (nested) flushNested();
    if (/^#{1,6}\s/.test(line) || /^\s*(-{3,}|\*{3,})\s*$/.test(line)) return;
    const b = line.match(BULLET);
    if (b) {
      const key = snake(b[2]);
      const col = b[1].length + 2;
      if (!cur || key === firstKey || (key in cur && col <= keyCol)) {
        flushItem();
        cur = { _line: baseLine + i };
        if (!firstKey) firstKey = key;
        keyCol = col;
      }
      setKey(key, b[3], col);
      return;
    }
    const k = line.match(KEYLINE);
    if (k && cur && k[1].length >= keyCol - 2) {
      setKey(snake(k[2]), k[3], k[1].length);
      return;
    }
    if (cur && indent > 0 && lastKey && typeof cur[lastKey] === 'string') {
      cur[lastKey] = `${cur[lastKey]} ${line.trim()}`;
      return;
    }
    if (indent === 0) flushItem();
  });
  flushItem();
  return items;

  function setKey(key, rawVal, col) {
    lastKey = key;
    if (rawVal === undefined || rawVal.trim() === '') {
      cur[key] = null;
      nested = { key, col, lines: [] };
      return;
    }
    cur[key] = parseScalar(rawVal);
  }
}

/**
 * Parse a section body into a list of objects. Fenced yaml wins when present.
 * @returns {{items: object[], format: 'yaml'|'loose'|'none'}}
 */
function parseItems(body, baseLine = 1, sectionName = '', onError = null) {
  const fenced = itemsFromFences(body, baseLine, sectionName, onError);
  if (fenced.length) return { items: fenced, format: 'yaml' };
  const loose = itemsFromBullets(body, baseLine);
  return { items: loose, format: loose.length ? 'loose' : 'none' };
}

module.exports = { parseItems, parseScalar, snake, normalizeKeys };
