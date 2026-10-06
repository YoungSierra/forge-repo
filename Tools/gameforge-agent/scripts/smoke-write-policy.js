/**
 * Offline check for the provider-repo write policy:
 *  - assertAgentWriteAllowed allow/deny table (VerticalSlice / Production / Prototype)
 *  - denial text tells the agent to log a D-### decision
 *  - Unity MCP path-bearing write tools are policy-checked (llm.js mcpWriteTargets)
 *  - llm.js write_file refuses provider-owned paths (stub /chat/completions, no provider billed)
 *  - post-turn write audit (git + non-git): provider edits reverted/quarantined, TDD edit = hard stop
 *
 *   node scripts/smoke-write-policy.js
 */
import fs from "node:fs";
import http from "node:http";
import os from "node:os";
import path from "node:path";
import { spawnSync } from "node:child_process";

process.env.GAMEFORGE_UNITY_MCP = "0";

const { assertAgentWriteAllowed, checkAgentWrite, detectRepoLayout, describeWriteRoots } = await import(
  "../src/agent/writePolicy.js"
);
const { createWriteAudit, resolveAuditMode } = await import("../src/agent/writeAudit.js");
const { createLlmProvider, mcpWriteTargets } = await import("../src/agent/providers/llm.js");

const errors = [];
function ok(cond, msg) {
  if (cond) console.log("OK  ", msg);
  else {
    console.error("FAIL", msg);
    errors.push(msg);
  }
}

// ── 1. policy table ─────────────────────────────────────────────────────────────────────────
const ALLOW_VS = [
  "Assets/_Game/Scripts/Gameplay/Flipper.cs",
  "Assets/_Game/Scripts/Gameplay.asmdef",
  "Assets/_Game/Scenes/SCN_WoodlandPond_Gameplay.unity",
  "Assets/_Game/Prefabs/Gameplay/PRF_Flipper.prefab",
  "Assets/_Game/Prefabs/Visual/Props/PRF_Flipper_Visual.prefab",
  "Assets/_Game/Data/SO_FlipperConfig.asset",
  "Assets/_Game/Settings/Rendering/URP.asset",
  "Assets/_Game/Tests/PlayMode/AcceptanceFlipper.cs",
  "Assets/_Game/Art/Props/Bumpers/Frog/Materials/MAT_Frog.mat",
  "Assets/_Game/Art/Props/Bumpers/Frog/Materials.meta",
  "Assets/_Game/Art/Props/Bumpers/Frog/Meshes/SM_Frog.fbx.meta",
  "Assets/_Game.meta",
  "Docs/V57/STATUS.json",
  "Docs/V57/DECISIONS.md",
  "Docs/V57/evidence/goldpath/20260928T120000Z/checks.json",
  "Docs/Generated/json/asset_manifest.json",
  "V57/specs/<slug>/features/FlipperController.yaml",
  "Packages/manifest.json",
  "ProjectSettings/ProjectSettings.asset",
];
const DENY_PROVIDER = [
  "Docs/Design/TDD.md",
  "Docs/Design/LevelMaps/L01.png",
  "Docs/ArtDirection/ArtDirectionDocument.md",
  "Docs/Audio/AudioParameters.md",
  "Assets/_Game/Art/Props/Bumpers/Frog/Meshes/SM_Frog.fbx",
  "Assets/_Game/Art/Props/Bumpers/Frog/Textures/T_Frog_BC.png",
  "Assets/_Game/Audio/SFX/Bumper/SFX_Bumper_Hit.wav",
];
const DENY_OTHER = [
  "Assets/Prototypes/Pinball/Scripts/A.cs",
  "Tools/gameforge-agent/index.js",
  "Packages/com.v57.unity-game-forge/Runtime/X.cs",
  "Source/work.blend",
  ".env",
];

