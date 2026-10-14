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
 * V57-owned layout aliases (Docs/V57/layout_aliases.json, logged as a D-###):
 * - `aliases`: a layout asset_id served by another delivered model (e.g. a static export superseded by a rigged character);
 *   `yaw_deg` turns each instance about its own up axis (model facing differs); `static: false` keeps the instances
 *   dynamic (not static-batched: vertex-animated plants, moving pieces). `model` is optional when only `static` is set.
 * - `marker_actors`: a gameplay prefab instanced for every layout marker of a type (`tipo`), named after the marker id,
 *   at the marker pose (`anchor: "marker"`) or at the start of the marker's path facing its end (`anchor: "path"`).
 * Shape: { "aliases": { "<asset_id>": { "model": "<stem>", "yaw_deg": 180, "static": false } },
 *          "marker_actors": { "<Tipo>": { "prefab": "Assets/...prefab", "layer": "Enemies", "anchor": "path" } } }.
 */
function readAliasConfig(root, issues) {
  const rel = 'Docs/V57/layout_aliases.json';
  const out = { aliases: new Map(), markerActors: new Map() };
  if (!fs.existsSync(path.join(root, rel))) return out;
  const doc = readJson(root, rel, issues, 'LAYOUT_ALIASES_UNREADABLE');
  for (const [id, a] of Object.entries((doc && doc.aliases) || {})) {
    if (a && (a.model || a.static === false)) {
      out.aliases.set(id.toLowerCase(), { model: a.model ? String(a.model) : null, yaw: Number(a.yaw_deg) || 0, dynamic: a.static === false });
    }
  }
  for (const [type, a] of Object.entries((doc && doc.marker_actors) || {})) {
    if (a && a.prefab) {
      out.markerActors.set(type.toLowerCase(), { prefab: String(a.prefab), layer: a.layer ? englishLayer(a.layer).name : 'Gameplay', anchor: a.anchor === 'path' ? 'path' : 'marker' });
    }
  }
  return out;
}

function readAliases(root, issues) {
  return readAliasConfig(root, issues).aliases;
}

/** DCC custom properties → flat [{ key, value }] (strings; arrays comma-joined, objects as JSON) so Unity's JsonUtility reads them. */
function flattenProps(props) {
  const out = [];
  for (const [key, v] of Object.entries(props || {})) {
    if (v === null || v === undefined) continue;
    const value = Array.isArray(v) ? v.map((x) => (typeof x === 'object' ? JSON.stringify(x) : String(x))).join(',')
      : typeof v === 'object' ? JSON.stringify(v) : String(v);
    out.push({ key: String(key), value });
  }
  return out;
}

/** unity_scene/1.1 `marcadores` (markers without mesh) → `Marker_<Type>_<Id>` names; `MarkerPatrol_X` → `Marker_Patrol_X`. */
function contractMarkerName(m, taken) {
  const raw = String(m.nombre || m.name || '').trim();
  let name;
  if (/^Marker_/.test(raw)) name = raw;
  else if (/^Marker[A-Z][A-Za-z]*_.+$/.test(raw)) name = raw.replace(/^Marker([A-Z][A-Za-z]*)_(.+)$/, 'Marker_$1_$2');
  else name = `Marker_${cleanBase(m.tipo || m.type || 'Other')}_${cleanBase(m.id || raw || 'Marker')}`;
  return markerName({ nombre: name }, taken);
}

/** `Marker_<Type>_<Id>` → the id part (actor names follow it, e.g. a crab named after its patrol marker). */
function markerIdOf(name) {
  const m = String(name).match(/^Marker_[^_]+_(.+)$/);
  return m ? m[1] : String(name).replace(/^Marker_/, '');
}

/** Yaw quaternion (x,y,z,w) turning local +X towards the horizontal direction (dx, dz). */
function yawFacingX(dx, dz) {
  const yaw = Math.atan2(-dz, dx);
  return [0, Math.sin(yaw / 2), 0, Math.cos(yaw / 2)];
}

/** unity_scene/1.1 `trayectorias`: sampled curves in Unity axes. Points are flattened (x,y,z,…) for JsonUtility. */
function readPaths(doc) {
  const known = new Set(['nombre', 'name', 'tipo', 'type', 'actor', 'ciclo', 'loop', 'modo', 'mode', 'velocidad_mps', 'speed_mps',
    'zona', 'zone', 'puntos', 'points', 'puntos_control', 'instancias', 'instances']);
  return (doc.trayectorias || doc.paths || []).filter((t) => t && Array.isArray(t.puntos || t.points)).map((t) => {
    const props = {};
    for (const [k, v] of Object.entries(t)) if (!known.has(k)) props[k] = v;
    return {
      name: cleanBase(t.nombre || t.name || 'Path'), kind: String(t.tipo || t.type || ''), actor: String(t.actor || ''),
      loop: !!(t.ciclo ?? t.loop), mode: String(t.modo || t.mode || ''), speed: Number(t.velocidad_mps ?? t.speed_mps) || 0,
      zone: String(t.zona || t.zone || ''), points: (t.puntos || t.points).flatMap((p) => p.slice(0, 3).map(Number)),
      instances: (t.instancias || t.instances || []).map((i) => String(typeof i === 'object' ? i.nombre || i.name : i)),
      props: flattenProps(props),
    };
  });
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
/**
 * V57-owned additions to a provider map (`Docs/V57/layout_extras/<LevelId>.json`, `{ "objetos": [...] }` in the
 * unity_scene object format): e.g. more ambient creatures placed by V57. Appended to the map's objects, so they survive
 * every rebuild; the provider's map is never edited.
 */
function readExtras(root, levelId, issues) {
  const rel = `Docs/V57/layout_extras/${levelId}.json`;
  if (!fs.existsSync(path.join(root, rel))) return [];
  const doc = readJson(root, rel, issues, 'LAYOUT_EXTRAS_UNREADABLE');
  const objects = (doc && (doc.objetos || doc.objects)) || [];
  if (objects.length) {
    issues.add({ code: 'LAYOUT_EXTRAS', level: 'fixable', area: 'design', where: rel,
      message: `${objects.length} V57 object(s) added to ${levelId} on top of the provider map`, fix: 'appended to layouts.json; the provider map is unchanged', refs: [rel] });
  }
  return objects;
}

function readLayouts(root, inv, issues) {
  const models = modelIndex(inv);
  const aliasConfig = readAliasConfig(root, issues);
  const aliases = aliasConfig.aliases;
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
    const raw = [...(doc.objetos || doc.objects || []), ...readExtras(root, levelId, issues)].map((o) => ({ ...o, _layer: englishLayer(o.capa || o.layer).name }));
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
      const model = marker ? null : models.get((alias && alias.model ? alias.model : String(o.asset_id || '')).toLowerCase()) || null;
      if (alias && alias.model) aliased.add(`${o.asset_id}→${alias.model}`);
      if (!marker && !model) missing.add(o.asset_id);
      return { name: marker ? markerName(o, markerNames) : names[i], kind: marker ? 'marker' : 'model', shape: marker ? markerShape(o) : null,
        source_name: o.nombre || o.name || null, asset_id: marker ? 'Marker' : o.asset_id, model, layer: marker ? 'Markers' : layer.name,
        position: (o.position || [0, 0, 0]).slice(0, 3), rotation: withYaw((o.rotation || [0, 0, 0, 1]).slice(0, 4), alias ? alias.yaw : 0), scale: (o.scale || [1, 1, 1]).slice(0, 3),
        prefab: null, dynamic: !!(alias && alias.dynamic), props: flattenProps(o.props) };
    });
    const paths = readPaths(doc);
    // unity_scene/1.1 markers: a separate `marcadores` array (Spawn/Zone/Exit/Kill/CameraZone/Patrol + typed gameplay markers).
    const actorNames = new Set();
    for (const m of doc.marcadores || doc.markers || []) {
      const name = contractMarkerName(m, markerNames);
      const vol = m.volumen || m.volume || null;
      const box = !!vol && /^(box|cube|caja|cubo)$/i.test(String(vol.forma || vol.shape || ''));
      const size = box && Array.isArray(vol.tamano || vol.size) ? (vol.tamano || vol.size) : [1, 1, 1];
      const type = String(m.tipo || m.type || '');
      const props = { ...(m.props || {}), tipo: type };
      if (vol && !box) Object.assign(props, { volume_shape: vol.forma || vol.shape, volume_radius: vol.radio ?? vol.radius });
      const path = paths.find((p) => p.actor === String(m.nombre || m.name || '') || (m.props && m.props.path && p.name === cleanBase(m.props.path)));
      const pts = path ? path.points : null;
      // A patrol marker is the END of its route (V57 contract); the provider's marker sits mid-route, so it moves there.
      const position = /^patrol$/i.test(type) && pts && pts.length >= 6 ? pts.slice(-3) : (m.position || [0, 0, 0]).slice(0, 3);
      objects.push({ name, kind: 'marker', shape: box ? 'box' : 'point', source_name: m.nombre || m.name || null, asset_id: 'Marker', model: null,
        layer: 'Markers', position, rotation: (m.rotation || [0, 0, 0, 1]).slice(0, 4),
        scale: (m.scale || [1, 1, 1]).slice(0, 3).map((s, i) => s * (Number(size[i]) || 1)), prefab: null, dynamic: false, props: flattenProps(props) });
      const actor = aliasConfig.markerActors.get(type.toLowerCase());
      if (actor) {
        let aPos = (m.position || [0, 0, 0]).slice(0, 3);
        let aRot = (m.rotation || [0, 0, 0, 1]).slice(0, 4);
        if (actor.anchor === 'path' && pts && pts.length >= 6) {
          aPos = pts.slice(0, 3);
          aRot = yawFacingX(pts[pts.length - 3] - pts[0], pts[pts.length - 1] - pts[2]);
        }
        let actorName = markerIdOf(name);
        for (let n = 2; actorNames.has(actorName); n++) actorName = `${markerIdOf(name)}_${String(n).padStart(2, '0')}`;
        actorNames.add(actorName);
        objects.push({ name: actorName, kind: 'model', shape: null, source_name: m.nombre || m.name || null, asset_id: `Marker:${type}`, model: null,
          layer: actor.layer, position: aPos, rotation: aRot, scale: [1, 1, 1], prefab: actor.prefab, dynamic: true, props: flattenProps(props) });
        aliased.add(`marker ${type}→${actor.prefab.split('/').pop()}`);
      }
    }
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
    layouts.push({ level_id: levelId, source: rel, manifest: manifest ? manifestRel : null, objects, paths, materials: normalizeMaterials(manifest, textures) });
  }
  return layouts;
}

module.exports = { readLayouts, englishLayer, splitInstanceName, instanceNames, isMarker, markerName, markerShape, flattenProps,
  contractMarkerName, yawFacingX, readPaths, LAYOUT_RE };
