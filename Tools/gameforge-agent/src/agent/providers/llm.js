import fs from "node:fs/promises";
import path from "node:path";
import { assertAgentWriteAllowed, describeWriteRoots, detectRepoLayout, normalizeForgeMode } from "../writePolicy.js";
import {
  canonicalizeRel,
  isSensitiveRel,
  resolveWithinRoot,
} from "../../security/paths.js";
import { createRunMeter } from "../runMeter.js";
import { getSharedUnityMcpSession } from "../unityMcpPool.js";
import {
  buildUserContent,
  modelLikelySupportsVision,
} from "../attachments.js";
import { classifyAgentError, formatClassifiedError } from "../errors.js";

/** Per-request hang limit — then retry (does not end the run). */
const TURN_FETCH_TIMEOUT_MS = 12 * 60 * 1000;
const RETRY_BASE_MS = 1500;
const RETRY_MAX_MS = 45_000;

/**
 * Tool-result budgets. File reads keep a wide window (a truncated read must
 * never become a truncated write); Editor dumps, console logs and eval output
 * are clipped hard because they are resent on every later turn.
 */
const FILE_READ_TOOLS = new Set(["read_file", "read_text_file", "Unity_read_text_file"]);
const TOOL_RESULT_MAX_CHARS = 12_000;
const FILE_RESULT_MAX_CHARS = 120_000;

function clampToolResult(name, value) {
  const text = String(value ?? "");
  const max = FILE_READ_TOOLS.has(name) ? FILE_RESULT_MAX_CHARS : TOOL_RESULT_MAX_CHARS;
  if (text.length <= max) return text;
  if (FILE_READ_TOOLS.has(name)) return text.slice(0, max);
  const head = Math.floor(max * 0.75);
  const tail = max - head;
  return `${text.slice(0, head)}\n…[${text.length - max} chars omitted from the middle of this result — narrow the query if you need them]…\n${text.slice(-tail)}`;
}

/**
 * Older tool results are the bulk of a long run and get resent on every turn.
 * Keep the newest verbatim and collapse the rest to their head, which is where
 * status lines and errors live. Assistant reasoning is never touched.
 */
const HISTORY_FULL_TOOL_RESULTS = Math.max(
  2,
  Number(process.env.GAMEFORGE_HISTORY_TOOL_RESULTS || 10),
);
const HISTORY_DIGEST_CHARS = 400;

function compactHistory(messages) {
  const toolIdx = [];
  messages.forEach((m, i) => {
    if (m?.role === "tool") toolIdx.push(i);
  });
  const stale = toolIdx.length - HISTORY_FULL_TOOL_RESULTS;
  let digested = 0;
  for (let k = 0; k < stale; k += 1) {
    const msg = messages[toolIdx[k]];
    if (!msg || msg._digested) continue;
    const text = String(msg.content ?? "");
    if (text.length <= HISTORY_DIGEST_CHARS) continue;
    msg.content = `${text.slice(0, HISTORY_DIGEST_CHARS)}\n…[trimmed after later turns — ${text.length} chars; re-run the tool if you still need it]`;
    msg._digested = true;
    digested += 1;
  }
  return digested;
}

/** Vision costs repeat on every turn; keep the images for the early turns only. */
const IMAGE_TURNS = Math.max(1, Number(process.env.GAMEFORGE_IMAGE_TURNS || 8));

function stubImageParts(messages, attachments) {
  let stubbed = 0;
  for (const msg of messages) {
    if (msg?.role !== "user" || !Array.isArray(msg.content)) continue;
    const images = msg.content.filter((p) => p?.type === "image_url").length;
    if (!images) continue;
    const text = msg.content
      .filter((p) => p?.type === "text")
      .map((p) => p.text)
      .join("\n");
    const paths = (attachments || [])
      .map((a) => a.relPath || a.absPath)
      .filter(Boolean)
      .map((p) => `- ${p}`)
      .join("\n");
    msg.content = [
      text,
      "",
      `## Attached image(s) shown earlier in this run (${images}) — no longer resent`,
      paths || "(paths unavailable — re-capture with capture_game_view if you need another look)",
    ].join("\n");
    stubbed += images;
  }
  return stubbed;
}

/**
 * read_file budgets. Source files the agent may rewrite keep a generous
 * window so a truncated read can never become a truncated write; Unity YAML
 * and docs are clipped hard (a single .unity scene can be 2.5M chars).
 */
