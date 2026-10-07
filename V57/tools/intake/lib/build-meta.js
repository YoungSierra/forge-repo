'use strict';
// Generated: package, camera, rendering, audio, localization.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { parseTables, cell, stripMd } = require('./md');
const { pascal } = require('./naming');

const sha = (s) => crypto.createHash('sha256').update(s).digest('hex');

function readmeValue(root, re) {
  try {
    const rows = parseTables(fs.readFileSync(path.join(root, 'README.md'), 'utf8')).flatMap((t) => t.rows);
    const r = rows.find((x) => re.test(stripMd(x._cells[0] || '')));
    const v = r ? stripMd(r._cells[1]).split(/\s+\(/)[0].trim() : '';
    return v && !/^<.*>$/.test(v) ? v : null;
  } catch (_) { return null; }
}

function resolution(s) {
  const m = String(s || '').match(/(\d{3,4})\s*[x×]\s*(\d{3,4})/);
  return m ? [Number(m[1]), Number(m[2])] : null;
}

function platformId(p) {
  const s = p.toLowerCase();
  if (/ios|iphone|ipad/.test(s)) return 'ios';
  if (/android/.test(s)) return 'android';
  if (/switch/.test(s)) return 'switch';
  if (/\bpc\b|steam|windows|mac|linux|desktop/.test(s)) return 'pc';
  if (/web/.test(s)) return 'webgl';
  if (/playstation|ps5|xbox|console/.test(s)) return 'console';
  return s.replace(/[^a-z0-9]+/g, '-');
}

const GENRE_MODULES = ['pinball', 'platformer', 'racing', 'shooter', 'puzzle', 'rhythm', 'rpg', 'strategy', 'runner', 'arcade', 'kart', 'sports', 'fighting', 'survival', 'roguelike', 'simulation'];

function buildPackage(ctx, sliceScenes, gold) {
  const { tdd, add, inv, opts, tddText, addText } = ctx;
  const id = tdd.identity;
  const title = id.title || 'Untitled';
  const dim = `${id.dimension || ''} ${tdd.engine.dimension || ''}`.toLowerCase();
  const perspective = /2\.5d/.test(dim) ? '2.5d' : /3d/.test(dim) ? '3d' : /2d/.test(dim) ? '2d' : '3d';
  const physText = `${JSON.stringify(tdd.movement)} ${tdd.engine.architecture || ''} ${id.raw && id.raw.pattern ? id.raw.pattern : ''}`;
  const physics = perspective === '2d' || /rigidbody2d|physics ?2d/i.test(physText) ? '2d' : '3d';
  const genreText = `${id.genre || ''} / ${tdd.overview.genre_sub_genre || ''}`;
  const genre = [...new Set(genreText.split(/\s*[/,;]\s*/).map((g) => g.trim()).filter(Boolean))];
  const modules = [];
  GENRE_MODULES.forEach((g) => { if (new RegExp(`\\b${g}`, 'i').test(genreText)) modules.push(g); });
  if (/new/i.test(id.input_system || '') || tdd.inputs.length) modules.push('input-system');
  modules.push(physics === '2d' ? 'physics2d' : 'physics3d');
  if (id.save_model && !/^n\/?a$/i.test(String(id.save_model).trim())) modules.push('save');
  if (id.multiplayer_model && !/^n\/?a$/i.test(String(id.multiplayer_model).trim())) modules.push('multiplayer');
  if (/fmod|wwise/i.test(tdd.audio.text)) modules.push('audio-middleware');
  if (tdd.screens.length) modules.push('ui');
  const targets = [...new Set((id.target_platform.length ? id.target_platform : String(tdd.overview.primary_platform || '').split(/\s*·\s*|,\s*/)).filter(Boolean).map(platformId))];
  const uiRes = resolution(tdd.uiText);
  const firstRes = tdd.perf.length ? resolution(tdd.perf[0].resolution) : null;
  const ref = uiRes || firstRes;
  const orientText = `${tdd.uiText} ${tdd.movement.in_play_camera || ''}`;
  const orientation = /portrait/i.test(orientText) ? 'portrait' : /landscape/i.test(orientText) ? 'landscape' : (ref && ref[1] > ref[0] ? 'portrait' : 'landscape');
  const addPath = ctx.addPath ? path.relative(inv.root, ctx.addPath).replace(/\\/g, '/') : null;
  return {
    slug: readmeValue(inv.root, /slug/i) || pascal(title),
    title,
    version: readmeValue(inv.root, /versi[oó]n|version/i) || id.version || '0.0.0',
    genre,
    modules: [...new Set(modules)],
    perspective,
    physics,
    players: Number(id.max_players) || 1,
    engine: { unity_pinned: opts.unityPin, tdd_declared: id.engine || tdd.engine.engine || null },
    platform: { targets, orientation, reference_resolution: ref || null, target_fps: tdd.perf.length ? Math.max(...tdd.perf.map((p) => p.fps || 0)) || 60 : 60 },
    world: { units: 'meters', up: 'Y', forward: 'Z', play_area_m: null },
    slice: { scenes: sliceScenes, gold_path: gold ? `Docs/Generated/json/acceptance.json#/gold_path (${gold.status})` : null },
    sources: {
      tdd: { path: path.relative(inv.root, ctx.tddPath).replace(/\\/g, '/'), version: id.version || null, standard: id.standard || null, sha256: sha(tddText) },
      add: addPath ? { path: addPath, sha256: sha(addText) } : null,
    },
  };
}

/** follow target id (string) or null for a fixed camera. */
function followTarget(d) {
  if (/does not move|fixed|static/i.test(d)) return null;
  const m = d.match(/follows?\s+(?:the\s+)?([A-Za-z_]\w*)/i);
  return m ? m[1] : (/follow/i.test(d) ? 'player' : null);
}

function buildCamera(ctx, scenes) {
  const { tdd, add, inv, issues } = ctx;
  const views = [];
  const slice = scenes.find((s) => s.slice) || scenes[0];
  const d = tdd.movement.in_play_camera || '';
  if (d) {
    const num = (re) => { const m = d.match(re); return m ? Number(m[1]) : null; };
    views.push({ id: 'CAM_Main', scene: slice ? slice.id : null, type: /ortho/i.test(d) ? 'orthographic' : 'perspective',
      angle_deg: num(/(\d+(?:\.\d+)?)\s*°/), fov_or_size: num(/(?:fov|size)\s*[:=]?\s*(\d+(?:\.\d+)?)/i),
      distance_m: num(/(\d+(?:\.\d+)?)\s*m\b(?!\/)/), follow: followTarget(d), notes: d });
  } else {
    issues.add({ code: 'CAMERA_UNDECLARED', level: 'missing', area: 'tdd', where: `${tdd.file}:§11.5`, message: '§11.5 has no "In-play camera" decision',
      fix: 'default perspective camera (60° FOV) framing the slice blockout bounds', refs: ['camera'] });
  }
  for (const c of add.camera_views) {
    views.push({ id: String(c.id), scene: c.scene || null, type: /ortho/i.test(c.type || '') ? 'orthographic' : 'perspective',
      angle_deg: c.angle_deg ?? null, fov_or_size: c.fov_or_size ?? c.fov ?? null, distance_m: c.distance_m ?? null,
      follow: c.follow === true ? 'player' : (c.follow ? String(c.follow) : null), notes: c.notes || 'ADD camera_views' });
  }
  inv.docs.filter((f) => /^Docs\/ArtDirection\/Camera\//.test(f)).forEach((f) => {
    const vid = path.basename(f).replace(/\.[^.]+$/, '');
    const v = views.find((x) => x.id === vid);
    if (v) v.notes = `${v.notes || ''} (reference: ${f})`.trim();
    else views.push({ id: vid, scene: slice ? slice.id : null, type: views[0] ? views[0].type : 'perspective', angle_deg: null, fov_or_size: null, distance_m: null, follow: null, notes: `reference image ${f}` });
  });
  return { views };
}

