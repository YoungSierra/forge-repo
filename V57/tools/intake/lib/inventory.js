'use strict';
// On-disk inventory + structure validation (brief §1 allowed roots, forbidden files, naming).
const fs = require('fs');
const path = require('path');
const { checkFile } = require('./naming');

const TOP_FILES = new Set(['README.md', '.gitignore', '.gitattributes', 'ignore.conf', 'LICENSE', 'LICENSE.md', 'AGENTS.md', 'gameforge.env.example']);
const TOP_DIRS = new Set(['Docs', 'Source', 'Assets', 'V57', '.v57', '.git', '.plastic', '.github', 'Tools', '.cursor', '.claude']);
const UNITY_DIRS = new Set(['Library', 'ProjectSettings', 'Packages', 'Logs', 'Temp', 'UserSettings', 'obj', 'Build', 'Builds', 'MemoryCaptures']);
const V57_GAME_DIRS = new Set(['Prefabs', 'Scenes', 'Scripts', 'Data', 'Settings', 'Tests']);
const SKIP_NAMES = new Set(['.gitkeep', '.DS_Store', 'Thumbs.db', 'desktop.ini', 'node_modules']);

const DOC_RULES = [
  /^Docs\/Design\/TDD\.md$/,
  /^Docs\/Design\/LevelMaps\/[^/]+\.(png|jpe?g)$/i,
  /^Docs\/Design\/LevelMaps\/[^/]+\/(unity_scene|manifest)\.json$/i,
  /^Docs\/Design\/Encounters\/[^/]+\.csv$/i,
  /^Docs\/ArtDirection\/(ArtDirectionDocument|VisualProductionBlueprint)\.md$/,
  /^Docs\/ArtDirection\/ADI\/ADI_11\.\d+_[^/]+\.md$/,
  /^Docs\/ArtDirection\/Reference\/ref\d+_[^/]+\.(png|jpe?g|webp)$/i,
  /^Docs\/ArtDirection\/(Concept|AnimationPreviews)\/.+/,
  /^Docs\/ArtDirection\/UIMockups\/[^/]+\.(png|jpe?g)$/i,
  /^Docs\/ArtDirection\/Camera\/[^/]+\.(png|jpe?g)$/i,
  /^Docs\/ArtDirection\/VFXReference\/[^/]+\.(mp4|webm|mov|gif)$/i,
  /^Docs\/Audio\/AudioParameters\.md$/,
  /^Docs\/Marketing\/.+/,
  /^Docs\/LICENSES\.md$/,
];

function walk(root, rel = '', out = []) {
  let entries;
  try { entries = fs.readdirSync(path.join(root, rel), { withFileTypes: true }); } catch (_) { return out; }
  for (const e of entries.sort((a, b) => a.name.localeCompare(b.name))) {
    if (SKIP_NAMES.has(e.name)) continue;
    const r = rel ? `${rel}/${e.name}` : e.name;
    if (e.isDirectory()) {
      out.push({ rel: r, dir: true });
      walk(root, r, out);
    } else out.push({ rel: r, dir: false });
  }
  return out;
}

function isV57Managed(root) {
  if (fs.existsSync(path.join(root, 'Docs/V57/STATUS.json'))) return true;
  try { return /com\.v57\.assembly/.test(fs.readFileSync(path.join(root, 'Packages/manifest.json'), 'utf8')); } catch (_) { return false; }
}

