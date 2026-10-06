import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import dotenv from "dotenv";
import express from "express";
import { randomUUID } from "node:crypto";
import { listTdds, readTdd, importTddUpload } from "./src/tdd/parser.js";
import { createSession } from "./src/agent/session.js";
import {
  providerStatus,
  initProviderCatalog,
  setActiveProvider,
} from "./src/agent/providers/catalog.js";
import { initBenchmarkStore, getBenchmarkState, clearBenchmark } from "./src/agent/benchmarkStore.js";
import { runContextPostmortem, getGraphHtmlPath } from "./src/agent/contextPostmortem.js";
import { assertSafeSlug } from "./src/security/paths.js";
import {
  getUnityMcpResolution,
  isOfficialUnityMcpConfigured,
  isUnityMcpClientEnabled,
} from "./src/agent/mcp-config.js";
import { pingUnityMcp, resetSharedUnityMcpSession } from "./src/agent/unityMcpPool.js";
import {
  isUnityCliIntegrationEnabled,
  preferUnityTestViaCli,
  unityCliPreflight,
  unityCommand,
  unityEval,
  unityTest,
} from "./src/agent/unityCli.js";
import { normalizeForgeMode } from "./src/agent/writePolicy.js";
import multer from "multer";
import {
  saveAttachment,
  ATTACHMENT_LIMITS,
  ensureAttachmentsDir,
} from "./src/agent/attachments.js";
import { classifyAgentError, formatClassifiedError } from "./src/agent/errors.js";
import { ensureTlsExtraCa } from "./src/agent/tlsCa.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const AGENT_ROOT = __dirname;
const PROJECT_ROOT = path.resolve(process.env.GAMEFORGE_PROJECT_ROOT || path.resolve(__dirname, "../.."));
const TDDS = path.join(PROJECT_ROOT, "Docs", "tdds");
const SESSIONS = path.join(AGENT_ROOT, "sessions");
const PORT = Number(process.env.GAMEFORGE_PORT || process.env.PORT || 3857);
const HOST = process.env.GAMEFORGE_HOST || process.env.HOST || "127.0.0.1";
const TOKEN = process.env.GAMEFORGE_TOKEN || "";

const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: ATTACHMENT_LIMITS.maxBytes, files: 1 },
});

// Load secrets: user profile then project .env.local (never log values)
function loadEnvFiles() {
  const home = process.env.USERPROFILE || process.env.HOME || "";
  const candidates = [
    path.join(home, ".v57", "gameforge.env"),
    path.join(PROJECT_ROOT, ".env.local"),
    path.join(AGENT_ROOT, ".env"),
  ];
  for (const p of candidates) {
    if (!fs.existsSync(p)) continue;
    // Fill keys that are missing or blank. Never let an empty placeholder wipe a real key.
    const parsed = dotenv.parse(fs.readFileSync(p));
    for (const [key, value] of Object.entries(parsed)) {
      const next = String(value ?? "").trim();
      if (!next) continue;
      const current = process.env[key];
      if (current == null || String(current).trim() === "") process.env[key] = next;
    }
  }
}
loadEnvFiles();
const tlsCaStatus = ensureTlsExtraCa(AGENT_ROOT);

const sessions = new Map();
/** @type {string | null} */
let activeSessionId = null;
const app = express();
app.disable("x-powered-by");
// 4mb: PNG/JPEG ≤2MB as base64 (~2.7MB) for /api/attachments/json
app.use(express.json({ limit: "4mb" }));

app.use((req, res, next) => {
  res.setHeader("X-Content-Type-Options", "nosniff");
  res.setHeader("X-Frame-Options", "SAMEORIGIN");
  // Localhost-only agent: require session token when configured
  if (TOKEN) {
    const hdr = req.headers["x-gameforge-token"];
    if (hdr !== TOKEN && req.path !== "/api/health") {
      // health allowed without token for bootstrap probes? require token always except we allow health for readiness
    }
    if (req.path !== "/api/health" && hdr !== TOKEN) {
      return res.status(401).json({ error: "Unauthorized" });
    }
  }
  next();
});

/** Optional per-request autonomy override: "auto" | "staged" (anything else → forge-mode default). */
function parseAutonomy(raw) {
  const v = String(raw ?? "").trim().toLowerCase();
  if (raw === true || ["auto", "autonomous", "on"].includes(v)) return "auto";
  if (raw === false || ["staged", "off", "manual"].includes(v)) return "staged";
  return "";
}

