/**
 * Playability advisor.
 *
 * 1. `advisePlayability` — soft, WARN ONLY. Never throws, never blocks Generate Final / Chat / ready.
 *    Scans Unity mode-root C# + TDD text for likely loop gaps.
 *    Does not classify genre — loop expectations come from the active TDD only.
 * 2. `verifyGateClaims` — gate evidence verifier for autonomous VerticalSlice/Production runs.
 *    Gates are never self-graded: a PASS claimed in Docs/V57/STATUS.json `gates.*` must be backed by
 *    evidence files on disk. `GAMEFORGE_GATES=blocking` (default for VerticalSlice and Production)
 *    rewrites the unsupported claim to `{ state: "failed" }` in STATUS.json and returns a correction for the next turn;
 *    `GAMEFORGE_GATES=warn` only reports.
 */

import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { normalizeForgeMode } from "./writePolicy.js";
import { readStatusJson, writeStatusJson } from "./v57State.js";

/**
 * @typedef {{ id: string, severity: "info" | "warn", message: string, chatHint?: string }} Advice
 */

async function walkCs(dir, out) {
  let entries = [];
  try {
    entries = await fs.readdir(dir, { withFileTypes: true });
  } catch {
    return;
  }
  for (const ent of entries) {
    if (ent.name.startsWith(".")) continue;
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) await walkCs(abs, out);
    else if (ent.isFile() && ent.name.endsWith(".cs")) out.push(abs);
  }
}

async function readUnityGameplayBundle(root) {
  const folders = ["Assets/Prototypes", "Assets/VerticalSlice", "Assets/Scripts", "Assets/_Game/Scripts"];
  const files = [];
  for (const folder of folders) {
    await walkCs(path.join(root, ...folder.split("/")), files);
  }
  const chunks = [];
  for (const abs of files.slice(0, 80)) {
    try {
      const body = await fs.readFile(abs, "utf8");
      const rel = path.relative(root, abs).replace(/\\/g, "/");
      chunks.push(`\n// —— ${rel} ——\n${body}`);
    } catch {
      /* */
    }
  }
  return {
    text: chunks.join("\n"),
    files: files.map((f) => path.relative(root, f).replace(/\\/g, "/")),
    hasScripts: files.length > 0,
  };
}

/**
 * @param {{ root: string, tddText?: string }} opts
 * @returns {Promise<{ ok: true, advice: Advice[] }>}
 */
export async function advisePlayability({ root, tddText = "" }) {
  /** @type {Advice[]} */
  const advice = [];
  const bundle = await readUnityGameplayBundle(root);

  if (!bundle.hasScripts) {
    advice.push({
      id: "no-scripts",
      severity: "warn",
      message:
        "No C# under Assets/Prototypes, VerticalSlice, Scripts or _Game/Scripts yet — Generate may still be writing.",
      chatHint: "Ensure mode-root scripts + a Bootstrap or scene exist so Play Mode can run the loop",
    });
    return { ok: true, advice };
  }

  const src = bundle.text;
  const hasRestart =
    /\brestart\b/i.test(src) ||
    /Key\.R\b/.test(src) ||
    /\"r\"/i.test(src) ||
    /RestartPressed/.test(src);
  if (!hasRestart) {
    advice.push({
      id: "restart",
      severity: "info",
      message: "No obvious restart binding (often R) — confirm round can restart without domain reload.",
      chatHint: "Add restart that resets round state in Play Mode per TDD §B",
    });
  }

  if (!/Universal Render Pipeline\/Lit|GameForgePrimitives|Shader\.Find/.test(src)) {
    advice.push({
      id: "urp-mats",
      severity: "info",
      message: "Confirm MeshRenderers use URP Lit/Simple Lit or GameForgePrimitives (avoid magenta Default-Material).",
      chatHint: "Assign URP materials on every CreatePrimitive / graybox mesh",
    });
  }

  const tdd = String(tddText);
  if (tdd.length > 200) {
    const needsPresentation =
      /\b(atmosphere|§9|hud|camera|visual|mood|palette|bloom|fog|§8|§11\.5)\b/i.test(tdd);
    const hasPresentationDetail =
      /\b(vignette|grain|postExposure|saturation|§8|atmosphere appendix|§11\.5)\b/i.test(tdd);
    if (needsPresentation && !hasPresentationDetail) {
      advice.push({
        id: "tdd-presentation",
        severity: "info",
        message:
          "TDD implies presentation — enrich §8/§9/atmosphere/§11.5 (camera profile + HUD metrics) before G-POST/G-FEEL.",
        chatHint: "Add quantified fog, grade, HUD metrics, and camera literals to the TDD",
      });
    }
  }

  if (!advice.length) {
    advice.push({
      id: "looks-ok",
      severity: "info",
      message: "Soft check: Unity sources look OK — still Play-test the loop against the TDD.",
    });
  }

  return { ok: true, advice };
}

