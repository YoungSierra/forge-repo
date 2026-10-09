'use strict';
// Level layouts exported from a DCC tool: Docs/Design/LevelMaps/<LevelId>/unity_scene.json (+ optional manifest.json).
// Normalized here (English layer names, `<Name>_NN` instance names, delivered file paths) so the Unity assembly only builds.
const fs = require('fs');
const path = require('path');

const LAYOUT_RE = /^Docs\/Design\/LevelMaps\/([^/]+)\/unity_scene\.json$/i;

// Common non-English layer names → English scene group names (scene hierarchy is always English).
const LAYER_EN = {
  oceano: 'Ocean', mar: 'Ocean', agua: 'Water', estructura: 'Structure', estructuras: 'Structure', vestido: 'Dressing',
  vestuario: 'Dressing', decoracion: 'Decoration', decorado: 'Decoration', eventos: 'Events', evento: 'Events',
  personajes: 'Characters', jugabilidad: 'Gameplay', iluminacion: 'Lighting', luces: 'Lighting', fondo: 'Background',
  terreno: 'Terrain', suelo: 'Ground', vegetacion: 'Vegetation', props: 'Props', objetos: 'Props', efectos: 'VFX',
  camaras: 'Cameras', interactivos: 'Interactables', enemigos: 'Enemies', coleccionables: 'Collectibles',
};

function ascii(s) {
  return String(s).normalize('NFD').replace(/[̀-ͯ]/g, '');
}

function pascal(s) {
  return ascii(s).replace(/[^A-Za-z0-9]+/g, ' ').trim().split(/\s+/).filter(Boolean)
    .map((w) => w[0].toUpperCase() + w.slice(1)).join('');
}

/** English PascalCase group name for a layer; `renamed` when an alias or transliteration was applied. */
function englishLayer(name) {
  const raw = String(name || '').trim() || 'Ungrouped';
  const alias = LAYER_EN[ascii(raw).toLowerCase()];
  const out = alias || pascal(raw) || 'Ungrouped';
  return { name: out, renamed: out !== raw };
}

/** Blender-style `Name.003` / Unity-style `Name (3)` → `{ base: 'Name', index: 3 }`. */
function splitInstanceName(name) {
  const s = String(name || '').trim();
  let m = s.match(/^(.*)\.(\d{3,})$/) || s.match(/^(.*) \((\d+)\)$/) || s.match(/^(.*)_(\d+)$/);
  return m ? { base: m[1], index: parseInt(m[2], 10) } : { base: s, index: null };
}

function cleanBase(base) {
  return ascii(base).replace(/\s+/g, '_').replace(/[^A-Za-z0-9_-]/g, '');
}

/** Instance names `<Base>_NN` (2 digits, more only when a group passes 99), unique per layer group. */
function instanceNames(objects) {
  const used = new Map();
  const picked = objects.map((o) => {
    const { base, index } = splitInstanceName(o.nombre || o.name || o.asset_id);
    const b = cleanBase(base) || cleanBase(o.asset_id) || 'Object';
    const key = `${o._layer}|${b}`;
    if (!used.has(key)) used.set(key, new Set());
    const taken = used.get(key);
    let n = index && !taken.has(index) ? index : 1;
    while (taken.has(n)) n++;
    taken.add(n);
    return { b, key, n };
  });
  const width = new Map([...used].map(([key, set]) => [key, Math.max(2, String(Math.max(...set)).length)]));
  return picked.map(({ b, key, n }) => `${b}_${String(n).padStart(width.get(key), '0')}`);
}

/** Level markers (`Marker_<Type>_<Id>`, same contract as BLK_ marker nodes): no model; the scene gets a typed V57Marker. */
function isMarker(o) {
  return /^marker$/i.test(String(o.asset_id || '')) || /^Marker_/.test(String(o.nombre || o.name || ''));
}

/** Marker names keep their id verbatim; only a Blender `.NNN` duplicate suffix becomes `_NN`. */
function markerName(o, taken) {
  const raw = String(o.nombre || o.name || 'Marker_Other').trim();
  const m = raw.match(/^(.*)\.(\d{3,})$/);
  let name = cleanBase(m ? `${m[1]}_${String(parseInt(m[2], 10)).padStart(2, '0')}` : raw);
  if (!name.startsWith('Marker_')) name = `Marker_${name}`;
  let out = name;
  for (let n = 2; taken.has(out); n++) out = `${name}_${String(n).padStart(2, '0')}`;
  taken.add(out);
  return out;
}

