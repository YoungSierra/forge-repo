'use strict';
// V57 per-asset build rules (Docs/V57/asset_rules.json, game data written by V57, never by the provider).
// They fill what a delivery without ADD briefs cannot say, for any genre: collision, shadow casting, a game shader to
// keep, companion prefabs placed with every layout instance of the asset, and a draw/animate distance (cull_below). Later rules override earlier ones.
//
// { "rules": [ { "match": "AST-PROP-*",
//                "collision": "none|simple|exact|mesh|convex",
//                "shadows": "on|off|two_sided",
//                "shader": "<shader name>",
//                "cull_below": 0.015,   // stop drawing (and animating) below this screen-height fraction
//                "attach": [ { "prefab": "Assets/_Game/Prefabs/...", "offset": [0, 0.05, 0], "align": "world|instance" } ] } ] }
const fs = require('fs');
const path = require('path');
const { norm } = require('./fuzzy');

const REL = 'Docs/V57/asset_rules.json';
const RULE_COLLISIONS = ['none', 'simple', 'exact', 'mesh', 'convex'];
const SHADOWS = ['on', 'off', 'two_sided'];
const ALIGNS = ['world', 'instance'];

/** Glob on normalized ids: `*` any run, everything else literal (case, hyphens and underscores ignored). */
function matcher(pattern) {
  const parts = String(pattern).split('*').map((p) => norm(p));
  const re = new RegExp(`^${parts.map((p) => p.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('.*')}$`);
  return (id) => re.test(norm(id));
}

function readRules(root, issues) {
  const file = path.join(root, REL);
  if (!fs.existsSync(file)) return [];
  let doc;
  try { doc = JSON.parse(fs.readFileSync(file, 'utf8')); } catch (e) {
    issues.add({ code: 'ASSET_RULES_UNREADABLE', level: 'conflict', area: 'repo', where: REL, message: `unreadable JSON: ${e.message}`, fix: 'asset rules ignored until fixed', refs: [REL] });
    return [];
  }

  const rules = [];
  ((doc && doc.rules) || []).forEach((r, index) => {
    const where = `${REL}#rules[${index}]`;
    const bad = (field, value, allowed) => issues.add({ code: 'ASSET_RULE_INVALID', level: 'fixable', area: 'repo', where,
      message: `${field} '${value}' is not one of ${allowed.join('|')}`, fix: 'field ignored', refs: [REL] });
    if (!r || !r.match) {
      issues.add({ code: 'ASSET_RULE_INVALID', level: 'fixable', area: 'repo', where, message: 'rule without "match"', fix: 'rule ignored', refs: [REL] });
      return;
    }

    const rule = { match: String(r.match), test: matcher(r.match), used: false };
    if (r.collision !== undefined) {
      if (RULE_COLLISIONS.includes(r.collision)) rule.collision = r.collision; else bad('collision', r.collision, RULE_COLLISIONS);
    }
    if (r.shadows !== undefined) {
      if (SHADOWS.includes(r.shadows)) rule.shadows = r.shadows; else bad('shadows', r.shadows, SHADOWS);
    }
    if (r.shader !== undefined) rule.shader = String(r.shader);
    if (r.cull_below !== undefined) {
      const v = Number(r.cull_below);
      if (v > 0 && v < 1) rule.cull_below = v; else bad('cull_below', r.cull_below, ['a screen-height fraction between 0 and 1']);
    }
    if (Array.isArray(r.attach)) {
      rule.attach = r.attach.filter((a) => a && a.prefab).map((a) => {
        const align = ALIGNS.includes(a.align) ? a.align : 'world';
        const offset = Array.isArray(a.offset) && a.offset.length === 3 ? a.offset.map(Number) : [0, 0, 0];
        if (!fs.existsSync(path.join(root, a.prefab))) {
          issues.add({ code: 'ASSET_RULE_PREFAB_MISSING', level: 'missing', area: 'repo', where, message: `attach prefab ${a.prefab} does not exist`,
            fix: 'nothing attached until it exists', refs: [a.prefab] });
        }
        return { prefab: String(a.prefab), offset, align };
      });
    }
    rules.push(rule);
  });
  return rules;
}

/** Applies the rules to the manifest assets in order (later wins; `attach` lists replace). Reports rules that match nothing. */
function applyAssetRules(root, assets, issues) {
  const rules = readRules(root, issues);
  for (const asset of assets) {
    const ids = [asset.asset_name, asset.asset_id, ...modelStems(asset)];
    for (const rule of rules) {
      if (!ids.some((id) => rule.test(id))) continue;
      rule.used = true;
      asset.ruled = true;
      if (rule.collision) {
        asset.collision = rule.collision;
        asset.collision_ruled = true;
      }
      if (rule.shadows) asset.shadows = rule.shadows;
      if (rule.shader) asset.shader = rule.shader;
      if (rule.attach) asset.attach = rule.attach;
      if (rule.cull_below) asset.cull_below = rule.cull_below;
    }
  }

  rules.filter((r) => !r.used).forEach((r) => issues.add({ code: 'ASSET_RULE_UNUSED', level: 'fixable', area: 'repo', where: REL,
    message: `rule '${r.match}' matches no asset`, fix: 'rule has no effect; fix the pattern or remove it', refs: [REL] }));
  return rules.length;
}

function modelStems(asset) {
  const mesh = asset.files && asset.files.mesh;
  return mesh ? [path.basename(mesh).replace(/\.[^.]+$/, '')] : [];
}

module.exports = { applyAssetRules, readRules, matcher, RULE_COLLISIONS, SHADOWS };