/**
 * One chat-ready paragraph from advice list.
 * @param {Advice[]} advice
 */
export function formatAdviceForChat(advice = []) {
  const warns = advice.filter((a) => a.severity === "warn");
  const infos = advice.filter((a) => a.severity === "info" && a.id !== "looks-ok");
  if (!warns.length && !infos.length) {
    return "";
  }
  const lines = [];
  if (warns.length) {
    lines.push("Playability hints (does not block play — fix via chat if needed):");
    for (const w of warns) {
      lines.push(`• ${w.message}`);
      if (w.chatHint) lines.push(`  → Try: ${w.chatHint}`);
    }
  } else {
    lines.push("Soft notes:");
    for (const i of infos.slice(0, 3)) lines.push(`• ${i.message}`);
  }
  return lines.join("\n");
}

/* ------------------------------------------------------------------------------------------ */
/* Gate evidence verifier (autonomous VerticalSlice / Production)                              */
/* ------------------------------------------------------------------------------------------ */

/** @returns {"blocking"|"warn"} */
export function resolveGateMode(forgeMode, env = process.env) {
  const v = String(env.GAMEFORGE_GATES || "").trim().toLowerCase();
  if (v === "blocking" || v === "block") return "blocking";
  if (v === "warn" || v === "warn-only" || v === "soft") return "warn";
  const mode = normalizeForgeMode(forgeMode);
  return mode === "VerticalSlice" || mode === "Production" ? "blocking" : "warn";
}

const GOLDPATH_DIR = "Docs/V57/evidence/goldpath";
/** Only the reviewer's verdict counts — `Docs/V57/evidence/review/<UTC>/review.json` (unity-independent-review-skill). */
const REVIEW_DIR = "Docs/V57/evidence/review";
const REVIEW_FILE = "review.json";
const DEFAULT_MIN_SCORE = 7;
const ASSEMBLY_REPORT = "Docs/V57/reports/assembly-report.json";
const HIERARCHY_DIFF = "Docs/V57/reports/hierarchy-diff.json";
const LINT_SCRIPT = "V57/tools/lint/runtime-lint.js";

/** Evidence kinds required per gate id (DESIGN_BRIEF §3). */
export function evidenceForGate(gateId) {
  const g = String(gateId || "").toUpperCase().replace(/\s+/g, "");
  if (/^(A0|I0)|INTAKE/.test(g)) return ["intake"];
  if (/^I2|ASSEMBLY/.test(g)) return ["assembly"];
  if (/^M1|GOLD/.test(g)) return ["goldpath"];
  if (/^M2/.test(g)) return ["goldpath", "lint", "hierarchy"];
  if (/^M3|CRAFT|REVIEW/.test(g)) return ["goldpath", "review"];
  if (/^M4|ACCEPT/.test(g)) return ["goldpath", "lint", "hierarchy", "review"];
  if (/^F($|[-_:.\d])|^SPEC/.test(g)) return ["goldpath"];
  return [];
}

