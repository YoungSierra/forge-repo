import { Agent } from "@cursor/sdk";
import { createRunMeter } from "../runMeter.js";
import { loadUnityMcpServers } from "../mcp-config.js";
import { describeWriteRoots, detectRepoLayout, normalizeForgeMode } from "../writePolicy.js";
import { ensureTlsExtraCa } from "../tlsCa.js";

function toolPathFromEvent(event) {
  const raw =
    event?.toolCall?.arguments ??
    event?.arguments ??
    event?.input ??
    event?.params ??
    null;
  if (!raw) return "";
  let args = raw;
  if (typeof raw === "string") {
    try {
      args = JSON.parse(raw);
    } catch {
      return "";
    }
  }
  if (typeof args !== "object" || !args) return "";
  return String(args.path || args.file || args.target || "").replace(/\\/g, "/");
}

function usageFromResult(result) {
  if (!result || typeof result !== "object") return null;
  return (
    result.usage ||
    result.tokenUsage ||
    result.tokens ||
    result.info?.usage ||
    result.stats?.usage ||
    null
  );
}

/**
 * Cursor may stream either raw token deltas or growing message snapshots.
 * Never concatenate two snapshots of the same answer (breaks on markdown like
 * `**Biol**` → `**Biolum**`, which is not a strict string prefix).
 * @param {string} prev
 * @param {string} next
 */
export function absorbAssistantDelta(prev, next) {
  const a = String(prev || "");
  const b = String(next || "");
  if (!b) return a;
  if (!a) return b;
  if (b.startsWith(a)) return b;
  if (a.startsWith(b)) return a;
  // Growing snapshot rewrite (markdown / retokenize) — prefer newer when longer
  if (b.length >= a.length) return b;
  // Short token delta
  if (b.length <= 32) return a + b;
  // Shorter snapshot — keep the fuller text
  return a;
}

/** Unwrap Cursor SDK NetworkError → actionable TLS MITM when AV inspects HTTPS. */
export function enrichCursorTlsError(err) {
  const parts = [];
  let cur = err;
  let depth = 0;
  while (cur && depth < 8) {
    parts.push(String(cur.code || ""), String(cur.message || cur || ""));
    cur = cur.cause;
    depth += 1;
  }
  const blob = parts.join(" ");
  if (!/SELF_SIGNED_CERT|UNABLE_TO_VERIFY_LEAF|certificate/i.test(blob)) return err;
  const enriched = new Error(
    "Cursor API TLS failed (antivirus HTTPS inspection). Bootstrap Agent exports the AV root CA; or disable HTTPS scan for Node / set NODE_EXTRA_CA_CERTS.",
  );
  enriched.code = "TLS_MITM";
  enriched.cause = err;
  return enriched;
}

/**
 * Cursor SDK provider — local cwd agent. Model may be "auto" or a listed id.
 * @param {{ root: string, apiKey?: string, model?: string, writeMode?: string, forgeMode?: string, slug?: string }} opts
 */
