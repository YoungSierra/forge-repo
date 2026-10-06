import { spawnSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { isPipelinePackageInstalled, resolveUnityCliCommand } from "./mcp-config.js";

/** Kill switch for Pipeline `command` / `eval` / `test` helpers (MCP transport unaffected). */
export function isUnityCliIntegrationEnabled() {
  const v = String(process.env.GAMEFORGE_UNITY_CLI || "1").trim().toLowerCase();
  return !(v === "0" || v === "false" || v === "off" || v === "no");
}

/** Prefer `unity test` over Test Runner MCP when CLI is available. */
export function preferUnityTestViaCli() {
  const v = String(process.env.GAMEFORGE_UNITY_TEST_VIA_CLI ?? "1").trim().toLowerCase();
  return !(v === "0" || v === "false" || v === "off" || v === "no");
}

/**
 * @param {string} raw
 * @returns {object | null}
 */
export function parseCliJsonEnvelope(raw) {
  if (!raw?.trim()) return null;
  const text = String(raw).trim();
  try {
    return JSON.parse(text);
  } catch {
    const start = text.indexOf("{");
    const end = text.lastIndexOf("}");
    if (start >= 0 && end > start) {
      try {
        return JSON.parse(text.slice(start, end + 1));
      } catch {
        return null;
      }
    }
    return null;
  }
}

/**
 * @param {object | null} envelope
 * @returns {unknown}
 */
export function unwrapCliData(envelope) {
  if (!envelope || typeof envelope !== "object") return envelope;
  if ("data" in envelope) return envelope.data;
  return envelope;
}

/**
 * @param {string[]} argv
 * @param {object} [opts]
 * @returns {{ ok: boolean, exitCode: number, stdout: string, stderr: string, envelope: object | null, data: unknown, error?: string }}
 */
export function runUnityCli(argv, opts = {}) {
  const unity = resolveUnityCliCommand();
  if (!unity) {
    return {
      ok: false,
      exitCode: 127,
      stdout: "",
      stderr: "",
      envelope: null,
      data: null,
      error: "Unity CLI not found on PATH (set UNITY_CLI_PATH)",
    };
  }

  const projectRoot = opts.projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const timeoutMs = opts.timeoutMs ?? 120_000;
  const args = [...argv];
  if (opts.json !== false && !args.includes("--format")) {
    args.push("--format", "json");
  }
  if (opts.quiet !== false && !args.includes("--quiet")) {
    args.push("--quiet", "--no-banner");
  }

  const result = spawnSync(unity, args, {
    encoding: "utf8",
    timeout: timeoutMs,
    cwd: opts.cwd || projectRoot,
    shell: false,
    windowsHide: true,
    maxBuffer: 16 * 1024 * 1024,
  });

  const stdout = result.stdout || "";
  const stderr = result.stderr || "";
  const exitCode = result.status ?? (result.error ? 1 : 0);
  const envelope = parseCliJsonEnvelope(stdout) || parseCliJsonEnvelope(stderr);
  const data = unwrapCliData(envelope);
  const cliSuccess = envelope?.success !== false && exitCode === 0;

  return {
    ok: cliSuccess,
    exitCode,
    stdout,
    stderr,
    envelope,
    data,
    error: result.error?.message || (!cliSuccess ? stderr.trim() || stdout.trim() || `exit ${exitCode}` : undefined),
  };
}

function withProjectPath(args, projectRoot) {
  if (args.some((a) => a === "--project-path" || String(a).startsWith("--project-path="))) {
    return args;
  }
  return [...args, "--project-path", projectRoot];
}

/**
 * @param {string} [projectRoot]
 */
export function unityPipelineList(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  return runUnityCli(withProjectPath(["pipeline", "list"], root), { projectRoot: root, timeoutMs: 45_000 });
}

/**
 * @param {string} [projectRoot]
 */
export function unityStatus(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  return runUnityCli(withProjectPath(["status"], root), { projectRoot: root, timeoutMs: 30_000 });
}

/**
 * @param {string} [projectRoot]
 */
export function unityList(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  return runUnityCli(withProjectPath(["list"], root), { projectRoot: root, timeoutMs: 45_000 });
}

/**
 * @param {string} commandName
 * @param {string[]} [commandArgs]
 * @param {object} [opts]
 */
export function unityCommand(commandName, commandArgs = [], opts = {}) {
  const root = opts.projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const args = withProjectPath(["command", commandName, ...commandArgs], root);
  if (opts.timeoutMs) args.push("--timeout", String(opts.timeoutMs));
  return runUnityCli(args, { projectRoot: root, timeoutMs: opts.timeoutMs ?? 120_000 });
}

/**
 * @param {string} code
 * @param {object} [opts]
 */
export function unityEval(code, opts = {}) {
  return unityCommand("eval", [code], { ...opts, timeoutMs: opts.timeoutMs ?? 90_000 });
}

/**
 * @param {string} filePath
 * @param {object} [opts]
 */
export function unityEvalFile(filePath, opts = {}) {
  return unityCommand("eval_file", [filePath], { ...opts, timeoutMs: opts.timeoutMs ?? 90_000 });
}

/**
 * @param {object} [opts]
 */
export function unityTest(opts = {}) {
  const root = opts.projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const args = ["test", root];
  if (opts.mode) args.push("--mode", opts.mode);
  if (opts.filter) args.push("--filter", opts.filter);
  const output =
    opts.output ||
    path.join(root, "V57", "docs", "reports", `unity-test-${opts.mode || "default"}-${Date.now()}.xml`);
  fs.mkdirSync(path.dirname(output), { recursive: true });
  args.push("--output", output);
  if (opts.reportFormat) args.push("--report-format", opts.reportFormat);
  if (opts.junitOutput) args.push("--junit-output", opts.junitOutput);
  if (opts.coverage) args.push("--coverage");
  if (opts.timeoutMs) args.push("--timeout", String(Math.ceil(opts.timeoutMs / 1000)));
  return runUnityCli(args, { projectRoot: root, timeoutMs: opts.timeoutMs ?? 600_000 });
}

/** Poll `recompile_status` until completed or timeout. */
export async function unityWaitRecompile(projectRoot, { timeoutMs = 120_000, pollMs = 500 } = {}) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const started = Date.now();
  unityCommand("recompile", [], { projectRoot: root, timeoutMs: 30_000 });
  while (Date.now() - started < timeoutMs) {
    const status = unityCommand("recompile_status", [], { projectRoot: root, timeoutMs: 15_000 });
    const state =
      typeof status.data === "string"
        ? status.data
        : status.data?.status || status.data?.state || status.envelope?.data;
    if (state === "completed" || state === "idle" || state === "done") {
      return { ok: true, elapsedMs: Date.now() - started, status: state, last: status };
    }
    if (state === "failed" || state === "error") {
      return { ok: false, elapsedMs: Date.now() - started, status: state, last: status };
    }
    await new Promise((r) => setTimeout(r, pollMs));
  }
  return { ok: false, elapsedMs: Date.now() - started, status: "timeout", last: null };
}