/** @param {unknown} v gate value from STATUS.json */
export function gateClaim(v) {
  if (v == null) return { pass: false, at: 0, sig: "" };
  if (typeof v === "string" || typeof v === "boolean") {
    const pass = v === true || /^pass/i.test(String(v).trim());
    return { pass, at: 0, sig: String(v) };
  }
  if (typeof v === "object") {
    // STATUS template shape `{ state, evidence, at_utc }` wins over legacy `status`/`result`.
    const s = v.state ?? v.status ?? v.result ?? v.verdict ?? (v.pass === true ? "PASS" : "");
    const pass = /^pass/i.test(String(s || "").trim());
    const atRaw = v.at ?? v.at_utc ?? v.claimed_at ?? v.updated_at ?? v.time ?? null;
    const at = atRaw ? Date.parse(String(atRaw)) || 0 : 0;
    return { pass, at, sig: `${s}|${atRaw || ""}|${v.evidence ? JSON.stringify(v.evidence) : ""}` };
  }
  return { pass: false, at: 0, sig: "" };
}

async function listJsonFiles(root, relDir, out, depth = 0) {
  if (depth > 4) return;
  let ents = [];
  try {
    ents = await fs.readdir(path.join(root, ...relDir.split("/")), { withFileTypes: true });
  } catch {
    return;
  }
  for (const e of ents) {
    const rel = `${relDir}/${e.name}`;
    if (e.isDirectory()) await listJsonFiles(root, rel, out, depth + 1);
    else if (e.isFile() && e.name.toLowerCase().endsWith(".json")) out.push(rel);
  }
}

async function newestFresh(root, rels, since) {
  const rows = [];
  for (const rel of rels) {
    try {
      const st = await fs.stat(path.join(root, ...rel.split("/")));
      rows.push({ rel, mtimeMs: st.mtimeMs });
    } catch {
      /* */
    }
  }
  rows.sort((a, b) => b.mtimeMs - a.mtimeMs);
  return { newest: rows[0] || null, fresh: rows.filter((r) => r.mtimeMs >= since) };
}

async function readJson(root, rel) {
  try {
    return JSON.parse((await fs.readFile(path.join(root, ...rel.split("/")), "utf8")).replace(/^﻿/, ""));
  } catch {
    return undefined;
  }
}

function checksPass(json) {
  if (!json || typeof json !== "object") return false;
  if (json.pass === true || json.ok === true || /^pass/i.test(String(json.result || json.status || ""))) {
    const list = Array.isArray(json.checks) ? json.checks : null;
    return !list || list.every((c) => c?.pass !== false && c?.ok !== false);
  }
  if (Array.isArray(json.checks) && json.checks.length) {
    return json.checks.every((c) => c?.pass === true || c?.ok === true);
  }
  return false;
}

/**
 * @param {string} root
 * @param {string} kind goldpath|lint|review|assembly|intake
 * @param {number} since evidence must be newer than this (ms epoch; 0 = any age)
 * @param {{ runLint?: (root: string) => { ok: boolean, errors: number, detail: string } }} [opts]
 * @returns {Promise<{ kind: string, ok: boolean, detail: string, file?: string }>}
 */