export function createCursorProvider({ root, apiKey, model, writeMode = "generate", forgeMode = "Prototype" }) {
  const key = apiKey || process.env.CURSOR_API_KEY;
  if (!key) {
    throw new Error("CURSOR_API_KEY missing. Set it in .env.");
  }
  const modelId = model || process.env.CURSOR_MODEL || "auto";
  const modeLabel = normalizeForgeMode(forgeMode);
  // Cursor edits files inside its own local agent; the sidecar cannot veto a write before it
  // lands, so session.js runs a post-turn diff audit (writeAudit.js) on top of this prompt policy.
  const layout = detectRepoLayout(root);
  const askMode = writeMode === "ask";

  let agent = null;
  let aborted = false;
  /** @type {import("@cursor/sdk").Run | null} */
  let activeRun = null;

  return {
    id: "cursor",
    supportsCheckpoint: false,
    getCheckpoint() {
      return null;
    },
    model: modelId,
    async run(prompt, { onEvent, signal, attachments = [] } = {}) {
      aborted = false;
      activeRun = null;
      const meter = createRunMeter();
      const onAbort = () => {
        aborted = true;
        try {
          activeRun?.cancel?.();
        } catch {
          /* */
        }
      };
      signal?.addEventListener?.("abort", onAbort);

      const emitBenchmark = (status, usage, errorMessage) => {
        if (usage && !meter.tokensKnown) meter.noteUsage(usage);
        onEvent?.(
          meter.finish({
            provider: "cursor",
            model: modelId,
            status,
            ...(errorMessage
              ? { errorMessage: String(errorMessage).slice(0, 800) }
              : {}),
          }),
        );
      };

      const visionNote =
        Array.isArray(attachments) && attachments.length
          ? [
              "",
              "## Attached gameplay captures (read these image files)",
              ...attachments.map(
                (a) =>
                  `- ${a.relPath || a.path || a.id}${a.mime ? ` (${a.mime})` : ""}`,
              ),
              "Open/read these paths for visual context. If you cannot view images, say so and use paths + user text.",
              "",
            ].join("\n")
          : "";

      const fullPrompt = askMode
          ? [
              "ASK MODE (read-only). Do NOT edit, create, delete, or patch any files.",
              "Only inspect the codebase and answer with diagnosis + a concrete fix plan.",
              "If you would normally write code, describe the edits instead.",
              "",
              describeWriteRoots(modeLabel, { layout }),
              visionNote,
              prompt,
            ].join("\n")
          : [
              describeWriteRoots(modeLabel, { layout }),
              "",
              modeLabel === "Production"
                ? layout === "provider"
                  ? "PRODUCTION (provider repo): write only V57-owned roots listed above (Assets/_Game/{Scripts,Scenes,Prefabs,Data,Settings,Tests}, Art Materials, Docs/V57, V57/specs). Provider files are read-only — the runner audits the diff after every turn and reverts provider-owned edits."
                  : "PRODUCTION (/game-setup or /game-setup-run-all): production roots only. NEVER write under Assets/Prototypes/ or Assets/VerticalSlice/. Use Assets/Scripts, Assets/Scenes, Assets/Prefabs, Assets/UI. Staged /game-setup: one stage per continue. /game-setup-run-all: chain stages after INIT confirm; hard STOP on FAIL only."
                : modeLabel === "VerticalSlice"
                  ? layout === "provider"
                    ? "VERTICAL SLICE RUN (provider repo): write only V57-owned roots listed above. Provider files (Docs/Design, Docs/ArtDirection, Docs/Audio, Assets/_Game/Art except Materials, Assets/_Game/Audio) are read-only — log a D-### decision instead. The runner audits the diff after every turn and reverts provider-owned edits; editing the TDD is a hard stop."
                    : "VERTICAL SLICE RUN: write under Assets/VerticalSlice/ only."
                  : "PROTOTYPE RUN: write under Assets/Prototypes/ only.",
              visionNote,
              prompt,
            ].join("\n");

      try {
        const mcpServers = loadUnityMcpServers(root);
        onEvent?.({
          type: "status",
          message: askMode
              ? `Cursor · model ${modelId} · ASK (read-only request)`
              : mcpServers
                ? `Cursor · model ${modelId} · Editor MCP attached`
                : `Cursor · model ${modelId} · Editor MCP unavailable (post-generate still runs)`,
        });
        const createOpts = {
          apiKey: key,
          model: { id: modelId },
          local: { cwd: root },
        };
        if (mcpServers) createOpts.mcpServers = mcpServers;
        // Trust Kaspersky/etc. roots from Tools/gameforge-agent/.certs (attachments unrelated).
        ensureTlsExtraCa();
        try {
          agent = await Agent.create(createOpts);
        } catch (createErr) {
          throw enrichCursorTlsError(createErr);
        }

        const run = await agent.send(fullPrompt);
        activeRun = run;
        if (aborted) {
          try {
            await run.cancel?.();
          } catch {
            /* */
          }
        }
        // Cursor stream text is often growing snapshots, not finished deltas.
        // Absorb into live; emit status snippets while streaming; one assistant at flush/end.
        /** @type {string[]} */
        const replyParts = [];
        let live = "";
        let lastStatusAt = 0;
        const flushLive = () => {
          const t = live.trim();
          if (t) replyParts.push(t);
          live = "";
        };
        for await (const event of run.stream()) {
          if (aborted) {
            try {
              await run.cancel?.();
            } catch {
              /* */
            }
            break;
          }
          if (event.type === "assistant") {
            meter.noteTurn(event.usage || event.message?.usage);
            for (const block of event.message?.content || []) {
              if (block.type !== "text" || block.text == null) continue;
              const piece = String(block.text);
              if (!piece) continue;
              live = absorbAssistantDelta(live, piece);
              // Status only while streaming — do NOT emit every snapshot as assistant
              // (the chat UI would concatenate them into gibberish).
              const now = Date.now();
              if (now - lastStatusAt > 400) {
                lastStatusAt = now;
                const snip = live.replace(/\s+/g, " ").trim().slice(-120);
                if (snip.length >= 8) onEvent?.({ type: "status", message: snip });
              }
            }
          } else if (event.type === "tool_call") {
            flushLive();
            meter.noteTool();
            const name = event.name || event.toolCall?.name || "tool";
            const relPath = toolPathFromEvent(event);
            if (/write|edit|apply|patch/i.test(name) || /\.(js|ts|css|html|md)$/i.test(relPath)) {
              meter.noteFile();
              if (relPath) onEvent?.({ type: "file", path: relPath });
            }
            onEvent?.({ type: "tool", name, path: relPath || undefined, status: "call" });
          }
        }
        flushLive();
        const result = await run.wait();
        activeRun = null;
        // Prefer run.wait() result when it is at least as long as streamed absorb.
        const waitText =
          result && typeof result.result === "string" ? result.result.trim() : "";
        let assistantAcc = replyParts.join("\n\n").trim();
        if (waitText && waitText.length >= assistantAcc.length) assistantAcc = waitText;
        if (assistantAcc) onEvent?.({ type: "assistant", text: assistantAcc });
        const status = aborted ? "cancelled" : result?.status || "finished";
        emitBenchmark(status, usageFromResult(result));
        onEvent?.({ type: "done", status });
        return result;
      } catch (err) {
        emitBenchmark("error", null, err?.message || err);
        throw enrichCursorTlsError(err);
      } finally {
        signal?.removeEventListener?.("abort", onAbort);
        activeRun = null;
        try {
          await agent?.[Symbol.asyncDispose]?.();
        } catch {
          /* */
        }
        agent = null;
      }
    },
    cancel() {
      aborted = true;
      try {
        activeRun?.cancel?.();
      } catch {
        /* */
      }
    },
  };
}
