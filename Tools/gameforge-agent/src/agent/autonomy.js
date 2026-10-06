/**
 * Autonomous stage loop for VerticalSlice / Production runs.
 *
 * After each agent turn the runner:
 *   1. audits the files the turn touched (writeAudit — provider files reverted/flagged, TDD edit = hard stop),
 *   2. verifies gate PASS claims in Docs/V57/STATUS.json against evidence (blocking or warn),
 *   3. re-reads the run state (STATUS.json, fallback: legacy GF-PROGRESS line),
 *   4. decides: stop, or send the next-stage continuation (re-anchor prompt + findings).
 *
 * Stop reasons (first match wins):
 *   cancelled · hard-stop:<reason> · owner-stop-token · done · blocked · max-turns · max-hours · stalled
 *
 * Config (env, overridable per request):
 *   GAMEFORGE_AUTONOMY=auto|staged     default: auto for VerticalSlice and Production;
 *                                      staged only when explicitly requested (Prototype never loops)
 *   GAMEFORGE_AUTO_MAX_TURNS=60        agent turns per run, including the first
 *   GAMEFORGE_AUTO_MAX_HOURS=12        wall clock per run
 *   GAMEFORGE_AUTO_MAX_STALLS=3        consecutive turns without a STATUS.json progress change
 *                                      (timestamps ignored; measured before the gate verifier rewrites it)
 */

import { hasOwnerStopToken, readRunState } from "./v57State.js";
import { normalizeForgeMode } from "./writePolicy.js";

/**
 * @param {{ forgeMode?: string, op?: string, requested?: string, env?: NodeJS.ProcessEnv }} opts
 */
export function resolveAutonomyConfig({ forgeMode, op = "", requested = "", env = process.env } = {}) {
  const mode = normalizeForgeMode(forgeMode);
  const want = String(requested || env.GAMEFORGE_AUTONOMY || "").trim().toLowerCase();
  let enabled;
  if (mode === "Prototype") enabled = false;
  else if (["auto", "autonomous", "on", "true", "1"].includes(want)) enabled = true;
  else if (["staged", "off", "false", "0", "manual"].includes(want)) enabled = false;
  else enabled = mode === "VerticalSlice" || mode === "Production";
  const num = (v, d) => {
    const n = Number(v);
    return Number.isFinite(n) && n > 0 ? n : d;
  };
  return {
    enabled,
    forgeMode: mode,
    maxTurns: Math.floor(num(env.GAMEFORGE_AUTO_MAX_TURNS, 60)),
    maxHours: num(env.GAMEFORGE_AUTO_MAX_HOURS, 12),
    maxStalls: Math.floor(num(env.GAMEFORGE_AUTO_MAX_STALLS, 3)),
  };
}

/** `HARD-STOP: <reason>` / `GF-HARD-STOP <reason>` line in the agent's final message. */
export function detectHardStopInText(text) {
  const m = /(?:^|\n)\s*(?:\*\*)?(?:GF-)?HARD[-_ ]STOP(?:\*\*)?\s*[:=-]?\s*(.{0,300})/i.exec(String(text || ""));
  if (!m) return "";
  return m[1].trim().replace(/\*+$/, "") || "hard stop";
}

/**
 * Pure decision function (unit-tested).
 * @param {{ state: object, cancelled?: boolean, hardStop?: string, ownerStop?: boolean,
 *   turns: number, startedAt: number, now: number, stalls: number, config: ReturnType<typeof resolveAutonomyConfig> }} ctx
 * @returns {{ stop: boolean, reason: string }}
 */
export function decideNext(ctx) {
  const { state, config } = ctx;
  if (ctx.cancelled) return { stop: true, reason: "cancelled" };
  if (ctx.hardStop) return { stop: true, reason: `hard-stop: ${ctx.hardStop}` };
  if (state?.hardStop) return { stop: true, reason: `hard-stop: ${state.hardStop}` };
  if (ctx.ownerStop) return { stop: true, reason: "owner-stop-token (.v57/ALLOW_STOP)" };
  if (state?.status === "done") return { stop: true, reason: "done" };
  if (state?.status === "blocked") return { stop: true, reason: `blocked${state.next ? `: ${state.next}` : ""}` };
  if (ctx.turns >= config.maxTurns) return { stop: true, reason: `max-turns (${config.maxTurns})` };
  if (ctx.now - ctx.startedAt >= config.maxHours * 3_600_000) {
    return { stop: true, reason: `max-hours (${config.maxHours})` };
  }
  if (ctx.stalls >= config.maxStalls) {
    return { stop: true, reason: `stalled (${ctx.stalls} turns without a STATUS.json / progress change)` };
  }
  return { stop: false, reason: "" };
}

/**
 * Mutable, JSON-safe status exposed via session activity / events.
 */
export function createAutonomyStatus(config) {
  return {
    enabled: !!config?.enabled,
    active: false,
    turns: 0,
    maxTurns: config?.maxTurns || 0,
    maxHours: config?.maxHours || 0,
    startedAt: 0,
    lastTurnAt: 0,
    lastStage: "",
    lastStatus: "",
    next: "",
    nextSpec: [],
    stateSource: "none",
    stopReason: "",
    gateMode: "",
    auditMode: "",
    lastViolations: 0,
    lastGateRejections: 0,
  };
}