export async function checkEvidence(root, kind, since, opts = {}) {
  const ageNote = since ? ` newer than ${new Date(since).toISOString()}` : "";
  if (kind === "goldpath") {
    const dirs = [];
    try {
      for (const e of await fs.readdir(path.join(root, ...GOLDPATH_DIR.split("/")), { withFileTypes: true })) {
        if (e.isDirectory()) dirs.push(`${GOLDPATH_DIR}/${e.name}/checks.json`);
      }
    } catch {
      /* */
    }
    const { fresh } = await newestFresh(root, dirs, since);
    if (!fresh.length) {
      return { kind, ok: false, detail: `no ${GOLDPATH_DIR}/<UTC>/checks.json${ageNote} (run the GoldPathDriver)` };
    }
    const top = fresh[0];
    const json = await readJson(root, top.rel);
    if (!checksPass(json)) return { kind, ok: false, file: top.rel, detail: `${top.rel} is not pass=true` };
    return { kind, ok: true, file: top.rel, detail: `${top.rel} pass=true` };
  }
  if (kind === "lint") {
    const res = (opts.runLint || runRuntimeLint)(root);
    return { kind, ok: res.ok, detail: res.detail };
  }
  if (kind === "review") {
    // Only `<REVIEW_DIR>/**/review.json` written by the fresh-context reviewer. Implementer files
    // (review-pack/, areas.json, reports/*.md) never satisfy the gate.
    const files = [];
    await listJsonFiles(root, REVIEW_DIR, files);
    const reviews = files.filter((rel) => rel.split("/").pop().toLowerCase() === REVIEW_FILE);
    const { fresh } = await newestFresh(root, reviews, since);
    if (!fresh.length) {
      return { kind, ok: false, detail: `no ${REVIEW_DIR}/<UTC>/${REVIEW_FILE}${ageNote} (run /independent-review in a fresh session)` };
    }
    const top = fresh[0];
    const verdict = reviewVerdict(await readJson(root, top.rel), { threshold: opts.reviewThreshold !== false });
    return { kind, ok: verdict.ok, file: top.rel, detail: `${top.rel}: ${verdict.detail}` };
  }
  if (kind === "hierarchy") {
    const { fresh } = await newestFresh(root, [HIERARCHY_DIFF], since);
    if (!fresh.length) return { kind, ok: false, detail: `${HIERARCHY_DIFF} missing${ageNote} (run HierarchyDiffRunner)` };
    const json = await readJson(root, HIERARCHY_DIFF);
    if (json?.pass !== true) return { kind, ok: false, detail: `${HIERARCHY_DIFF} is not pass=true` };
    return { kind, ok: true, detail: `${HIERARCHY_DIFF} pass=true` };
  }
  if (kind === "assembly") {
    const { fresh } = await newestFresh(root, [ASSEMBLY_REPORT], since);
    if (!fresh.length) return { kind, ok: false, detail: `${ASSEMBLY_REPORT} missing${ageNote}` };
    const json = await readJson(root, ASSEMBLY_REPORT);
    if (!json || typeof json !== "object") return { kind, ok: false, detail: `${ASSEMBLY_REPORT} unreadable` };
    if (json.pass !== true) return { kind, ok: false, detail: `${ASSEMBLY_REPORT}: pass is not true` };
    const errs = Array.isArray(json.errors) ? json.errors.length : Number(json.errors ?? 0) || 0;
    if (errs > 0) return { kind, ok: false, detail: `${ASSEMBLY_REPORT}: ${errs} error(s)` };
    const missing =
      json.missing_refs ?? json.missingRefs ?? json.counts?.missing_refs ?? json.summary?.missing_refs ?? json.summary?.missingRefs ?? 0;
    const n = Array.isArray(missing) ? missing.length : Number(missing) || 0;
    if (n > 0) return { kind, ok: false, detail: `${ASSEMBLY_REPORT}: ${n} missing reference(s)` };
    return { kind, ok: true, detail: `${ASSEMBLY_REPORT}: pass=true, 0 errors, 0 missing refs` };
  }
  if (kind === "intake") {
    const ok =
      fsSync.existsSync(path.join(root, "Docs", "Generated", "json")) &&
      fsSync.existsSync(path.join(root, "Docs", "V57", "INTAKE_REPORT.md"));
    return { kind, ok, detail: ok ? "Docs/Generated/json + INTAKE_REPORT.md present" : "intake output missing (Docs/Generated/json, Docs/V57/INTAKE_REPORT.md)" };
  }
  return { kind, ok: true, detail: "no evidence rule" };
}

/**
 * Independent review verdict (unity-independent-review-skill `review.json` schema):
 * non-empty `reviewer`, `areas[]` non-empty, every area `score` ≥ `min_score` (default 7).
 * @param {any} json
 * @param {{ threshold?: boolean }} [opts] threshold=false: M4 after M3 `failed-timeboxed` — review must exist, scores may be below
 */