const READ_BUDGETS = [
  { re: /\.(cs|js|mjs|ts|tsx|shader|hlsl|cginc|uss|uxml)$/i, lines: 4000, chars: 200_000 },
  { re: /\.(unity|prefab|asset|mat|controller|anim|mixer|preset|meta)$/i, lines: 600, chars: 25_000, hint: " Prefer get_scene_hierarchy / find_gameobjects over raw YAML." },
  { re: /\.(md|txt|log|json|ya?ml|csv)$/i, lines: 1200, chars: 50_000 },
];
const READ_DEFAULT_BUDGET = { lines: 1500, chars: 60_000 };

function readBudgetFor(rel) {
  return READ_BUDGETS.find((b) => b.re.test(rel)) || READ_DEFAULT_BUDGET;
}

/** Return a bounded, self-describing window of a text file. */
function windowFileText(text, rel, { offset, limit } = {}) {
  const budget = readBudgetFor(rel);
  const lines = String(text).split(/\r?\n/);
  const start = Math.min(Math.max(1, Math.floor(Number(offset) || 1)), Math.max(1, lines.length));
  const wanted = Math.floor(Number(limit) || budget.lines);
  const take = Math.min(budget.lines, Math.max(1, wanted));
  const slice = lines.slice(start - 1, start - 1 + take);
  let body = slice.join("\n");
  let clippedChars = false;
  if (body.length > budget.chars) {
    body = body.slice(0, budget.chars);
    clippedChars = true;
  }
  const end = start - 1 + slice.length;
  if (start === 1 && end >= lines.length && !clippedChars) return body;
  const next = clippedChars
    ? `narrow the range (char budget ${budget.chars})`
    : `re-read with offset ${end + 1}`;
  return `[${rel}: lines ${start}-${end} of ${lines.length} — ${next}.${budget.hint || ""}]\n${body}`;
}

function sleep(ms, signal) {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(Object.assign(new Error("Aborted"), { name: "AbortError" }));
      return;
    }
    const t = setTimeout(resolve, ms);
    const onAbort = () => {
      clearTimeout(t);
      reject(Object.assign(new Error("Aborted"), { name: "AbortError" }));
    };
    signal?.addEventListener?.("abort", onAbort, { once: true });
  });
}

function isAbortError(err) {
  if (!err) return false;
  if (err.name === "AbortError") return true;
  const msg = String(err.message || err);
  return /aborted|AbortError/i.test(msg);
}

function isRetryableHttp(status) {
  return status === 408 || status === 429 || status === 500 || status === 502 || status === 503 || status === 504;
}

/** OpenAI-compatible content may be a string or an array of text parts (MiniMax et al.). */
function extractMessageText(content) {
  if (content == null) return "";
  if (typeof content === "string") return content;
  if (Array.isArray(content)) {
    return content
      .map((part) => {
        if (typeof part === "string") return part;
        if (part && typeof part.text === "string") return part.text;
        if (part && typeof part.content === "string") return part.content;
        return "";
      })
      .join("");
  }
  if (typeof content === "object" && typeof content.text === "string") return content.text;
  return String(content);
}

function isRetryableNetwork(err) {
  if (!err || isAbortError(err)) return false;
  const msg = String(err.message || err);
  const code = err.code || err.cause?.code || "";
  return (
    /fetch failed|network|ECONNRESET|ECONNREFUSED|ETIMEDOUT|ENOTFOUND|EAI_AGAIN|socket|TLS|undici|other side closed|terminated|timeout|UND_ERR/i.test(
      msg,
    ) ||
    /ECONNRESET|ECONNREFUSED|ETIMEDOUT|ENOTFOUND|EAI_AGAIN|UND_ERR/i.test(String(code))
  );
}

function backoffMs(attempt) {
  const exp = Math.min(RETRY_MAX_MS, RETRY_BASE_MS * 2 ** Math.min(attempt - 1, 6));
  const jitter = Math.floor(Math.random() * 400);
  return exp + jitter;
}

/** OpenAI chat/completions rejects tools arrays longer than 128. */
const OPENAI_TOOL_CAP = 128;

/**
 * Unity MCP exposes ~140 tools; every schema is resent on every turn (~28k
 * tokens). GameForge flows only need this subset — inspection, scene/asset
 * authoring, and the play/compile loop. Verify, compile poll and tests run
 * through Unity CLI Pipeline, so their MCP twins stay out.
 */
const UNITY_READ_TOOLS = [
  "editor_status",
  "get_console_logs",
  "get_scene_hierarchy",
  "list_open_scenes",
  "find_gameobjects",
  "find_assets",
  "get_component_properties",
  "read_text_file",
  "capture_game_view",
  "capture_scene_view",
  "recompile_status",
  "get_authoring_root",
];