function minNumber(texts, re) {
  const nums = [];
  texts.forEach((t) => { for (const m of String(t).matchAll(re)) nums.push(Number((m[1] || m[2]).replace(/,/g, ''))); });
  return nums.length ? Math.min(...nums) : null;
}

function buildRendering(ctx, mismatch) {
  const { tdd, add, issues } = ctx;
  const pipeText = `${tdd.engine.render_pipeline || ''} ${tdd.identity.render_pipeline || ''}`;
  const pipeline = /\burp\b|universal/i.test(pipeText) ? 'URP' : /hdrp|high definition/i.test(pipeText) ? 'HDRP' : /built-?in/i.test(pipeText) ? 'Built-in' : 'URP';
  const palette = [...tdd.art.palette];
  const tddHex = new Set(palette.map((p) => p.hex));
  const addOnly = add.palette.filter((p) => !tddHex.has(p.hex));
  if (tddHex.size && addOnly.length && add.palette.length) {
    issues.add({ code: 'PALETTE_CONFLICT', level: 'conflict', area: 'cross', where: add.file,
      message: `${addOnly.length}/${add.palette.length} ADD palette colors are not in the TDD §8 palette`,
      fix: mismatch ? 'TDD palette only (ADD describes another game)' : 'TDD §8 colors keep primary roles; ADD colors appended with their ADD roles', refs: addOnly.map((p) => p.hex) });
  }
  if (!mismatch) palette.push(...addOnly);
  palette.filter((p) => p.status === 'draft').forEach((p) => issues.add({ code: 'PALETTE_DRAFT', level: 'missing', area: 'add', where: add.file,
    message: `palette color ${p.name} ${p.hex} is DRAFT (artist approval required)`, fix: 'used as provisional; flagged in M3 review', refs: [p.hex] }));
  const texts = [tdd.art.text, tdd.engine.render_pipeline || '', ...tdd.perf.map((p) => p.notes), add.visual_targets];
  const post = [];
  const all = texts.join(' ');
  [['bloom', /bloom pass|post[- ]?stack[^.]*bloom|\bbloom\b(?! sequence| sting| threshold|sequence)/i], ['depth-of-field', /depth[- ]of[- ]field|\bdof\b/i],
    ['vignette', /vignette/i], ['color-grading', /colou?r grading|\blut\b/i], ['tonemapping', /tone ?mapping/i]]
    .forEach(([n, re]) => { if (re.test(all)) post.push(n); });
  return {
    pipeline,
    palette: palette.map(({ name, hex, role, status, source }) => ({ name, hex, role, status, source })),
    budgets: {
      platforms: tdd.perf.map((p) => ({ platform: p.platform, resolution: resolution(p.resolution), fps: p.fps, frame_ms: p.frame_ms, memory: p.memory || null })),
      draw_calls_max: minNumber(texts, /≤\s*([\d,]+)\s*draw calls/gi),
      particles_max: minNumber(texts, /(?:≤\s*([\d,]+)\s*(?:active )?particles)|(?:particles?[^.;\d]{0,30}cap:?\s*([\d,]+))/gi),
    },
    post,
  };
}