export function reviewVerdict(json, { threshold = true } = {}) {
  if (!json || typeof json !== "object") return { ok: false, detail: "unreadable review.json" };
  const reviewer = String(json.reviewer ?? "").trim();
  if (!reviewer) return { ok: false, detail: "review.json has no reviewer" };
  if (json.independent === false || /^self$/i.test(reviewer)) return { ok: false, detail: "review is not independent" };
  const areas = Array.isArray(json.areas) ? json.areas : [];
  if (!areas.length) return { ok: false, detail: "review.json has no areas[]" };
  const min = Number(json.min_score ?? DEFAULT_MIN_SCORE);
  const bad = areas.filter((a) => typeof a?.score !== "number" || Number.isNaN(a.score));
  if (bad.length) return { ok: false, detail: `${bad.length} area(s) without a numeric score` };
  if (!threshold) return { ok: true, detail: `reviewed by ${reviewer} (${areas.length} areas; M3 timeboxed, threshold not applied)` };
  const below = areas.filter((a) => a.score < min);
  if (below.length) {
    return {
      ok: false,
      detail: `${below.length} area(s) below min_score ${min}: ${below.map((a) => `${a.area ?? "?"}=${a.score}`).join(", ")}`,
    };
  }
  return { ok: true, detail: `reviewed by ${reviewer}, all ${areas.length} areas ≥ ${min}` };
}

/**
 * `node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts [--root Assets/_Game/Tests]` → JSON findings + exit code.
 * @param {string} root
 */
export function runRuntimeLint(root) {
  const script = path.join(root, ...LINT_SCRIPT.split("/"));
  if (!fsSync.existsSync(script)) return { ok: false, errors: -1, detail: `${LINT_SCRIPT} not found` };
  const args = [script, "--root", "Assets/_Game/Scripts"];
  if (fsSync.existsSync(path.join(root, "Assets", "_Game", "Tests"))) args.push("--root", "Assets/_Game/Tests");
  const r = spawnSync(process.execPath, args, {
    cwd: root,
    encoding: "utf8",
    timeout: 180_000,
    maxBuffer: 32 * 1024 * 1024,
  });
  if (r.error) return { ok: false, errors: -1, detail: `runtime-lint failed to run: ${r.error.message}` };
  let json = null;
  try {
    json = JSON.parse(String(r.stdout || "").trim());
  } catch {
    const m = String(r.stdout || "").match(/\{[\s\S]*\}\s*$/);
    if (m) {
      try {
        json = JSON.parse(m[0]);
      } catch {
        /* */
      }
    }
  }
  let errors = null;
  if (json) {
    errors =
      json.errors ??
      json.errorCount ??
      json.summary?.errors ??
      (Array.isArray(json.findings)
        ? json.findings.filter((f) => String(f?.severity || "error").toLowerCase() === "error").length
        : Array.isArray(json)
          ? json.filter((f) => String(f?.severity || "error").toLowerCase() === "error").length
          : null);
    if (Array.isArray(errors)) errors = errors.length;
  }
  if (errors == null) errors = r.status === 0 ? 0 : -1;
  const ok = r.status === 0 && Number(errors) === 0;
  return {
    ok,
    errors: Number(errors),
    detail: ok ? "runtime-lint: 0 errors" : `runtime-lint: exit ${r.status}, ${errors < 0 ? "unparseable output" : `${errors} error(s)`}`,
  };
}

/**
 * Verify every gate PASS claimed in STATUS.json that was not already verified.
 *
 * Freshness: a claim that appeared during this turn needs evidence newer than the turn start
 * (minus GAMEFORGE_GATE_EVIDENCE_SLACK_MIN, default 10) — stale evidence from an earlier
 * attempt does not count. Claims inherited from before the run only need the evidence to exist.
 * `status: "done"` is treated as an M4 claim.
 *
 * @param {{ root: string, forgeMode?: string, mode?: "blocking"|"warn", prevGates?: Record<string, any>,
 *   inheritedGates?: Record<string, any>, turnStartedAt?: number, verified?: Set<string>,
 *   runLint?: (root: string) => { ok: boolean, errors: number, detail: string } }} opts
 * @returns {Promise<{ mode: string, checked: object[], rejected: object[], rewritten: boolean, message: string }>}
 */
