/**
 * Offline check for the autonomous VerticalSlice runner.
 *
 * Part 1 — unit: config defaults, decideNext stop reasons, HARD-STOP text detection, the loop
 *          itself with a scripted fake agent (owner stop token, max turns, stall, cancel,
 *          hard stop, legacy GF-PROGRESS fallback).
 * Part 2 — end-to-end through session.generateFinal() with a stub OpenAI-compatible server
 *          (no provider billed, Unity MCP off): intake skill routing, re-anchor prompt, blocking
 *          gate rejection + correction, next_spec-driven TDD extract, provider-file denial,
 *          stop on STATUS done, autonomy status exposed; plus `autonomy: "staged"` = one turn.
 *
 *   node scripts/smoke-autonomous.js
 */
import fs from "node:fs";
import http from "node:http";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

process.env.GAMEFORGE_UNITY_MCP = "0";
for (const k of Object.keys(process.env)) {
  if (/_API_KEY$/.test(k)) delete process.env[k];
}
delete process.env.GAMEFORGE_AUTONOMY;
delete process.env.GAMEFORGE_GATES;

const here = path.dirname(fileURLToPath(import.meta.url));
const { resolveAutonomyConfig, decideNext, detectHardStopInText, runAutonomousLoop, createAutonomyStatus } =
  await import("../src/agent/autonomy.js");
const { readRunState } = await import("../src/agent/v57State.js");
const { verifyGateClaims } = await import("../src/agent/playabilityAdvisor.js");

const errors = [];
function ok(cond, msg) {
  if (cond) console.log("OK  ", msg);
  else {
    console.error("FAIL", msg);
    errors.push(msg);
  }
}
function write(root, rel, text) {
  const abs = path.join(root, ...rel.split("/"));
  fs.mkdirSync(path.dirname(abs), { recursive: true });
  fs.writeFileSync(abs, typeof text === "string" ? text : JSON.stringify(text, null, 2));
}
const status = (root, obj) => write(root, "Docs/V57/STATUS.json", obj);

// ── Part 1: unit ────────────────────────────────────────────────────────────────────────────
const cfgVs = resolveAutonomyConfig({ forgeMode: "VerticalSlice", env: {} });
ok(cfgVs.enabled && cfgVs.maxTurns === 60 && cfgVs.maxHours === 12, "VerticalSlice defaults: auto, 60 turns, 12 h");
ok(!resolveAutonomyConfig({ forgeMode: "VerticalSlice", requested: "staged", env: {} }).enabled, "staged opt-in disables autonomy");
ok(resolveAutonomyConfig({ forgeMode: "Production", op: "game-setup", env: {} }).enabled, "Production /game-setup autonomous by default");
ok(!resolveAutonomyConfig({ forgeMode: "Production", op: "game-setup", requested: "staged", env: {} }).enabled, "Production staged only when requested");
ok(!resolveAutonomyConfig({ forgeMode: "Production", env: { GAMEFORGE_AUTONOMY: "staged" } }).enabled, "GAMEFORGE_AUTONOMY=staged honoured");
ok(!resolveAutonomyConfig({ forgeMode: "Prototype", requested: "auto", env: {} }).enabled, "Prototype never autonomous");
const envCfg = resolveAutonomyConfig({ forgeMode: "VerticalSlice", env: { GAMEFORGE_AUTO_MAX_TURNS: "5", GAMEFORGE_AUTO_MAX_HOURS: "0.5" } });
ok(envCfg.maxTurns === 5 && envCfg.maxHours === 0.5, "budget env vars honoured");

const base = { turns: 1, startedAt: 0, now: 1000, stalls: 0, config: cfgVs };
ok(decideNext({ ...base, state: { status: "done" } }).reason === "done", "stop on done");
ok(decideNext({ ...base, state: { status: "blocked", next: "x" } }).reason.startsWith("blocked"), "stop on blocked");
ok(decideNext({ ...base, state: { hardStop: "TDD conflict" } }).reason.startsWith("hard-stop"), "stop on STATUS hard_stop");
ok(decideNext({ ...base, state: {}, ownerStop: true }).reason.startsWith("owner-stop-token"), "stop on owner token");
ok(decideNext({ ...base, state: {}, turns: 60 }).reason.startsWith("max-turns"), "stop on max turns");
ok(decideNext({ ...base, state: {}, now: 12 * 3_600_000 }).reason.startsWith("max-hours"), "stop on max hours");
ok(decideNext({ ...base, state: {}, stalls: 3 }).reason.startsWith("stalled"), "stop when stalled");
ok(decideNext({ ...base, state: {}, cancelled: true }).reason === "cancelled", "stop on cancel");
ok(!decideNext({ ...base, state: { status: "running" } }).stop, "continue while running");
ok(detectHardStopInText("…\nHARD-STOP: provider TDD contradicts itself") === "provider TDD contradicts itself", "HARD-STOP line detected");
ok(detectHardStopInText("no hard stop here") === "", "no false HARD-STOP");

