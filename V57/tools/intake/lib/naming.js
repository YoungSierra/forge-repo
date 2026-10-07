'use strict';
// V57 naming contract (brief §1) for files under Assets/_Game/{Art,Audio}.
const path = require('path');

const MESH = /\.(fbx)$/i;
const IMAGE = /\.(png|tga|jpg|jpeg|exr|hdr|tif|tiff)$/i;
const AUDIO = /\.(wav|ogg|mp3|aif|aiff|flac)$/i;
const FONT = /\.(ttf|otf)$/i;
const TEX_SUFFIX = ['BC', 'N', 'ORM', 'E', 'Mask'];
const TEX_GUESS = [
  [/albedo|basecolou?r|diffuse|colou?r|base|_d$|_bc$/i, 'BC'],
  [/normal|nrm|_n$/i, 'N'],
  [/orm|occlusion|rough|metal/i, 'ORM'],
  [/emissi|glow|_e$/i, 'E'],
  [/mask/i, 'Mask'],
];

const FORBIDDEN_NAME = [
  [/(^|[^a-z])(temp|tmp)([^a-z]|$)/i, 'temporary file ("temp")'],
  [/(^|[^a-z])copy([^a-z]|$)|\(\d+\)|\s-\s?copia/i, 'duplicate/copy file'],
  [/new[ _-]?folder|untitled/i, 'default "New Folder"/"Untitled" name'],
  [/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}|(^|[^0-9a-z])[0-9a-f]{16,}([^0-9a-z]|$)/i, 'generator/UUID-looking name'],
  [/^(image|img|dsc|screenshot|comfyui|output|generated|midjourney|dall-?e)[ _-]?\d/i, 'automatic generator name'],
  [/final[ _-]?final|_v\d+_final|backup|\.bak$|~$/i, 'candidate/backup file'],
];

function pascal(s) {
  return String(s)
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^A-Za-z0-9]+/g, ' ')
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .map((w) => w[0].toUpperCase() + w.slice(1))
    .join('');
}

/** Split a relative Art/Audio path into {type, asset, expected} according to the contract tree. */
function classify(rel) {
  const p = rel.replace(/\\/g, '/');
  const seg = p.split('/');
  const file = seg[seg.length - 1];
  const at = (i) => seg[i] || null;
  const r = (type, asset, prefix, folderAsset = true) => ({ type, asset, prefix, folderAsset, file });
  if (!p.startsWith('Assets/_Game/')) return r('unknown', null, null);
  const [, , area, a1, a2, a3] = seg;
  if (area === 'Audio') {
    if (a1 === 'Music') return r('music', null, 'MUS_', false);
    if (a1 === 'Ambience') return r('ambience', null, 'AMB_', false);
    if (a1 === 'SFX') return r('sfx', null, 'SFX_', false);
    if (a1 === 'Voice') return r('voice', null, 'VO_', false);
    return r('unknown', null, null);
  }
  if (area !== 'Art') return r('unknown', null, null);
  if (a1 === 'Environment' && a2 === 'Blockout') return at(6) === 'Textures' ? r('texture', a3, 'T_') : r('blockout', a3, 'BLK_');
  if (a1 === 'Environment' && (a2 === 'Kits' || a2 === 'Decoration')) return byKind(at(6), a3);
  if (a1 === 'Environment' && a2 === 'Sky') return r('texture', 'Sky', null, false);
  if (a1 === 'Props') return byKind(at(6), a3);
  if (a1 === 'Characters' && seg.length === 6 && file === `${a2}_export.json`) return r('sidecar', a2, null);
  if (a1 === 'Characters') return byKind(at(5), a2, true);
  if (a1 === 'Shared') return r('texture', null, 'T_', false);
  if (a1 === 'UI' && a2 === 'Sprites') return r('sprite', a3, 'SPR_');
  if (a1 === 'UI' && a2 === 'Icons') return r('sprite', null, 'ICO_', false);
  if (a1 === 'UI' && a2 === 'Fonts') return r('font', null, 'FNT_', false);
  if (a1 === 'VFX') return r('vfx', a2, 'VFX_');
  return r('unknown', null, null);

  function byKind(kindFolder, asset, character = false) {
    if (kindFolder === 'Meshes') return r(character ? 'character' : 'model', asset, character ? 'SK_' : 'SM_');
    if (kindFolder === 'Animations') return r('animation', asset, 'ANIM_');
    if (kindFolder === 'Textures') return r('texture', asset, 'T_');
    return r('unknown', asset, null);
  }
}

function expectedExt(type) {
  return { blockout: MESH, model: MESH, character: MESH, animation: MESH, texture: IMAGE, sprite: IMAGE, vfx: IMAGE, font: FONT, sidecar: /\.json$/i, music: AUDIO, sfx: AUDIO, ambience: AUDIO, voice: AUDIO }[type] || null;
}

