import { execSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

/** Legacy in-Editor MCP server id (relay + com.unity.ai.assistant). */
export const UNITY_MCP_SERVER_KEY = "unity-mcp";

/** Unity CLI MCP server id (unity mcp configure cursor). */
export const UNITY_CLI_MCP_SERVER_KEY = "unity-editor-mcp";

export const UNITY_MCP_SERVER_KEYS = [UNITY_MCP_SERVER_KEY, UNITY_CLI_MCP_SERVER_KEY];

/**
 * Kill switch for Forge Chat / sidecar Unity MCP clients.
 * Default ON. Set GAMEFORGE_UNITY_MCP=0 (or false/off) to disable
 * so Cursor can hold the single Unity MCP seat alone.
 */
export function isUnityMcpClientEnabled() {
  const v = String(process.env.GAMEFORGE_UNITY_MCP || "1").trim().toLowerCase();
  return !(v === "0" || v === "false" || v === "off" || v === "no");
}

/** @returns {"auto" | "cli" | "legacy"} */
export function getUnityMcpModePreference() {
  const v = String(process.env.GAMEFORGE_UNITY_MCP_MODE || "auto").trim().toLowerCase();
  if (v === "cli" || v === "legacy") return v;
  return "auto";
}

export function resolveOfficialRelayPath() {
  const fromEnv = process.env.UNITY_MCP_RELAY?.trim();
  if (fromEnv) return fromEnv;

  const home = os.homedir();
  const relayDir = path.join(home, ".unity", "relay");
  if (process.platform === "win32") {
    return path.join(relayDir, "relay_win.exe");
  }
  if (process.platform === "darwin") {
    const arch = process.arch === "arm64" ? "arm64" : "x64";
    return path.join(
      relayDir,
      `relay_mac_${arch}.app`,
      "Contents",
      "MacOS",
      `relay_mac_${arch}`,
    );
  }
  return path.join(relayDir, "relay_linux");
}

/** @returns {string | null} */
export function resolveUnityCliCommand() {
  const fromEnv = process.env.UNITY_CLI_PATH?.trim();
  if (fromEnv) return fromEnv;

  try {
    const cmd =
      process.platform === "win32"
        ? execSync("where unity", { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] })
        : execSync("which unity", { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
    const first = cmd
      .split(/\r?\n/)
      .map((l) => l.trim())
      .find(Boolean);
    return first || null;
  } catch {
    return null;
  }
}

/** @returns {boolean} */
export function isUnityCliAvailable() {
  return !!resolveUnityCliCommand();
}

/** @returns {string | null} */
export function getUnityCliVersion() {
  const unity = resolveUnityCliCommand();
  if (!unity) return null;
  try {
    const out = execSync(`"${unity}" --version`, {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
    });
    return out.trim().split(/\r?\n/)[0] || null;
  } catch {
    return null;
  }
}

/** @param {string} [projectRoot] */
export function isPipelinePackageInstalled(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const manifestPath = path.join(root, "Packages", "manifest.json");
  if (!fs.existsSync(manifestPath)) return false;
  try {
    const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
    return !!manifest?.dependencies?.["com.unity.pipeline"];
  } catch {
    return false;
  }
}

/**
 * @param {object} entry
 * @returns {"cli" | "legacy" | null}
 */
function classifyMcpEntry(entry) {
  if (!entry?.command) return null;
  const cmd = String(entry.command).replace(/\\/g, "/").toLowerCase();
  const args = (entry.args ?? []).map((a) => String(a).toLowerCase());
  const isAnkleBreaker =
    cmd.includes("unity-mcp-server") || cmd.includes("tools/unity-mcp-server");

  if (isAnkleBreaker) return null;

  const isCli =
    cmd === "unity" ||
    cmd.endsWith("/unity") ||
    cmd.endsWith("\\unity") ||
    cmd.endsWith("/unity.exe") ||
    cmd.endsWith("\\unity.exe");
  if (isCli && args.includes("mcp")) return "cli";

  const hasMcpFlag = args.some((a) => a === "--mcp");
  const isRelay =
    cmd.includes("relay_win") ||
    cmd.includes("relay_linux") ||
    cmd.includes("relay_mac") ||
    cmd.includes("/.unity/relay/");
  if (hasMcpFlag && isRelay) return "legacy";

  return null;
}

/** @deprecated use classifyMcpEntry */
function looksLikeOfficialUnityMcp(entry) {
  return classifyMcpEntry(entry) === "legacy";
}

/**
 * @param {string} mcpPath
 * @returns {{ entry: object, mode: "cli" | "legacy" } | null}
 */
function readMcpEntryFromFile(mcpPath) {
  if (!fs.existsSync(mcpPath)) return null;
  try {
    const parsed = JSON.parse(fs.readFileSync(mcpPath, "utf8"));
    const servers = parsed.mcpServers ?? {};
    for (const key of UNITY_MCP_SERVER_KEYS) {
      const entry = servers[key];
      const mode = classifyMcpEntry(entry);
      if (mode) return { entry, mode };
    }
    for (const entry of Object.values(servers)) {
      const mode = classifyMcpEntry(entry);
      if (mode) return { entry, mode };
    }
    return null;
  } catch {
    return null;
  }
}

/**
 * @param {string} projectRoot
 * @returns {{ servers: object, mode: "cli" | "legacy" } | null}
 */
function buildCliMcpConfig(projectRoot) {
  const unity = resolveUnityCliCommand();
  if (!unity) return null;

  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  return {
    mode: "cli",
    servers: {
      [UNITY_MCP_SERVER_KEY]: {
        type: "stdio",
        command: unity,
        args: ["mcp"],
        env: {
          ...(process.env.UNITY_PROJECT_PATH ? {} : { UNITY_PROJECT_PATH: root }),
        },
      },
    },
  };
}

function buildLegacyRelayConfig() {
  const relay = resolveOfficialRelayPath();
  if (!fs.existsSync(relay)) return null;
  return {
    mode: "legacy",
    servers: {
      [UNITY_MCP_SERVER_KEY]: {
        type: "stdio",
        command: relay,
        args: ["--mcp"],
        env: {},
      },
    },
  };
}

/**
 * Resolve Unity MCP stdio config (ignores GAMEFORGE_UNITY_MCP kill switch).
 * Preference (auto): mcp.json CLI → auto CLI → mcp.json legacy → relay legacy.
 * @param {string} [projectRoot]
 * @returns {{ servers: object, mode: "cli" | "legacy" } | null}
 */
function resolveUnityMcpServersConfig(projectRoot) {
  const root = projectRoot || process.env.GAMEFORGE_PROJECT_ROOT || process.cwd();
  const pref = getUnityMcpModePreference();
  const projectMcp = path.join(root, ".cursor", "mcp.json");
  const userMcp = path.join(os.homedir(), ".cursor", "mcp.json");

  const fromFiles = [readMcpEntryFromFile(projectMcp), readMcpEntryFromFile(userMcp)].filter(Boolean);

  if (pref === "cli") {
    for (const hit of fromFiles) {
      if (hit.mode === "cli" && hit.entry?.command) {
        return {
          mode: "cli",
          servers: {
            [UNITY_MCP_SERVER_KEY]: {
              type: "stdio",
              command: hit.entry.command,
              args: hit.entry.args?.length ? hit.entry.args : ["mcp"],
              env: hit.entry.env,
            },
          },
        };
      }
    }
    return buildCliMcpConfig(root);
  }

  if (pref === "legacy") {
    for (const hit of fromFiles) {
      if (hit.mode === "legacy" && hit.entry?.command) {
        return {
          mode: "legacy",
          servers: {
            [UNITY_MCP_SERVER_KEY]: {
              type: "stdio",
              command: hit.entry.command,
              args: hit.entry.args?.length ? hit.entry.args : ["--mcp"],
              env: hit.entry.env,
            },
          },
        };
      }
    }
    return buildLegacyRelayConfig();
  }

  // auto: CLI first
  for (const hit of fromFiles) {
    if (hit.mode === "cli" && hit.entry?.command) {
      return {
        mode: "cli",
        servers: {
          [UNITY_MCP_SERVER_KEY]: {
            type: "stdio",
            command: hit.entry.command,
            args: hit.entry.args?.length ? hit.entry.args : ["mcp"],
            env: hit.entry.env,
          },
        },
      };
    }
  }

  const autoCli = buildCliMcpConfig(root);
  if (autoCli) return autoCli;

  for (const hit of fromFiles) {
    if (hit.mode === "legacy" && hit.entry?.command) {
      return {
        mode: "legacy",
        servers: {
          [UNITY_MCP_SERVER_KEY]: {
            type: "stdio",
            command: hit.entry.command,
            args: hit.entry.args?.length ? hit.entry.args : ["--mcp"],
            env: hit.entry.env,
          },
        },
      };
    }
  }

  return buildLegacyRelayConfig();
}

/**
 * @param {string} [projectRoot]
 * @returns {Record<string, { type: string, command: string, args: string[], env?: object }> | null}
 */
export function loadUnityMcpServers(projectRoot) {
  if (!isUnityMcpClientEnabled()) return null;
  return resolveUnityMcpServersConfig(projectRoot)?.servers ?? null;
}

/** @param {string} [projectRoot] */
export function getUnityMcpResolution(projectRoot) {
  const resolved = resolveUnityMcpServersConfig(projectRoot);
  return {
    configured: !!resolved,
    mode: resolved?.mode ?? null,
    cliAvailable: isUnityCliAvailable(),
    cliVersion: getUnityCliVersion(),
    pipelineInstalled: isPipelinePackageInstalled(projectRoot),
    legacyRelayPath: resolveOfficialRelayPath(),
    legacyRelayExists: fs.existsSync(resolveOfficialRelayPath()),
    preference: getUnityMcpModePreference(),
  };
}

export function isOfficialUnityMcpConfigured(projectRoot) {
  return resolveUnityMcpServersConfig(projectRoot) != null;
}

export { looksLikeOfficialUnityMcp };