async function loopScenario(name, { maxTurns = 60, script, abortAt = 0, env = {} }) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), `gf-auto-${name}-`));
  const config = { ...resolveAutonomyConfig({ forgeMode: "VerticalSlice", env: {} }), maxTurns, ...env };
  const st = createAutonomyStatus(config);
  const ac = new AbortController();
  const prompts = [];
  const initialState = await readRunState(root);
  const turn = async (n) => {
    const text = (await script(root, n)) || "";
    if (abortAt && n >= abortAt) ac.abort();
    return { result: { status: ac.signal.aborted ? "cancelled" : "finished" }, text, startedAt: Date.now(), audit: null };
  };
  const first = await turn(1);
  const out = await runAutonomousLoop({
    root,
    config,
    status: st,
    signal: ac.signal,
    initialState,
    firstTurn: first,
    runTurn: async ({ turn: n, state, warnings }) => {
      prompts.push({ n, state, warnings });
      return turn(n);
    },
  });
  return { out, st, prompts, root };
}

{
  const { out, st, prompts } = await loopScenario("progress", {
    script: (root, n) => status(root, n < 4 ? { stage: `S${n}`, status: "running", next: `S${n + 1}` } : { stage: "M4", status: "done" }),
  });
  ok(out.stopReason === "done" && out.turns === 4, `progress → done after 4 turns (${out.stopReason}, ${out.turns})`);
  ok(prompts[0].state.next === "S2", "continuation receives STATUS.next");
  ok(st.lastStage === "M4" && !st.active, "status exposes last stage, inactive after stop");
}
{
  const { out } = await loopScenario("token", {
    script: (root, n) => {
      status(root, { stage: `S${n}`, status: "running", next: "more" });
      if (n === 3) write(root, ".v57/ALLOW_STOP", "");
    },
  });
  ok(out.stopReason.startsWith("owner-stop-token") && out.turns === 3, `owner token stops after turn 3 (${out.stopReason})`);
}
{
  const { out } = await loopScenario("budget", {
    maxTurns: 4,
    script: (root, n) => status(root, { stage: `S${n}`, status: "running", next: "more" }),
  });
  ok(out.stopReason.startsWith("max-turns") && out.turns === 4, `max turns = 4 (${out.stopReason})`);
}
{
  const { out } = await loopScenario("stall", {
    script: (root) => status(root, { stage: "S1", status: "running", next: "same" }),
  });
  ok(out.stopReason.startsWith("stalled") && out.turns === 4, `stalled after 3 unchanged turns (${out.stopReason}, ${out.turns})`);
}
{
  // Agent re-claims M1 PASS every turn without evidence, bumping updated_utc: verifier rewrites it,
  // but the progress fingerprint is taken before the rewrite → stalled, not an endless loop.
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "gf-auto-reclaim-"));
  const config = resolveAutonomyConfig({ forgeMode: "VerticalSlice", env: {} });
  const st = createAutonomyStatus(config);
  const verified = new Set();
  const claim = () => {
    status(root, { stage: "M1", status: "running", next: "M1 gold path", gates: { M1: { state: "passed", at_utc: new Date().toISOString() } }, updated_utc: new Date().toISOString() });
    return { result: { status: "finished" }, text: "", startedAt: Date.now(), audit: null };
  };
  const initialState = await readRunState(root);
  const out = await runAutonomousLoop({
    root,
    config,
    status: st,
    initialState,
    firstTurn: claim(),
    verifyGates: ({ prevState, turnStartedAt }) =>
      verifyGateClaims({ root, mode: "blocking", prevGates: prevState?.gates || {}, turnStartedAt, verified }),
    runTurn: async () => {
      await new Promise((r) => setTimeout(r, 5));
      return claim();
    },
  });
  ok(out.stopReason.startsWith("stalled") && out.turns === 4, `re-claiming a rejected gate stalls (${out.stopReason}, ${out.turns})`);
}
{
  // Template STATUS: blockers[] / stage_state stopped win over status "running"; failed ≠ blocked.
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "gf-state-"));
  status(root, { stage: "I0", stage_state: "running", status: "running", blockers: ["intake exit 2: 3 blocking issues"] });
  ok((await readRunState(root)).status === "blocked", "blockers[] non-empty → blocked");
  status(root, { stage: "M2", stage_state: "stopped", status: "running", blockers: [] });
  ok((await readRunState(root)).status === "blocked", "stage_state stopped → blocked");
  status(root, { stage: "M3", stage_state: "failed", status: "failed", blockers: [] });
  ok((await readRunState(root)).status !== "blocked", "status failed is not a stop");
  status(root, { stage: "M3", stage_state: "failed-timeboxed", status: "running", blockers: [] });
  ok((await readRunState(root)).status === "running", "stage failed-timeboxed keeps running");
}
{
  const { out } = await loopScenario("cancel", {
    abortAt: 2,
    script: (root, n) => status(root, { stage: `S${n}`, status: "running", next: "more" }),
  });
  ok(out.stopReason === "cancelled" && out.turns === 2, `cancel stops the loop (${out.stopReason})`);
}
{
  const { out } = await loopScenario("hardstop", {
    script: (root, n) => {
      status(root, { stage: `S${n}`, status: "running", next: "more" });
      return n === 2 ? "Could not continue.\nHARD-STOP: destructive migration needs owner" : "ok";
    },
  });
  ok(out.stopReason === "hard-stop: destructive migration needs owner", `HARD-STOP text stops (${out.stopReason})`);
}
{
  const { out, prompts } = await loopScenario("legacy", {
    script: (root, n) =>
      write(
        root,
        "Docs/V57/reports/game-setup-progress.md",
        n < 3
          ? `GF-PROGRESS stage=S${n} spec=ScoreSystem done=${n} total=3 action="stage ${n}" status=ready\n`
          : `GF-PROGRESS stage=DONE done=3 total=3 action="all" status=ready\n`,
      ),
  });
  ok(out.stopReason === "done" && prompts[0].state.source === "legacy", `legacy GF-PROGRESS fallback (${out.stopReason})`);
  ok(prompts[0].state.nextSpec.join() === "ScoreSystem", "legacy spec → nextSpec");
}

