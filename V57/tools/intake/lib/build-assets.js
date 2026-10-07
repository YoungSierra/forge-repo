'use strict';
// Generated: asset_manifest (ADD asset_briefs x on-disk inventory) + orphans.
const fs = require('fs');
const path = require('path');
const { pascal } = require('./naming');
const { norm, matchId } = require('./fuzzy');
const { kindOf } = require('./build-core');
const { unknownAddRef } = require('./build-world');
const bf = require('./brief-fields');
const { PIVOTS, SIDES, COLLISIONS, TYPES, enumOr, deriveName, fromConstraints, rigOf, normAnims } = bf;

const V57_FIELDS = ['asset_name', 'serves', 'size_m', 'pivot', 'side', 'collision', 'tris_lod0', 'texture_size', 'bones', 'status', 'files'];
const TYPE_BY_KIND = { character: 'character', vfx: 'vfx', ui: 'sprite', environment: 'model', audio: 'sfx', prop: 'model' };

function groupFiles(files) {
  const g = { mesh: null, textures: [], animations: [], sprites: [], vfx: [], other: [] };
  for (const f of files) {
    if (['model', 'character', 'blockout'].includes(f.type) && !g.mesh) g.mesh = f.rel;
    else if (f.type === 'texture') g.textures.push(f.rel);
    else if (f.type === 'animation') g.animations.push(f.rel);
    else if (f.type === 'sprite') g.sprites.push(f.rel);
    else if (f.type === 'vfx') g.vfx.push(f.rel);
    else g.other.push(f.rel);
  }
  return g;
}

function listPaths(files) {
  if (!files || typeof files !== 'object') return [];
  return Object.values(files).flatMap((v) => (Array.isArray(v) ? v : [v])).filter((v) => typeof v === 'string' && v);
}

