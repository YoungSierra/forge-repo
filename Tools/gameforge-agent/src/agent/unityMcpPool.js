import { getUnityMcpResolution, isUnityMcpClientEnabled } from "./mcp-config.js";
import { openUnityMcpSession } from "./unityMcpSession.js";

/** @type {Awaited<ReturnType<typeof openUnityMcpSession>>} */
let shared = null;
/** @type {Promise<Awaited<ReturnType<typeof openUnityMcpSession>>> | null} */
let connecting = null;
let lastError = "";
let lastOkAt = 0;
/** @type {"cli" | "legacy" | null} */
let lastMode = null;

const DISABLED_HINT =
  "Forge Chat Unity MCP is disabled (GAMEFORGE_UNITY_MCP=0). Unset or set to 1 to re-enable.";

function cliHint() {
  return "Install Unity CLI, run `unity pipeline install` in the project, keep the Editor open, then `unity mcp configure cursor`. See V57/docs/mcp/UNITY_CLI_MIGRATION.md.";
}

function legacyHint() {
  return "Legacy relay (deprecated end 2026): Project Settings → AI → Unity MCP → Running, Allow «GameForge Chat». Prefer Unity CLI — see V57/docs/mcp/UNITY_CLI_MIGRATION.md.";
}

/**
 * Reuse one MCP stdio connection for the whole agent process.
 * Opening a new relay per chat turn creates a new Unity client and often
 * flips the previous approval to Revoked.
 */
export async function getSharedUnityMcpSession(projectRoot) {
  if (!isUnityMcpClientEnabled()) {
    lastError = DISABLED_HINT;
    return null;
  }
  if (shared) return shared;
  if (connecting) return connecting;

  connecting = (async () => {
    try {
      const session = await openUnityMcpSession(projectRoot);
      shared = session;
      const resolution = getUnityMcpResolution(projectRoot);
      lastMode = resolution.mode;
      if (!session) {
        lastError =
          resolution.mode === "cli" || resolution.cliAvailable
            ? "Unity CLI MCP not configured — " + cliHint()
            : "Unity MCP relay not configured (check .cursor/mcp.json / ~/.unity/relay)";
      } else {
        lastError = "";
      }
      if (session) lastOkAt = Date.now();
      return session;
    } catch (err) {
      shared = null;
      lastError = String(err?.message || err);
      throw err;
    } finally {
      connecting = null;
    }
  })();

  return connecting;
}

/** Drop the shared session (e.g. after hard failure). Next call reconnects. */
export async function resetSharedUnityMcpSession() {
  const prev = shared;
  shared = null;
  connecting = null;
  if (prev) {
    try {
      await prev.close();
    } catch {
      /* ignore */
    }
  }
}

/**
 * Liveness probe: connect (or reuse) and list tools.
 * Does not call Editor mutation tools.
 */
export async function pingUnityMcp(projectRoot) {
  const started = Date.now();
  const resolution = getUnityMcpResolution(projectRoot);

  if (!isUnityMcpClientEnabled()) {
    await resetSharedUnityMcpSession();
    return {
      ok: false,
      configured: true,
      disabled: true,
      mode: resolution.mode,
      cliAvailable: resolution.cliAvailable,
      toolCount: 0,
      ms: Date.now() - started,
      error: "Unity MCP client disabled in GameForge agent",
      hint: DISABLED_HINT,
    };
  }

  try {
    let session = shared;
    if (!session) {
      session = await getSharedUnityMcpSession(projectRoot);
    } else if (!session.toolCount) {
      await resetSharedUnityMcpSession();
      session = await getSharedUnityMcpSession(projectRoot);
    }

    if (!session) {
      const hint =
        resolution.mode === "legacy" || (!resolution.cliAvailable && resolution.legacyRelayExists)
          ? legacyHint()
          : cliHint();
      return {
        ok: false,
        configured: resolution.configured,
        mode: resolution.mode,
        cliAvailable: resolution.cliAvailable,
        legacyRelayExists: resolution.legacyRelayExists,
        toolCount: 0,
        ms: Date.now() - started,
        error: lastError || "Unity MCP not configured",
        hint,
      };
    }

    lastOkAt = Date.now();
    lastError = "";
    lastMode = resolution.mode ?? lastMode;
    const sample = (session.openaiTools || [])
      .slice(0, 12)
      .map((t) => t.function?.name)
      .filter(Boolean);

    if (session.toolCount === 0) {
      const isCli = lastMode === "cli";
      return {
        ok: false,
        configured: resolution.configured,
        mode: lastMode,
        cliAvailable: resolution.cliAvailable,
        cliVersion: resolution.cliVersion,
        pipelineInstalled: resolution.pipelineInstalled,
        legacyRelayExists: resolution.legacyRelayExists,
        toolCount: 0,
        sampleTools: sample,
        ms: Date.now() - started,
        error: isCli
          ? "Unity CLI MCP connected but no tools (Editor not open on this project or Pipeline not running)"
          : "Unity MCP connected but no tools listed",
        hint: isCli
          ? "Open Unity on THIS project (see projectRoot), ensure com.unity.pipeline is loaded (`unity pipeline list` → Pipeline true, Server Reachable true), then re-run Test MCP."
          : legacyHint(),
      };
    }

    const fewTools = session.toolCount > 0 && session.toolCount < 15;
    const isLegacy = lastMode === "legacy";
    return {
      ok: true,
      configured: true,
      mode: lastMode,
      cliAvailable: resolution.cliAvailable,
      cliVersion: resolution.cliVersion,
      pipelineInstalled: resolution.pipelineInstalled,
      legacyRelayExists: resolution.legacyRelayExists,
      toolCount: session.toolCount,
      sampleTools: sample,
      ms: Date.now() - started,
      lastOkAt,
      clientName: "GameForge Chat",
      capacityWarning: isLegacy && fewTools,
      hint: isLegacy
        ? fewTools
          ? "Legacy MCP: few tools — Capacity limit and/or Cursor also holding unity-mcp. Migrate to Unity CLI."
          : "Legacy MCP (deprecated). Migrate: " + cliHint()
        : fewTools
          ? "Few tools visible — ensure Editor is open on this project and com.unity.pipeline is installed."
          : "Unity CLI MCP connected. Editor must stay open on this project.",
    };
  } catch (err) {
    const msg = String(err?.message || err);
    lastError = msg;
    const revoked = /revok|approv|pending|denied|unauthorized/i.test(msg);
    const isLegacy = resolution.mode === "legacy";
    return {
      ok: false,
      configured: resolution.configured,
      mode: resolution.mode,
      cliAvailable: resolution.cliAvailable,
      toolCount: 0,
      ms: Date.now() - started,
      error: msg.slice(0, 400),
      revoked,
      hint: revoked && isLegacy
        ? "Legacy: Allow «GameForge Chat» in Project Settings → AI → Unity MCP, or migrate to Unity CLI."
        : revoked
          ? "Reconnect: ensure Editor is open; re-run Test MCP."
          : isLegacy
            ? legacyHint()
            : cliHint(),
    };
  }
}

export function getUnityMcpStatusSnapshot() {
  return {
    connected: !!shared,
    toolCount: shared?.toolCount || 0,
    mode: lastMode,
    lastOkAt,
    lastError: lastError || "",
  };
}