// Gate verifier: blocking vs warn.
{
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "gf-gates-"));
  status(root, { stage: "M1", status: "running", gates: { M1: "PASS" } });
  const warn = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(warn.rejected.length === 1 && !warn.rewritten, "warn mode reports but keeps STATUS");
  const block = await verifyGateClaims({ root, mode: "blocking", turnStartedAt: Date.now() });
  const after = JSON.parse(fs.readFileSync(path.join(root, "Docs/V57/STATUS.json"), "utf8"));
  ok(block.rewritten && after.gates.M1.state === "failed" && !("status" in after.gates.M1) && after.gates.M1.claimed === "PASS", "blocking mode rewrites gate to {state: failed, reason, claimed}");
  write(root, "Docs/V57/evidence/goldpath/20260101T000000Z/checks.json", { pass: false, checks: [{ id: "a", pass: false }] });
  status(root, { stage: "M1", status: "running", gates: { M1: { state: "passed", status: "failed", at_utc: new Date().toISOString() } } });
  const failing = await verifyGateClaims({ root, mode: "blocking", turnStartedAt: Date.now() });
  ok(failing.rejected[0]?.missing[0]?.includes("not pass=true"), "template `state: passed` wins over `status`; failing checks.json rejected");
  write(root, "Docs/V57/evidence/goldpath/20260101T000100Z/checks.json", { pass: true, checks: [{ id: "a", pass: true }] });
  status(root, { stage: "M2", status: "running", gates: { M1: "PASS", M2: "PASS" } });
  const m2 = await verifyGateClaims({
    root,
    mode: "blocking",
    turnStartedAt: Date.now(),
    runLint: () => ({ ok: false, errors: 2, detail: "runtime-lint: exit 1, 2 error(s)" }),
  });
  ok(m2.rejected.map((r) => r.gate).join() === "M2" && /runtime-lint/.test(m2.message), "M2 needs lint 0 errors (M1 accepted)");
  ok(/hierarchy-diff\.json/.test(m2.message), "M2 needs hierarchy-diff.json");
  write(root, "Docs/V57/reports/hierarchy-diff.json", { pass: true, violations: [] });
  status(root, { stage: "M2", status: "running", gates: { M2: { state: "passed", at_utc: "x2" } } });
  const m2ok = await verifyGateClaims({ root, mode: "blocking", turnStartedAt: Date.now(), runLint: () => ({ ok: true, errors: 0, detail: "ok" }) });
  ok(!m2ok.rejected.length, "M2 passes with lint 0 + hierarchy pass + gold path");

  // I2: pass, errors[] and missing refs
  write(root, "Docs/V57/reports/assembly-report.json", { pass: false, errors: [], missing_refs: [] });
  status(root, { stage: "I2", status: "running", gates: { I2: { state: "passed", at_utc: "a1" } } });
  const i2a = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(/pass is not true/.test(i2a.message), "I2 rejects pass=false");
  write(root, "Docs/V57/reports/assembly-report.json", { pass: true, errors: ["Material MAT_Frog failed"], missing_refs: [] });
  const i2b = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(/1 error/.test(i2b.message), "I2 rejects errors[]");
  write(root, "Docs/V57/reports/assembly-report.json", { pass: true, errors: [], missing_refs: [{ asset: "PRF_Frog" }] });
  const i2c = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(/1 missing reference/.test(i2c.message), "I2 rejects missing refs");
  write(root, "Docs/V57/reports/assembly-report.json", { pass: true, errors: [], missing_refs: [], counts: { missing_refs: 0 } });
  const i2d = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(!i2d.rejected.length, "I2 accepts pass=true, 0 errors, 0 missing refs");

  // M3 review: implementer-written pack files never count; reviewer + areas ≥ min_score required.
  write(root, "Docs/V57/evidence/M3/20260101T000200Z/review-pack/areas.json", { reviewer: "me", areas: [{ area: "Camera", score: 10 }] });
  write(root, "Docs/V57/evidence/review/20260101T000200Z/areas.json", { reviewer: "me", areas: [{ area: "Camera", score: 10 }] });
  status(root, { stage: "M3", status: "running", gates: { M3: { state: "passed", at_utc: "r1" } } });
  const r1 = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(r1.rejected[0]?.gate === "M3" && /review\.json/.test(r1.message), "implementer review-pack/areas.json does not satisfy M3");
  write(root, "Docs/V57/evidence/review/20260101T000300Z/review.json", { round: 1, reviewer: "", min_score: 7, areas: [{ area: "Camera", score: 9 }] });
  const r2 = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(/no reviewer/.test(r2.message), "review.json without reviewer rejected");
  write(root, "Docs/V57/evidence/review/20260101T000300Z/review.json", { round: 1, reviewer: "fresh-session-1", min_score: 7, areas: [{ area: "Camera", score: 9 }, { area: "UI vs mockups", score: 6 }] });
  const r3 = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(/below min_score 7: UI vs mockups=6/.test(r3.message), "area below min_score rejected");
  write(root, "Docs/V57/evidence/review/20260101T000300Z/review.json", { round: 2, reviewer: "fresh-session-2", areas: [{ area: "Camera", score: 7 }, { area: "UI vs mockups", score: 8 }] });
  const r4 = await verifyGateClaims({ root, mode: "warn", turnStartedAt: Date.now() });
  ok(!r4.rejected.length, "reviewer review.json with every area ≥ 7 (default min) accepted");
  write(root, "Docs/V57/evidence/review/20260101T000300Z/review.json", { round: 3, reviewer: "fresh-session-3", min_score: 7, areas: [{ area: "Camera", score: 5 }] });
  status(root, { stage: "M4", status: "done", gates: {} });
  const done = await verifyGateClaims({ root, mode: "blocking", turnStartedAt: Date.now(), runLint: () => ({ ok: true, errors: 0, detail: "ok" }) });
  const afterDone = JSON.parse(fs.readFileSync(path.join(root, "Docs/V57/STATUS.json"), "utf8"));
  ok(done.rejected[0]?.gate === "status=done" && afterDone.status === "running", "done with below-threshold review → running");
  status(root, { stage: "M4", status: "done", gates: { M3: { state: "failed-timeboxed" } } });
  const doneTb = await verifyGateClaims({ root, mode: "blocking", turnStartedAt: Date.now(), runLint: () => ({ ok: true, errors: 0, detail: "ok" }) });
  ok(!doneTb.rejected.length, "done accepted after M3 failed-timeboxed (review exists, threshold waived)");
}

