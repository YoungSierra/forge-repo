'use strict';
const crypto = require('crypto');

const LEVELS = ['blocking', 'conflict', 'fixable', 'missing'];

const LEVEL_MEANING = {
  blocking: 'stop run (A0 gate fails, exit 2)',
  conflict: 'resolved by authority rule (TDD > ADD > disk), logged to DECISIONS',
  fixable: 'V57 auto-fixes / normalizes, logged to DECISIONS',
  missing: 'listed in MISSING_ASSETS.md, slot left empty, run continues',
};

class Issues {
  constructor() {
    this.list = [];
    this.byKey = new Map();
  }

  /**
   * @param {object} i {code, level, area, where, message, fix, refs[]}
   * Duplicate (same code+where+refs) issues are merged (count++).
   */
  add(i) {
    if (!LEVELS.includes(i.level)) throw new Error(`bad issue level ${i.level}`);
    const refs = (i.refs || []).map(String);
    const key = `${i.code}|${i.where || ''}|${refs.join(',')}`;
    const prev = this.byKey.get(key);
    if (prev) { prev.count += 1; return prev; }
    const issue = {
      id: null,
      code: i.code,
      level: i.level,
      area: i.area || 'repo',
      where: i.where || '',
      message: i.message,
      fix: i.fix || '',
      refs,
      count: 1,
      key: crypto.createHash('sha1').update(key).digest('hex').slice(0, 12),
    };
    this.byKey.set(key, issue);
    this.list.push(issue);
    return issue;
  }

  has(level) { return this.list.some((i) => i.level === level); }

  counts() {
    const c = Object.fromEntries(LEVELS.map((l) => [l, 0]));
    this.list.forEach((i) => { c[i.level] += 1; });
    return c;
  }

  /** Stable ordering + ids I-001.. (deterministic across runs). */
  finalize() {
    const order = (l) => LEVELS.indexOf(l);
    this.list.sort((a, b) => order(a.level) - order(b.level)
      || a.area.localeCompare(b.area) || a.code.localeCompare(b.code)
      || a.where.localeCompare(b.where, 'en', { numeric: true }) || a.message.localeCompare(b.message));
    this.list.forEach((i, n) => { i.id = `I-${String(n + 1).padStart(3, '0')}`; });
    return this.list;
  }

  forRefs(refs) {
    const set = new Set(refs);
    return this.list.filter((i) => i.refs.some((r) => set.has(r)));
  }
}

module.exports = { Issues, LEVELS, LEVEL_MEANING };