function buildAudio(ctx) {
  const { tdd, add, inv, issues } = ctx;
  const events = [];
  const apPath = path.join(inv.root, 'Docs/Audio/AudioParameters.md');
  if (fs.existsSync(apPath)) {
    parseTables(fs.readFileSync(apPath, 'utf8')).flatMap((t) => t.rows).forEach((r) => {
      const ev = cell(r, 'event', 'id', 'name');
      if (ev) events.push({ event: ev, file: cell(r, 'file', 'asset') || null, volume: Number(cell(r, 'volume')) || null, loop: /true|yes|loop/i.test(cell(r, 'loop')), source: 'AudioParameters.md' });
    });
  }
  add.audio_events.forEach((e) => events.push({ event: String(e.event || e.id), file: e.file || null, volume: e.volume ?? null, loop: !!e.loop, source: 'ADD' }));
  const claimed = new Set(events.map((e) => e.file).filter(Boolean).map((f) => path.basename(f)));
  inv.audio.forEach((f) => {
    const base = path.basename(f.rel);
    const ev = events.find((e) => e.file && path.basename(e.file) === base);
    if (ev) { ev.file = f.rel; return; }
    if (!claimed.has(base)) events.push({ event: base.replace(/\.[^.]+$/, ''), file: f.rel, volume: null, loop: f.type === 'music' || f.type === 'ambience', source: 'inventory' });
  });
  events.filter((e) => e.file && !e.file.startsWith('Assets/') && !fs.existsSync(path.join(inv.root, 'Assets/_Game/Audio', e.file))).forEach((e) => issues.add({
    code: 'AUDIO_FILE_MISSING', level: 'missing', area: 'cross', where: e.file, message: `audio event ${e.event} file not on disk`, fix: 'event left unbound; listed in Docs/V57/MISSING_ASSETS.md; slot left empty', refs: [e.event] }));
  if (!events.length) {
    issues.add({ code: 'AUDIO_NONE', level: 'missing', area: 'repo', where: 'Assets/_Game/Audio/', message: 'no audio events and no audio files delivered',
      fix: 'events left unbound; M3 audio review scored as N/A; listed in Docs/V57/MISSING_ASSETS.md; slot left empty', refs: ['audio'] });
  }
  const t = tdd.audio.text;
  const middleware = /fmod/i.test(t) && /wwise/i.test(t) ? 'FMOD or Wwise (TDD undecided; V57 default: Unity Audio Mixer until chosen)' : /fmod/i.test(t) ? 'FMOD' : /wwise/i.test(t) ? 'Wwise' : 'Unity Audio';
  const notes = stripMd((t.split(/\r?\n/).find((l) => l.trim().startsWith('-')) || '').replace(/^\s*-\s*/, '')).slice(0, 400);
  return { events, middleware, notes };
}

function buildLocalization(tdd, ui) {
  const rows = new Map();
  ui.screens.forEach((s) => s.text_keys.forEach((k) => { if (!rows.has(k)) rows.set(k, ''); }));
  tdd.mechanics.forEach((m) => m.levers.filter((l) => l.unit === 'text').forEach((l) => rows.set(`${m.name}.${l.param}`, l.value)));
  return [...rows.entries()].map(([key, en]) => ({ key, en }));
}

module.exports = { buildPackage, buildCamera, buildRendering, buildAudio, buildLocalization, resolution };
