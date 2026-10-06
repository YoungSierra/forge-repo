'use strict';
// Palette extraction from ADD `## color_palette` bullets or TDD §8 prose.
const { stripMd } = require('./md');

const HEX = /#([0-9A-Fa-f]{6})\b/;

function cleanName(s) {
  return stripMd(s).replace(/[`*_]/g, '').replace(/^[\s:,;(—–-]+|[\s:,;(—–-]+$/g, '').trim();
}

/**
 * @param {string} body
 * @param {'add'|'tdd'} source
 * @returns [{name, hex, role, status, source}]
 */
function parsePalette(body, source) {
  const out = [];
  const seen = new Set();
  const push = (e) => {
    if (!e.hex || seen.has(e.hex)) return;
    seen.add(e.hex);
    out.push(e);
  };
  for (const raw of String(body || '').split(/\r?\n/)) {
    const line = raw.replace(/`/g, '');
    if (!HEX.test(line)) continue;
    const draft = /DRAFT/i.test(line);
    const bullet = line.match(/^\s*[-*]\s*(.+?):\s*#([0-9A-Fa-f]{6})\b/);
    if (bullet && source === 'add') {
      const label = stripMd(bullet[1]);
      const parts = label.split(/\s+[—–]\s+|\s+-\s+/);
      const name = cleanName(parts.length > 1 ? parts.slice(1).join(' ') : parts[0]);
      const role = parts.length > 1 ? cleanName(parts[0]) : null;
      const use = (line.match(/Use:\s*([^.]+)/i) || [])[1];
      push({ name, hex: `#${bullet[2].toUpperCase()}`, role: role || (use ? use.trim() : null), status: draft ? 'draft' : 'locked', source });
      continue;
    }
    // prose: "... amber #F5A623, teal #1A7A6E ..." -> name = words since last delimiter
    const re = /([A-Za-z][A-Za-z \-]{0,40}?)\s*\(?#([0-9A-Fa-f]{6})\b/g;
    let m;
    while ((m = re.exec(line))) {
      const before = line.slice(0, m.index + m[1].length);
      const seg = before.split(/[:,;(.]/).pop();
      const words = cleanName(seg).split(/\s+/).filter(Boolean).slice(-3);
      push({ name: words.join(' ') || `color_${m[2]}`, hex: `#${m[2].toUpperCase()}`, role: null, status: source === 'tdd' ? 'tdd' : (draft ? 'draft' : 'locked'), source });
    }
  }
  return out;
}

module.exports = { parsePalette };