const UNITY_WRITE_TOOLS = [
  "eval",
  "eval_file",
  "create_scene",
  "open_scene",
  "save_scene",
  "save_all",
  "set_active_scene",
  "add_scene_to_build",
  "create_gameobject",
  "delete_gameobject",
  "set_parent",
  "set_transform",
  "set_active",
  "set_layer",
  "set_tag",
  "set_tags_layers",
  "add_component",
  "set_component_properties",
  "set_serialized_field",
  "create_asset",
  "create_folder",
  "create_prefab",
  "instantiate_prefab",
  "create_script",
  "attach_script",
  "write_text_file",
  "set_material_properties",
  "set_authoring_root",
  "recompile",
  "editor_play",
  "editor_stop",
  "menu",
];

/** Legacy relay names (Unity.RunCommand → Unity_RunCommand) so old setups keep working. */
const UNITY_LEGACY_TOOLS = [
  "Unity_RunCommand",
  "Unity_ReadConsole",
  "Unity_ManageScene",
  "Unity_ManageGameObject",
  "Unity_ManageAsset",
  "Unity_ManageScript",
  "Unity_ManageEditor",
  "Unity_ManageShader",
  "Unity_ExecuteMenuItem",
];

function envToolAllowlist() {
  const raw = String(process.env.GAMEFORGE_UNITY_MCP_TOOLS || "").trim();
  if (!raw) return null;
  const names = raw
    .split(/[,\s]+/)
    .map((s) => s.trim())
    .filter(Boolean);
  return names.length ? names : null;
}

/**
 * Keep only the tools the flows use. Falls back to the full list when no name
 * matches (unknown naming scheme) so a relay change can never leave the model
 * tool-less. `slots` caps the result for endpoints with a hard tools limit.
 */
export function selectUnityTools(mcpTools, { writeMode, slots }) {
  const all = Array.isArray(mcpTools) ? mcpTools : [];
  if (!all.length) return all;
  const cap = (list) => (slots > 0 && list.length > slots ? list.slice(0, slots) : list);
  if (/^(1|true|yes)$/i.test(String(process.env.GAMEFORGE_UNITY_MCP_ALL_TOOLS || ""))) {
    return cap(all);
  }
  const wanted = new Set(
    envToolAllowlist() ||
      (writeMode === "ask"
        ? [...UNITY_READ_TOOLS, ...UNITY_LEGACY_TOOLS]
        : [...UNITY_READ_TOOLS, ...UNITY_WRITE_TOOLS, ...UNITY_LEGACY_TOOLS]),
  );
  const picked = all.filter((tool) => wanted.has(tool?.function?.name || ""));
  return picked.length ? cap(picked) : cap(all);
}

function isOpenAiToolsEndpoint(baseUrl, model) {
  const host = String(baseUrl || "").toLowerCase();
  const id = String(model || "").toLowerCase();
  return host.includes("api.openai.com") || id.startsWith("gpt-");
}

/** gpt-6-astra tool calling is only supported on /v1/responses, not chat/completions. */
function usesResponsesApi(baseUrl, model) {
  const id = String(model || "").toLowerCase();
  if (!id.startsWith("gpt-6")) return false;
  return isOpenAiToolsEndpoint(baseUrl, model);
}

function toResponsesTools(chatTools) {
  return (chatTools || []).map((tool) => {
    const fn = tool?.function || {};
    return {
      type: "function",
      name: fn.name,
      description: fn.description || fn.name || "tool",
      parameters: fn.parameters || { type: "object", properties: {} },
      strict: false,
    };
  });
}

function extractResponsesText(output) {
  const parts = [];
  for (const item of output || []) {
    if (item?.type === "message" && Array.isArray(item.content)) {
      for (const part of item.content) {
        if (typeof part?.text === "string") parts.push(part.text);
      }
    } else if (typeof item?.text === "string" && item.type !== "function_call") {
      parts.push(item.text);
    }
  }
  return parts.join("\n").trim();
}

function responsesToChatMessage(data) {
  const output = Array.isArray(data?.output) ? data.output : [];
  const toolCalls = [];
  for (const item of output) {
    if (item?.type !== "function_call") continue;
    toolCalls.push({
      id: item.call_id || item.id,
      type: "function",
      function: {
        name: item.name,
        arguments:
          typeof item.arguments === "string"
            ? item.arguments
            : JSON.stringify(item.arguments || {}),
      },
    });
  }
  const msg = {
    role: "assistant",
    content: extractResponsesText(output) || null,
    _responsesId: data?.id || null,
    _responsesOutput: output,
  };
  if (toolCalls.length) msg.tool_calls = toolCalls;
  return msg;
}

