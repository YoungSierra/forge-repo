'use strict';
// Brief field normalization: enums, numbers, size_m, file paths, rig, animations.
// Unity JsonUtility needs strict types, so values are coerced or nulled (never null inside arrays).
const fs = require('fs');
const path = require('path');
const { pascal } = require('./naming');

const PIVOTS = ['feet', 'base', 'center', 'hinge', 'axle'];
const SIDES = ['left', 'right', 'center'];
const COLLISIONS = ['none', 'simple', 'exact'];
const TYPES = ['model', 'character', 'animation', 'texture', 'sprite', 'atlas', 'font', 'vfx', 'music', 'sfx', 'ambience', 'voice', 'blockout', 'reference'];

/** Keep a provider enum value when valid, else default (logged fixable). */
function enumOr(ctx, b, field, allowed, def) {
  const v = b[field] === undefined || b[field] === null ? null : String(b[field]).toLowerCase().trim();
  if (v === null) return def;
  if (allowed.includes(v)) return v;
  ctx.issues.add({ code: 'BRIEF_BAD_VALUE', level: 'fixable', area: 'add', where: `${ctx.add.file}:${b._line}`,
    message: `brief ${b.asset_id} ${field}="${b[field]}" is not one of ${allowed.join('|')}`, fix: `use "${def}"`, refs: [String(b.asset_id), field] });
  return def;
}

function deriveName(b) {
  const n = String(b.name || b.asset_id || '').replace(/\(.*?\)/g, '').replace(/^the\s+/i, '');
  return pascal(n) || pascal(b.asset_id);
}

/** Best-effort numbers from free-text technical_constraints. */
function fromConstraints(t) {
  const s = String(t || '');
  const tris = s.match(/(\d+(?:\.\d+)?)\s*[–-]\s*(\d+(?:\.\d+)?)k\s*tris\s*LOD0/i) || s.match(/LODs?\s*(\d+(?:\.\d+)?)k/i) || s.match(/(\d+(?:\.\d+)?)k\s*tris/i);
  const tex = s.match(/(\d{3,4})\s*(?:px\s*)?(?:albedo|diffuse|tiled|textures?|emissive)/i) || s.match(/textures?\s*(\d{3,4})/i);
  const bones = s.match(/≤\s*(\d+)\s*bones/i) || s.match(/(\d+)\s*bones/i);
  return {
    tris_lod0: tris ? Math.round(Number(tris[2] || tris[1]) * 1000) : null,
    texture_size: tex ? Number(tex[1]) : null,
    bones: bones ? Number(bones[1]) : null,
  };
}

/** Rig type for skinned assets: explicit `rig` field, else humanoid hints in the brief, else generic. */
function rigOf(b, kind, files) {
  const explicit = String((b.rig && (b.rig.type || b.rig)) || '').toLowerCase();
  if (explicit === 'humanoid' || explicit === 'generic') return explicit;
  const skinned = kind === 'character' || (files && files.mesh && /\/SK_/.test(files.mesh));
  if (!skinned) return null;
  const text = `${b.technical_constraints || ''} ${b.rig || ''}`;
  return /humanoid|mixamo|biped/i.test(text) ? 'humanoid' : 'generic';
}

/** Unity JsonUtility needs strict types: loop → bool, event frame → int. */
function normAnims(list) {
  if (!Array.isArray(list)) return [];
  return list.map((a) => ({ ...a, loop: a.loop === true || /^(true|yes|1)$/i.test(String(a.loop)),
    events: Array.isArray(a.events) ? a.events.map((e) => ({ ...e, frame: Math.round(Number(e.frame) || 0) })) : [] }));
}


/** "~2000" -> 2000, "2k" -> 2000, 1024.0 -> 1024; unparsable -> null + BRIEF_BAD_VALUE (missing). */
function toInt(v) {
  if (typeof v === 'number') return Number.isFinite(v) ? Math.round(v) : null;
  const m = String(v).replace(/,/g, '').match(/(-?\d+(?:\.\d+)?)\s*(k)?/i);
  if (!m) return null;
  const n = Number(m[1]) * (m[2] ? 1000 : 1);
  return Number.isFinite(n) ? Math.round(n) : null;
}

function badValue(ctx, b, field, fix) {
  ctx.issues.add({ code: 'BRIEF_BAD_VALUE', level: 'missing', area: 'add', where: `${ctx.add.file}:${b._line}`,
    message: `brief ${b.asset_id} ${field}=${JSON.stringify(b[field])} is not a valid number`, fix, refs: [String(b.asset_id), field] });
}

function intOf(ctx, b, field, fallback) {
  const v = b[field];
  if (v === undefined || v === null || v === '') return fallback === undefined ? null : fallback;
  const n = toInt(v);
  if (n === null) { badValue(ctx, b, field, `${field}=${fallback ?? 'null'} (from technical_constraints or unset)`); return fallback ?? null; }
  if (typeof v !== 'number' || !Number.isInteger(v)) {
    ctx.issues.add({ code: 'BRIEF_BAD_VALUE', level: 'missing', area: 'add', where: `${ctx.add.file}:${b._line}`,
      message: `brief ${b.asset_id} ${field}=${JSON.stringify(v)} coerced to ${n}`, fix: `use ${n}`, refs: [String(b.asset_id), field] });
  }
  return n;
}

/** size_m must be 3 finite meters, else null (+ issue). */
function sizeOf(ctx, b) {
  const v = b.size_m;
  if (v === undefined || v === null) return null;
  const arr = Array.isArray(v) ? v : String(v).split(/[x×,\s]+/).filter(Boolean);
  const nums = arr.map((x) => (typeof x === 'number' ? x : Number(String(x).replace(/[^0-9.eE+-]/g, ''))));
  if (nums.length === 3 && nums.every((n) => Number.isFinite(n) && n > 0)) return nums;
  badValue(ctx, b, 'size_m', 'size_m=null; placeholder sized from category defaults');
  return null;
}

/** Relative brief paths ("Props/..", "Art/..", "_Game/..") -> repo path when that file exists. */
function resolvePath(root, p) {
  const s = String(p).replace(/\\/g, '/').replace(/^\.?\//, '');
  const cands = [s, `Assets/_Game/Art/${s}`, `Assets/_Game/${s}`, `Assets/${s}`];
  return cands.find((c) => fs.existsSync(path.join(root, c))) || s;
}

function normalizeFilePaths(ctx, b, files) {
  const walk = (v) => {
    if (typeof v === 'string') return v ? resolvePath(ctx.inv.root, v) : v;
    if (Array.isArray(v)) return v.map(walk).filter((x) => x !== null && x !== undefined && x !== '');
    if (v && typeof v === 'object') return Object.fromEntries(Object.entries(v).map(([k, x]) => [k, walk(x)]));
    return v;
  };
  return walk(files);
}

module.exports = { PIVOTS, SIDES, COLLISIONS, TYPES, enumOr, deriveName, fromConstraints, rigOf, normAnims, toInt, intOf, sizeOf, resolvePath, normalizeFilePaths };