for (const forgeMode of ["VerticalSlice", "Production"]) {
  for (const rel of ALLOW_VS) {
    ok(checkAgentWrite(rel, "generate", { forgeMode }).ok, `${forgeMode} allows ${rel}`);
  }
  for (const rel of DENY_PROVIDER) {
    const v = checkAgentWrite(rel, "generate", { forgeMode });
    ok(!v.ok && v.code === "PROVIDER_OWNED", `${forgeMode} denies provider-owned ${rel}`);
  }
  for (const rel of DENY_OTHER) {
    ok(!checkAgentWrite(rel, "generate", { forgeMode }).ok, `${forgeMode} denies ${rel}`);
  }
}
// Legacy behaviour kept.
ok(checkAgentWrite("Assets/VerticalSlice/Scripts/A.cs", "generate", { forgeMode: "VerticalSlice" }).ok, "VS legacy root still allowed");
ok(checkAgentWrite("Assets/Scripts/Player.cs", "generate", { forgeMode: "Production" }).ok, "Production legacy root still allowed");
ok(checkAgentWrite("Assets/Prototypes/Pinball/A.cs", "generate", { forgeMode: "Prototype" }).ok, "Prototype sandbox allowed");
ok(!checkAgentWrite("ProjectSettings/ProjectSettings.asset", "generate", { forgeMode: "Prototype" }).ok, "Prototype still may not write ProjectSettings");
ok(!checkAgentWrite("Assets/_Game/Scripts/A.cs", "ask", { forgeMode: "VerticalSlice" }).ok, "ask mode is read-only");

let denial = "";
try {
  assertAgentWriteAllowed("Docs/Design/TDD.md", "generate", { forgeMode: "VerticalSlice" });
} catch (err) {
  denial = err.message;
}
ok(/D-###/.test(denial) && /DECISIONS\.md/.test(denial), "denial tells the agent to log a D-### decision");
ok(/HARD STOP/.test(denial), "TDD denial mentions the hard stop");
ok(/Assets\/_Game\/Scripts/.test(describeWriteRoots("VerticalSlice")), "VS prompt roots describe the provider layout");

// ── 2. MCP path extraction ──────────────────────────────────────────────────────────────────
ok(mcpWriteTargets("write_text_file", { path: "Docs/Design/TDD.md" })[0] === "Docs/Design/TDD.md", "MCP write_text_file path extracted");
ok(mcpWriteTargets("move_asset", { path: "Assets/_Game/Art/a.fbx", destination: "Assets/_Game/Scripts/a.fbx" }).length === 2, "MCP move_asset both paths extracted");
ok(mcpWriteTargets("get_scene_hierarchy", { path: "Assets/_Game/Art/a" }).length === 0, "read-only MCP tool ignored");

// ── 3. llm.js write_file enforcement (stub server) ──────────────────────────────────────────
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "gf-policy-"));
fs.mkdirSync(path.join(tmp, "Docs", "Design"), { recursive: true });
fs.writeFileSync(path.join(tmp, "Docs", "Design", "TDD.md"), "# TDD\noriginal\n");
ok(detectRepoLayout(tmp) === "provider", "provider layout detected");

const bodies = [];
const server = http.createServer((req, res) => {
  let raw = "";
  req.on("data", (c) => (raw += c));
  req.on("end", () => {
    const body = JSON.parse(raw || "{}");
    bodies.push(body);
    const message =
      bodies.length === 1
        ? {
            role: "assistant",
            content: "Editing.",
            tool_calls: [
              { id: "a", type: "function", function: { name: "write_file", arguments: JSON.stringify({ path: "Docs/Design/TDD.md", content: "hacked" }) } },
              { id: "b", type: "function", function: { name: "write_file", arguments: JSON.stringify({ path: "Assets/_Game/Scripts/Gameplay/A.cs", content: "class A {}" }) } },
            ],
          }
        : { role: "assistant", content: "Done." };
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ choices: [{ message }], usage: { prompt_tokens: 1, completion_tokens: 1 } }));
  });
});
await new Promise((r) => server.listen(0, "127.0.0.1", r));
const provider = createLlmProvider({
  root: tmp,
  apiKey: "stub",
  baseUrl: `http://127.0.0.1:${server.address().port}/v1`,
  model: "gpt-4o",
  writeMode: "generate",
  slug: "Pinball",
  forgeMode: "VerticalSlice",
});
await provider.run("go", { onEvent: () => {} });
server.close();
const toolMsgs = bodies[bodies.length - 1].messages.filter((m) => m.role === "tool");
ok(/^ERROR: Write denied/.test(String(toolMsgs[0]?.content)), "llm write_file to Docs/Design/TDD.md refused");
ok(fs.readFileSync(path.join(tmp, "Docs/Design/TDD.md"), "utf8").includes("original"), "TDD unchanged on disk");
ok(fs.existsSync(path.join(tmp, "Assets/_Game/Scripts/Gameplay/A.cs")), "llm write_file to Assets/_Game/Scripts allowed");
ok(/provider-delivered repo/.test(String(bodies[0].messages[0].content)), "llm VS system role uses provider layout");

