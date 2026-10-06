'use strict';
// V57 runtime-lint rules (brief §3 M2 + §5). Severity depends on the file context:
//   runtime       gameplay code (default)
//   editor        Editor folder / Editor-only asmdef
//   test-edit     EditMode tests (editor platform)
//   test          PlayMode tests
//   acceptance    PlayMode acceptance tests (Tests/PlayMode/Acceptance*)
//   verify        non-test "verification" scripts (*Smoke*/*Verify*/*Probe*/*Gate<N>) — treated like acceptance

const sev = (map) => (ctx) => (map[ctx] === undefined ? map.default : map[ctx]) || null;
const FAKE = sev({ acceptance: 'error', verify: 'error', test: 'warning', default: 'warning', editor: 'warning', 'test-edit': 'warning' });

/** Line rules: {id, re, severity(ctx), message} — re is tested per (comment-stripped) line. */
const LINE_RULES = [
  { id: 'no-gameobject-find', re: /\bGameObject\.Find(?:WithTag|GameObjectWithTag|GameObjectsWithTag)?\s*\(/,
    severity: sev({ runtime: 'error', verify: 'warning', editor: 'warning', 'test-edit': 'warning', test: 'warning', acceptance: 'warning' }), message: 'GameObject.Find* in runtime code: wire references via serialized fields / prefabs' },
  { id: 'no-find-object', re: /\b(?:FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|FindObjectsByType)\s*[<(]/,
    severity: sev({ runtime: 'error', verify: 'warning', editor: 'warning', 'test-edit': 'warning', test: 'warning', acceptance: 'warning' }), message: 'Find*ObjectByType/OfType in runtime code: inject or serialize the reference' },
  { id: 'no-reflection-private', re: /\bBindingFlags\.NonPublic\b/,
    severity: sev({ runtime: 'error', verify: 'error', acceptance: 'error', test: 'warning' }), message: 'reflection into non-public members: expose a serialized field or public API instead' },
  { id: 'no-config-createinstance-fallback', re: /\bCreateInstance\s*(?:<\s*\w*(?:Config|Def|Definition|Settings|Data)\s*>|\(\s*typeof\s*\(\s*\w*(?:Config|Def|Definition|Settings|Data)\s*\))/,
    severity: sev({ runtime: 'error', verify: 'error' }), message: 'ScriptableObject.CreateInstance<*Config> fallback at runtime hides missing asset wiring: reference the SO asset' },
  { id: 'no-editor-api-in-runtime', re: /\busing\s+UnityEditor\b|\bUnityEditor\.|\bAssetDatabase\.|\bEditorSceneManager\.|\bPrefabUtility\./,
    severity: sev({ runtime: 'error', verify: 'error', test: 'error', acceptance: 'error' }),
    message: 'UnityEditor/AssetDatabase in non-Editor code (even under #if UNITY_EDITOR): move to an Editor asmdef' },
  { id: 'no-physics-simulate', re: /\b\w*[Pp]hysics\w*\.Simulate\s*\(/,
    severity: FAKE, message: 'manual physics stepping (Physics.Simulate): acceptance must run real FixedUpdate physics' },
  { id: 'no-manual-simulation-mode', re: /\bPhysics2?D?\.(?:autoSimulation\s*=\s*false|simulationMode\s*=\s*SimulationMode2?D?\.Script)/,
    severity: FAKE, message: 'switching physics to script simulation: acceptance must run real physics' },
  { id: 'no-disable-collisions', re: /\.detectCollisions\s*=\s*false\b/,
    severity: FAKE, message: 'detectCollisions = false fakes physics outcomes' },
  { id: 'no-disable-simulation', re: /\.simulated\s*=\s*false\b|\bPhysics2?D?\.IgnoreCollision\s*\(/,
    severity: sev({ acceptance: 'error', verify: 'error', test: 'warning' }), message: 'disabling simulation/collisions inside a test fakes physics outcomes' },
  { id: 'no-sendmessage-lifecycle', re: /\bSendMessage\s*\(\s*"(?:FixedUpdate|Update|LateUpdate|Awake|Start|OnEnable|OnCollision\w*|OnTrigger\w*)"/,
    severity: sev({ editor: 'warning', 'test-edit': 'warning', default: 'error' }), message: 'SendMessage("FixedUpdate"/lifecycle) drives Unity callbacks by hand: let the player loop run' },
  { id: 'no-manual-clamp', re: /\bMathf\.Clamp\s*\([^;]*\b(?:position|pos|velocity|linearVelocity)\b|\.(?:position|localPosition|linearVelocity|velocity)\s*=[^;]*\bMathf\.Clamp\b/,
    severity: sev({ acceptance: 'error', verify: 'error', test: 'warning' }), message: 'manual position/velocity clamping in a test masks physics defects' },
  { id: 'no-teleport-in-acceptance', re: /\b\w+\.(?:position|linearVelocity|velocity|angularVelocity)\s*=(?!=)(?!.*\bMathf\.Clamp\b)/,
    severity: sev({ acceptance: 'warning', verify: 'warning' }), message: 'writing Rigidbody/Transform state directly in acceptance: drive the game through input (GoldPathDriver)' },
];

/** Multi-line rule: GetField/GetProperty(...) followed by SetValue within 4 lines. */
function reflectionSetValue(lines) {
  const hits = [];
  lines.forEach((l, i) => {
    if (!/\.SetValue\s*\(/.test(l)) return;
    const win = lines.slice(Math.max(0, i - 4), i + 1).join(' ');
    if (/\bGet(?:Field|Property)\s*\(/.test(win)) hits.push(i);
  });
  return hits;
}

const FILE_RULES = {
  maxLines: { id: 'max-lines', severity: sev({ default: 'error' }) },
  noVar: { id: 'no-var', severity: sev({ default: 'warning' }), re: /(?:^|[\s(;,])var\s+[A-Za-z_@(]/ },
  reflection: { id: 'no-reflection-setvalue', severity: sev({ runtime: 'error', verify: 'error', acceptance: 'error', test: 'warning' }),
    message: 'reflection GetField/GetProperty(...).SetValue: expose a serialized field or public API' },
};

module.exports = { LINE_RULES, FILE_RULES, reflectionSetValue };