// ── Part 2: end-to-end through the session ──────────────────────────────────────────────────
const fixtureCandidates = [
  path.resolve(here, "../../../V57/tools/intake/test/fixtures/TDD_HappyHabitat.md"),
];
const fixture = fixtureCandidates.find((p) => fs.existsSync(p));

function makeProviderRepo() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "gf-e2e-"));
  if (fixture) fs.copyFileSync(fixture, (fs.mkdirSync(path.join(root, "Docs/Design"), { recursive: true }), path.join(root, "Docs/Design/TDD.md")));
  else write(root, "Docs/Design/TDD.md", "# TDD\n## Mechanic: FlipperController\nflip\n");
  write(root, "Assets/_Game/Art/Props/.gitkeep", "");
  write(root, "V57/agents/skills/commands/setup/unity-vertical-slice-skill.md", "# VS-DRIVER-SKILL\nStages I0..M4.");
  write(root, "V57/agents/skills/commands/setup/unity-intake-skill.md", "# INTAKE-SKILL\nRun intake.");
  write(root, "V57/knowledge/goldpath-driver.md", "gold path notes");
  write(root, "V57/knowledge/unity-physics.md", "physics notes");
  write(
    root,
    "V57/tools/lint/runtime-lint.js",
    'console.log(JSON.stringify({ errors: 0, findings: [] })); process.exit(0);\n',
  );
  return root;
}

