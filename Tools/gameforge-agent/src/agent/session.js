import {
  getActiveProvider,
  initProviderCatalog,
  requireActiveProvider,
} from "./providers/catalog.js";
import { createCursorProvider } from "./providers/cursor.js";
import { createLlmProvider } from "./providers/llm.js";
import {
  buildChatPrompt,
  buildGenerateFinalPrompt,
  buildReanchorBlock,
  buildSyncPreviewPrompt,
  buildSyncPrompt,
  loadPromptPack,
} from "./prompts/index.js";
import { loadForgeModeSkill, formatSkillBlock, detectChatSkillId, loadChatSkill } from "./v57Skills.js";
import { detectRepoLayout, ensurePrototypeProjectFolders, normalizeForgeMode } from "./writePolicy.js";
import { parseMechanics, parseProjectName, readTdd, resolveTddFilePath } from "../tdd/parser.js";
import { buildTddContext } from "../tdd/extract.js";
import { finalizeTddSync } from "../tdd/sync.js";
import {
  formatSelectedSyncItems,
  listGameplayFiles,
  mergeChatDigest,
  parseSyncProposal,
} from "./gameplayEvidence.js";
import {
  advisePlayability,
  formatAdviceForChat,
  resolveGateMode,
  verifyGateClaims,
} from "./playabilityAdvisor.js";
import { mergeAssistantChunks } from "./assistantMerge.js";
import { createAutonomyStatus, resolveAutonomyConfig, runAutonomousLoop } from "./autonomy.js";
import { createWriteAudit, resolveAuditMode } from "./writeAudit.js";
import { listKnowledgeFiles, readRunState } from "./v57State.js";
import { recordBenchmark } from "./benchmarkStore.js";
import { buildResumeUserMessage } from "./resumeMessage.js";
import { buildScopeMap } from "./scopeMap.js";
import {
  resolveAttachments,
  modelLikelySupportsVision,
} from "./attachments.js";
import { classifyAgentError, formatClassifiedError } from "./errors.js";
import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";

/** Provider-repo TDD (DESIGN_BRIEF §1). Preferred over Docs/tdds/<slug>/ for VerticalSlice/Production. */
const PROVIDER_TDD_REL = "Docs/Design/TDD.md";

async function readProviderTdd(root, slug) {
  const abs = path.join(root, ...PROVIDER_TDD_REL.split("/"));
  const text = await fs.readFile(abs, "utf8");
  return {
    slug,
    path: abs,
    relPath: PROVIDER_TDD_REL,
    text,
    projectName: parseProjectName(text),
    mechanics: parseMechanics(text),
  };
}

export async function pickProvider(root, { writeMode = "generate", slug, forgeMode = "Prototype" } = {}) {
  await initProviderCatalog(root);
  const slot = requireActiveProvider();
  if (slot.kind === "cursor") {
    return {
      id: slot.id,
      kind: "cursor",
      model: slot.model,
      label: slot.label,
      provider: createCursorProvider({
        root,
        apiKey: slot.apiKey,
        model: slot.model,
        writeMode,
        forgeMode,
        slug,
      }),
    };
  }
  return {
    id: slot.id,
    kind: "llm",
    model: slot.model,
    label: slot.label,
    provider: createLlmProvider({
      root,
      apiKey: slot.apiKey,
      baseUrl: slot.baseUrl,
      model: slot.model,
      writeMode,
      slug,
      forgeMode,
    }),
  };
}

function wrapEvents({ root, op, slug, picked, onEvent }) {
  return async (ev) => {
    // Providers emit "done" when the LLM turn ends, but the session may still
    // run soft-advice (busy=true). Forwarding that early done makes the Editor
    // FinalizeThinking, then RefreshBusyUi restarts Thinking as "Agent running
    // (sidecar)". Terminal finished/cancelled is owned by the HTTP handler after
    // session.* returns (busy already false). Still forward cancelled so UI
    // can show checkpoint before the handler re-emits.
    if (ev?.type === "done") {
      const cancelled = ev.status === "cancelled" || !!ev.resumable;
      if (!cancelled) return;
    }
    if (ev?.type === "benchmark") {
      const enriched = await recordBenchmark(root, {
        ...ev,
        op,
        slug,
        providerId: picked.id,
        providerLabel: picked.label || picked.id,
        model: ev.model || picked.model,
      });
      onEvent?.(enriched);
      return;
    }
    onEvent?.(ev);
  };
}

/**
 * Soft advisor after LLM writes — never throws into the happy path.
 */
async function emitSoftAdvice({ root, tddText = "", onEvent }) {
  try {
    const report = await advisePlayability({ root, tddText });
    const digest = formatAdviceForChat(report.advice);
    onEvent?.({
      type: "advice",
      advice: report.advice,
      digest,
    });
    const warns = report.advice.filter((a) => a.severity === "warn");
    if (warns.length) {
      onEvent?.({
        type: "status",
        message: `Soft check: ${warns.length} hint(s) — playable still opens; fix via chat if needed`,
      });
    } else {
      onEvent?.({ type: "status", message: "Soft check: no blocking issues (play-test the loop)" });
    }
    return report;
  } catch (err) {
    onEvent?.({
      type: "status",
      message: `Soft check skipped: ${err.message || err}`,
    });
    return null;
  }
}