export async function verifyGateClaims(opts) {
  const { root, prevGates = {}, inheritedGates = {}, turnStartedAt = 0, verified = new Set(), runLint } = opts;
  const mode = opts.mode || resolveGateMode(opts.forgeMode);
  const slackMs = Math.max(0, Number(process.env.GAMEFORGE_GATE_EVIDENCE_SLACK_MIN ?? 10)) * 60_000;
  const status = await readStatusJson(root);
  const result = { mode, checked: [], rejected: [], rewritten: false, message: "" };
  if (!status) return result;
  const json = status.json;
  const gates = json.gates && typeof json.gates === "object" ? json.gates : {};

  const claims = [];
  for (const [gate, value] of Object.entries(gates)) {
    const claim = gateClaim(value);
    if (!claim.pass) continue;
    const key = `${gate}|${claim.sig}`;
    if (verified.has(key)) continue;
    const inherited = gateClaim(inheritedGates[gate]).pass && gateClaim(inheritedGates[gate]).sig === claim.sig;
    const wasPass = gateClaim(prevGates[gate]).pass && gateClaim(prevGates[gate]).sig === claim.sig;
    const since = inherited ? 0 : wasPass ? 0 : Math.max(0, turnStartedAt - slackMs);
    claims.push({ gate, key, since, kinds: evidenceForGate(gate) });
  }
  const statusDone = String(json.status || "").toLowerCase() === "done";
  if (statusDone && !verified.has("__done__")) {
    claims.push({ gate: "status=done", key: "__done__", since: Math.max(0, turnStartedAt - slackMs), kinds: evidenceForGate("M4") });
  }

  let lintCache = null;
  const cachedLint = (r) => (lintCache ||= (runLint || runRuntimeLint)(r));
  const m3 = gates.M3 && typeof gates.M3 === "object" ? gates.M3 : { state: gates.M3 };
  const m3Timeboxed = /timeboxed/i.test(String(m3.state ?? m3.status ?? ""));
  for (const c of claims) {
    const missing = [];
    const found = [];
    // M3 gate always needs every area ≥ min_score; M4/done accept a below-threshold review only
    // after M3 was logged failed-timeboxed (review skill §4.3).
    const reviewThreshold = /^M3/i.test(c.gate) || !m3Timeboxed;
    for (const kind of c.kinds) {
      const ev = await checkEvidence(root, kind, c.since, { runLint: cachedLint, reviewThreshold });
      (ev.ok ? found : missing).push(ev.detail);
    }
    const row = { gate: c.gate, ok: missing.length === 0, missing, found };
    result.checked.push(row);
    if (row.ok) verified.add(c.key);
    else result.rejected.push(row);
  }
  if (!result.rejected.length) return result;

  const lines = [
    mode === "blocking"
      ? "## Gate claims REJECTED by the runner (gates are never self-graded)"
      : "## Gate claims without evidence (warn-only mode)",
  ];
  for (const r of result.rejected) {
    lines.push(`- ${r.gate}: ${r.missing.join("; ")}`);
  }
  if (mode === "blocking") {
    const now = new Date().toISOString();
    const next = { ...json, gates: { ...gates } };
    for (const r of result.rejected) {
      if (r.gate === "status=done") {
        next.status = "running";
        continue;
      }
      // STATUS template gate shape: { state, evidence, at_utc } — no `status` key.
      const prev = gates[r.gate] && typeof gates[r.gate] === "object" ? { ...gates[r.gate] } : {};
      delete prev.status;
      next.gates[r.gate] = {
        ...prev,
        state: "failed",
        reason: `runner: missing evidence — ${r.missing.join("; ")}`,
        claimed: gates[r.gate],
        at_utc: now,
      };
    }
    if (result.rejected.some((r) => r.gate === "status=done")) next.status = "running";
    await writeStatusJson(root, next);
    result.rewritten = true;
    lines.push(
      'STATUS.json was set back (gate → state "failed", done → running). Produce the missing evidence with the real tools ' +
        "(GoldPathDriver checks.json, `node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests`, " +
        "HierarchyDiffRunner hierarchy-diff.json, fresh-session /independent-review review.json), then re-claim. Do not edit tests, review.json or the TDD to make a gate pass.",
    );
  } else {
    lines.push("Warn-only mode: STATUS.json left unchanged — produce the evidence before moving on.");
  }
  result.message = lines.join("\n");
  return result;
}