function buildAssets(ctx, ui, scenes) {
  const { add, inv, issues, c } = ctx;
  const assets = [];
  const claimed = new Set();
  const byAsset = new Map();
  inv.art.forEach((f) => { if (f.asset) { const k = norm(f.asset); if (!byAsset.has(k)) byAsset.set(k, []); byAsset.get(k).push(f); } });
  const noFiles = [];

  for (const b of add.briefs) {
    const own = [];
    // `serves: []` is a valid answer (pure decoration); mesh-only fields are not required for textures, sprites or audio.
    const meshType = !b.type || ['model', 'character', 'blockout'].includes(String(b.type).toLowerCase());
    const required = meshType ? V57_FIELDS : ['asset_name', 'serves', 'status', 'files'];
    const missingFields = required.filter((k) => b[k] === undefined || b[k] === null || (k !== 'serves' && Array.isArray(b[k]) && !b[k].length));
    const asset_name = b.asset_name ? String(b.asset_name) : deriveName(b);
    const kind = kindOf(b.category);
    let files = b.files && typeof b.files === 'object' ? bf.normalizeFilePaths(ctx, b, b.files) : null;
    if (files) {
      listPaths(files).forEach((p) => {
        if (!fs.existsSync(path.join(inv.root, p))) {
          own.push(issues.add({ code: 'BRIEF_FILE_MISSING', level: 'missing', area: 'cross', where: `${add.file}:${b._line}`,
            message: `brief ${b.asset_id} references ${p}, which does not exist`, fix: 'listed in Docs/V57/MISSING_ASSETS.md; slot left empty', refs: [b.asset_id, p] }));
        } else claimed.add(p);
      });
    } else {
      let found = byAsset.get(norm(asset_name));
      if (!found) {
        const r = matchId(asset_name, inv.art.map((f) => f.asset).filter(Boolean));
        if (r.status === 'typo') {
          found = byAsset.get(norm(r.match));
          own.push(issues.add({ code: 'ID_TYPO', level: 'fixable', area: 'cross', where: `${add.file}:${b._line}`,
            message: `brief ${b.asset_id} asset_name "${asset_name}" has no folder — did you mean "${r.match}"?`, fix: `bind brief to folder ${r.match}`, refs: [asset_name, r.match] }));
        }
      }
      if (found) { files = groupFiles(found); found.forEach((f) => claimed.add(f.rel)); }
    }
    const derived = fromConstraints(b.technical_constraints);
    if (missingFields.length) {
      own.push(issues.add({ code: 'BRIEF_V57_FIELDS', level: 'missing', area: 'add', where: `${add.file}:${b._line}`,
        message: `brief ${b.asset_id} (${b.name}) lacks V57 fields: ${missingFields.join(', ')}`,
        fix: `defaults: asset_name=${asset_name}${b.asset_name ? '' : ' (derived)'}, status=provisional, pivot=${kind === 'character' ? 'feet' : 'base'}, collision=simple; numbers parsed from technical_constraints where present`,
        refs: [b.asset_id] }));
    }
    if (!files || !listPaths(files).length) noFiles.push(b.asset_id);
    (b.serves || []).filter((s) => matchId(s, c.systems).status === 'unknown').forEach((s) => unknownAddRef(ctx, s, 'asset_briefs serves'));
    const defType = files && files.mesh && /(^|\/)BLK_[^/]+$/i.test(files.mesh) ? 'blockout'
      : files && files.mesh && /\/Characters\//.test(files.mesh) ? 'character' : TYPE_BY_KIND[kind];
    assets.push({
      asset_id: b.asset_id || `BRIEF-${asset_name}`, asset_name, category: b.category, type: enumOr(ctx, b, 'type', TYPES, defType),
      serves: (b.serves || []).map((s) => matchId(s, c.systems).match).filter(Boolean), files: files || {},
      size_m: bf.sizeOf(ctx, b),
      pivot: enumOr(ctx, b, 'pivot', PIVOTS, kind === 'character' ? 'feet' : 'base'), side: enumOr(ctx, b, 'side', SIDES, 'center'),
      collision: enumOr(ctx, b, 'collision', COLLISIONS, 'simple'),
      tris_lod0: bf.intOf(ctx, b, 'tris_lod0', derived.tris_lod0), texture_size: bf.intOf(ctx, b, 'texture_size', derived.texture_size),
      bones: bf.intOf(ctx, b, 'bones', derived.bones),
      rig: rigOf(b, kind, files),
      status: enumOr(ctx, b, 'status', ['final', 'provisional'], 'provisional'), animations: normAnims(b.animations),
      source: 'brief', issues: own,
    });
  }
  if (noFiles.length) {
    const i = issues.add({ code: 'BRIEF_NO_FILES', level: 'missing', area: 'cross', where: 'Assets/_Game/Art/',
      message: `${noFiles.length} brief(s) have no files on disk (${noFiles.slice(0, 6).join(', ')}${noFiles.length > 6 ? ', …' : ''})`,
      fix: 'no Visual prefab built; listed in Docs/V57/MISSING_ASSETS.md; slot left empty', refs: noFiles });
    assets.filter((a) => noFiles.includes(a.asset_id)).forEach((a) => a.issues.push(i));
  }

  // reference images
  const refFiles = inv.docs.filter((d) => d.startsWith('Docs/ArtDirection/Reference/'));
  const refMissing = [];
  for (const r of add.reference_images) {
    const f = refFiles.find((x) => path.basename(x).toLowerCase().startsWith(`${r.id.toLowerCase()}_`) || path.basename(x).replace(/\.[^.]+$/, '').toLowerCase() === r.id.toLowerCase());
    if (!f) refMissing.push(r.id);
    assets.push({ asset_id: r.id, asset_name: r.id, category: 'Reference', type: 'reference', serves: [], files: f ? { reference: f } : {},
      size_m: null, pivot: null, side: null, collision: 'none', tris_lod0: null, texture_size: null, bones: null, rig: null, status: f ? 'final' : 'provisional', animations: [], source: 'brief', issues: [] });
  }
  if (refMissing.length) {
    const i = issues.add({ code: 'REFERENCE_IMAGE_MISSING', level: 'missing', area: 'cross', where: 'Docs/ArtDirection/Reference/',
      message: `${refMissing.length} reference_images id(s) without file ref<N>_<Slug>.png (${refMissing.join(', ')})`,
      fix: 'M3 reviewer scores against ADD text only for these areas', refs: refMissing });
    assets.filter((a) => a.type === 'reference' && refMissing.includes(a.asset_id)).forEach((a) => a.issues.push(i));
  }

  // claims by screens (sprite folders) and scenes (blockouts)
  const spriteDirs = new Set(ui.screens.map((s) => s.sprites).filter(Boolean));
  const blockouts = new Set(scenes.map((s) => s.blockout).filter(Boolean));
  inv.art.forEach((f) => {
    if (f.type === 'sprite' && [...spriteDirs].some((d) => f.rel.startsWith(`${d}/`))) claimed.add(f.rel);
    if (f.type === 'blockout' && blockouts.has(f.rel)) claimed.add(f.rel);
  });
  const orphanFiles = inv.art.filter((f) => !claimed.has(f.rel));
  const groups = new Map();
  orphanFiles.forEach((f) => { const k = `${f.type === 'blockout' ? 'BLK' : ''}${f.asset || path.basename(f.rel).replace(/\.[^.]+$/, '')}`; if (!groups.has(k)) groups.set(k, []); groups.get(k).push(f); });
  for (const [k, files] of groups) {
    const name = pascal(k);
    const i = issues.add({ code: 'ASSET_ORPHAN', level: 'missing', area: 'repo', where: path.dirname(files[0].rel),
      message: `${files.length} file(s) on disk without asset brief (${name})`, fix: 'inventory-only asset entry created (status provisional); provider should add an asset_brief', refs: files.map((f) => f.rel) });
    const primary = files.find((f) => ["character", "model", "blockout"].includes(f.type)) || files[0];
    assets.push({ asset_id: `INV-${name}`, asset_name: name, category: primary.type, type: primary.type, serves: [], files: groupFiles(files),
      size_m: null, pivot: null, side: null, collision: 'simple', tris_lod0: null, texture_size: null, bones: null, rig: null, status: 'provisional', animations: [], source: 'inventory', issues: [i] });
  }
  return { assets, orphans: orphanFiles.map((f) => f.rel) };
}

module.exports = { buildAssets, deriveName, fromConstraints };