export async function createSession({ root, tddsRoot, slug, forgeMode = "Prototype", autonomy = "" }) {
  await initProviderCatalog(root);
  let agentsMd = "";
  try {
    agentsMd = await fs.readFile(path.join(root, "AGENTS.md"), "utf8");
  } catch {
    agentsMd = "# GameForge agent\nWrite under Assets/Prototypes (or mode root). Respect V57 TDD.\n";
  }
  const pack = await loadPromptPack();
  let busy = false;
  let controller = null;
  let activeProvider = null;
  /** @type {{ role: string, message: string, at: number }[]} */
  let chatHistory = [];
  /** @type {string} */
  let lastAdviceDigest = "";
  /** @type {{ messages: object[], turn: number, writeMode: string, model: string, mode: string, op: string, at: number } | null} */
  let checkpoint = null;
  const modeLabel = normalizeForgeMode(forgeMode);
  /** @type {{ mode: string, op: string, chatSkillId?: string, forgeMode?: string }} */
  let runMeta = { mode: "agent", op: "chat", chatSkillId: "", forgeMode: modeLabel };
  /** Sticky skill context for Continue when provider cannot LLM-checkpoint (e.g. Cursor). */
  let activeChatSkillId = "";
  let activeForgeMode = modeLabel;
  /** @type {{ path: string, mtimeMs: number, value: object } | null} */
  let tddCache = null;
  /** Set by the event stream when a run touched disk or the Editor. */
  let wroteFiles = false;
  /** provider = Docs/Design + Assets/_Game repo (DESIGN_BRIEF §1); legacy = V57 template project. */
  const layout = detectRepoLayout(root);
  /** Requested autonomy for this session ("auto" | "staged" | "" = default per forge mode). */
  const requestedAutonomy = String(autonomy || "");
  let autonomyConfig = resolveAutonomyConfig({ forgeMode: modeLabel, requested: requestedAutonomy });
  let autonomyStatus = createAutonomyStatus(autonomyConfig);

  /**
   * Read the canonical TDD once per edit. `contextText` is the bounded extract
   * used in prompts; `text` stays full for TDD sync, which rewrites the file.
   */
  async function loadTdd({ nextSpec = [] } = {}) {
    const providerTdd =
      modeLabel !== "Prototype" && fsSync.existsSync(path.join(root, ...PROVIDER_TDD_REL.split("/")));
    let tddPath;
    if (providerTdd) {
      tddPath = path.join(root, ...PROVIDER_TDD_REL.split("/"));
    } else {
      try {
        tddPath = await resolveTddFilePath(tddsRoot, slug);
      } catch (err) {
        if (!fsSync.existsSync(path.join(root, ...PROVIDER_TDD_REL.split("/")))) throw err;
        tddPath = path.join(root, ...PROVIDER_TDD_REL.split("/"));
      }
    }
    const { mtimeMs } = await fs.stat(tddPath);
    let tdd;
    if (tddCache && tddCache.path === tddPath && tddCache.mtimeMs === mtimeMs) {
      tdd = tddCache.value;
    } else {
      const isProvider = tddPath === path.join(root, ...PROVIDER_TDD_REL.split("/"));
      tdd = isProvider ? await readProviderTdd(root, slug) : await readTdd(tddsRoot, slug);
      tddCache = { path: tddPath, mtimeMs, value: tdd };
    }
    // Section-index extract depends on the current STATUS next_spec — cheap, rebuilt per call.
    const context = buildTddContext(tdd.text, { slug, relPath: tdd.relPath, nextSpec });
    return {
      ...tdd,
      contextText: context.text,
      contextChars: context.chars,
      fullChars: context.fullChars,
      focusMechanics: context.mechanics,
    };
  }

  /** Ring buffer + fan-out so Editor Play/reload can reattach without killing the run. */
  const EVENT_LOG_MAX = 500;
  /** @type {object[]} */
  const eventLog = [];
  /** @type {Set<(ev: object) => void>} */
  const eventListeners = new Set();

  function pushEvent(ev) {
    if (!ev || typeof ev !== "object") return;
    eventLog.push(ev);
    while (eventLog.length > EVENT_LOG_MAX) eventLog.shift();
    for (const listener of [...eventListeners]) {
      try {
        listener(ev);
      } catch {
        /* drop broken subscriber */
      }
    }
  }

  /**
   * @param {(ev: object) => void} listener
   * @param {{ replay?: boolean }} [opts]
   * @returns {() => void} unsubscribe
   */
  function subscribe(listener, opts = {}) {
    if (opts.replay !== false) {
      for (const ev of eventLog) {
        try {
          listener(ev);
        } catch {
          /* ignore */
        }
      }
    }
    eventListeners.add(listener);
    return () => eventListeners.delete(listener);
  }

  function clearEventLog() {
    eventLog.length = 0;
  }

  function captureCheckpoint(meta = runMeta) {
    const cp = activeProvider?.getCheckpoint?.();
    if (!cp?.messages?.length || !activeProvider?.supportsCheckpoint) return null;
    let writeMode = "chat";
    if (
      meta.op === "generate" ||
      meta.op === "game-setup" ||
      meta.op === "game-setup-run-all" ||
      meta.op === "vertical-slice" ||
      meta.op === "prototype-full"
    ) {
      writeMode = "generate";
    } else if (meta.op === "sync") writeMode = "sync";
    else if (meta.mode === "ask" || meta.op === "ask") writeMode = "ask";
    else if (cp.writeMode) writeMode = cp.writeMode;
    checkpoint = {
      messages: cp.messages,
      turn: cp.turn || 1,
      writeMode,
      model: cp.model,
      mode: meta.mode || "agent",
      op: meta.op || "chat",
      chatSkillId: meta.chatSkillId || "",
      forgeMode: meta.forgeMode || modeLabel,
      autonomy: !!meta.autonomy,
      at: cp.at || Date.now(),
    };
    return checkpoint;
  }

  function checkpointInfo() {
    if (!checkpoint?.messages?.length) return { resumable: false };
    return {
      resumable: true,
      turn: checkpoint.turn,
      mode: checkpoint.mode,
      op: checkpoint.op,
      chatSkillId: checkpoint.chatSkillId || "",
      forgeMode: checkpoint.forgeMode || modeLabel,
      model: checkpoint.model,
      at: checkpoint.at,
    };
  }

  /** Last provider turn: assistant text (for HARD-STOP detection) and its write audit. */
  let lastTurn = { text: "", startedAt: 0, audit: null };

  async function runProvider(prompt, { writeMode, op, mode, onEvent, resume, forgeModeOverride, attachments } = {}) {
    const forgeMode = forgeModeOverride || modeLabel;
    const picked = await pickProvider(root, { writeMode, slug, forgeMode });
    activeProvider = picked.provider;
    handlersStatus(picked, mode, op, onEvent, resume);
    wroteFiles = false;
    const assistantChunks = [];
    const normalizedForge = normalizeForgeMode(forgeMode);
    // Post-turn diff audit (VerticalSlice/Production writes). Cursor edits inside its own
    // sandbox, and MCP `eval` can write anything, so policy is enforced on the resulting diff.
    const audit =
      writeMode !== "ask" && writeMode !== "sync" && normalizedForge !== "Prototype" && resolveAuditMode(normalizedForge) !== "off"
        ? createWriteAudit({ root, forgeMode: normalizedForge, slug, writeMode: writeMode || "generate" })
        : null;
    if (audit) {
      try {
        await audit.begin();
      } catch (err) {
        onEvent?.({ type: "status", message: `Write audit snapshot failed: ${err?.message || err}` });
      }
    }
    lastTurn = { text: "", startedAt: Date.now(), audit };
    const trackWrites = (ev) => {
      if (ev?.type === "file") wroteFiles = true;
      if (ev?.type === "assistant" && ev.text) {
        assistantChunks.push(String(ev.text));
        lastTurn.text = mergeAssistantChunks(assistantChunks);
      }
      onEvent?.(ev);
    };
    const wrapped = wrapEvents({
      root,
      op: op || (mode === "ask" ? mode : "chat"),
      slug,
      picked,
      onEvent: trackWrites,
    });
    if (resume?.messages) {
      return picked.provider.run("", {
        onEvent: wrapped,
        signal: controller.signal,
        resumeMessages: resume.messages,
        resumeTurn: resume.turn,
        resumeUserMessage: resume.userMessage,
        resumeContext: resume.context,
        attachments: attachments || [],
      });
    }
    return picked.provider.run(prompt, {
      onEvent: wrapped,
      signal: controller.signal,
      attachments: attachments || [],
    });
  }

  function handlersStatus(picked, mode, op, onEvent, resume) {
    if (resume) {
      onEvent?.({
        type: "status",
        message: `Continue · ${picked.label || picked.id} · ${picked.model} · turn ${(resume.turn || 1) + 1}+`,
      });
      return;
    }
    if (op === "generate") {
      onEvent?.({
        type: "status",
        message: `Provider ${picked.id} · model ${picked.model}`,
      });
      return;
    }
    onEvent?.({
      type: "status",
      message: `${picked.label || picked.id} · ${picked.model} · ${
        mode === "ask" ? "Ask" : "Agent"
      }`,
    });
  }

  /** Close a single (non-looping) turn's audit and surface its findings. */
  async function finishStandaloneAudit(onEvent) {
    const audit = lastTurn.audit;
    lastTurn.audit = null;
    if (!audit) return null;
    try {
      const report = await audit.end();
      if (report.violations.length || report.hardStop) {
        onEvent?.({ type: "write-audit", violations: report.violations, hardStop: report.hardStop });
        onEvent?.({
          type: "notice",
          message: `Write audit: ${report.violations.length} path(s) outside policy${report.hardStop ? " — HARD STOP (TDD restored)" : ""}.`,
        });
        if (report.warningText) {
          lastAdviceDigest = [report.warningText, lastAdviceDigest].filter(Boolean).join("\n\n");
        }
      }
      return report;
    } catch (err) {
      onEvent?.({ type: "status", message: `Write audit failed: ${err?.message || err}` });
      return null;
    }
  }

  async function reanchorFor(state, { turn = 1, warnings = [] } = {}) {
    const knowledge = await listKnowledgeFiles(root, state || {});
    return buildReanchorBlock({
      pack,
      state,
      turn,
      maxTurns: autonomyConfig.maxTurns,
      elapsedMs: autonomyStatus.startedAt ? Date.now() - autonomyStatus.startedAt : 0,
      maxHours: autonomyConfig.maxHours,
      knowledge,
      warnings,
    });
  }

  function continuationRequest(state) {
    const next = state?.next ? String(state.next) : "the next incomplete stage in Docs/V57/PLAN.md";
    return [
      `Continue the autonomous run: do \`next\` = ${next}.`,
      state?.nextSpec?.length ? `Focus spec(s): ${state.nextSpec.join(", ")} (§B/§C inlined in the TDD extract).` : "",
      "Complete it, verify with real evidence, update Docs/V57/STATUS.json + DEVLOG + TODO, then end the turn. The runner will start the following stage.",
    ]
      .filter(Boolean)
      .join("\n");
  }

  /**
   * After the first successful turn: loop continuation turns until a stop condition.
   * @param {{ onEvent?: Function, initialState: object, firstResult: any,
   *   buildPrompt: (ctx: { turn: number, state: object, warnings: string[] }) => Promise<string>,
   *   runOpts: object }} opts
   */
  async function maybeRunAutonomy({ onEvent, initialState, firstResult, buildPrompt, runOpts }) {
    if (!autonomyConfig.enabled) {
      await finishStandaloneAudit(onEvent);
      return firstResult;
    }
    const gateMode = resolveGateMode(autonomyConfig.forgeMode);
    const verified = new Set();
    const inheritedGates = initialState?.gates || {};
    onEvent?.({
      type: "status",
      message: `Autonomous mode · max ${autonomyConfig.maxTurns} turns / ${autonomyConfig.maxHours} h · gates ${gateMode} · stop token .v57/ALLOW_STOP`,
    });
    try {
      const out = await runAutonomousLoop({
        root,
        config: autonomyConfig,
        status: autonomyStatus,
        signal: controller?.signal,
        onEvent,
        initialState,
        firstTurn: { result: firstResult, text: lastTurn.text, startedAt: lastTurn.startedAt, audit: lastTurn.audit },
        verifyGates: ({ prevState, turnStartedAt }) =>
          verifyGateClaims({
            root,
            forgeMode: autonomyConfig.forgeMode,
            mode: gateMode,
            prevGates: prevState?.gates || {},
            inheritedGates,
            turnStartedAt,
            verified,
          }),
        runTurn: async ({ turn, state, warnings }) => {
          const prompt = await buildPrompt({ turn, state, warnings });
          const result = await runProvider(prompt, runOpts);
          return { result, text: lastTurn.text, startedAt: lastTurn.startedAt, audit: lastTurn.audit };
        },
      });
      lastTurn.audit = null;
      return out.lastResult;
    } catch (err) {
      autonomyStatus.stopReason = `error: ${String(err?.message || err).slice(0, 200)}`;
      throw err;
    }
  }

  function resetAutonomy(op, requested = requestedAutonomy) {
    autonomyConfig = resolveAutonomyConfig({ forgeMode: activeForgeMode || modeLabel, op, requested });
    autonomyStatus = createAutonomyStatus(autonomyConfig);
    autonomyStatus.startedAt = Date.now();
  }

  function autonomyInfo() {
    return JSON.parse(JSON.stringify({ ...autonomyStatus, enabled: autonomyConfig.enabled }));
  }

  return {
    slug,
    forgeMode: modeLabel,
    layout,
    get busy() {
      return busy;
    },
    get providerInfo() {
      const s = getActiveProvider();
      return s ? { id: s.id, model: s.model, label: s.label } : null;
    },
    get lastAdviceDigest() {
      return lastAdviceDigest;
    },
    pushEvent,
    subscribe,
    clearEventLog,
    get eventCount() {
      return eventLog.length;
    },
    /** Incremental poll for Editor clients — survives domain reload (no long-lived SSE). */
    getEventsAfter(after = 0) {
      const start = Math.max(0, Math.min(Number(after) || 0, eventLog.length));
      return {
        after: start,
        nextAfter: eventLog.length,
        busy,
        forgeMode: modeLabel,
        autonomy: autonomyInfo(),
        events: eventLog.slice(start),
      };
    },
    getCheckpointInfo() {
      return { ...checkpointInfo(), autonomy: autonomyInfo() };
    },
    /** Autonomous-run status: turns, last stage/status/next, stop reason, gate/audit modes. */
    getAutonomy() {
      return autonomyInfo();
    },
    /** Non-blocking snapshot for "What's happening?" UI — does not touch busy/LLM. */
    getActivity() {
      const statuses = [];
      const files = [];
      const tools = [];
      let lastAssistant = "";
      let turn = checkpoint?.turn || 0;
      for (const ev of eventLog) {
        if (!ev || typeof ev !== "object") continue;
        const t = ev.type;
        if (t === "status" && ev.message) statuses.push(String(ev.message));
        if (t === "file" && ev.path) files.push(String(ev.path).replace(/\\/g, "/"));
        if (t === "tool") {
          tools.push({
            name: ev.name ? String(ev.name) : "tool",
            path: ev.path ? String(ev.path).replace(/\\/g, "/") : undefined,
            status: ev.status ? String(ev.status) : undefined,
          });
        }
        if (t === "assistant" && ev.text) lastAssistant = String(ev.text);
        if (t === "checkpoint" && ev.turn) turn = Number(ev.turn) || turn;
      }
      const uniqFiles = [...new Set(files)];
      const active = getActiveProvider();
      return {
        ok: true,
        busy,
        slug,
        forgeMode: modeLabel,
        op: runMeta.op,
        mode: runMeta.mode,
        turn,
        lastStatus: statuses.slice(-10),
        recentFiles: uniqFiles.slice(-24),
        recentTools: tools.slice(-16),
        lastAssistantSnippet: lastAssistant ? lastAssistant.slice(-500) : "",
        eventCount: eventLog.length,
        layout,
        autonomy: autonomyInfo(),
        provider: active
          ? { id: active.id, model: active.model, label: active.label }
          : null,
        at: Date.now(),
      };
    },
    async generateFinal(handlers = {}) {
      if (busy) throw new Error("Session busy");
      busy = true;
      controller = new AbortController();
      runMeta = { mode: "agent", op: "generate" };
      try {
        const forgeMode = handlers.forgeMode || modeLabel;
        activeForgeMode = normalizeForgeMode(forgeMode);
        resetAutonomy("generate", handlers.autonomy || requestedAutonomy);
        runMeta.autonomy = autonomyConfig.enabled;
        const initialState = autonomyConfig.forgeMode !== "Prototype" ? await readRunState(root) : null;
        const tdd = await loadTdd({ nextSpec: initialState?.nextSpec || [] });
        const skill = await loadForgeModeSkill(root, forgeMode, { firstTurn: true });
        const modeSkillBlock = formatSkillBlock(skill);
        if (skill.path && modeSkillBlock) {
          handlers.onEvent?.({
            type: "status",
            message: `V57 skill · ${skill.command}`,
          });
        }
        if (tdd.contextChars < tdd.fullChars) {
          handlers.onEvent?.({
            type: "status",
            message: `TDD extract · ${Math.round(tdd.contextChars / 1000)}k of ${Math.round(
              tdd.fullChars / 1000,
            )}k chars inlined (rest indexed by line)`,
          });
        }
        // Re-anchor from disk when a previous run left state behind (resume across sessions).
        const firstReanchor =
          initialState && initialState.source !== "none" ? await reanchorFor(initialState, { turn: 1 }) : "";
        const prompt = buildGenerateFinalPrompt({
          slug,
          tddText: tdd.contextText,
          tddRelPath: tdd.relPath,
          agentsMd,
          pack,
          forgeMode,
          qualityNotes: handlers.qualityNotes || "",
          modeSkillBlock,
          layout,
          reanchorBlock: firstReanchor,
        });
        const runOpts = {
          writeMode: "generate",
          op: "generate",
          mode: "agent",
          onEvent: handlers.onEvent,
          forgeModeOverride: normalizeForgeMode(forgeMode),
        };
        let result = await runProvider(prompt, runOpts);
        if (result?.status === "cancelled" || result?.resumable) {
          await finishStandaloneAudit(handlers.onEvent);
          autonomyStatus.stopReason = autonomyConfig.enabled ? "cancelled" : "";
          captureCheckpoint(runMeta);
          return result;
        }
        const continueSkill = await loadForgeModeSkill(root, forgeMode, { firstTurn: false });
        result = await maybeRunAutonomy({
          onEvent: handlers.onEvent,
          initialState: initialState || (await readRunState(root)),
          firstResult: result,
          runOpts,
          buildPrompt: async ({ turn, state, warnings }) => {
            const turnTdd = await loadTdd({ nextSpec: state?.nextSpec || [] });
            return buildGenerateFinalPrompt({
              slug,
              tddText: turnTdd.contextText,
              tddRelPath: turnTdd.relPath,
              agentsMd,
              pack,
              forgeMode,
              qualityNotes: handlers.qualityNotes || "",
              modeSkillBlock: formatSkillBlock(continueSkill),
              layout,
              reanchorBlock: await reanchorFor(state, { turn, warnings }),
              userRequest: continuationRequest(state),
            });
          },
        });
        if (result?.status === "cancelled" || result?.resumable) {
          captureCheckpoint(runMeta);
          return result;
        }
        checkpoint = null;
        const report = await emitSoftAdvice({
          root,
          tddText: tdd.text,
          onEvent: handlers.onEvent,
        });
        lastAdviceDigest = formatAdviceForChat(report?.advice || []);
        return result;
      } catch (err) {
        captureCheckpoint(runMeta);
        throw err;
      } finally {
        if (activeProvider?.getCheckpoint?.()?.messages?.length) {
          captureCheckpoint(runMeta);
        }
        busy = false;
        controller = null;
        activeProvider = null;
      }
    },
    async chat(message, handlers = {}) {
      if (busy) throw new Error("Session busy");
      busy = true;
      controller = new AbortController();
      const trimmed = String(message || "").trim();
      const rawMode = String(handlers.mode || "agent").toLowerCase();
      let mode = rawMode === "ask" ? "ask" : "agent";
      if (rawMode === "plan") {
        mode = "agent";
        handlers.onEvent?.({
          type: "notice",
          message: "Plan mode is disabled — using Agent. Choose Ask for read-only.",
        });
      }
      const readOnly = mode === "ask";
      if (trimmed) {
        chatHistory.push({
          role: "user",
          message: trimmed,
          mode,
          at: Date.now(),
        });
      }
      try {
        let tddText = "";
        let tddRelPath = "";
        /** Only skill runs inline the TDD — Ask and casual Agent read it with tools. */
        const loadTddForPrompt = async (nextSpec = []) => {
          try {
            const tdd = await loadTdd({ nextSpec });
            tddText = tdd.contextText;
            tddRelPath = tdd.relPath;
            return tdd;
          } catch {
            return null;
          }
        };
        const chatSkillId = !readOnly
          ? handlers.forceChatSkillId || detectChatSkillId(trimmed)
          : handlers.forceChatSkillId || null;
        let chatSkillBlock = "";
        let effectiveForgeMode = handlers.forgeMode || modeLabel;
        let providerWriteMode = readOnly ? mode : "chat";
        if (chatSkillId === "vertical-slice") {
          effectiveForgeMode = "VerticalSlice";
          providerWriteMode = "generate";
          activeChatSkillId = chatSkillId;
          activeForgeMode = "VerticalSlice";
          const skill = await loadForgeModeSkill(root, "VerticalSlice", { firstTurn: true });
          chatSkillBlock = formatSkillBlock(skill);
          handlers.onEvent?.({
            type: "status",
            message: `V57 skill · ${skill.command}${skill.paths?.length > 1 ? " + intake" : ""} · autonomous stage driver`,
          });
        } else if (chatSkillId === "game-setup" || chatSkillId === "game-setup-run-all") {
          effectiveForgeMode = "Production";
          providerWriteMode = "generate";
          activeChatSkillId = chatSkillId;
          activeForgeMode = "Production";
          const skill = await loadChatSkill(root, chatSkillId);
          // Run-all skill is a thin override — also inject the full staged skill body.
          if (chatSkillId === "game-setup-run-all") {
            const base = await loadChatSkill(root, "game-setup");
            chatSkillBlock = [
              formatSkillBlock(skill),
              "",
              "## Parent skill body (mandatory — apply run_all overrides above)",
              "",
              base.text || "",
            ]
              .filter(Boolean)
              .join("\n");
            handlers.onEvent?.({
              type: "status",
              message: `V57 skill · ${skill.command} · run_all (chain stages after INIT)`,
            });
          } else {
            chatSkillBlock = formatSkillBlock(skill);
            handlers.onEvent?.({
              type: "status",
              message: `V57 skill · ${skill.command} · staged pipeline (confirm/continue gates)`,
            });
          }
        } else if (chatSkillId === "prototype-full") {
          effectiveForgeMode = "Prototype";
          providerWriteMode = "generate";
          activeChatSkillId = "prototype-full";
          activeForgeMode = "Prototype";
          const skill = await loadChatSkill(root, chatSkillId);
          chatSkillBlock = formatSkillBlock(skill);
          try {
            const folders = await ensurePrototypeProjectFolders(root, slug);
            handlers.onEvent?.({
              type: "status",
              message: folders.created.length
                ? `V57 skill · ${skill.command} · created ${folders.created.join(", ")}`
                : `V57 skill · ${skill.command} · project root ${folders.projectRel}`,
            });
          } catch (err) {
            handlers.onEvent?.({
              type: "status",
              message: `V57 skill · ${skill.command} · folder bootstrap failed: ${err?.message || err}`,
            });
            throw err;
          }
        }
        const op = readOnly
          ? mode
          : chatSkillId === "prototype-full"
            ? "prototype-full"
            : chatSkillId === "game-setup-run-all"
              ? "game-setup-run-all"
              : chatSkillId === "game-setup"
                ? "game-setup"
                : chatSkillId === "vertical-slice"
                  ? "vertical-slice"
                  : "chat";
        // Autonomy applies to VerticalSlice/Production skill runs only (never Ask / casual chat).
        const skillRun = !!chatSkillBlock && normalizeForgeMode(effectiveForgeMode) !== "Prototype";
        resetAutonomy(op, skillRun ? handlers.autonomy || requestedAutonomy : "staged");
        if (!skillRun) {
          autonomyConfig = { ...autonomyConfig, enabled: false };
          autonomyStatus.enabled = false;
        }
        const initialState = skillRun ? await readRunState(root) : null;
        if (chatSkillBlock) await loadTddForPrompt(initialState?.nextSpec || []);
        runMeta = {
          mode,
          op,
          chatSkillId: chatSkillId || activeChatSkillId || "",
          forgeMode: effectiveForgeMode,
          autonomy: autonomyConfig.enabled,
        };
        const scopeMap =
          !readOnly && !chatSkillBlock
            ? await buildScopeMap({
                root,
                slug,
                forgeMode: effectiveForgeMode,
              })
            : "";
        let attachments = [];
        try {
          attachments = await resolveAttachments(
            path.join(root, "Tools", "gameforge-agent"),
            handlers.attachmentIds || [],
          );
        } catch (attErr) {
          const classified = classifyAgentError(attErr);
          handlers.onEvent?.({
            type: "error",
            error: formatClassifiedError(classified),
            errorKind: classified.kind,
            retryable: classified.retryable,
          });
          throw attErr;
        }
        if (attachments.length) {
          const visionOk = modelLikelySupportsVision(
            getActiveProvider()?.model || "",
          );
          handlers.onEvent?.({
            type: "notice",
            message: visionOk
              ? `Attached ${attachments.length} image(s) for this turn.`
              : `Attached ${attachments.length} image(s). This model may not see images — paths listed in the prompt; switch to a vision-capable model if needed.`,
          });
        }
        const firstReanchor =
          skillRun && initialState && initialState.source !== "none"
            ? await reanchorFor(initialState, { turn: 1 })
            : "";
        const prompt = buildChatPrompt({
          slug,
          message: trimmed,
          agentsMd,
          pack,
          tddText,
          tddRelPath,
          adviceDigest: lastAdviceDigest,
          mode,
          chatSkillId: chatSkillId || activeChatSkillId || "",
          chatSkillBlock,
          forgeMode: effectiveForgeMode,
          scopeMap,
          layout,
          reanchorBlock: firstReanchor,
        });
        const onEvent = (ev) => {
          handlers.onEvent?.(ev);
        };
        const runOpts = {
          writeMode: providerWriteMode,
          op,
          mode,
          onEvent,
          forgeModeOverride: effectiveForgeMode,
        };
        let result = await runProvider(prompt, { ...runOpts, attachments });
        if (result?.status === "cancelled" || result?.resumable) {
          await finishStandaloneAudit(handlers.onEvent);
          if (autonomyConfig.enabled) autonomyStatus.stopReason = "cancelled";
          captureCheckpoint(runMeta);
          return { result, mode, resumable: true };
        }
        if (skillRun) {
          // VerticalSlice: the intake companion skill is only for the first turn.
          const continueBlock =
            chatSkillId === "vertical-slice"
              ? formatSkillBlock(await loadForgeModeSkill(root, "VerticalSlice", { firstTurn: false }))
              : chatSkillBlock;
          result = await maybeRunAutonomy({
            onEvent: handlers.onEvent,
            initialState,
            firstResult: result,
            runOpts,
            buildPrompt: async ({ turn, state, warnings }) => {
              const turnTdd = await loadTdd({ nextSpec: state?.nextSpec || [] }).catch(() => null);
              return buildChatPrompt({
                slug,
                message: continuationRequest(state),
                agentsMd,
                pack,
                tddText: turnTdd?.contextText || tddText,
                tddRelPath: turnTdd?.relPath || tddRelPath,
                mode,
                chatSkillId: chatSkillId || activeChatSkillId || "",
                chatSkillBlock: continueBlock,
                forgeMode: effectiveForgeMode,
                layout,
                reanchorBlock: await reanchorFor(state, { turn, warnings }),
              });
            },
          });
          if (result?.status === "cancelled" || result?.resumable) {
            captureCheckpoint(runMeta);
            return { result, mode, resumable: true };
          }
        } else {
          await finishStandaloneAudit(handlers.onEvent);
        }
        checkpoint = null;
        if (!readOnly && (wroteFiles || chatSkillBlock)) {
          const advised = await loadTdd().catch(() => null);
          const report = await emitSoftAdvice({
            root,
            tddText: advised?.text || "",
            onEvent: handlers.onEvent,
          });
          lastAdviceDigest = formatAdviceForChat(report?.advice || []);
          handlers.onEvent?.({
            type: "notice",
            message: "Enter Play Mode to verify the changes.",
          });
        }
        return { result, mode };
      } catch (err) {
        captureCheckpoint(runMeta);
        throw err;
      } finally {
        if (activeProvider?.getCheckpoint?.()?.messages?.length) {
          captureCheckpoint(runMeta);
        }
        busy = false;
        controller = null;
        activeProvider = null;
      }
    },
    async continueFromCheckpoint(handlers = {}) {
      if (busy) throw new Error("Session busy");
      const continueMessage = String(handlers.continueMessage || "continue").trim() || "continue";

      // No LLM checkpoint (Cursor): caller should use /chat with forceChatSkillId instead.
      if (!checkpoint?.messages?.length) {
        throw new Error(
          activeChatSkillId === "game-setup" || activeChatSkillId === "game-setup-run-all"
            ? "No LLM checkpoint — type continue in Chat (or use an OpenAI-compatible provider). Cursor cannot resume mid-run."
            : "No checkpoint — Stop an LLM run first (Cursor cannot resume mid-run)",
        );
      }

      busy = true;
      controller = new AbortController();
      const cp = checkpoint;
      runMeta = {
        mode: cp.mode || "agent",
        op: cp.op || "chat",
        chatSkillId: cp.chatSkillId || activeChatSkillId || "",
        forgeMode: cp.forgeMode || activeForgeMode || modeLabel,
        autonomy: !!cp.autonomy,
      };
      // Every resume of a VerticalSlice/Production write run re-anchors on disk state first.
      const resumeForge = normalizeForgeMode(cp.forgeMode || activeForgeMode || modeLabel);
      const reanchorResume = cp.mode !== "ask" && cp.op !== "ask" && resumeForge !== "Prototype";
      const resumeState = reanchorResume ? await readRunState(root).catch(() => null) : null;
      if (cp.autonomy) {
        // Keep the run's budget (turns/started) — a resume continues the same autonomous run.
        const prev = autonomyStatus;
        autonomyConfig = resolveAutonomyConfig({ forgeMode: resumeForge, op: cp.op, requested: "auto" });
        autonomyStatus = { ...createAutonomyStatus(autonomyConfig), turns: prev.turns, startedAt: prev.startedAt || Date.now() };
      }
      const resumeUserMessage = [
        reanchorResume ? await reanchorFor(resumeState, { turn: (autonomyStatus.turns || 0) + 1 }) : "",
        buildResumeUserMessage(cp, continueMessage),
      ]
        .filter(Boolean)
        .join("\n\n");
      try {
        const onEvent = (ev) => {
          handlers.onEvent?.(ev);
        };
        // Migrate legacy plan checkpoints to agent
        const resumeMode = cp.mode === "plan" ? "agent" : cp.mode || "agent";
        const resumeWriteMode =
          cp.writeMode === "plan" ? "chat" : cp.writeMode || "chat";
        let result = await runProvider("", {
          writeMode: resumeWriteMode,
          op: cp.op === "plan" ? "chat" : cp.op,
          mode: resumeMode,
          forgeModeOverride: cp.forgeMode || activeForgeMode || modeLabel,
          onEvent,
          resume: {
            messages: cp.messages,
            turn: cp.turn,
            userMessage: resumeUserMessage,
            context: {
              op: cp.op === "plan" ? "chat" : cp.op,
              chatSkillId: cp.chatSkillId,
              forgeMode: cp.forgeMode,
            },
          },
        });
        if (result?.status === "cancelled" || result?.resumable) {
          await finishStandaloneAudit(handlers.onEvent);
          captureCheckpoint(runMeta);
          return { result, mode: resumeMode, resumable: true, continued: true };
        }
        activeProvider?.clearCheckpoint?.();
        if (cp.autonomy && autonomyConfig.enabled) {
          // Resume finished its stage — keep driving the autonomous run with fresh-prompt turns.
          const skill =
            cp.op === "vertical-slice" || cp.op === "generate"
              ? formatSkillBlock(await loadForgeModeSkill(root, resumeForge, { firstTurn: false }))
              : formatSkillBlock(await loadChatSkill(root, cp.chatSkillId || "game-setup"));
          result = await maybeRunAutonomy({
            onEvent: handlers.onEvent,
            initialState: resumeState || (await readRunState(root)),
            firstResult: result,
            runOpts: {
              writeMode: "generate",
              op: cp.op,
              mode: "agent",
              onEvent,
              forgeModeOverride: resumeForge,
            },
            buildPrompt: async ({ turn, state, warnings }) => {
              const turnTdd = await loadTdd({ nextSpec: state?.nextSpec || [] }).catch(() => null);
              return buildGenerateFinalPrompt({
                slug,
                tddText: turnTdd?.contextText || "",
                tddRelPath: turnTdd?.relPath || "",
                agentsMd,
                pack,
                forgeMode: resumeForge,
                modeSkillBlock: skill,
                layout,
                reanchorBlock: await reanchorFor(state, { turn, warnings }),
                userRequest: continuationRequest(state),
              });
            },
          });
          if (result?.status === "cancelled" || result?.resumable) {
            captureCheckpoint(runMeta);
            return { result, mode: resumeMode, resumable: true, continued: true };
          }
        } else {
          await finishStandaloneAudit(handlers.onEvent);
        }
        checkpoint = null;
        if (resumeMode !== "ask" && cp.op !== "ask" && (wroteFiles || cp.op !== "chat")) {
          const advised = await loadTdd().catch(() => null);
          const report = await emitSoftAdvice({
            root,
            tddText: advised?.text || "",
            onEvent: handlers.onEvent,
          });
          lastAdviceDigest = formatAdviceForChat(report?.advice || []);
        }
        return { result, mode: resumeMode, continued: true };
      } catch (err) {
        captureCheckpoint(runMeta);
        throw err;
      } finally {
        if (activeProvider?.getCheckpoint?.()?.messages?.length) {
          captureCheckpoint(runMeta);
        }
        busy = false;
        controller = null;
        activeProvider = null;
      }
    },
    async previewSyncTdd({ summary = "", chatDigest = "" } = {}, handlers = {}) {
      if (busy) throw new Error("Session busy");
      busy = true;
      controller = new AbortController();
      runMeta = { mode: "ask", op: "sync" };
      const assistantChunks = [];
      try {
        const tdd = await loadTdd();
        const gameplayFiles = await listGameplayFiles(root);
        const digest = mergeChatDigest(chatHistory, chatDigest);
        const prompt = buildSyncPreviewPrompt({
          slug,
          tddText: tdd.text,
          tddRelPath: tdd.relPath,
          summary:
            summary ||
            "List TDD updates implied by the current playable vs the spec.",
          chatDigest: digest,
          gameplayFiles,
          root,
          pack,
        });
        const onEvent = (ev) => {
          if (ev?.type === "assistant" && ev.text) assistantChunks.push(String(ev.text));
          handlers.onEvent?.(ev);
        };
        const result = await runProvider(prompt, {
          writeMode: "ask",
          op: "sync",
          mode: "ask",
          onEvent,
        });
        if (result?.status === "cancelled" || result?.resumable) {
          captureCheckpoint(runMeta);
          return { items: [], cancelled: true };
        }
        const proposal = parseSyncProposal(mergeAssistantChunks(assistantChunks));
        handlers.onEvent?.({ type: "sync-proposal", items: proposal.items });
        return proposal;
      } catch (err) {
        captureCheckpoint(runMeta);
        throw err;
      } finally {
        if (activeProvider?.getCheckpoint?.()?.messages?.length) {
          captureCheckpoint(runMeta);
        }
        busy = false;
        controller = null;
        activeProvider = null;
      }
    },
    async syncTdd({ summary = "", chatDigest = "", selectedItems = [] } = {}, handlers = {}) {
      if (busy) throw new Error("Session busy");
      busy = true;
      controller = new AbortController();
      runMeta = { mode: "agent", op: "sync" };
      try {
        const tdd = await loadTdd();
        const gameplayFiles = await listGameplayFiles(root);
        const digest = mergeChatDigest(chatHistory, chatDigest);
        const selected = Array.isArray(selectedItems) ? selectedItems : [];
        const prompt = buildSyncPrompt({
          slug,
          tddText: tdd.text,
          tddRelPath: tdd.relPath,
          summary:
            summary ||
            (digest
              ? "Sync validated chat iterations and prototype behavior into the TDD"
              : "Promote validated prototype behavior into the TDD product spec"),
          chatDigest: digest,
          selectedItems: formatSelectedSyncItems(selected),
          gameplayFiles,
          root,
          agentsMd,
          pack,
        });
        const result = await runProvider(prompt, {
          writeMode: "sync",
          op: "sync",
          mode: "agent",
          onEvent: handlers.onEvent,
        });
        if (result?.status === "cancelled" || result?.resumable) {
          captureCheckpoint(runMeta);
          return result;
        }
        checkpoint = null;
        return await finalizeTddSync(tddsRoot, slug);
      } catch (err) {
        captureCheckpoint(runMeta);
        throw err;
      } finally {
        if (activeProvider?.getCheckpoint?.()?.messages?.length) {
          captureCheckpoint(runMeta);
        }
        busy = false;
        controller = null;
        activeProvider = null;
      }
    },
    cancel() {
      if (autonomyStatus.active) autonomyStatus.stopReason = "cancelled";
      controller?.abort?.();
      try {
        activeProvider?.cancel?.();
      } catch {
        /* ignore */
      }
      // Abort again on next tick — tool/LLM awaits sometimes miss the first signal.
      setTimeout(() => {
        try {
          controller?.abort?.();
          activeProvider?.cancel?.();
        } catch {
          /* ignore */
        }
      }, 50);
      setTimeout(() => {
        try {
          controller?.abort?.();
          activeProvider?.cancel?.();
        } catch {
          /* ignore */
        }
      }, 250);
      captureCheckpoint(runMeta);
      return checkpointInfo();
    },
  };
}