/** Optional marker shape from the export (`shape`/`forma`): `box` = volume (1 m cube × scale), `point` = position only. */
function markerShape(o) {
  const v = ascii(o.shape || o.forma || '').toLowerCase();
  if (/^(box|cube|caja|cubo|volume|volumen)$/.test(v)) return 'box';
  if (/^(point|punto)$/.test(v)) return 'point';
  return null;
}

function readJson(root, rel, issues, code) {
  try { return JSON.parse(fs.readFileSync(path.join(root, rel), 'utf8')); } catch (e) {
    issues.add({ code, level: 'conflict', area: 'design', where: rel, message: `unreadable JSON: ${e.message}`, fix: 'layout ignored; provider must re-export', refs: [rel] });
    return null;
  }
}

function modelIndex(inv) {
  const byStem = new Map();
  inv.art.filter((f) => /\.fbx$/i.test(f.rel)).forEach((f) => {
    const stem = path.basename(f.rel).replace(/\.[^.]+$/, '');
    [stem, stem.replace(/^(SM|SK|BLK)_/, '')].forEach((k) => { if (!byStem.has(k.toLowerCase())) byStem.set(k.toLowerCase(), f.rel); });
  });
  return byStem;
}

function textureIndex(inv) {
  const byName = new Map();
  inv.art.filter((f) => /\.(png|jpe?g|tga|exr|tif|tiff)$/i.test(f.rel)).forEach((f) => byName.set(path.basename(f.rel).toLowerCase(), f.rel));
  return byName;
}

function normalizeMaterials(manifest, textures) {
  const out = [];
  const tex = (p) => (p ? textures.get(path.basename(String(p)).toLowerCase()) || null : null);
  for (const a of (manifest && Array.isArray(manifest.assets) ? manifest.assets : [])) {
    for (const m of a.materials || []) {
      out.push({
        asset_id: a.asset_id, name: m.name || a.asset_id,
        albedo: tex(m.albedo), normal: tex(m.normal), metallic_smoothness: tex(m.metallic_smoothness),
        base_color: Array.isArray(m.base_color) ? m.base_color.slice(0, 4) : [1, 1, 1, 1],
        metallic: typeof m.metallic === 'number' ? m.metallic : 0, smoothness: typeof m.smoothness === 'number' ? m.smoothness : 0.5,
        alpha_mode: m.alpha_mode || 'OPAQUE', alpha_cutoff: typeof m.alpha_cutoff === 'number' ? m.alpha_cutoff : 0.5,
        double_sided: !!m.double_sided,
      });
    }
  }
  return out;
}

/**
 * V57-owned layout aliases (Docs/V57/layout_aliases.json, logged as a D-###): a layout asset_id served by another delivered
 * model, e.g. a static export superseded by a rigged character. `yaw_deg` turns each instance about its own up axis
 * (model facing differs). Shape: { "aliases": { "<asset_id>": { "model": "<model stem>", "yaw_deg": 180 } } }.
 */
function readAliases(root, issues) {
  const rel = 'Docs/V57/layout_aliases.json';
  if (!fs.existsSync(path.join(root, rel))) return new Map();
  const doc = readJson(root, rel, issues, 'LAYOUT_ALIASES_UNREADABLE');
  const out = new Map();
  for (const [id, a] of Object.entries((doc && doc.aliases) || {})) {
    if (a && a.model) out.set(id.toLowerCase(), { model: String(a.model), yaw: Number(a.yaw_deg) || 0 });
  }
  return out;
}

/** Local yaw about Y applied after the instance rotation (quaternion x,y,z,w). */
function withYaw(q, deg) {
  if (!deg) return q;
  const h = (deg * Math.PI) / 360;
  const s = Math.sin(h);
  const c = Math.cos(h);
  const [x, y, z, w] = q;
  return [x * c - z * s, w * s + y * c, z * c + x * s, w * c - y * s];
}

