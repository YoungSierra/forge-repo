'use strict';
// Provider delivery that arrives as a loose folder (zip, drive download): plans how its files land in the provider-owned
// areas of the repo, and applies the plan on request. Never deletes and never renames repo files:
//   - same file name already in the repo (case-insensitive) → replaced in place (its .meta and GUID are kept);
//   - same name once hyphens/underscores/case are ignored → replaced in place, reported as a naming mismatch (the
//     repo keeps its path; the provider fix list gets the delivered name);
//   - new file of a family already in the repo (same name up to its last '_', e.g. a new ANIM_Shark_Attack next to
//     ANIM_Shark_PassiveTransit) → that family's folder;
//   - new files named by convention (SK_/SM_/BLK_<Asset>[LOD##], ANIM_<Asset>_<Clip>, <Asset>[LOD##]_export.json) are
//     grouped per asset → <newRoot>/<Asset>/{Meshes,Animations,(root)} (or the asset's folder when the repo has it), and
//     loose textures of a single-asset delivery go to that asset's Textures/;
//   - other new model → <newRoot>/<Asset>/Meshes/, its textures (`<model stem>_*`) → <newRoot>/<Asset>/Textures/;
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
/** 'ANIM_Shark_Attack.fbx' → 'anim_shark_|.fbx'. Needs two name parts before the last '_' (a bare 'SM_' prefix is no family). */
const familyKey = (name) => {
  const stem = stemOf(name);
  const cut = stem.lastIndexOf('_');
  return cut > 0 && stem.slice(0, cut).includes('_') ? `${stem.slice(0, cut + 1).toLowerCase()}|${path.extname(name).toLowerCase()}` : null;
};

function repoIndex(repo) {
  const exact = new Map();
  const loose = new Map();
  const families = new Map(); // "<name up to the last _>|<ext>" (lowercase) → repo folder
  for (const area of PROVIDER_AREAS) {
    for (const abs of walk(path.join(repo, area))) {
      if (abs.endsWith('.meta')) continue;
      const rel = posix(path.relative(repo, abs));
      const base = path.basename(rel).toLowerCase();
      if (!exact.has(base)) exact.set(base, rel);
      const key = norm(path.basename(rel));
      if (!loose.has(key)) loose.set(key, rel);
      const family = familyKey(path.basename(rel));
      if (family && !families.has(family)) families.set(family, path.posix.dirname(rel));
    }
  }
  return { exact, loose, families };
}

/** Asset a convention-named file belongs to (SK_X, SK_XLOD01, SM_X_LOD2, ANIM_X_Clip, X_export.json), or null. */
const assetOf = (name) => {
  const raw = stemOf(name);
  const exported = /_export$/i.test(raw);
  const stem = raw.replace(/_export$/i, '');
  let m = /^ANIM_([^_]+)_.+$/.exec(stem);
  if (m) return m[1];
  m = /^(?:SK|SM|BLK)_(.+?)(?:_?LOD\d+)?$/i.exec(stem);
  if (m) return m[1];
  return exported ? stem.replace(/_?LOD\d+$/i, '') : null;
};
const subfolderOf = (name) => (/^ANIM_/i.test(name) ? 'Animations' : MODEL.test(name) ? 'Meshes' : null);

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
  const groups = new Map(); // asset (lowercase) → repo asset folder, for convention-named new files
  const groupFolder = (asset) => {
    const key = asset.toLowerCase();
    if (!groups.has(key)) {
      const existing = [...index.exact.entries()].find(([base]) => ['sk_', 'sm_', 'blk_'].some((prefix) => base.startsWith(`${prefix}${key}.`)));
      groups.set(key, existing ? assetFolderOf(existing[1]) : `${newRoot}/${asset}`);
    }
    return groups.get(key);
  };
  const placeInGroup = (f) => {
    const asset = assetOf(f.name);
    if (!asset) return null;
    const sub = subfolderOf(f.name);
    return `${groupFolder(asset)}${sub ? `/${sub}` : ''}/${f.name}`;
  };

  // Models first: they decide where their textures go.
  for (const f of files.filter((x) => MODEL.test(x.name))) {
    const hit = target(f);
    const looseHit = hit ? null : looseTarget(f);
    if (hit || looseHit) {
      (hit ? plan.replace : plan.renamedMatch).push({ from: f.rel, to: hit || looseHit });
      assetFolders.set(stemOf(f.name).toLowerCase(), assetFolderOf(hit || looseHit));
    } else if (index.families.has(familyKey(f.name))) {
      const dir = index.families.get(familyKey(f.name));
      plan.add.push({ from: f.rel, to: `${dir}/${f.name}` });
      assetFolders.set(stemOf(f.name).toLowerCase(), assetFolderOf(`${dir}/${f.name}`));
    } else if (placeInGroup(f)) {
      plan.add.push({ from: f.rel, to: placeInGroup(f) });
      assetFolders.set(stemOf(f.name).toLowerCase(), groupFolder(assetOf(f.name)));
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
      if (groups.size === 1) { plan.add.push({ from: f.rel, to: `${[...groups.values()][0]}/Textures/${f.name}` }); continue; }
    }
    if (placeInGroup(f)) { plan.add.push({ from: f.rel, to: placeInGroup(f) }); continue; }
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