function call(id, name, args) {
  return { id, type: "function", function: { name, arguments: JSON.stringify(args) } };
}

/** Scripted agent: one tool batch per turn, then a short text reply. */
function startStub(root) {
  const turns = []; // user prompt of each new provider.run
  const toolResults = [];
  let turn = 0;
  const server = http.createServer((req, res) => {
    let raw = "";
    req.on("data", (c) => (raw += c));
    req.on("end", () => {
      const body = JSON.parse(raw || "{}");
      const last = body.messages[body.messages.length - 1];
      let message;
      if (last.role === "user") {
        turn += 1;
        turns.push(String(typeof last.content === "string" ? last.content : JSON.stringify(last.content)));
        const now = new Date().toISOString();
        const ts = now.replace(/[-:]/g, "").replace(/\.\d+Z$/, "Z");
        const calls = [];
        if (turn === 1) {
          calls.push(call("t1a", "write_file", { path: "Docs/Generated/json/package.json", content: "{}" }));
          calls.push(call("t1b", "write_file", { path: "Docs/V57/INTAKE_REPORT.md", content: "# Intake\n0 blocking" }));
          calls.push(call("t1c", "write_file", { path: "Docs/Design/TDD.md", content: "edited" }));
          calls.push(call("t1d", "write_file", { path: "Docs/V57/STATUS.json", content: JSON.stringify({ stage: "I0", status: "running", next: "I1 PROJECT", next_spec: [], gates: { A0: { status: "PASS", at: now } } }) }));
        } else if (turn === 2) {
          calls.push(call("t2a", "write_file", { path: "Assets/_Game/Scripts/Gameplay/Flipper.cs", content: "class Flipper {}" }));
          calls.push(call("t2b", "write_file", { path: "Docs/V57/STATUS.json", content: JSON.stringify({ stage: "M1", status: "running", next: "F FlipperController", next_spec: ["C-02"], gates: { A0: { state: "passed" }, M1: { state: "passed", evidence: null, at_utc: now } } }) }));
        } else if (turn === 3) {
          calls.push(call("t3a", "write_file", { path: `Docs/V57/evidence/goldpath/${ts}/checks.json`, content: JSON.stringify({ pass: true, checks: [{ id: "launch", pass: true }] }) }));
          calls.push(call("t3b", "write_file", { path: "Docs/V57/STATUS.json", content: JSON.stringify({ stage: "F", status: "running", next: "M4 ACCEPTANCE", next_spec: ["C-02"], gates: { A0: { status: "PASS" }, M1: { status: "PASS", at: now } } }) }));
        } else {
          calls.push(call("t4a", "write_file", { path: `Docs/V57/evidence/review/${ts}/review.json`, content: JSON.stringify({ round: 1, reviewer: "fresh-session-1", min_score: 7, areas: [{ area: "Camera", score: 7 }, { area: "UI vs mockups", score: 8 }] }) }));
          calls.push(call("t4c", "write_file", { path: "Docs/V57/reports/hierarchy-diff.json", content: JSON.stringify({ pass: true, violations: [] }) }));
          calls.push(call("t4b", "write_file", { path: "Docs/V57/STATUS.json", content: JSON.stringify({ stage: "M4", status: "done", next: "", gates: { A0: { status: "PASS" }, M1: { status: "PASS" }, M4: { status: "PASS", at: now } } }) }));
        }
        message = { role: "assistant", content: `Turn ${turn} working.`, tool_calls: calls };
      } else {
        for (const m of body.messages.filter((x) => x.role === "tool")) toolResults.push(String(m.content));
        message = { role: "assistant", content: `Turn ${turn} complete. STATUS.json updated.` };
      }
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ choices: [{ message }], usage: { prompt_tokens: 1, completion_tokens: 1 } }));
    });
  });
  return { server, turns, toolResults };
}

