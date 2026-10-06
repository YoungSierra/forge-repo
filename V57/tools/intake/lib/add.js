'use strict';
// Provider Art Direction Document parser: `## key` sections holding loose pseudo-YAML
// bullet lists (trailing double spaces) or fenced ```yaml blocks.
const { splitSections, stripMd } = require('./md');
const { parseItems, parseScalar } = require('./loose');
const { parsePalette } = require('./palette');
const { findMarkers } = require('./tdd');

const KEYS = {
  asset_briefs: ['asset_briefs', 'asset_brief', 'assets'],
  ui_screens: ['ui_screens', 'ui_screen_registry', 'ui_registry'],
  screen_layouts: ['screen_layouts', 'layouts'],
  scene_manifest: ['scene_manifest', 'scenes'],
  color_palette: ['color_palette', 'palette', 'colour_palette'],
  visual_targets: ['visual_targets'],
  style_guide: ['style_guide'],
  reference_images: ['reference_images', 'references'],
  camera_views: ['camera_views', 'cameras', 'camera'],
  audio_events: ['audio_events'],
  gold_path: ['gold_path'],
};

function sectionFor(sections, aliases) {
  for (const a of aliases) {
    const s = sections.find((x) => x.key === a || x.key.endsWith(`_${a}`));
    if (s) return s;
  }
  return null;
}

function asList(v) {
  if (v === null || v === undefined || v === '') return [];
  if (Array.isArray(v)) return v.map((x) => (typeof x === 'string' ? x.trim() : x)).filter((x) => x !== '' && x !== null);
  if (typeof v === 'string') {
    const p = parseScalar(v.startsWith('[') ? v : `[${v}]`);
    return Array.isArray(p) ? p : [v];
  }
  return [v];
}

function str(v) {
  if (v === null || v === undefined) return null;
  return typeof v === 'string' ? stripMd(v) : v;
}

/** Standalone menu screens declared in prose ("... UI_MainMenu, UI_PauseMenu ... consumed_by: [standalone]"). */
function standaloneScreens(body, baseLine, known) {
  const out = [];
  body.split(/\r?\n/).forEach((line, i) => {
    if (!/standalone/i.test(line) || /^\s*[-*]\s+\w+:/.test(line)) return;
    for (const m of line.matchAll(/\bUI_[A-Za-z0-9_]+/g)) {
      if (!known.has(m[0])) {
        known.add(m[0]);
        out.push({ id: m[0], purpose: 'standalone menu screen (declared in prose)', states: [], consumed_by: ['standalone'], presentation: 'screen', sprites: null, asset_id: null, text_keys: [], _line: baseLine + i, _prose: true });
      }
    }
  });
  return out;
}

function parseAdd(text, file, issues) {
  const sections = splitSections(text).filter((s) => s.level <= 2);
  const sec = {};
  const found = {};
  const formats = {};
  for (const [k, aliases] of Object.entries(KEYS)) {
    sec[k] = sectionFor(sections, aliases);
    found[k] = !!sec[k];
  }
  const items = (k) => {
    if (!sec[k]) return [];
    const r = parseItems(sec[k].body, sec[k].bodyLine, k, (msg, line) => issues.add({
      code: 'ADD_YAML_INVALID', level: 'fixable', area: 'add', where: `${file}:${line}`,
      message: `## ${k}: ${msg}`, fix: 'fall back to loose bullet parsing', refs: [k] }));
    formats[k] = r.format;
    return r.items;
  };

  const briefs = items('asset_briefs').filter((b) => b.asset_id || b.id || b.name).map((b) => ({
    ...b,
    asset_id: str(b.asset_id || b.id || null),
    name: str(b.name || null),
    category: str(b.category || null),
    serves: asList(b.serves),
    size_m: Array.isArray(b.size_m) ? b.size_m : (b.size_m ? asList(b.size_m) : null),
  }));

  const uiItems = items('ui_screens').filter((s) => s.id).map((s) => ({
    id: str(s.id), purpose: str(s.purpose), states: asList(s.states).map(String), consumed_by: asList(s.consumed_by).map(String),
    presentation: str(s.presentation || null), sprites: str(s.sprites || null), asset_id: str(s.asset_id || null),
    text_keys: asList(s.text_keys).map(String), _line: s._line,
  }));
  if (sec.ui_screens) uiItems.push(...standaloneScreens(sec.ui_screens.body, sec.ui_screens.bodyLine, new Set(uiItems.map((u) => u.id))));

  const layouts = items('screen_layouts').filter((l) => l.id).map((l) => ({
    id: str(l.id), mockup: str(l.mockup || null), contains: asList(l.contains).map(String), _line: l._line,
  }));

  const scenes = items('scene_manifest').filter((s) => s.id).map((s) => ({
    id: str(s.id), purpose: str(s.purpose), systems: asList(s.systems).map(String),
    slice: s.slice === true || /^true$/i.test(String(s.slice)), acs: asList(s.acs_covered || s.acs).map(String),
    blockout: str(s.blockout || null), kit: str(s.kit || null), markers: asList(s.markers).map(String),
    camera_ref: str(s.camera_ref || null), _line: s._line,
  }));

  const refs = items('reference_images').filter((r) => r.id).map((r) => ({
    id: String(str(r.id)), prompt: str(r.prompt || null), placement: str(r.placement || null), _line: r._line,
  }));

  const cameras = items('camera_views').filter((c) => c.id);
  const audioEvents = items('audio_events').filter((e) => e.event || e.id);
  const goldItems = sec.gold_path ? items('gold_path') : [];

  return {
    file, found, formats,
    briefs,
    ui_screens: uiItems,
    screen_layouts: layouts,
    scenes,
    palette: sec.color_palette ? parsePalette(sec.color_palette.body, 'add') : [],
    visual_targets: sec.visual_targets ? sec.visual_targets.body.trim() : '',
    style_guide: sec.style_guide ? sec.style_guide.body.trim() : '',
    reference_images: refs,
    camera_views: cameras,
    audio_events: audioEvents,
    gold_path: goldItems.length ? { steps: goldItems.map(({ _line, ...s }) => s) } : null,
    markers: findMarkers(text),
    sectionLines: Object.fromEntries(Object.entries(sec).filter(([, s]) => s).map(([k, s]) => [k, s.line])),
    text,
  };
}

module.exports = { parseAdd, ADD_KEYS: KEYS };
