/**
 * On-disk run state for autonomous VerticalSlice / Production runs.
 *
 * Primary:  Docs/V57/STATUS.json  (written by the agent every stage)
 *   { stage, status: "running"|"ready"|"done"|"blocked", next, next_spec,
 *     hard_stop?: string|{reason}, stop_reason?, gates?: { <GateId>: "PASS"|{status, at, evidence} },
 *     updated_at? }
 * Fallback: Docs/V57/reports/game-setup-progress.md  (legacy `GF-PROGRESS stage=… status=…` line)
 *
 * Reads are tolerant: a missing or malformed file yields `null`, never throws.
 */

import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";
import crypto from "node:crypto";

export const STATUS_REL = "Docs/V57/STATUS.json";
export const LEGACY_PROGRESS_REL = "Docs/V57/reports/game-setup-progress.md";
export const OWNER_STOP_TOKEN_REL = ".v57/ALLOW_STOP";

const abs = (root, rel) => path.join(root, ...rel.split("/"));

/** @param {string} root */
export async function readStatusJson(root) {
  try {
    const p = abs(root, STATUS_REL);
    const [raw, st] = await Promise.all([fs.readFile(p, "utf8"), fs.stat(p)]);
    const json = JSON.parse(raw.replace(/^﻿/, ""));
    if (!json || typeof json !== "object" || Array.isArray(json)) return null;
    return { json, raw, mtimeMs: st.mtimeMs };
  } catch {
    return null;
  }
}

/**
 * Parse the last `GF-PROGRESS key=value …` line.
 * @param {string} text
 */
export function parseGfProgress(text) {
  const lines = String(text || "")
    .split(/\r?\n/)
    .filter((l) => /GF-PROGRESS\b/.test(l));
  const line = lines[lines.length - 1];
  if (!line) return null;
  const out = {};
  const re = /(\w[\w-]*)=("([^"]*)"|\S+)/g;
  let m;
  while ((m = re.exec(line))) out[m[1]] = m[3] !== undefined ? m[3] : m[2];
  return out;
}

/** @param {string} root */
export async function readLegacyProgress(root) {
  try {
    const p = abs(root, LEGACY_PROGRESS_REL);
    const [raw, st] = await Promise.all([fs.readFile(p, "utf8"), fs.stat(p)]);
    const parsed = parseGfProgress(raw);
    if (!parsed) return null;
    return { fields: parsed, raw, mtimeMs: st.mtimeMs };
  } catch {
    return null;
  }
}

function normStatus(s) {
  const v = String(s || "").trim().toLowerCase();
  if (!v) return "";
  if (["done", "complete", "completed", "finished"].includes(v)) return "done";
  // A failed gate or stage is NOT a stop condition — only explicit blocked/stopped/hard stop is.
  if (["blocked", "stopped", "hard-stop", "hard_stop"].includes(v)) return "blocked";
  if (["ready", "stage-complete", "stage_complete"].includes(v)) return "ready";
  return v;
}

/** Template-style STATUS.json (stage/stage_state/blockers) without an explicit `status`. */
function deriveStatus(j) {
  if (Array.isArray(j?.blockers) && j.blockers.length) return "blocked";
  const st = String(j?.stage || "").toUpperCase();
  const ss = String(j?.stage_state || "").toLowerCase();
  if (st === "M4" && ss === "passed") return "done";
  if (ss === "stopped") return "blocked";
  if (ss === "passed") return "ready";
  return ss ? "running" : "";
}

function hardStopOf(json) {
  const h = json?.hard_stop ?? json?.hardStop;
  if (!h) return "";
  if (typeof h === "string") return h;
  if (typeof h === "object") return String(h.reason || h.message || "hard stop");
  return "hard stop";
}

/**
 * @param {unknown} v
 * @returns {string[]} spec names/ids (normalized strings, possibly empty)
 */
export function normalizeNextSpec(v) {
  if (!v) return [];
  const list = Array.isArray(v) ? v : String(v).split(/[,;]/);
  return list
    .map((s) => (typeof s === "object" && s ? s.name || s.id || "" : s))
    .map((s) => String(s || "").trim())
    .filter(Boolean);
}

/**
 * Run status. Stop conditions win over the template default `status: "running"`:
 * non-empty `blockers[]` or `stage_state: "stopped"` → blocked.
 */
function statusOf(j) {
  if (Array.isArray(j?.blockers) && j.blockers.length) return "blocked";
  if (String(j?.stage_state || "").toLowerCase() === "stopped") return "blocked";
  return normStatus(j?.status) || deriveStatus(j);
}

/** Keys that change every turn without meaning progress (timestamps, runner bookkeeping). */
const VOLATILE_KEYS = new Set(["updated_utc", "updated_at", "at", "at_utc", "reset_by", "reason", "claimed", "last_run_utc"]);

function stableStringify(v) {
  if (Array.isArray(v)) return `[${v.map(stableStringify).join(",")}]`;
  if (v && typeof v === "object") {
    return `{${Object.keys(v)
      .filter((k) => !VOLATILE_KEYS.has(k))
      .sort()
      .map((k) => `${JSON.stringify(k)}:${stableStringify(v[k])}`)
      .join(",")}}`;
  }
  return JSON.stringify(v ?? null);
}