/** Deterministic rename suggestion or null. */
function suggest(c, stem, ext) {
  const clean = pascal(stem.replace(/^(SM|SK|ANIM|BLK|T|SPR|ICO|FNT|VFX|MUS|SFX|AMB|VO)_/i, ''));
  if (!clean) return null;
  const asset = c.asset ? pascal(c.asset) : null;
  switch (c.type) {
    case 'blockout': return asset ? `BLK_${asset}${ext}` : null;
    case 'model': case 'character': return `${c.prefix}${asset || clean}${ext}`;
    case 'animation': {
      const clip = asset && clean.startsWith(asset) ? clean.slice(asset.length) : clean;
      return asset && clip ? `ANIM_${asset}_${clip}${ext}` : null;
    }
    case 'texture': {
      const g = TEX_GUESS.find(([re]) => re.test(stem));
      if (!g) return null;
      const base = pascal(stem.replace(/^T_/i, '').replace(/(albedo|basecolou?r|diffuse|colou?r|normal|nrm|orm|occlusion|roughness|metallic|emissi\w*|glow|mask|_(bc|n|e|d))$/i, ''));
      return `T_${asset || base || clean}_${g[1]}${ext}`;
    }
    case 'sprite': return c.prefix === 'SPR_' ? `SPR_${c.asset}_${clean}${ext}` : `${c.prefix}${clean}${ext}`;
    default: return c.prefix ? `${c.prefix}${clean}${ext}` : null;
  }
}

function patternFor(c) {
  const A = c.asset ? c.asset.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') : '[A-Z][A-Za-z0-9]*';
  switch (c.type) {
    case 'blockout': return new RegExp(`^BLK_${A}\\.fbx$`, 'i');
    case 'model': return /^SM_[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*\.fbx$/;
    case 'character': return /^S[KM]_[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*\.fbx$/;
    case 'animation': return new RegExp(`^ANIM_${A}_[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*\\.fbx$`);
    case 'texture': return c.prefix ? new RegExp(`^T_[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*_(${TEX_SUFFIX.join('|')})\\.[a-z]+$`) : /^[A-Z][A-Za-z0-9_]*\.[a-z]+$/;
    case 'sprite': return c.prefix === 'ICO_' ? /^ICO_[A-Z][A-Za-z0-9_]*\.[a-z]+$/ : /^SPR_[A-Za-z0-9]+(_[A-Za-z0-9]+)+?(_9s-\d+)?\.png$/;
    case 'font': return /^FNT_[A-Z][A-Za-z0-9_]*\.(ttf|otf)$/;
    case 'vfx': return /^VFX_[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*?(_Sheet_\d+x\d+)?\.[a-z0-9]+$/;
    default: return c.prefix ? new RegExp(`^${c.prefix}[A-Z][A-Za-z0-9]*(_[A-Za-z0-9]+)*\\.[a-z0-9]+$`) : null;
  }
}

/**
 * Check one file. Returns {classification, problems:[{kind:'forbidden'|'naming'|'folder', message, suggestion}]}.
 */
function checkFile(rel) {
  const c = classify(rel);
  const base = path.basename(rel);
  const ext = path.extname(base);
  const stem = base.slice(0, base.length - ext.length);
  const problems = [];
  const segs = rel.split('/');
  for (const s of segs) {
    const hit = FORBIDDEN_NAME.find(([re]) => re.test(s));
    if (hit) { problems.push({ kind: 'forbidden', message: `${hit[1]}: "${s}"`, suggestion: null }); break; }
  }
  if (problems.length) return { classification: c, problems };
  if (/\s/.test(rel)) problems.push({ kind: 'naming', message: 'contains spaces', suggestion: `${pascal(stem)}${ext.toLowerCase()}` });
  if (/[^\x00-\x7F]/.test(rel)) problems.push({ kind: 'naming', message: 'contains accents/non-ASCII characters', suggestion: `${pascal(stem)}${ext.toLowerCase()}` });
  if (c.type === 'unknown') {
    problems.push({ kind: 'folder', message: 'path is outside the V57 contract tree (brief §1)', suggestion: null });
    return { classification: c, problems };
  }
  const extRe = expectedExt(c.type);
  if (extRe && !extRe.test(base)) {
    problems.push({ kind: 'naming', message: `extension ${ext || '(none)'} not allowed for ${c.type}`, suggestion: null });
    return { classification: c, problems };
  }
  const pat = patternFor(c);
  if (pat && !pat.test(base)) {
    const s = suggest(c, stem, ext.toLowerCase());
    const exp = c.type === 'texture' && c.prefix ? 'T_<Asset>_<BC|N|ORM|E|Mask>' : c.type === 'sprite' && c.prefix === 'SPR_' ? 'SPR_<Id>_<State>[_9s-<px>]' : `${c.prefix || ''}<Name>`;
    problems.push({ kind: 'naming', message: `name does not follow ${exp}${c.type === 'blockout' ? ` (BLK_${c.asset})` : ''}`, suggestion: s && s !== base ? s : null });
  } else if (c.folderAsset && c.asset && ['model', 'character', 'texture'].includes(c.type) && c.prefix) {
    const second = stem.split('_')[1];
    if (second && second.toLowerCase() !== pascal(c.asset).toLowerCase() && !stem.toLowerCase().includes(pascal(c.asset).toLowerCase())) {
      problems.push({ kind: 'naming', message: `asset segment "${second}" differs from asset folder "${c.asset}"`, suggestion: base.replace(second, pascal(c.asset)) });
    }
  }
  return { classification: c, problems };
}

module.exports = { checkFile, classify, pascal, FORBIDDEN_NAME };