/**
 * Drive turns until a stop condition. The first turn has already run (its result is passed in),
 * so this only post-processes it and then loops.
 *
 * @param {{
 *   root: string,
 *   config: ReturnType<typeof resolveAutonomyConfig>,
 *   status: ReturnType<typeof createAutonomyStatus>,
 *   signal?: AbortSignal,
 *   onEvent?: (ev: object) => void,
 *   initialState: object,
 *   firstTurn: { result: any, text: string, startedAt: number, audit: { end: () => Promise<any> } | null },
 *   runTurn: (ctx: { turn: number, state: object, warnings: string[] }) => Promise<{ result: any, text: string, startedAt: number, audit: { end: () => Promise<any> } | null }>,
 *   verifyGates?: (ctx: { state: object, prevState: object, turnStartedAt: number }) => Promise<{ rejected: object[], message: string, mode: string }>,
 *   now?: () => number,
 * }} opts
 * @returns {Promise<{ stopReason: string, turns: number, lastResult: any, state: object }>}
 */
export async function runAutonomousLoop(opts) {
  const { root, config, status, signal, onEvent, runTurn, verifyGates } = opts;
  const now = opts.now || Date.now;
  status.active = true;
  status.startedAt = status.startedAt || opts.firstTurn.startedAt || now();
  status.turns = Math.max(1, status.turns || 0);
  status.stopReason = "";

  let prevState = opts.initialState;
  let turnOut = opts.firstTurn;
  let stalls = 0;
  let lastResult = turnOut.result;
  let state = prevState;
  /** Progress fingerprint of the agent's own STATUS.json (before the gate verifier rewrites it). */
  let lastProgress = prevState?.progressHash ?? prevState?.hash ?? "";

  const emit = (extra = {}) =>
    onEvent?.({ type: "autonomy", ...JSON.parse(JSON.stringify(status)), ...extra });

  try {
    for (;;) {
      lastResult = turnOut.result;
      const warnings = [];
      let hardStop = "";
      const cancelled = !!signal?.aborted || lastResult?.status === "cancelled" || !!lastResult?.resumable;

      // 1. write audit
      if (turnOut.audit) {
        try {
          const audit = await turnOut.audit.end();
          status.auditMode = audit.mode;
          status.lastViolations = audit.violations.length;
          if (audit.violations.length || audit.hardStop) {
            onEvent?.({
              type: "status",
              message: `Write audit · ${audit.violations.length} violation(s)${audit.hardStop ? " · HARD STOP" : ""}`,
            });
            onEvent?.({ type: "write-audit", violations: audit.violations, hardStop: audit.hardStop });
          }
          if (audit.warningText) warnings.push(audit.warningText);
          if (audit.hardStop) hardStop = audit.hardStop;
        } catch (err) {
          onEvent?.({ type: "status", message: `Write audit failed: ${err?.message || err}` });
        }
      }

      // 2. stall fingerprint of what the agent wrote, then gate verification (skip on cancel)
      const agentState = await readRunState(root);
      const agentProgress = agentState.progressHash ?? agentState.hash ?? "";
      if (agentProgress && agentProgress !== lastProgress) stalls = 0;
      else stalls += 1;
      lastProgress = agentProgress;
      if (!cancelled && verifyGates) {
        try {
          const cur = agentState;
          const gates = await verifyGates({ state: cur, prevState, turnStartedAt: turnOut.startedAt });
          status.gateMode = gates.mode;
          status.lastGateRejections = gates.rejected.length;
          if (gates.rejected.length) {
            onEvent?.({
              type: "status",
              message: `Gate check (${gates.mode}) · rejected ${gates.rejected.map((r) => r.gate).join(", ")}`,
            });
            onEvent?.({ type: "gate-check", mode: gates.mode, rejected: gates.rejected });
            if (gates.message) warnings.push(gates.message);
          }
        } catch (err) {
          onEvent?.({ type: "status", message: `Gate check failed: ${err?.message || err}` });
        }
      }

      // 3. state
      state = await readRunState(root);
      const textStop = detectHardStopInText(turnOut.text);
      if (textStop && !hardStop) hardStop = textStop;
      status.lastTurnAt = now();
      status.lastStage = state.stage || status.lastStage;
      status.lastStatus = state.status;
      status.next = state.next;
      status.nextSpec = state.nextSpec;
      status.stateSource = state.source;

      // 4. decide
      const decision = decideNext({
        state,
        cancelled,
        hardStop,
        ownerStop: hasOwnerStopToken(root),
        turns: status.turns,
        startedAt: status.startedAt,
        now: now(),
        stalls,
        config,
      });
      if (decision.stop) {
        status.stopReason = decision.reason;
        emit({ stopped: true });
        onEvent?.({ type: "status", message: `Autonomous run stopped · ${decision.reason} · ${status.turns} turn(s)` });
        return { stopReason: decision.reason, turns: status.turns, lastResult, state };
      }

      status.turns += 1;
      emit();
      onEvent?.({
        type: "status",
        message: `Autonomous · turn ${status.turns}/${config.maxTurns} · stage ${state.stage || "?"} → ${String(state.next || "next stage").slice(0, 120)}`,
      });
      prevState = state;
      turnOut = await runTurn({ turn: status.turns, state, warnings });
    }
  } finally {
    status.active = false;
  }
}