function inventory(root, issues) {
  const v57Managed = isV57Managed(root);
  const top = fs.existsSync(root) ? fs.readdirSync(root, { withFileTypes: true }) : [];
  const inv = { root, v57Managed, art: [], audio: [], docs: [], files: [], hasDocs: false, hasGame: false, forbidden: [] };
  const forbid = (rel, why, fix) => {
    inv.forbidden.push(rel);
    issues.add({ code: 'REPO_FORBIDDEN', level: 'fixable', area: 'repo', where: rel, message: why, fix, refs: [rel] });
  };
  for (const e of top) {
    if (SKIP_NAMES.has(e.name)) continue;
    if (e.isDirectory() && UNITY_DIRS.has(e.name)) {
      if (!v57Managed) forbid(`${e.name}/`, `Unity-generated folder "${e.name}/" delivered by provider (contract forbids Unity files)`, 'discard on v57/setup; V57 regenerates it from its own template');
      continue;
    }
    if (e.isDirectory() ? !TOP_DIRS.has(e.name) : !TOP_FILES.has(e.name)) {
      issues.add({ code: 'REPO_UNEXPECTED_ROOT', level: 'fixable', area: 'repo', where: e.name,
        message: `"${e.name}" is not an allowed repo root entry (brief §1)`, fix: 'ignored by V57 (provider may move it to Source/)', refs: [e.name] });
    }
  }
  inv.hasDocs = fs.existsSync(path.join(root, 'Docs'));
  inv.hasGame = fs.existsSync(path.join(root, 'Assets/_Game'));

  const assetsEntries = walk(root, 'Assets');
  for (const f of assetsEntries) {
    if (f.dir) {
      if (/(^|\/)New Folder( \(\d+\))?$/i.test(f.rel)) forbid(f.rel + '/', 'default "New Folder" directory', 'discard on v57/setup');
      continue;
    }
    if (/\.meta$/i.test(f.rel)) {
      if (!v57Managed) forbid(f.rel, '.meta file delivered by provider', 'discard; Unity regenerates .meta on import');
      continue;
    }
    const seg = f.rel.split('/');
    if (seg[1] !== '_Game') {
      if (!v57Managed || seg[1] !== 'Plugins') {
        issues.add({ code: 'REPO_OUTSIDE_GAME', level: 'conflict', area: 'repo', where: f.rel,
          message: 'file under Assets/ outside Assets/_Game/', fix: 'not imported by V57 assembly; listed as orphan', refs: [f.rel] });
      }
      continue;
    }
    if (V57_GAME_DIRS.has(seg[2]) || seg.includes('Materials')) {
      if (!v57Managed) forbid(f.rel, `Unity content (${seg[2]}) delivered by provider; V57 owns ${seg[2]}/`, 'discard on v57/setup; V57 generates it');
      continue;
    }
    if (/\.(mat|prefab|unity|asset|cs|asmdef|controller|anim|ya?ml|shadergraph)$/i.test(f.rel)) {
      forbid(f.rel, 'Unity-authored file (material/prefab/scene/script/yaml) delivered by provider', 'discard on v57/setup; V57 generates Unity assets');
      continue;
    }
    const chk = checkFile(f.rel);
    const entry = { rel: f.rel, ...chk.classification, problems: chk.problems };
    const bad = chk.problems.find((p) => p.kind === 'forbidden');
    if (bad) { forbid(f.rel, bad.message, 'excluded from import (candidate/temporary); provider should delete'); continue; }
    const naming = chk.problems.filter((p) => p.kind === 'naming');
    if (naming.length) {
      const sug = [...naming].reverse().find((p) => p.suggestion);
      issues.add({ code: 'NAME_CONVENTION', level: sug ? 'fixable' : 'conflict', area: 'repo', where: f.rel,
        message: naming.map((p) => p.message).join('; '),
        fix: sug ? `file kept as-is; V57 maps it via asset_manifest (canonical name ${sug.suggestion.replace(/\.[^.]+$/, '')}); provider may rename in a later delivery`
          : 'file kept as-is; imported by folder rule; provider should rename in a later delivery', refs: [f.rel] });
      entry.suggested = sug ? sug.suggestion : null;
    }
    if (chk.problems.some((p) => p.kind === 'folder')) {
      issues.add({ code: 'REPO_FOLDER', level: 'conflict', area: 'repo', where: f.rel,
        message: 'path is outside the V57 contract tree (brief §1)', fix: 'not imported by folder rules; listed as orphan', refs: [f.rel] });
    }
    (seg[2] === 'Audio' ? inv.audio : inv.art).push(entry);
    inv.files.push(entry);
  }

  for (const f of walk(root, 'Docs')) {
    if (f.dir || /^Docs\/(Generated|V57)\//.test(f.rel)) continue;
    inv.docs.push(f.rel);
    if (/\.meta$/i.test(f.rel)) { if (!v57Managed) forbid(f.rel, '.meta file delivered by provider', 'discard'); continue; }
    if (!DOC_RULES.some((re) => re.test(f.rel))) {
      issues.add({ code: 'DOCS_UNEXPECTED', level: 'fixable', area: 'repo', where: f.rel,
        message: 'file not part of the Docs/ contract tree (brief §1)', fix: 'ignored by intake', refs: [f.rel] });
    }
  }
  return inv;
}

module.exports = { inventory, walk, DOC_RULES };
