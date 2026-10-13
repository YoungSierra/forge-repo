'use strict';
// Provider delivery that arrives as a loose folder (zip, drive download): plans how its files land in the provider-owned
// areas of the repo, and applies the plan on request. Never deletes and never renames repo files:
//   - same file name already in the repo (case-insensitive) → replaced in place (its .meta and GUID are kept);
//   - same name once hyphens/underscores/case are ignored → replaced in place, reported as a naming mismatch (the
//     repo keeps its path; the provider fix list gets the delivered name);
//   - new model → <newRoot>/<Asset>/Meshes/, its textures (`<model stem>_*`) → <newRoot>/<Asset>/Textures/;
//   - anything else new is listed as unplaced for the agent to place (level maps, audio, docs);
//   - repo files of a delivered asset that the delivery does not contain are reported as kept (earlier maps stay).
const fs = require('fs');
const path = require('path');
const { norm } = require('./fuzzy');

const PROVIDER_AREAS = ['Assets/_Game/Art', 'Assets/_Game/Audio', 'Docs/Design', 'Docs/ArtDirection', 'Docs/Audio'];
const MODEL = /\.(fbx|obj|gltf|glb|blend|dae)$/i;
const TEXTURE = /\.(png|jpe?g|tga|tif|tiff|psd|exr|hdr|bmp)$/i;

function walk(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((e) => {
    const p = path.join(dir, e.name);
    return e.isDirectory() ? walk(p) : [p];
  });
}

const posix = (p) => p.split(path.sep).join('/');
const stemOf = (f) => path.basename(f).replace(/\.[^.]+$/, '');

function repoIndex(repo) {
  const exact = new Map();
  const loose = new Map();
  for (const area of PROVIDER_AREAS) {
    for (const abs of walk(path.join(repo, area))) {
      if (abs.endsWith('.meta')) continue;
      const rel = posix(path.relative(repo, abs));
      const base = path.basename(rel).toLowerCase();
      if (!exact.has(base)) exact.set(base, rel);
      const key = norm(path.basename(rel));
      if (!loose.has(key)) loose.set(key, rel);
    }
  }
  return { exact, loose };
}

/** Plan only; nothing is written. `newRoot` = repo folder for assets that are not in the repo yet. */
function planDelivery({ from, repo, newRoot = 'Assets/_Game/Art/Delivered' }) {
  const files = walk(from).filter((f) => !f.endsWith('.meta')).map((abs) => ({ abs, rel: posix(path.relative(from, abs)), name: path.basename(abs) }));
  const index = repoIndex(repo);
  const plan = { replace: [], renamedMatch: [], add: [], unplaced: [], keptNotDelivered: [] };
  const assetFolders = new Map(); // model stem (lowercase) → repo asset folder
  const placed = new Set();

  const target = (f) => index.exact.get(f.name.toLowerCase()) || null;
  const looseTarget = (f) => index.loose.get(norm(f.name)) || null;
  const assetFolderOf = (repoRel) => {
    const dir = path.posix.dirname(repoRel);
    return /\/(Meshes|Textures|Models|Animations)$/.test(dir) ? path.posix.dirname(dir) : dir;
  };

  // Models first: they decide where their textures go.
  for (const f of files.filter((x) => MODEL.test(x.name))) {
    const hit = target(f);
    const looseHit = hit ? null : looseTarget(f);
    if (hit || looseHit) {
      (hit ? plan.replace : plan.renamedMatch).push({ from: f.rel, to: hit || looseHit });
      assetFolders.set(stemOf(f.name).toLowerCase(), assetFolderOf(hit || looseHit));
    } else {
      const folder = `${newRoot}/${stemOf(f.name)}`;
      plan.add.push({ from: f.rel, to: `${folder}/Meshes/${f.name}` });
      assetFolders.set(stemOf(f.name).toLowerCase(), folder);
    }
    placed.add(f.rel);
  }

  for (const f of files.filter((x) => !placed.has(x.rel))) {
    const hit = target(f);
    const looseHit = hit ? null : looseTarget(f);
    if (hit) { plan.replace.push({ from: f.rel, to: hit }); continue; }
    if (looseHit) { plan.renamedMatch.push({ from: f.rel, to: looseHit }); continue; }
    if (TEXTURE.test(f.name)) {
      const owner = [...assetFolders.keys()].filter((s) => f.name.toLowerCase().startsWith(`${s}_`)).sort((a, b) => b.length - a.length)[0];
      if (owner) { plan.add.push({ from: f.rel, to: `${assetFolders.get(owner)}/Textures/${f.name}` }); continue; }
    }
    plan.unplaced.push(f.rel);
  }

  // Earlier files of the delivered assets that this delivery does not bring (e.g. maps sent only once): kept.
  const delivered = new Set([...plan.replace, ...plan.renamedMatch, ...plan.add].map((p) => p.to));
  for (const folder of new Set(assetFolders.values())) {
    for (const abs of walk(path.join(repo, folder))) {
      const rel = posix(path.relative(repo, abs));
      if (!rel.endsWith('.meta') && !delivered.has(rel) && !/\/Materials\//.test(rel)) plan.keptNotDelivered.push(rel);
    }
  }
  return plan;
}

function applyDelivery(plan, { from, repo }) {
  let copied = 0;
  for (const p of [...plan.replace, ...plan.renamedMatch, ...plan.add]) {
    const dest = path.join(repo, p.to);
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    fs.copyFileSync(path.join(from, p.from), dest);
    copied++;
  }
  return copied;
}

function renderPlan(plan, applied) {
  const lines = [];
  const section = (title, rows) => { if (rows.length) lines.push(`${title} (${rows.length})`, ...rows.map((r) => `  ${r}`)); };
  section('replace in place', plan.replace.map((p) => `${p.from} → ${p.to}`));
  section('replace in place, delivered under another name (provider fix list)', plan.renamedMatch.map((p) => `${p.from} → ${p.to}`));
  section('new', plan.add.map((p) => `${p.from} → ${p.to}`));
  section('unplaced (place by hand)', plan.unplaced);
  section('kept, not in this delivery', plan.keptNotDelivered);
  lines.push(applied ? 'APPLIED (nothing deleted)' : 'DRY RUN — add --apply to copy (nothing is ever deleted)');
  return lines.join('\n');
}

module.exports = { planDelivery, applyDelivery, renderPlan, PROVIDER_AREAS };