function messagesToResponsesInput(messages) {
  const input = [];
  for (const message of messages || []) {
    if (!message) continue;
    if (message.role === "system" || message.role === "user") {
      const text = extractMessageText(message.content);
      if (text) input.push({ role: message.role, content: text });
      continue;
    }
    if (message.role === "assistant") {
      if (Array.isArray(message._responsesOutput) && message._responsesOutput.length) {
        input.push(...message._responsesOutput);
        continue;
      }
      const text = extractMessageText(message.content);
      if (text) input.push({ role: "assistant", content: text });
      for (const call of message.tool_calls || []) {
        input.push({
          type: "function_call",
          call_id: call.id,
          name: call.function?.name,
          arguments: call.function?.arguments || "{}",
        });
      }
      continue;
    }
    if (message.role === "tool") {
      input.push({
        type: "function_call_output",
        call_id: message.tool_call_id,
        output: String(message.content ?? ""),
      });
    }
  }
  return input;
}

function pendingResponsesToolOutputs(messages) {
  let lastAssistant = -1;
  for (let i = messages.length - 1; i >= 0; i -= 1) {
    if (messages[i]?.role === "assistant") {
      lastAssistant = i;
      break;
    }
  }
  if (lastAssistant < 0) return null;
  const assistant = messages[lastAssistant];
  if (!assistant?._responsesId) return null;
  const tail = messages.slice(lastAssistant + 1);
  if (!tail.length || tail.some((m) => m.role !== "tool")) return null;
  return {
    previousResponseId: assistant._responsesId,
    input: tail.map((m) => ({
      type: "function_call_output",
      call_id: m.tool_call_id,
      output: String(m.content ?? ""),
    })),
  };
}

function cloneMessages(messages) {
  return JSON.parse(JSON.stringify(messages || []));
}

/** Drop our own bookkeeping fields (`_digested`, `_responsesId`, …) from the wire body. */
function stripInternalFields(message) {
  if (!message || typeof message !== "object") return message;
  const out = {};
  for (const [key, value] of Object.entries(message)) {
    if (key.startsWith("_")) continue;
    out[key] = value;
  }
  return out;
}

/** Ensure every assistant tool_call has a matching tool result (API requires it). */
function sealToolResults(messages) {
  const out = cloneMessages(messages);
  const pending = new Set();
  for (const m of out) {
    if (m.role === "assistant" && Array.isArray(m.tool_calls)) {
      for (const c of m.tool_calls) {
        if (c?.id) pending.add(c.id);
      }
    }
    if (m.role === "tool" && m.tool_call_id) pending.delete(m.tool_call_id);
  }
  for (const id of pending) {
    out.push({
      role: "tool",
      tool_call_id: id,
      content: "ERROR: Stopped by user before tool finished",
    });
  }
  return out;
}

/**
 * OpenAI-compatible tool-calling provider.
 * Keeps going through transient network/API failures until the model finishes
 * or the operator hits Stop. Snapshots messages for Continue-after-Stop.
 */
/** Unity MCP tools that create/move/delete/write project files (path args are policy-checked). */
const MCP_WRITE_TOOL_RE =
  /^(Unity_)?(write_text_file|create_script|create_asset|delete_asset|move_asset|rename_asset|copy_asset|create_prefab|create_prefab_variant|create_scene|save_scene|save_prefab_contents|create_folder|create_animation_clip|create_animator_controller|create_timeline|ManageScript|ManageAsset|ManageShader)$/i;
const MCP_PATH_KEYS = [
  "path",
  "assetPath",
  "filePath",
  "scenePath",
  "prefabPath",
  "savePath",
  "folderPath",
  "destination",
  "destinationPath",
  "newPath",
  "targetPath",
  "to",
];
const PROJECT_REL_RE = /^(Assets|Docs|Packages|ProjectSettings|V57|Source|Tools)(\/|\\|$)/i;

