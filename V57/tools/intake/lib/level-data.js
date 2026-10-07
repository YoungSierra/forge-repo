'use strict';
// Non-spatial level content (boards, waves, puzzles, …): Docs/Design/LevelData/<LevelId>.json, contract level_data/1.x.
// The `data` payload is game-specific (its fields and bounds are the TDD §6 level contract); intake only checks the
// envelope and indexes it. The assembly copies each file to Assets/_Game/Data/Levels/<LevelId>.json (TextAsset).
const fs = require('fs');
const path = require('path');

const LEVEL_DATA_RE = /^Docs\/Design\/LevelData\/([^/]+)\.json$/i;
const TARGET_ROOT = 'Assets/_Game/Data/Levels';

/** @returns {Array<{level_id, source, target, contract, keys}>} */
function readLevelData(root, inv, issues) {
  const out = [];
  for (const rel of inv.docs.filter((d) => LEVEL_DATA_RE.test(d))) {
    const stem = rel.match(LEVEL_DATA_RE)[1];
    let doc;
    try { doc = JSON.parse(fs.readFileSync(path.join(root, rel), 'utf8')); } catch (e) {
      issues.add({ code: 'LEVEL_DATA_UNREADABLE', level: 'conflict', area: 'design', where: rel, message: `unreadable JSON: ${e.message}`, fix: 'level ignored; provider must re-export', refs: [rel] });
      continue;
    }
    const contract = String((doc && (doc.contract || doc.contrato)) || '');
    if (!/^level_data\/1\./.test(contract) || !doc.data || typeof doc.data !== 'object') {
      issues.add({ code: 'LEVEL_DATA_CONTRACT', level: 'conflict', area: 'design', where: rel,
        message: `level data envelope "${contract}" not supported (expected { contract: "level_data/1.x", level_id, data: { … } })`, fix: 'level ignored', refs: [rel] });
      continue;
    }
    const declared = String(doc.level_id || '');
    if (declared && declared !== stem) {
      issues.add({ code: 'LEVEL_DATA_ID_MISMATCH', level: 'fixable', area: 'design', where: rel,
        message: `level_id "${declared}" differs from the file name "${stem}"`, fix: `file name "${stem}" used as the level id`, refs: [rel] });
    }
    out.push({ level_id: stem, source: rel, target: `${TARGET_ROOT}/${stem}.json`, contract, keys: Object.keys(doc.data).sort() });
  }
  return out;
}

module.exports = { readLevelData, LEVEL_DATA_RE, TARGET_ROOT };
