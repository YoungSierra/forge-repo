import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";
import { loadUnityMcpServers, UNITY_MCP_SERVER_KEY } from "./mcp-config.js";

/**
 * OpenAI function names must be [a-zA-Z0-9_-]. Unity MCP uses dots (Unity.RunCommand).
 */
export function toOpenAiToolName(mcpName) {
  return String(mcpName || "")
    .replace(/\./g, "_")
    .replace(/[^a-zA-Z0-9_-]/g, "_")
    .slice(0, 64);
}

function contentToText(result) {
  if (!result) return "";
  if (typeof result === "string") return result;
  const parts = [];
  if (Array.isArray(result.content)) {
    for (const block of result.content) {
      if (!block) continue;
      if (block.type === "text" && block.text) parts.push(block.text);
      else if (block.type === "resource" && block.resource?.text) parts.push(block.resource.text);
      else parts.push(JSON.stringify(block));
    }
  }
  if (result.structuredContent != null) {
    try {
      parts.push(JSON.stringify(result.structuredContent));
    } catch {
      /* ignore */
    }
  }
  if (parts.length) return parts.join("\n");
  try {
    return JSON.stringify(result);
  } catch {
    return String(result);
  }
}

/**
 * Connect to official Unity MCP relay and expose tools to any LLM provider.
 * @param {string} [projectRoot]
 * @returns {Promise<null | {
 *   toolCount: number,
 *   openaiTools: object[],
 *   hasOpenAiName: (name: string) => boolean,
 *   callOpenAiTool: (name: string, args: object) => Promise<string>,
 *   close: () => Promise<void>,
 * }>}
 */
export async function openUnityMcpSession(projectRoot) {
  const servers = loadUnityMcpServers(projectRoot);
  if (!servers?.[UNITY_MCP_SERVER_KEY]) return null;

  const cfg = servers[UNITY_MCP_SERVER_KEY];
  const transport = new StdioClientTransport({
    command: cfg.command,
    args: cfg.args?.length ? cfg.args : ["--mcp"],
    env: { ...process.env, ...(cfg.env || {}) },
    stderr: "pipe",
  });

  const client = new Client({ name: "GameForge Chat", version: "0.1.0" });
  await client.connect(transport);

  const listed = await client.listTools();
  const mcpTools = listed?.tools || [];
  /** @type {Map<string, string>} openaiName -> mcpName */
  const openaiToMcp = new Map();
  /** @type {Set<string>} */
  const openaiNames = new Set();

  const openaiTools = [];
  for (const tool of mcpTools) {
    const mcpName = tool.name;
    if (!mcpName) continue;
    let openaiName = toOpenAiToolName(mcpName);
    // Avoid collisions with file tools
    if (openaiName === "list_dir" || openaiName === "read_file" || openaiName === "write_file") {
      openaiName = `Unity_${openaiName}`;
    }
    if (openaiNames.has(openaiName)) {
      openaiName = `${openaiName}_${openaiNames.size}`;
    }
    openaiNames.add(openaiName);
    openaiToMcp.set(openaiName, mcpName);

    const schema =
      tool.inputSchema && typeof tool.inputSchema === "object"
        ? tool.inputSchema
        : { type: "object", properties: {} };

    openaiTools.push({
      type: "function",
      function: {
        name: openaiName,
        description: tool.description || `Unity MCP tool ${mcpName}`,
        parameters: schema.type ? schema : { ...schema, type: "object" },
      },
    });
  }

  return {
    toolCount: openaiTools.length,
    openaiTools,
    hasOpenAiName(name) {
      return openaiNames.has(name);
    },
    resolveMcpName(name) {
      return openaiToMcp.get(name) || name;
    },
    async callOpenAiTool(name, args) {
      const mcpName = openaiToMcp.get(name);
      if (!mcpName) throw new Error(`Unknown Unity MCP tool mapping for ${name}`);
      const result = await client.callTool({
        name: mcpName,
        arguments: args && typeof args === "object" ? args : {},
      });
      const text = contentToText(result);
      if (result?.isError) {
        throw new Error(text || `Unity MCP tool ${mcpName} failed`);
      }
      return text || `OK · ${mcpName}`;
    },
    async close() {
      try {
        await client.close();
      } catch {
        /* ignore */
      }
      try {
        await transport.close?.();
      } catch {
        /* ignore */
      }
    },
  };
}