async function e2e(autonomy) {
  const root = makeProviderRepo();
  const stub = startStub(root);
  await new Promise((r) => stub.server.listen(0, "127.0.0.1", r));
  process.env.AGENT_PROVIDER = "openai";
  process.env.OPENAI_API_KEY = "stub";
  process.env.OPENAI_BASE_URL = `http://127.0.0.1:${stub.server.address().port}/v1`;
  process.env.OPENAI_MODEL = "gpt-4o";
  const { createSession } = await import("../src/agent/session.js");
  const session = await createSession({
    root,
    tddsRoot: path.join(root, "Docs", "tdds"),
    slug: "Pinball",
    forgeMode: "VerticalSlice",
    autonomy,
  });
  const events = [];
  await session.generateFinal({ onEvent: (ev) => events.push(ev), forgeMode: "VerticalSlice", autonomy });
  stub.server.close();
  return { root, session, events, ...stub };
}

{
  const { root, session, events, turns, toolResults } = await e2e("");
  const auto = session.getAutonomy();
  console.log(`e2e: ${turns.length} turn(s), stop=${auto.stopReason}`);
  ok(turns.length === 4, `autonomous run took 4 turns without "continue" (got ${turns.length})`);
  ok(auto.stopReason === "done" && auto.turns === 4 && auto.lastStage === "M4", "stopped on STATUS done; status exposes turns/stage/reason");
  ok(turns[0].includes("VS-DRIVER-SKILL") && turns[0].includes("INTAKE-SKILL"), "turn 1: vertical-slice skill + intake companion (Docs/Generated missing)");
  ok(!turns[1].includes("INTAKE-SKILL"), "turn 2+: intake companion dropped");
  ok(turns[1].startsWith("# Re-anchor") && turns[1].includes("I1 PROJECT") && turns[1].includes("DEVLOG"), "turn 2 prompt starts with the re-anchor block + STATUS");
  ok(turns[1].includes("V57/knowledge/goldpath-driver.md"), "re-anchor lists knowledge files");
  ok(/Write roots \(Vertical Slice · provider repo/.test(turns[0]), "prompt uses provider-layout write roots");
  ok(turns[2].includes("Gate claims REJECTED") && turns[2].includes("M1"), "turn 3 carries the gate-rejection correction");
  ok(turns[2].includes("<!-- §B Mechanic: FlipperController") && turns[2].includes("§C C-02 FlipperController"), "turn 3 TDD extract follows next_spec C-02");
  ok(toolResults.some((t) => /^ERROR: Write denied: Docs\/Design\/TDD\.md/.test(t) && /D-###/.test(t)), "provider TDD write refused with D-### guidance");
  ok(fs.readFileSync(path.join(root, "Docs/Design/TDD.md"), "utf8").length > 100, "TDD untouched");
  ok(events.some((e) => e.type === "gate-check" && e.rejected?.[0]?.gate === "M1"), "gate-check event emitted");
  ok(events.some((e) => e.type === "autonomy" && e.stopped && e.stopReason === "done"), "autonomy stop event emitted");
  const activity = session.getActivity();
  ok(activity.autonomy?.stopReason === "done" && activity.layout === "provider", "activity snapshot exposes autonomy + layout");
  ok(session.getEventsAfter(0).autonomy?.turns === 4, "events/poll payload exposes autonomy");
}
{
  const { session, turns } = await e2e("staged");
  ok(turns.length === 1, `autonomy "staged" keeps one-stage behaviour (got ${turns.length} turn(s))`);
  ok(!session.getAutonomy().enabled, "staged run reports autonomy disabled");
}

if (errors.length) {
  console.error(`\n${errors.length} autonomous check(s) failed`);
  process.exit(1);
}
console.log("\nAll autonomous runner smoke checks passed");