/** @returns {string[]} project-relative paths a Unity MCP write tool would touch */
export function mcpWriteTargets(name, args) {
  if (!MCP_WRITE_TOOL_RE.test(String(name || ""))) return [];
  const out = [];
  const a = args && typeof args === "object" ? args : {};
  // Legacy relay tools (ManageScript/ManageAsset) only write on write-like actions.
  if (/Manage/.test(name) && a.action && !/create|update|write|delete|move|rename|save|import|duplicate/i.test(String(a.action))) {
    return [];
  }
  for (const k of MCP_PATH_KEYS) {
    const v = a[k];
    if (typeof v !== "string" || !v.trim()) continue;
    const rel = v.trim().replace(/\\/g, "/").replace(/^\.\//, "");
    if (PROJECT_REL_RE.test(rel)) out.push(rel);
  }
  return out;
}

export function createLlmProvider({
  root,
  apiKey,
  baseUrl = "https://api.openai.com/v1",
  model = "gpt-4o-mini",
  writeMode = "generate",
  slug,
  forgeMode = "Prototype",
}) {
  let aborted = false;
  const ctrl = { current: null };
  const modeLabel = normalizeForgeMode(forgeMode);
  const layout = detectRepoLayout(root);
  /** @type {{ messages: object[], turn: number, writeMode: string, model: string, at: number } | null} */
  let checkpoint = null;

  const readTools = [
    {
      type: "function",
      function: {
        name: "list_dir",
        description: "List files in a relative directory",
        parameters: {
          type: "object",
          properties: { path: { type: "string" } },
          required: ["path"],
        },
      },
    },
    {
      type: "function",
      function: {
        name: "read_file",
        description:
          "Read a UTF-8 text file relative to project root. Long files return a bounded window that states its line range — page through with offset/limit instead of re-reading.",
        parameters: {
          type: "object",
          properties: {
            path: { type: "string" },
            offset: { type: "number", description: "First line to return (1-based, default 1)" },
            limit: { type: "number", description: "Maximum lines to return" },
          },
          required: ["path"],
        },
      },
    },
  ];
  const writeTool = {
    type: "function",
    function: {
      name: "write_file",
      description: "Write a UTF-8 text file (subject to write policy)",
      parameters: {
        type: "object",
        properties: {
          path: { type: "string" },
          content: { type: "string" },
        },
        required: ["path", "content"],
      },
    },
  };
  /** @type {object[]} */
  let tools = writeMode === "ask" ? [...readTools] : [...readTools, writeTool];
  /** @type {Awaited<ReturnType<typeof getSharedUnityMcpSession>>} */
  let unityMcp = null;

  async function execTool(name, args) {
    if (unityMcp?.hasOpenAiName(name)) {
      // Path-bearing MCP writes obey the same policy as write_file (eval/menu cannot be
      // inspected — those are covered by the post-turn diff audit in session.js).
      for (const rel of mcpWriteTargets(name, args)) {
        assertAgentWriteAllowed(rel, writeMode, { slug, forgeMode: modeLabel, layout });
      }
      return await unityMcp.callOpenAiTool(name, args || {});
    }

    const rel = canonicalizeRel(args.path || "");
    const abs = resolveWithinRoot(root, rel);

    if (name === "list_dir") {
      if (isSensitiveRel(rel)) throw new Error("Access denied");
      const ents = await fs.readdir(abs, { withFileTypes: true });
      return ents
        .filter((e) => !isSensitiveRel(`${rel}/${e.name}`))
        .map((e) => (e.isDirectory() ? `${e.name}/` : e.name))
        .join("\n");
    }
    if (name === "read_file") {
      if (isSensitiveRel(rel)) throw new Error("Access denied to sensitive file");
      const text = await fs.readFile(abs, "utf8");
      return windowFileText(text, rel, args);
    }
    if (name === "write_file") {
      assertAgentWriteAllowed(rel, writeMode, { slug, forgeMode: modeLabel, layout });
      await fs.mkdir(path.dirname(abs), { recursive: true });
      await fs.writeFile(abs, args.content ?? "", "utf8");
      return `Wrote ${rel}`;
    }
    throw new Error(`Unknown tool ${name}`);
  }

  /** Holds a live reference — `getCheckpoint` is what seals and clones. */
  function saveCheckpoint(messages, turn) {
    checkpoint = {
      messages,
      turn: Math.max(1, turn || 1),
      writeMode,
      model,
      at: Date.now(),
    };
  }

  return {
    id: "llm",
    supportsCheckpoint: true,
    getCheckpoint() {
      return checkpoint
        ? { ...checkpoint, messages: sealToolResults(checkpoint.messages) }
        : null;
    },
    clearCheckpoint() {
      checkpoint = null;
    },
    async run(prompt, { onEvent, signal, resumeMessages, resumeTurn, resumeUserMessage, attachments = [] } = {}) {
      aborted = false;
      const ac = new AbortController();
      ctrl.current = ac;
      const onParentAbort = () => {
        aborted = true;
        ac.abort();
      };
      if (signal?.aborted) onParentAbort();
      else signal?.addEventListener?.("abort", onParentAbort);

      const meter = createRunMeter();
      const withVision =
        Array.isArray(attachments) &&
        attachments.length > 0 &&
        modelLikelySupportsVision(model);
      // Chat Completions multimodal is more reliable for image_url parts than Responses.
      const useResponses = !withVision && usesResponsesApi(baseUrl, model);
      const endpoint = `${baseUrl.replace(/\/$/, "")}/${useResponses ? "responses" : "chat/completions"}`;
      let lastResponsesId = null;
      const resuming = Array.isArray(resumeMessages) && resumeMessages.length >= 2;

      // Shared MCP connection — do not open/close per turn (Unity revokes on reconnect).
      const emitMcpApproval = (message, reason = "revoked") => {
        onEvent?.({
          type: "mcp-approval",
          reason,
          message: String(message || "").slice(0, 500),
        });
      };
      const isMcpApprovalError = (text) =>
        /connection revoked|capacity limit|0 direct connection|pending connection|not approved|approval/i.test(
          String(text || ""),
        );

      try {
        unityMcp = await getSharedUnityMcpSession(root);
        if (unityMcp?.openaiTools?.length) {
          const available = unityMcp.openaiTools.length;
          const slots = isOpenAiToolsEndpoint(baseUrl, model)
            ? Math.max(0, OPENAI_TOOL_CAP - tools.length)
            : 0;
          const mcpTools = selectUnityTools(unityMcp.openaiTools, { writeMode, slots });
          tools = [...tools, ...mcpTools];
          onEvent?.({
            type: "status",
            message:
              mcpTools.length < available
                ? `Unity MCP · ${mcpTools.length}/${available} Editor tool(s) sent (core set)`
                : `Unity MCP · ${unityMcp.toolCount} Editor tool(s) available`,
          });
        } else {
          onEvent?.({
            type: "status",
            message:
              "Unity MCP unavailable — install Unity CLI, `unity pipeline install`, Editor open (Workbench → Test MCP). See UNITY_CLI_MIGRATION.md",
          });
        }
      } catch (err) {
        unityMcp = null;
        const msg = String(err?.message || err);
        onEvent?.({
          type: "status",
          message: `Unity MCP connect failed: ${msg.slice(0, 120)}`,
        });
        emitMcpApproval(msg, isMcpApprovalError(msg) ? "revoked" : "connect-failed");
      }

      const mcpSystemNote = unityMcp?.toolCount
        ? " Unity MCP is connected with the core Editor tool set (scenes, GameObjects, components, assets, scripts, console, play, recompile, eval); anything not listed can be driven through eval. Verify/compile/test gates run on Unity CLI Pipeline, not MCP."
        : " Unity MCP is NOT connected — you can only use disk tools; scene/asset mounting in the Editor will be limited.";

      const agentRole =
        writeMode === "ask"
          ? `You answer questions about an existing Unity game project. Read-only disk tools; Unity MCP tools may be used for console/scene inspection when available.${mcpSystemNote} Match answer length to the question.`
          : modeLabel === "Production" && layout === "provider"
              ? `You are a Unity production agent working in a provider-delivered repo (/game-setup). Complete the current stage, update Docs/V57/STATUS.json, then end the turn — the runner decides whether to continue. ${describeWriteRoots("Production", { layout }).split("\n").slice(1).join(" ")} Use disk tools and Unity MCP Editor tools.${mcpSystemNote}`
          : modeLabel === "Production"
              ? `You are a Unity production setup agent (/game-setup). Stage-gated pipeline: complete ONLY the current stage, emit the skill Progress block (with Reply continue when applicable), then STOP. Ask the user ONLY at INIT confirm or stage continue — never offer optional extras ("If you want, I can...", "next steps", "would you like me to wire..."). Write ONLY under production roots: Assets/Scripts, Assets/Scenes, Assets/Prefabs, Assets/UI, Assets/Materials, Assets/Settings, etc. NEVER create or write under Assets/Prototypes/ or Assets/VerticalSlice/. Use disk tools and Unity MCP Editor tools. Obey write policy.${mcpSystemNote}`
              : modeLabel === "VerticalSlice"
                ? layout === "provider"
                  ? `You are a Unity vertical-slice agent working in a provider-delivered repo. Follow the /vertical-slice stage driver autonomously (no waiting for continue; keep Docs/V57/STATUS.json current). ${describeWriteRoots("VerticalSlice", { layout }).split("\n").slice(1).join(" ")} Use disk tools and Unity MCP.${mcpSystemNote} Keep working until the current stage is complete.`
                  : `You are a Unity vertical-slice agent. Write ONLY under Assets/VerticalSlice/ and V57/specs/<slug>/features/. NEVER write under Assets/Prototypes/. Use disk tools and Unity MCP.${mcpSystemNote} Keep working until the task is complete.`
                : `You are a gameplay prototyping agent. Write under Assets/Prototypes/ (sandbox). Use disk tools and Unity MCP Editor tools. Obey write policy. Mount scenes, create GameObjects/lights/cameras/assets via Unity MCP — do not stop at .cs files only.${mcpSystemNote} Keep working until the task is complete.`;

      const visionOk = modelLikelySupportsVision(model);
      let userContent = prompt;
      if (!resuming && attachments?.length) {
        if (visionOk) {
          userContent = await buildUserContent(prompt, attachments, true);
        } else {
          const paths = attachments
            .map((a) => `- ${a.relPath || a.absPath}`)
            .join("\n");
          userContent = `${prompt}\n\n## Attached images (model may not view these — paths only)\n${paths}`;
          onEvent?.({
            type: "notice",
            message:
              "This model may not support vision. Image paths are listed in the prompt — switch model to inspect captures.",
          });
        }
      }

      const messages = resuming
        ? sealToolResults(resumeMessages)
        : [
            {
              role: "system",
              content: agentRole,
            },
            { role: "user", content: userContent },
          ];

      if (resuming) {
        messages.push({
          role: "user",
          content:
            resumeUserMessage ||
            "Continue from this checkpoint. Finish remaining work. Do not redo completed writes unless they are broken.",
        });
      }

      let turn = resuming ? Math.max(0, Number(resumeTurn) || 0) : 0;
      saveCheckpoint(messages, Math.max(1, turn || 1));

      const emitBenchmark = (status, errorMessage) => {
        onEvent?.(
          meter.finish({
            provider: "llm",
            model,
            status,
            ...(errorMessage
              ? { errorMessage: String(errorMessage).slice(0, 800) }
              : {}),
          }),
        );
      };

      const emitCancelled = () => {
        saveCheckpoint(messages, turn);
        onEvent?.({
          type: "checkpoint",
          resumable: true,
          turn: checkpoint.turn,
          writeMode,
          model,
        });
        emitBenchmark("cancelled");
        onEvent?.({ type: "done", status: "cancelled", resumable: true });
      };

      async function fetchCompletion(turnNo) {
        let attempt = 0;
        while (!aborted) {
          attempt += 1;
          const turnAc = new AbortController();
          const timer = setTimeout(() => turnAc.abort(), TURN_FETCH_TIMEOUT_MS);
          const onCancel = () => turnAc.abort();
          ac.signal.addEventListener("abort", onCancel);

          try {
            const followup = useResponses ? pendingResponsesToolOutputs(messages) : null;
            if (followup?.previousResponseId) lastResponsesId = followup.previousResponseId;
            const body = useResponses
              ? {
                  model,
                  tools: toResponsesTools(tools),
                  tool_choice: "auto",
                  reasoning: { effort: "low" },
                  ...(followup
                    ? { previous_response_id: followup.previousResponseId, input: followup.input }
                    : { input: messagesToResponsesInput(messages) }),
                }
              : {
                  model,
                  messages: messages.map(stripInternalFields),
                  tools,
                  tool_choice: "auto",
                };
            const res = await fetch(endpoint, {
              method: "POST",
              headers: {
                Authorization: `Bearer ${apiKey}`,
                "Content-Type": "application/json",
              },
              body: JSON.stringify(body),
              signal: turnAc.signal,
            });

            if (!res.ok) {
              const errText = await res.text();
              const httpErr = new Error(`LLM HTTP ${res.status}: ${errText.slice(0, 400)}`);
              httpErr.status = res.status;
              if (isRetryableHttp(res.status) && !aborted) {
                const wait = backoffMs(attempt);
                onEvent?.({
                  type: "status",
                  message: `API ${res.status} — retry ${attempt} in ${(wait / 1000).toFixed(1)}s (Stop to cancel)`,
                });
                await sleep(wait, ac.signal);
                continue;
              }
              throw httpErr;
            }

            const payload = await res.json();
            if (!useResponses) return payload;
            if (payload?.id) lastResponsesId = payload.id;
            return {
              usage: payload.usage,
              choices: [{ message: responsesToChatMessage(payload) }],
            };
          } catch (err) {
            if (aborted || ac.signal.aborted || isAbortError(err)) {
              throw Object.assign(new Error("Stopped"), { name: "AbortError" });
            }
            if (isRetryableNetwork(err) || turnAc.signal.aborted) {
              const wait = backoffMs(attempt);
              const why =
                turnAc.signal.aborted && !ac.signal.aborted
                  ? "request timed out"
                  : err.message || "fetch failed";
              onEvent?.({
                type: "status",
                message: `${why} — retry ${attempt} in ${(wait / 1000).toFixed(1)}s (turn ${turnNo}, Stop to cancel)`,
              });
              await sleep(wait, ac.signal);
              continue;
            }
            throw err;
          } finally {
            clearTimeout(timer);
            ac.signal.removeEventListener("abort", onCancel);
          }
        }
        throw Object.assign(new Error("Stopped"), { name: "AbortError" });
      }

      try {
        if (resuming) {
          onEvent?.({
            type: "status",
            message: `Resuming checkpoint · ${model} · turn ${turn || 1}+`,
          });
        }

        while (!aborted) {
          turn += 1;
          onEvent?.({
            type: "status",
            message:
              turn === 1 && !resuming ? `LLM · ${model}` : `LLM turn ${turn}…`,
          });
          const digested = compactHistory(messages);
          if (digested) {
            onEvent?.({
              type: "status",
              message: `Context trimmed · ${digested} older tool result(s) digested`,
            });
          }
          if (turn > IMAGE_TURNS && withVision) {
            const stubbed = stubImageParts(messages, attachments);
            if (stubbed) {
              onEvent?.({
                type: "status",
                message: `Context trimmed · ${stubbed} image(s) replaced by paths after turn ${IMAGE_TURNS}`,
              });
            }
          }
          saveCheckpoint(messages, turn);

          const data = await fetchCompletion(turn);
          meter.noteTurn(data.usage);
          const msg = data.choices?.[0]?.message;
          if (!msg) {
            onEvent?.({
              type: "status",
              message: "Empty LLM response — retrying…",
            });
            await sleep(backoffMs(1), ac.signal);
            continue;
          }
          messages.push(msg);
          saveCheckpoint(messages, turn);

          if (msg.content) {
            const cleaned = extractMessageText(msg.content)
              .replace(/<think\b[^>]*>[\s\S]*?<\/think>/gi, "")
              .replace(/<\/?think\b[^>]*>/gi, "")
              .trim();
            // Do NOT mirror answer text into status — Chat would show it as SYSTEM.
            if (cleaned) onEvent?.({ type: "assistant", text: cleaned });
          }

          const calls = msg.tool_calls || [];
          if (!calls.length) {
            checkpoint = null;
            emitBenchmark("finished");
            onEvent?.({ type: "done", status: "finished" });
            return data;
          }

          for (const call of calls) {
            if (aborted || ac.signal.aborted) break;
            const name = call.function?.name;
            let args = {};
            try {
              args = JSON.parse(call.function?.arguments || "{}");
            } catch {
              args = {};
            }
            const relPath = String(args.path || "").replace(/\\/g, "/");
            meter.noteTool();
            onEvent?.({ type: "tool", name, path: relPath || undefined, status: "call" });
            let result;
            try {
              result = await execTool(name, args);
              if (aborted || ac.signal.aborted) break;
              if (name === "write_file") {
                meter.noteFile();
                onEvent?.({ type: "file", path: args.path });
              } else if (unityMcp?.hasOpenAiName(name)) {
                const hint =
                  args.path || args.assetPath || args.scenePath || args.name || args.objectName || "";
                if (hint) onEvent?.({ type: "file", path: String(hint) });
              }
            } catch (err) {
              if (aborted || ac.signal.aborted || isAbortError(err)) break;
              result = `ERROR: ${err.message}`;
              if (unityMcp?.hasOpenAiName?.(name) && isMcpApprovalError(err.message)) {
                emitMcpApproval(err.message, "revoked");
              }
            }
            messages.push({
              role: "tool",
              tool_call_id: call.id,
              content: clampToolResult(name, result),
            });
          }
          if (aborted || ac.signal.aborted) break;
          saveCheckpoint(messages, turn);
        }

        emitCancelled();
        return { status: "cancelled", resumable: true };
      } catch (err) {
        if (aborted || isAbortError(err)) {
          emitCancelled();
          return { status: "cancelled", resumable: true };
        }
        saveCheckpoint(messages, turn);
        const classified = classifyAgentError(err);
        onEvent?.({
          type: "error",
          error: formatClassifiedError(classified),
          errorKind: classified.kind,
          retryable: classified.retryable,
        });
        emitBenchmark("error", classified.message);
        throw err;
      } finally {
        signal?.removeEventListener?.("abort", onParentAbort);
        // Keep shared Unity MCP session alive across turns (closing revokes approval).
        unityMcp = null;
      }
    },
    cancel() {
      aborted = true;
      try {
        ctrl.current?.abort?.();
      } catch {
        /* ignore */
      }
      // Extra pulses — in-flight fetch/tool awaits sometimes miss the first abort.
      setTimeout(() => {
        aborted = true;
        try {
          ctrl.current?.abort?.();
        } catch {
          /* ignore */
        }
      }, 50);
      setTimeout(() => {
        aborted = true;
        try {
          ctrl.current?.abort?.();
        } catch {
          /* ignore */
        }
      }, 300);
    },
  };
}