/** @returns {Array<{level_id, source, manifest, objects, materials}>} */
function readLayouts(root, inv, issues) {
  const models = modelIndex(inv);
  const aliases = readAliases(root, issues);
  const textures = textureIndex(inv);
  const layouts = [];
  for (const rel of inv.docs.filter((d) => LAYOUT_RE.test(d))) {
    const levelId = rel.match(LAYOUT_RE)[1];
    const doc = readJson(root, rel, issues, 'LAYOUT_UNREADABLE');
    if (!doc) continue;
    const contract = String(doc.contrato || doc.contract || '');
    const axes = String(doc.convencion_ejes || doc.axis_convention || '');
    if (!/^unity_scene\/1\./.test(contract) || !/y_?up/i.test(axes)) {
      issues.add({ code: 'LAYOUT_CONTRACT', level: 'conflict', area: 'design', where: rel,
        message: `layout contract "${contract}" / axes "${axes}" not supported (expected unity_scene/1.x, Unity Y-up metres)`, fix: 'layout ignored', refs: [rel] });
      continue;
    }
    const manifestRel = rel.replace(/unity_scene\.json$/i, 'manifest.json');
    const manifest = inv.docs.includes(manifestRel) ? readJson(root, manifestRel, issues, 'LAYOUT_MANIFEST_UNREADABLE') : null;
    const raw = (doc.objetos || doc.objects || []).map((o) => ({ ...o, _layer: englishLayer(o.capa || o.layer).name }));
    const names = instanceNames(raw);
    const markerNames = new Set();
    const renamedLayers = new Map();
    const missing = new Set();
    const aliased = new Set();
    const objects = raw.map((o, i) => {
      const src = o.capa || o.layer || '';
      const layer = englishLayer(src);
      if (layer.renamed) renamedLayers.set(src, layer.name);
      const marker = isMarker(o);
      const alias = marker ? null : aliases.get(String(o.asset_id || '').toLowerCase()) || null;
      const model = marker ? null : models.get((alias ? alias.model : String(o.asset_id || '')).toLowerCase()) || null;
      if (alias) aliased.add(`${o.asset_id}→${alias.model}`);
      if (!marker && !model) missing.add(o.asset_id);
      return { name: marker ? markerName(o, markerNames) : names[i], kind: marker ? 'marker' : 'model', shape: marker ? markerShape(o) : null,
        source_name: o.nombre || o.name || null, asset_id: marker ? 'Marker' : o.asset_id, model, layer: marker ? 'Markers' : layer.name,
        position: (o.position || [0, 0, 0]).slice(0, 3), rotation: withYaw((o.rotation || [0, 0, 0, 1]).slice(0, 4), alias ? alias.yaw : 0), scale: (o.scale || [1, 1, 1]).slice(0, 3) };
    });
    if (renamedLayers.size) {
      issues.add({ code: 'LAYOUT_LAYER_RENAMED', level: 'fixable', area: 'design', where: rel,
        message: `layer names are not English PascalCase: ${[...renamedLayers].map(([a, b]) => `${a}→${b}`).join(', ')}`,
        fix: 'scene groups use the English names; provider should export English layer names', refs: [rel] });
    }
    if (aliased.size) {
      issues.add({ code: 'LAYOUT_ASSET_ALIASED', level: 'fixable', area: 'cross', where: rel,
        message: `layout asset_id(s) placed with another delivered model (Docs/V57/layout_aliases.json): ${[...aliased].join(', ')}`,
        fix: 'aliased instances use the mapped model and yaw; provider should export the final asset ids', refs: [...aliased] });
    }
    if (missing.size) {
      issues.add({ code: 'LAYOUT_ASSET_MISSING', level: 'missing', area: 'cross', where: rel,
        message: `${missing.size} layout asset_id(s) without a model on disk: ${[...missing].join(', ')}`,
        fix: 'instances skipped (slot left empty); listed in MISSING_ASSETS.md', refs: [...missing] });
    }
    layouts.push({ level_id: levelId, source: rel, manifest: manifest ? manifestRel : null, objects, materials: normalizeMaterials(manifest, textures) });
  }
  return layouts;
}

module.exports = { readLayouts, englishLayer, splitInstanceName, instanceNames, isMarker, markerName, markerShape, LAYOUT_RE };
