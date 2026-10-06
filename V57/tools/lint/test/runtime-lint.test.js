'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawnSync } = require('child_process');
const { lint, stripComments, contextOf } = require('../runtime-lint');

const CLI = path.join(__dirname, '..', 'runtime-lint.js');

function tree(files) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'v57-lint-'));
  for (const [rel, src] of Object.entries(files)) {
    const p = path.join(root, rel);
    fs.mkdirSync(path.dirname(p), { recursive: true });
    fs.writeFileSync(p, src);
  }
  return root;
}

const rules = (res, file) => res.findings.filter((f) => f.file.endsWith(file)).map((f) => `${f.rule}:${f.severity}`);

const BAD_RUNTIME = `using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class Boot : MonoBehaviour {
  void Awake() {
    var ball = GameObject.Find("Ball");
    var cam = FindFirstObjectByType<Camera>();
    var cfg = ScriptableObject.CreateInstance<PlungerConfig>();
    typeof(Plunger).GetField("config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
      .SetValue(this, cfg);
#if UNITY_EDITOR
    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/x.wav");
#endif
    // GameObject.Find("commented") must not count
  }
}
`;

const FAKE_ACCEPTANCE = `using NUnit.Framework;
public class AcceptanceBall {
  [Test] public void Drains() {
    rb.detectCollisions = false;
    Physics.simulationMode = SimulationMode.Script;
    for (int i = 0; i < 10; i++) { ctrl.SendMessage("FixedUpdate"); Physics.Simulate(0.02f); }
    rb.position = new Vector3(Mathf.Clamp(rb.position.x, -1f, 1f), 0f, 0f);
  }
}
`;

test('runtime file: every M2 rule fires with the right severity', () => {
  const root = tree({ 'Scripts/Game/Boot.cs': BAD_RUNTIME, 'Scripts/Game/Game.asmdef': '{"name":"Game"}' });
  const r = rules(lint([root]), 'Boot.cs');
  for (const want of ['no-gameobject-find:error', 'no-find-object:error', 'no-config-createinstance-fallback:error',
    'no-reflection-private:error', 'no-reflection-setvalue:error', 'no-editor-api-in-runtime:error', 'no-var:warning']) {
    assert.ok(r.includes(want), `${want} in ${r.join(', ')}`);
  }
  assert.equal(r.filter((x) => x.startsWith('no-gameobject-find')).length, 1, 'comment ignored');
  assert.equal(r.filter((x) => x.startsWith('no-editor-api')).length, 2, 'using + AssetDatabase even under #if UNITY_EDITOR');
});

test('Find* is an error in runtime but only a warning in Editor code and tests', () => {
  const src = 'class F { void M() { GameObject.Find("A"); FindAnyObjectByType<C>(); } }\n';
  const root = tree({ 'Scripts/F.cs': src, 'Tools/Editor/F.cs': src, 'Tests/PlayMode/FTest.cs': src });
  const res = lint([root], { varRule: false });
  assert.deepEqual(res.findings.filter((f) => f.file.endsWith('Scripts/F.cs')).map((f) => f.severity), ['error', 'error']);
  assert.ok(res.findings.filter((f) => !f.file.endsWith('Scripts/F.cs')).every((f) => f.severity === 'warning'));
});

test('Editor folder / Editor asmdef: editor APIs allowed', () => {
  const root = tree({
    'Tools/Editor/Builder.cs': 'using UnityEditor;\nclass B { void M() { AssetDatabase.Refresh(); } }\n',
    'Tools/Asm/X.cs': 'using UnityEditor;\nclass X {}\n',
    'Tools/Asm/X.Editor.asmdef': '{"name":"X.Editor","includePlatforms":["Editor"]}',
  });
  const res = lint([root], { varRule: false });
  assert.equal(res.summary.errors, 0, JSON.stringify(res.findings));
  assert.equal(contextOf(path.join(root, 'Tools/Asm/X.cs'), root), 'editor');
});

test('PlayMode acceptance tests: physics faking is an error; plain PlayMode test only warns', () => {
  const root = tree({
    'Tests/PlayMode/Acceptance/AcceptanceBall.cs': FAKE_ACCEPTANCE,
    'Tests/PlayMode/Other/BallTest.cs': FAKE_ACCEPTANCE.replace('AcceptanceBall', 'BallTest'),
  });
  const res = lint([root], { varRule: false });
  const acc = rules(res, 'AcceptanceBall.cs');
  for (const want of ['no-disable-collisions:error', 'no-manual-simulation-mode:error', 'no-sendmessage-lifecycle:error', 'no-physics-simulate:error', 'no-manual-clamp:error']) {
    assert.ok(acc.includes(want), `${want} in ${acc.join(', ')}`);
  }
  const other = rules(res, 'BallTest.cs');
  assert.ok(other.includes('no-physics-simulate:warning'));
  assert.ok(other.includes('no-sendmessage-lifecycle:error'), 'SendMessage("FixedUpdate") is never ok outside editor');
});

test('max-lines > 200 is an error; CLI exit codes', () => {
  const long = `class L {\n${'  int a;\n'.repeat(205)}}\n`;
  const root = tree({ 'Scripts/L.cs': long, 'Scripts/Ok.cs': 'class Ok { int a; }\n' });
  const res = lint([root]);
  assert.deepEqual(rules(res, 'L.cs'), ['max-lines:error']);
  assert.deepEqual(rules(res, 'Ok.cs'), []);
  const cli = spawnSync(process.execPath, [CLI, '--root', root], { encoding: 'utf8' });
  assert.equal(cli.status, 2);
  assert.equal(JSON.parse(cli.stdout).summary.errors, 1);
  const okRoot = tree({ 'Scripts/Ok.cs': 'class Ok { int a; }\n' });
  assert.equal(spawnSync(process.execPath, [CLI, '--root', okRoot]).status, 0);
  assert.equal(spawnSync(process.execPath, [CLI, '--bogus']).status, 1);
});

test('stripComments keeps strings and line numbers', () => {
  const s = 'a // x\n/* b\n c */ d "// not" @"q""//"\n';
  const out = stripComments(s);
  assert.equal(out.split('\n').length, s.split('\n').length);
  assert.ok(out.includes('"// not"') && !out.includes(' x') && !out.includes(' b'));
});