// ── 4. post-turn audit (git repo) ───────────────────────────────────────────────────────────
function write(root, rel, text) {
  const abs = path.join(root, ...rel.split("/"));
  fs.mkdirSync(path.dirname(abs), { recursive: true });
  fs.writeFileSync(abs, text);
}
function makeRepo(useGit) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `gf-audit-${useGit ? "git" : "plain"}-`));
  write(root, "Docs/Design/TDD.md", "# TDD\nv1\n");
  write(root, "Docs/ArtDirection/ArtDirectionDocument.md", "## style_guide\nv1\n");
  write(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbx", "FBX");
  write(root, "Assets/_Game/Audio/SFX/SFX_Hit.wav", "RIFF");
  if (useGit) {
    const g = (...a) => spawnSync("git", ["-C", root, ...a], { encoding: "utf8" });
    g("init", "-q");
    g("-c", "user.email=t@t", "-c", "user.name=t", "add", "-A");
    g("-c", "user.email=t@t", "-c", "user.name=t", "commit", "-qm", "provider");
  }
  return root;
}

for (const useGit of [true, false]) {
  const root = makeRepo(useGit);
  const label = useGit ? "git" : "no-git";
  const audit = createWriteAudit({ root, forgeMode: "VerticalSlice", slug: "Pinball" });
  ok(audit.mode === "revert", `[${label}] VerticalSlice audit defaults to revert`);
  await audit.begin();
  // simulated agent turn
  write(root, "Docs/Design/TDD.md", "# TDD\nv2 edited to pass a gate\n");
  write(root, "Docs/ArtDirection/ArtDirectionDocument.md", "## style_guide\nv2\n");
  write(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog_Fixed.fbx", "FBX2");
  write(root, "Assets/_Game/Art/Props/Frog/Materials/MAT_Frog.mat", "mat");
  write(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbx.meta", "meta");
  write(root, "Assets/_Game/Scripts/Gameplay/Frog.cs", "class Frog {}");
  write(root, "Docs/V57/STATUS.json", "{}");
  write(root, "Assets/Random/notes.txt", "x");
  write(root, "Library/ArtifactDB", "unity");
  const report = await audit.end();
  const byPath = Object.fromEntries(report.violations.map((v) => [v.path, v]));
  ok(!!report.hardStop, `[${label}] TDD edit reported as hard stop`);
  ok(fs.readFileSync(path.join(root, "Docs/Design/TDD.md"), "utf8") === "# TDD\nv1\n", `[${label}] TDD restored`);
  ok(fs.readFileSync(path.join(root, "Docs/ArtDirection/ArtDirectionDocument.md"), "utf8").includes("v1"), `[${label}] ADD restored`);
  ok(byPath["Assets/_Game/Art/Props/Frog/Meshes/SM_Frog_Fixed.fbx"]?.action === "quarantined", `[${label}] new provider art file quarantined`);
  ok(!fs.existsSync(path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog_Fixed.fbx")), `[${label}] quarantined file removed from Art`);
  ok(fs.existsSync(path.join(root, ".v57", "quarantine")), `[${label}] agent versions kept under .v57/quarantine`);
  ok(!byPath["Assets/_Game/Art/Props/Frog/Materials/MAT_Frog.mat"], `[${label}] Art Materials not flagged`);
  ok(!byPath["Assets/_Game/Scripts/Gameplay/Frog.cs"] && !byPath["Docs/V57/STATUS.json"], `[${label}] V57 roots not flagged`);
  ok(!byPath["Library/ArtifactDB"], `[${label}] Unity Library ignored`);
  if (useGit) {
    ok(byPath["Assets/Random/notes.txt"]?.action === "flagged", `[git] out-of-roots file flagged, not reverted`);
    ok(fs.existsSync(path.join(root, "Assets/Random/notes.txt")), "[git] flagged file left in place");
  }
  ok(/D-###/.test(report.warningText), `[${label}] warning for next turn mentions D-###`);
}

ok(resolveAuditMode("Production", {}) === "revert", "Production audit defaults to revert");
ok(resolveAuditMode("Prototype", {}) === "flag", "Prototype audit defaults to flag");

// ── 5. Unity noise + provider renames ───────────────────────────────────────────────────────
for (const useGit of [true, false]) {
  const root = makeRepo(useGit);
  const label = useGit ? "git" : "no-git";
  const audit = createWriteAudit({ root, forgeMode: "Production", slug: "Pinball" });
  await audit.begin();
  // Unity: FBX embedded media + template/URP/TMP root assets
  write(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbm/T_Embedded.png", "png");
  write(root, "Assets/UniversalRenderPipelineGlobalSettings.asset", "urp");
  write(root, "Assets/DefaultVolumeProfile.asset", "vol");
  write(root, "Assets/TextMesh Pro/Resources/TMP Settings.asset", "tmp");
  write(root, "Assets/Settings/PC_RPAsset.asset", "rp");
  // agent renames a provider mesh + a provider audio file moved
  fs.renameSync(path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbx"), path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_FrogRenamed.fbx"));
  fs.mkdirSync(path.join(root, "Assets/_Game/Audio/SFX/Bumper"), { recursive: true });
  fs.renameSync(path.join(root, "Assets/_Game/Audio/SFX/SFX_Hit.wav"), path.join(root, "Assets/_Game/Audio/SFX/Bumper/SFX_Hit.wav"));
  const report = await audit.end();
  const paths = report.violations.map((v) => v.path).join("\n");
  ok(!/\.fbm\//.test(paths) && !/UniversalRenderPipelineGlobalSettings|DefaultVolumeProfile|TextMesh Pro|Assets\/Settings/.test(paths), `[${label}] Unity-generated files not reported as violations`);
  if (useGit) {
    // Without git only provider trees are snapshotted, so noise outside them is simply unseen.
    ok(report.noise.some((n) => /\.fbm\//.test(n)) && report.noise.some((n) => /TextMesh Pro/.test(n)), `[${label}] Unity-generated files listed as noise`);
  }
  ok(fs.existsSync(path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbm/T_Embedded.png")), `[${label}] .fbm folder left in place`);
  ok(fs.existsSync(path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_Frog.fbx")) && !fs.existsSync(path.join(root, "Assets/_Game/Art/Props/Frog/Meshes/SM_FrogRenamed.fbx")), `[${label}] provider mesh rename undone`);
  ok(fs.existsSync(path.join(root, "Assets/_Game/Audio/SFX/SFX_Hit.wav")) && !fs.existsSync(path.join(root, "Assets/_Game/Audio/SFX/Bumper/SFX_Hit.wav")), `[${label}] provider audio move undone`);
  ok(report.violations.filter((v) => v.kind === "renamed").length === 2, `[${label}] renames reported as violations`);
}
ok(resolveAuditMode("VerticalSlice", { GAMEFORGE_WRITE_AUDIT: "off" }) === "off", "GAMEFORGE_WRITE_AUDIT=off honoured");

if (errors.length) {
  console.error(`\n${errors.length} write-policy check(s) failed`);
  process.exit(1);
}
console.log("\nAll write-policy smoke checks passed");