function parseSlug(raw) {
  try {
    return assertSafeSlug(raw);
  } catch {
    return null;
  }
}

function sseInit(res) {
  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");
  res.flushHeaders?.();
  return (obj) => res.write(`data: ${JSON.stringify(obj)}\n\n`);
}

/**
 * Fan-out session events to this HTTP SSE client. Client disconnect does NOT cancel the run.
 * @returns {{ send: (obj: object) => void, detach: () => void }}
 */
function attachSseClient(res, session, { replay = false } = {}) {
  const rawSend = sseInit(res);
  let closed = false;
  const send = (obj) => {
    if (closed) return;
    try {
      rawSend(obj);
    } catch {
      closed = true;
    }
  };
  const onClose = () => {
    closed = true;
  };
  res.on("close", onClose);
  const unsub = session.subscribe((ev) => send(ev), { replay });
  return {
    send,
    detach() {
      closed = true;
      try {
        res.removeListener("close", onClose);
      } catch {
        /* ignore */
      }
      unsub();
    },
  };
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

app.get("/api/health", (_req, res) => {
  const mcpResolution = getUnityMcpResolution(PROJECT_ROOT);
  res.json({
    ok: true,
    name: "v57-unity-gameforge-agent",
    port: PORT,
    projectRoot: PROJECT_ROOT,
    activeSessionId,
    busySessions: [...sessions.entries()].filter(([, s]) => s?.busy).map(([id]) => id),
    activeAutonomy: activeSessionId ? sessions.get(activeSessionId)?.getAutonomy?.() || null : null,
    unityMcpConfigured: isOfficialUnityMcpConfigured(PROJECT_ROOT),
    unityMcpClientEnabled: isUnityMcpClientEnabled(),
    unityMcpMode: mcpResolution.mode,
    unityCliAvailable: mcpResolution.cliAvailable,
    unityCliVersion: mcpResolution.cliVersion,
    unityPipelineInstalled: mcpResolution.pipelineInstalled,
    unityMcpPreference: mcpResolution.preference,
    unityCliIntegrationEnabled: isUnityCliIntegrationEnabled(),
    unityTestViaCliPreferred: preferUnityTestViaCli(),
    tlsExtraCa: !!tlsCaStatus?.ok,
    attachments: ATTACHMENT_LIMITS,
  });
});

/** Upload a compressed gameplay capture (multipart). */
app.post("/api/attachments", (req, res) => {
  upload.single("file")(req, res, async (err) => {
    try {
      if (err) {
        const classified = classifyAgentError(
          Object.assign(err, {
            code: err.code === "LIMIT_FILE_SIZE" ? "ATTACHMENT_TOO_LARGE" : err.code,
            message:
              err.code === "LIMIT_FILE_SIZE"
                ? `Attachment too large. Max ${ATTACHMENT_LIMITS.maxBytes / 1024} KB.`
                : err.message,
          }),
        );
        return res.status(400).json({
          ok: false,
          error: formatClassifiedError(classified),
          errorKind: classified.kind,
        });
      }
      if (!req.file?.buffer) {
        return res.status(400).json({
          ok: false,
          error: "Missing file field (multipart name: file)",
          errorKind: "attachment",
        });
      }
      await ensureAttachmentsDir(AGENT_ROOT);
      const meta = await saveAttachment(AGENT_ROOT, {
        buffer: req.file.buffer,
        mime: req.file.mimetype,
        originalName: req.file.originalname,
      });
      res.json({
        ok: true,
        id: meta.id,
        mime: meta.mime,
        bytes: meta.bytes,
        relPath: meta.relPath,
        limits: ATTACHMENT_LIMITS,
      });
    } catch (e) {
      const classified = classifyAgentError(e);
      res.status(400).json({
        ok: false,
        error: formatClassifiedError(classified),
        errorKind: classified.kind,
      });
    }
  });
});

/** JSON+base64 upload — preferred from Unity Editor (avoids MultipartFormDataContent NREs). */
app.post("/api/attachments/json", async (req, res) => {
  try {
    const b64 = String(req.body?.base64 || "").replace(/\s+/g, "");
    if (!b64) {
      return res.status(400).json({
        ok: false,
        error: "Missing base64 field",
        errorKind: "attachment",
      });
    }
    const buffer = Buffer.from(b64, "base64");
    await ensureAttachmentsDir(AGENT_ROOT);
    const meta = await saveAttachment(AGENT_ROOT, {
      buffer,
      mime: String(req.body?.mime || "image/jpeg"),
      originalName: String(req.body?.fileName || "capture.jpg").slice(0, 120),
    });
    res.json({
      ok: true,
      id: meta.id,
      mime: meta.mime,
      bytes: meta.bytes,
      relPath: meta.relPath,
      limits: ATTACHMENT_LIMITS,
    });
  } catch (e) {
    const classified = classifyAgentError(e);
    res.status(400).json({
      ok: false,
      error: formatClassifiedError(classified),
      errorKind: classified.kind,
    });
  }
});

/** Unity CLI Pipeline preflight (no MCP session; safe during Forge Chat runs). */
app.get("/api/unity-cli/preflight", async (_req, res) => {
  try {
    const result = await unityCliPreflight(PROJECT_ROOT);
    res.status(result.ok ? 200 : 503).json(result);
  } catch (err) {
    res.status(500).json({ ok: false, error: err.message });
  }
});

/** Run `unity command eval` against the warm Editor (read-back / OVR). */
app.post("/api/unity-cli/eval", async (req, res) => {
  try {
    if (!isUnityCliIntegrationEnabled()) {
      return res.status(503).json({ ok: false, error: "Unity CLI integration disabled (GAMEFORGE_UNITY_CLI=0)" });
    }
    const code = String(req.body?.code || "").trim();
    if (!code) return res.status(400).json({ ok: false, error: "Missing body.code" });
    const timeoutMs = Number(req.body?.timeoutMs) || 90_000;
    const result = unityEval(code, { projectRoot: PROJECT_ROOT, timeoutMs });
    res.status(result.ok ? 200 : 503).json(result);
  } catch (err) {
    res.status(500).json({ ok: false, error: err.message });
  }
});

/** Run an arbitrary Pipeline command (`save_all`, `get_scene_hierarchy`, `editor_play`, …). */
app.post("/api/unity-cli/command", async (req, res) => {
  try {
    if (!isUnityCliIntegrationEnabled()) {
      return res.status(503).json({ ok: false, error: "Unity CLI integration disabled (GAMEFORGE_UNITY_CLI=0)" });
    }
    const name = String(req.body?.name || "").trim();
    if (!name) return res.status(400).json({ ok: false, error: "Missing body.name" });
    const args = Array.isArray(req.body?.args) ? req.body.args.map(String) : [];
    const timeoutMs = Number(req.body?.timeoutMs) || 120_000;
    const result = unityCommand(name, args, { projectRoot: PROJECT_ROOT, timeoutMs });
    res.status(result.ok ? 200 : 503).json(result);
  } catch (err) {
    res.status(500).json({ ok: false, error: err.message });
  }
});

/** Run `unity test` in a separate batch Editor (does not thrash GUI MCP session). */
app.post("/api/unity-cli/test", async (req, res) => {
  try {
    if (!isUnityCliIntegrationEnabled()) {
      return res.status(503).json({ ok: false, error: "Unity CLI integration disabled (GAMEFORGE_UNITY_CLI=0)" });
    }
    const mode = req.body?.mode ? String(req.body.mode) : undefined;
    const filter = req.body?.filter ? String(req.body.filter) : undefined;
    const output = req.body?.output ? String(req.body.output) : undefined;
    const timeoutMs = Number(req.body?.timeoutMs) || 600_000;
    const result = unityTest({ projectRoot: PROJECT_ROOT, mode, filter, output, timeoutMs });
    res.status(result.ok ? 200 : 503).json(result);
  } catch (err) {
    res.status(500).json({ ok: false, error: err.message });
  }
});

/** Liveness probe for official Unity MCP (reuses shared connection). */
app.post("/api/mcp/ping", async (_req, res) => {
  try {
    const result = await pingUnityMcp(PROJECT_ROOT);
    // 200 when intentionally disabled so Workbench does not treat it as a hard failure UX
    const status = result.disabled ? 200 : result.ok ? 200 : 503;
    res.status(status).json(result);
  } catch (err) {
    res.status(500).json({
      ok: false,
      error: err.message,
      hint: "Restart agent (Bootstrap) and verify Unity CLI (`unity mcp`) or legacy bridge. See V57/docs/mcp/UNITY_CLI_MIGRATION.md.",
    });
  }
});

app.post("/api/mcp/reset", async (_req, res) => {
  try {
    await resetSharedUnityMcpSession();
    const result = await pingUnityMcp(PROJECT_ROOT);
    const status = result.disabled ? 200 : result.ok ? 200 : 503;
    res.status(status).json({ reset: true, ...result });
  } catch (err) {
    res.status(500).json({ ok: false, reset: true, error: err.message });
  }
});

app.get("/api/tdds", async (_req, res) => {
  try {
    res.json({ tdds: await listTdds(TDDS) });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.get("/api/tdds/:slug", async (req, res) => {
  const slug = parseSlug(req.params.slug);
  if (!slug) return res.status(400).json({ error: "Invalid slug" });
  try {
    const tdd = await readTdd(TDDS, slug);
    res.json({
      slug: tdd.slug,
      projectName: tdd.projectName,
      mechanics: tdd.mechanics.map((m) => ({ id: m.id, title: m.title, type: m.type })),
    });
  } catch (err) {
    res.status(404).json({ error: err.message });
  }
});

app.get("/api/agent/providers", async (_req, res) => {
  await initProviderCatalog(PROJECT_ROOT);
  res.json(providerStatus());
});

app.post("/api/agent/provider", async (req, res) => {
  try {
    await initProviderCatalog(PROJECT_ROOT);
    const slot = await setActiveProvider(req.body?.id, req.body?.model);
    res.json({ active: slot.id, model: slot.model, ...providerStatus() });
  } catch (err) {
    res.status(400).json({ error: err.message });
  }
});

app.post("/api/sessions/resume", async (req, res) => {
  res.json({ ok: true, message: "Resume playable: open the active Prototype/VerticalSlice scene in Unity and enter Play Mode." });
});

/** Cancel one session (body.id) or every busy session. */
app.post("/api/sessions/cancel", (req, res) => {
  const want = typeof req.body?.id === "string" ? req.body.id : "";
  let cancelled = 0;
  const ids = [];
  if (want && sessions.has(want)) {
    try {
      sessions.get(want).cancel();
      cancelled = 1;
      ids.push(want);
    } catch (err) {
      return res.status(500).json({ error: err.message });
    }
  } else {
    for (const [id, session] of sessions) {
      try {
        // Cancel every known session — not only those still flagged busy
        // (busy may lag behind an in-flight LLM turn).
        session.cancel();
        cancelled += 1;
        ids.push(id);
      } catch {
        /* continue */
      }
    }
  }
  if (activeSessionId && ids.includes(activeSessionId)) activeSessionId = null;
  res.json({ ok: true, cancelled, ids, activeSessionId });
});

app.post("/api/sessions/:id/cancel", (req, res) => {
  const id = req.params.id;
  const session = sessions.get(id);
  if (!session) return res.status(404).json({ error: "session not found" });
  try {
    const info = session.cancel();
    if (activeSessionId === id) activeSessionId = null;
    res.json({ ok: true, id, ...info });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post("/api/sessions/generate-final", async (req, res) => {
  const slug = parseSlug(req.body?.slug);
  if (!slug) return res.status(400).json({ error: "slug required" });
  const forgeMode = normalizeForgeMode(req.body?.forgeMode || "Prototype");
  try {
    await initProviderCatalog(PROJECT_ROOT);
    await initBenchmarkStore(PROJECT_ROOT);
    const id = randomUUID();
    const session = await createSession({
      id,
      root: PROJECT_ROOT,
      tddsRoot: TDDS,
      sessionsRoot: SESSIONS,
      slug,
      forgeMode,
      autonomy: parseAutonomy(req.body?.autonomy),
    });
    sessions.set(id, session);
    activeSessionId = id;
    const { detach } = attachSseClient(res, session, { replay: false });
    session.pushEvent({ type: "session", id, forgeMode, op: "generate", autonomy: session.getAutonomy?.() });
    try {
      const out = await session.generateFinal({
        onEvent: (ev) => session.pushEvent(ev),
        forgeMode,
        qualityNotes: req.body?.qualityNotes || "",
        autonomy: parseAutonomy(req.body?.autonomy),
      });
      if (out?.status === "cancelled" || out?.resumable) {
        const cp = session.getCheckpointInfo?.() || {};
        session.pushEvent({
          type: "done",
          status: "cancelled",
          resumable: true,
          turn: cp.turn || 0,
          autonomy: session.getAutonomy?.(),
        });
      } else {
        session.pushEvent({ type: "done", status: "finished", autonomy: session.getAutonomy?.() });
      }
    } catch (err) {
      const msg = err?.message || String(err);
      const cancelled = /abort|stopp?ed|cancel/i.test(msg);
      session.pushEvent({
        type: cancelled ? "cancelled" : "error",
        error: msg,
        status: cancelled ? "cancelled" : "error",
      });
    } finally {
      if (activeSessionId === id) activeSessionId = null;
      detach();
    }
    try {
      res.end();
    } catch {
      /* client already gone */
    }
  } catch (err) {
    if (!res.headersSent) res.status(500).json({ error: err.message });
    else {
      try {
        res.write(`data: ${JSON.stringify({ type: "error", error: err.message })}\n\n`);
        res.end();
      } catch {
        /* ignore */
      }
    }
  }
});

app.post("/api/sessions/chat", async (req, res) => {
  try {
    await initProviderCatalog(PROJECT_ROOT);
    const slug = parseSlug(req.body?.slug) || "MyGame";
    const forgeMode = normalizeForgeMode(req.body?.forgeMode || "Prototype");
    const id = randomUUID();
    const session = await createSession({
      id,
      root: PROJECT_ROOT,
      tddsRoot: TDDS,
      sessionsRoot: SESSIONS,
      slug,
      forgeMode,
      autonomy: parseAutonomy(req.body?.autonomy),
    });
    sessions.set(id, session);
    activeSessionId = id;
    const { detach } = attachSseClient(res, session, { replay: false });
    session.pushEvent({ type: "session", id, forgeMode, op: "chat" });
    try {
      const attachmentIds = Array.isArray(req.body?.attachmentIds)
        ? req.body.attachmentIds.map(String)
        : [];
      const out = await session.chat(req.body?.message || "", {
        mode: req.body?.chatMode || "agent",
        onEvent: (ev) => session.pushEvent(ev),
        forgeMode,
        forceChatSkillId: req.body?.forceChatSkillId || "",
        attachmentIds,
        autonomy: parseAutonomy(req.body?.autonomy),
      });
      // Emit terminal done only after session.chat finally cleared busy
      // (soft-advice included). Provider-level "done" is stripped in wrapEvents.
      if (out?.resumable || out?.result?.status === "cancelled" || out?.result?.resumable) {
        const cp = session.getCheckpointInfo?.() || {};
        session.pushEvent({
          type: "done",
          status: "cancelled",
          resumable: true,
          turn: cp.turn || out?.result?.turn || 0,
          autonomy: session.getAutonomy?.(),
        });
      } else {
        session.pushEvent({ type: "done", status: "finished", autonomy: session.getAutonomy?.() });
      }
    } catch (err) {
      const msg = err?.message || String(err);
      const cancelled = /abort|stopp?ed|cancel/i.test(msg);
      const cp = session.getCheckpointInfo?.() || {};
      session.pushEvent({
        type: cancelled ? "cancelled" : "error",
        error: msg,
        status: cancelled ? "cancelled" : "error",
        resumable: !!cp.resumable,
        turn: cp.turn,
      });
    } finally {
      if (activeSessionId === id) activeSessionId = null;
      detach();
    }
    try {
      res.end();
    } catch {
      /* client already gone */
    }
  } catch (err) {
    if (!res.headersSent) res.status(500).json({ error: err.message });
    else {
      try {
        res.write(`data: ${JSON.stringify({ type: "error", error: err.message })}\n\n`);
        res.end();
      } catch {
        /* ignore */
      }
    }
  }
});

/**
 * Poll session events (short HTTP) — preferred Unity client path after domain reload.
 * ?after=N returns events[N..] plus busy flag; safe to call every ~500ms.
 */
app.get("/api/sessions/:id/events/poll", (req, res) => {
  const id = req.params.id;
  const session = sessions.get(id);
  if (!session) return res.status(404).json({ error: "session not found" });
  const after = Math.max(0, parseInt(String(req.query.after || "0"), 10) || 0);
  const payload = session.getEventsAfter?.(after) || {
    after,
    nextAfter: after,
    busy: false,
    events: [],
  };
  res.json({ ok: true, sessionId: id, ...payload });
});

/**
 * Reattach to a live (or just-finished) session after Play Mode / domain reload.
 * Replays buffered events, then streams live until the run ends.
 */
app.get("/api/sessions/:id/events", async (req, res) => {
  const id = req.params.id;
  const session = sessions.get(id);
  if (!session) return res.status(404).json({ error: "session not found" });

  const { send, detach } = attachSseClient(res, session, { replay: true });
  send({ type: "session", id, op: "reattach", busy: !!session.busy, forgeMode: session.forgeMode });
  send({
    type: "status",
    message: session.busy
      ? "Reconnected to agent run (Editor reload survived)"
      : "Reconnected — run already finished (replay)",
  });

  try {
    // Stay open while busy so live events keep flowing after replay.
    // Autonomous VerticalSlice runs can last GAMEFORGE_AUTO_MAX_HOURS (default 12 h).
    const maxWaitMs = Math.max(1, Number(process.env.GAMEFORGE_AUTO_MAX_HOURS) || 12) * 3_600_000 + 3_600_000;
    let waited = 0;
    while (session.busy && waited < maxWaitMs) {
      await sleep(250);
      waited += 250;
      if (res.writableEnded || res.destroyed) break;
    }
    // Drain: done/error is pushed after busy flips false
    await sleep(400);
  } finally {
    detach();
    try {
      if (!res.writableEnded) res.end();
    } catch {
      /* ignore */
    }
  }
});

/** Lab-parity: inspect whether Stop left a resumable LLM checkpoint. */
app.get("/api/sessions/:id/checkpoint", (req, res) => {
  const session = sessions.get(req.params.id);
  if (!session) return res.status(404).json({ error: "session not found", resumable: false });
  res.json(session.getCheckpointInfo?.() || { resumable: false });
});

/** Resolve active busy session activity without knowing the id (Editor convenience). */
app.get("/api/sessions/activity", (req, res) => {
  const want = String(req.query.id || "").trim();
  let id = want;
  let session = id ? sessions.get(id) : null;
  if (!session) {
    const busyId = [...sessions.entries()].find(([, s]) => s?.busy)?.[0];
    id = busyId || activeSessionId || [...sessions.keys()].pop() || "";
    session = id ? sessions.get(id) : null;
  }
  if (!session) {
    return res.status(200).json({
      ok: true,
      busy: false,
      eventCount: 0,
      lastStatus: [],
      recentFiles: [],
      recentTools: [],
      lastAssistantSnippet: "",
      note: "No live session — start a Chat/Generate run to see activity.",
      at: Date.now(),
    });
  }
  const activity = session.getActivity?.() || { ok: true, busy: !!session.busy, eventCount: 0 };
  res.json({ ...activity, sessionId: id });
});

/**
 * Non-blocking "What's happening?" snapshot for a live (or just-finished) session.
 * Does not cancel, pause, or share the LLM turn — safe while /prototype-full runs.
 */
app.get("/api/sessions/:id/activity", (req, res) => {
  const id = req.params.id;
  const session = sessions.get(id);
  if (!session) {
    // Soft miss: fall back to any busy/active session instead of hard 404
    const busyId = [...sessions.entries()].find(([, s]) => s?.busy)?.[0];
    const fallbackId = busyId || activeSessionId || "";
    const fallback = fallbackId ? sessions.get(fallbackId) : null;
    if (fallback) {
      const activity = fallback.getActivity?.() || { ok: true, busy: !!fallback.busy, eventCount: 0 };
      return res.json({ ...activity, sessionId: fallbackId, note: "requested session gone; showing active run" });
    }
    return res.status(200).json({
      ok: true,
      busy: false,
      sessionId: id,
      eventCount: 0,
      lastStatus: [],
      recentFiles: [],
      recentTools: [],
      lastAssistantSnippet: "",
      note: "Session not found (run finished or Editor reconnected).",
      at: Date.now(),
    });
  }
  const activity = session.getActivity?.() || { ok: true, busy: !!session.busy, eventCount: 0 };
  res.json({ ...activity, sessionId: id });
});

/** Resume from last Stop checkpoint (OpenAI-compatible providers; Cursor cannot mid-run resume). */
app.post("/api/sessions/:id/continue", async (req, res) => {
  const id = req.params.id;
  const session = sessions.get(id);
  if (!session) return res.status(404).json({ error: "session not found" });

  activeSessionId = id;
  const { detach } = attachSseClient(res, session, { replay: false });
  session.pushEvent({ type: "session", id, forgeMode: session.forgeMode, op: "continue" });
  try {
    const outcome = await session.continueFromCheckpoint({
      onEvent: (ev) => session.pushEvent(ev),
      continueMessage: req.body?.message || "continue",
    });
    const cp = session.getCheckpointInfo?.() || {};
    session.pushEvent({
      type: "done",
      status: outcome?.resumable ? "cancelled" : "finished",
      continued: true,
      resumable: !!outcome?.resumable,
      turn: cp.turn,
      autonomy: session.getAutonomy?.(),
      ...(outcome?.plan ? { plan: outcome.plan } : {}),
    });
  } catch (err) {
    const msg = err?.message || String(err);
    const cancelled = /abort|stopp?ed|cancel/i.test(msg);
    const cp = session.getCheckpointInfo?.() || {};
    session.pushEvent({
      type: cancelled ? "cancelled" : "error",
      error: msg,
      status: cancelled ? "cancelled" : "error",
      resumable: !!cp.resumable,
      turn: cp.turn,
    });
  } finally {
    if (activeSessionId === id) activeSessionId = null;
    detach();
    try {
      res.end();
    } catch {
      /* ignore */
    }
  }
});

app.post("/api/sessions/sync-tdd/preview", async (req, res) => {
  try {
    await initProviderCatalog(PROJECT_ROOT);
    const slug = parseSlug(req.body?.slug);
    if (!slug) return res.status(400).json({ error: "slug required" });
    const id = randomUUID();
    const session = await createSession({
      id,
      root: PROJECT_ROOT,
      tddsRoot: TDDS,
      sessionsRoot: SESSIONS,
      slug,
      forgeMode: req.body?.forgeMode || "Prototype",
    });
    const events = [];
    await session.previewSyncTdd({ onEvent: (e) => events.push(e) });
    res.json({ ok: true, events });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.get("/api/benchmark", async (_req, res) => {
  await initBenchmarkStore(PROJECT_ROOT);
  res.json(getBenchmarkState());
});

app.delete("/api/benchmark", async (_req, res) => {
  await initBenchmarkStore(PROJECT_ROOT);
  await clearBenchmark(PROJECT_ROOT);
  res.json({ ok: true });
});

app.post("/api/context/postmortem", async (req, res) => {
  try {
    const result = await runContextPostmortem(PROJECT_ROOT, {
      force: !!req.body?.force,
    });
    res.json(result);
  } catch (err) {
    res.status(500).json({ error: err?.message || String(err) });
  }
});

app.get("/api/context/graph", async (_req, res) => {
  const abs = getGraphHtmlPath(PROJECT_ROOT);
  if (!abs) {
    return res.status(404).json({
      error: "Graph HTML missing — run Context Post-Mortem first.",
      graphRel: "Docs/V57/reports/context-graph-viewer/index.html",
    });
  }
  res.json({
    ok: true,
    path: abs.replace(/\\/g, "/"),
    graphRel: "Docs/V57/reports/context-graph-viewer/index.html",
    viewUrl: `http://${HOST}:${PORT}/api/context/graph.html`,
  });
});

/** Serve embedded graph HTML (vis-network via CDN; needs network once). */
app.get("/api/context/graph.html", async (_req, res) => {
  const abs = getGraphHtmlPath(PROJECT_ROOT);
  if (!abs) {
    res.status(404).type("html").send("<h1>No graph yet</h1><p>Run Context Post-Mortem from GameForge.</p>");
    return;
  }
  try {
    const html = await fs.promises.readFile(abs, "utf8");
    res.type("html").send(html);
  } catch (err) {
    res.status(500).type("html").send(`<pre>${err.message}</pre>`);
  }
});

if (HOST !== "127.0.0.1" && HOST !== "localhost") {
  console.error("[GameForge Agent] Refusing to bind non-localhost host:", HOST);
  process.exit(1);
}

await fs.promises.mkdir(SESSIONS, { recursive: true });
await fs.promises.mkdir(TDDS, { recursive: true });

app.listen(PORT, HOST, () => {
  console.log(`[GameForge Agent] http://${HOST}:${PORT} project=${PROJECT_ROOT}`);
});
