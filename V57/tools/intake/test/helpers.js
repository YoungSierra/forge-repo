'use strict';
// Builds provider fixture repos programmatically in a temp dir.
const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawnSync } = require('child_process');

const FIX = path.join(__dirname, 'fixtures');
const TEMPLATE = path.join(FIX, 'provider-repo-template');
const TDD = path.join(FIX, 'TDD_HappyHabitat.md');
const ADD = path.join(FIX, 'ADD_Carton.md');
const CLI = path.join(__dirname, '..', 'index.js');

function tmpDir(prefix = 'v57-intake-') {
  return fs.mkdtempSync(path.join(os.tmpdir(), prefix));
}

function touch(root, rel, content = '') {
  const p = path.join(root, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content);
  return p;
}

/**
 * @param {object} o
 * @param {boolean} [o.tdd=true] copy HH TDD to Docs/Design/TDD.md
 * @param {string|false} [o.add] ADD text (default Cartón ADD), false = none
 * @param {string[]} [o.files] extra files (relative paths) to create
 */
function makeRepo(o = {}) {
  const root = path.join(tmpDir(), 'HappyHabitat');
  fs.cpSync(TEMPLATE, root, { recursive: true });
  if (o.tdd !== false) fs.copyFileSync(TDD, path.join(root, 'Docs/Design/TDD.md'));
  if (o.add !== false) touch(root, 'Docs/ArtDirection/ArtDirectionDocument.md', o.add || fs.readFileSync(ADD, 'utf8'));
  (o.files || []).forEach((f) => touch(root, f, 'x'));
  return root;
}

/** Standard fixture: good + bad names, blockout, orphan, forbidden files. */
const STANDARD_FILES = [
  'Assets/_Game/Art/Characters/Carton/Meshes/SK_Carton.fbx',
  'Assets/_Game/Art/Characters/Carton/Animations/ANIM_Carton_Swing.fbx',
  'Assets/_Game/Art/Characters/Carton/Textures/T_Carton_BC.png',
  'Assets/_Game/Art/Characters/Carton/Textures/carton normal.png',
  'Assets/_Game/Art/Characters/Carton/Meshes/SK_Carton.fbx.meta',
  'Assets/_Game/Art/Props/Bumpers/Koala/Meshes/SM_Koala.fbx',
  'Assets/_Game/Art/Props/Bumpers/Koala/Textures/T_Koala_BC.png',
  'Assets/_Game/Art/Props/Bumpers/Koala/Textures/T_Koala_BC temp.png',
  'Assets/_Game/Art/Environment/Blockout/WoodlandPond/BLK_WoodlandPond.fbx',
  'Assets/_Game/Art/Environment/Kits/BiolumeBloom/Meshes/SM_BiolumeBloom_Platform.fbx',
  'Assets/_Game/Art/VFX/PerfectBurst/3f2a9c1e-1111-2222-3333-444455556666.png',
  'Assets/_Game/Audio/SFX/Flipper/SFX_Flipper_Creak.wav',
  'Assets/_Game/Audio/Music/Música Pond.wav',
  'Docs/ArtDirection/Reference/ref1_CitadelRender.png',
];

function runCli(args) {
  const r = spawnSync(process.execPath, [CLI, ...args], { encoding: 'utf8' });
  return { code: r.status, stdout: r.stdout, stderr: r.stderr };
}

function readJson(root, name) {
  return JSON.parse(fs.readFileSync(path.join(root, 'Docs/Generated/json', `${name}.json`), 'utf8'));
}

module.exports = { makeRepo, touch, tmpDir, runCli, readJson, STANDARD_FILES, FIX, TDD, ADD };