/**
 * Progress fingerprint of STATUS.json: ignores timestamps and runner bookkeeping, so an agent
 * that only re-claims the same gate PASS (and bumps updated_utc) every turn counts as stalled.
 * @param {object|null} json
 */
export function progressHash(json) {
  return json ? sha1(stableStringify(json)) : "";
}

/**
 * Normalized view of the run state.
 * @param {string} root
 * @returns {Promise<{ source: "status"|"legacy"|"none", stage: string, status: string, next: string,
 *   nextSpec: string[], hardStop: string, gates: Record<string, any>, json: object|null, hash: string,
 *   progressHash: string, mtimeMs: number }>}
 */
export async function readRunState(root) {
  const status = await readStatusJson(root);
  if (status) {
    const j = status.json;
    return {
      source: "status",
      stage: String(j.stage || j.current_stage || ""),
      status: statusOf(j),
      next: typeof j.next === "string" ? j.next : j.next ? JSON.stringify(j.next) : "",
      nextSpec: normalizeNextSpec(j.next_spec ?? j.nextSpec),
      hardStop: hardStopOf(j),
      gates: j.gates && typeof j.gates === "object" ? j.gates : {},
      json: j,
      hash: sha1(status.raw),
      progressHash: progressHash(j),
      mtimeMs: status.mtimeMs,
    };
  }
  const legacy = await readLegacyProgress(root);
  if (legacy) {
    const f = legacy.fields;
    const stage = String(f.stage || "");
    let st = normStatus(f.status);
    if (/^(done|complete|checklist-done)$/i.test(stage)) st = "done";
    return {
      source: "legacy",
      stage,
      status: st,
      next: st === "ready" ? `Continue after stage ${stage}${f.action ? ` (${f.action})` : ""}` : String(f.action || ""),
      nextSpec: normalizeNextSpec(f.spec && f.spec !== "none" ? f.spec : ""),
      hardStop: "",
      gates: {},
      json: null,
      hash: sha1(legacy.raw),
      progressHash: sha1(legacy.raw),
      mtimeMs: legacy.mtimeMs,
    };
  }
  return {
    source: "none",
    stage: "",
    status: "",
    next: "",
    nextSpec: [],
    hardStop: "",
    gates: {},
    json: null,
    hash: "",
    progressHash: "",
    mtimeMs: 0,
  };
}

/** Rewrite STATUS.json (used by the blocking gate verifier). */
export async function writeStatusJson(root, json) {
  const p = abs(root, STATUS_REL);
  await fs.mkdir(path.dirname(p), { recursive: true });
  await fs.writeFile(p, `${JSON.stringify(json, null, 2)}\n`, "utf8");
}

/** Owner stop token: `.v57/ALLOW_STOP` in the game repo. */
export function hasOwnerStopToken(root) {
  try {
    return fsSync.existsSync(abs(root, OWNER_STOP_TOKEN_REL));
  } catch {
    return false;
  }
}

function sha1(text) {
  return crypto.createHash("sha1").update(String(text || "")).digest("hex");
}

/** Does the repo still need intake (Docs/Generated missing)? */
export function needsIntake(root) {
  try {
    const dir = abs(root, "Docs/Generated");
    if (!fsSync.existsSync(dir)) return true;
    return fsSync.readdirSync(dir).filter((n) => !n.startsWith(".")).length === 0;
  } catch {
    return true;
  }
}

/**
 * Knowledge files (`V57/knowledge/*.md`) with a relevance flag for the current stage/spec.
 * @param {string} root
 * @param {{ stage?: string, next?: string, nextSpec?: string[] }} state
 */
export async function listKnowledgeFiles(root, state = {}) {
  let names = [];
  try {
    names = (await fs.readdir(abs(root, "V57/knowledge"))).filter((n) => n.toLowerCase().endsWith(".md")).sort();
  } catch {
    return [];
  }
  const words = [state.stage, state.next, ...(state.nextSpec || [])]
    .join(" ")
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((w) => w.length >= 3);
  const stageWords = stageKeywords(state.stage);
  return names.map((n) => {
    const stem = n.toLowerCase().replace(/\.md$/, "");
    const parts = stem.split(/[^a-z0-9]+/).filter((w) => w.length >= 3);
    const relevant =
      parts.some((p) => words.includes(p) || stageWords.includes(p)) ||
      /^(index|readme|autonomy|pitfalls|lessons)/.test(stem);
    return { rel: `V57/knowledge/${n}`, relevant };
  });
}

function stageKeywords(stage) {
  const s = String(stage || "").toUpperCase();
  if (s.startsWith("I0")) return ["intake", "provider", "assets"];
  if (s.startsWith("I1")) return ["project", "packages", "baseline"];
  if (s.startsWith("I2")) return ["assembly", "import", "materials", "prefabs", "assets"];
  if (s.startsWith("M1")) return ["goldpath", "greybox", "gold", "input", "physics"];
  if (s.startsWith("F")) return ["specs", "tests", "testing", "physics"];
  if (s.startsWith("M2")) return ["scene", "prefabs", "lint", "runtime", "build"];
  if (s.startsWith("M3")) return ["craft", "camera", "lighting", "vfx", "audio", "review"];
  if (s.startsWith("M4")) return ["acceptance", "build", "review"];
  return [];
}
