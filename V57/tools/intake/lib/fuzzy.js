'use strict';
// Fuzzy id matching: optimal-string-alignment (Damerau) distance on normalized ids,
// plus digit/letter confusables (G0OD -> GOOD).

function osa(a, b) {
  const n = a.length;
  const m = b.length;
  if (!n) return m;
  if (!m) return n;
  const d = Array.from({ length: n + 1 }, (_, i) => [i, ...new Array(m).fill(0)]);
  for (let j = 0; j <= m; j++) d[0][j] = j;
  for (let i = 1; i <= n; i++) {
    for (let j = 1; j <= m; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + cost);
      if (i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) {
        d[i][j] = Math.min(d[i][j], d[i - 2][j - 2] + 1);
      }
    }
  }
  return d[n][m];
}

/** Lowercase, strip accents and every non-alphanumeric char. */
function norm(id) {
  return String(id || '')
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

function deconfuse(s) {
  return norm(s).replace(/0/g, 'o').replace(/1/g, 'l').replace(/5/g, 's');
}

function maxDist(len) {
  if (len >= 10) return 2;
  if (len >= 5) return 1;
  return 0;
}

/**
 * Match an id against canonical candidates.
 * @returns {{status:'exact'|'case'|'typo'|'unknown', match:string|null, dist:number}}
 */
function matchId(id, candidates) {
  const list = [...new Set((candidates || []).filter(Boolean))];
  if (list.includes(id)) return { status: 'exact', match: id, dist: 0 };
  const n = norm(id);
  const same = list.find((c) => norm(c) === n);
  if (same) return { status: 'case', match: same, dist: 0 };
  const dc = deconfuse(id);
  const conf = list.find((c) => deconfuse(c) === dc);
  if (conf) return { status: 'typo', match: conf, dist: 1 };
  let best = null;
  for (const c of list) {
    const d = osa(n, norm(c));
    if (d <= maxDist(Math.min(n.length, norm(c).length)) && (!best || d < best.dist)) best = { match: c, dist: d };
  }
  if (best) return { status: 'typo', match: best.match, dist: best.dist };
  return { status: 'unknown', match: null, dist: Infinity };
}

/** Tokens of ALL-CAPS words (>= 4 chars, digits allowed, split on non-alphanumerics). */
function capsTokens(text) {
  const out = [];
  const re = /[A-Za-z0-9]+/g;
  let m;
  while ((m = re.exec(text))) {
    const t = m[0];
    if (t.length >= 4 && /^[A-Z][A-Z0-9]+$/.test(t) && /[A-Z].*[A-Z]/.test(t)) out.push({ token: t, index: m.index });
  }
  return out;
}

/**
 * Detect rare ALL-CAPS tokens that look like typos of frequent ones (PREFECT vs PERFECT, G0OD vs GOOD).
 * @param docs [{name, text}]
 * @returns [{token, suggestion, doc, line, count}]
 */
function proseTypos(docs, { rareMax = 2, frequentMin = 3 } = {}) {
  const freq = new Map();
  const occ = [];
  for (const d of docs) {
    const lines = String(d.text || '').split(/\r?\n/);
    lines.forEach((l, i) => {
      for (const t of capsTokens(l)) {
        freq.set(t.token, (freq.get(t.token) || 0) + 1);
        occ.push({ token: t.token, doc: d.name, line: i + 1 });
      }
    });
  }
  const frequent = [...freq.entries()].filter(([, c]) => c >= frequentMin).map(([t]) => t);
  const found = new Map();
  for (const o of occ) {
    if (freq.get(o.token) > rareMax || found.has(o.token)) continue;
    let sug = null;
    if (/\d/.test(o.token)) sug = frequent.find((f) => f !== o.token && deconfuse(f) === deconfuse(o.token)) || null;
    if (!sug && o.token.length >= 5) {
      sug = frequent.find((f) => f.length >= 5 && f !== o.token && osa(o.token, f) <= (o.token.length >= 8 ? 2 : 1)
        && osa(o.token, f) < Math.min(o.token.length, f.length) / 3) || null;
    }
    if (sug) found.set(o.token, { token: o.token, suggestion: sug, doc: o.doc, line: o.line, count: freq.get(o.token) });
  }
  return [...found.values()];
}

module.exports = { osa, norm, deconfuse, matchId, proseTypos, capsTokens };