/**
 * Combined preflight: CLI on PATH, pipeline package, editor reachable, eval probe optional.
 * @param {string} [projectRoot]
 */
export async function unityCliPreflight(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const cliPath = resolveUnityCliCommand();
  const integrationEnabled = isUnityCliIntegrationEnabled();
  const pipelineInstalled = isPipelinePackageInstalled(root);

  const base = {
    integrationEnabled,
    cliAvailable: !!cliPath,
    cliPath,
    pipelineInstalled,
    projectRoot: root,
    preferTestViaCli: preferUnityTestViaCli(),
  };

  if (!integrationEnabled || !cliPath) {
    return { ...base, ok: false, reason: "cli_disabled_or_missing" };
  }
  if (!pipelineInstalled) {
    return { ...base, ok: false, reason: "pipeline_not_installed" };
  }

  const pipeline = unityPipelineList(root);
  const status = unityStatus(root);
  const instances = pipeline.data?.instances ?? pipeline.envelope?.data?.instances ?? [];
  const summary = pipeline.data?.summary ?? pipeline.envelope?.data?.summary ?? {};
  const safeModeCount = summary.instancesInSafeMode ?? 0;
  const reachable = instances.some(
    (i) =>
      i?.serverReachable === true ||
      i?.pipeline?.serverReachable === true ||
      String(i?.state || "").toLowerCase() === "ready",
  );

  let evalProbe = null;
  if (reachable && safeModeCount === 0) {
    evalProbe = unityEval('return Application.unityVersion;', { projectRoot: root, timeoutMs: 45_000 });
  }

  const ok =
    pipeline.ok &&
    safeModeCount === 0 &&
    reachable &&
    (!evalProbe || evalProbe.ok);

  return {
    ...base,
    ok,
    reason: ok
      ? "ready"
      : safeModeCount > 0
        ? "safe_mode"
        : !reachable
          ? "editor_unreachable"
          : "pipeline_check_failed",
    pipeline,
    status,
    evalProbe,
    safeModeCount,
    reachable,
  };
}
