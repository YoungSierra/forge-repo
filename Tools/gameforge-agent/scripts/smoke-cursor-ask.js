/**
 * Smoke: Cursor Ask "Hi" — never prints secrets.
 * Run: node scripts/smoke-cursor-ask.js
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import dotenv from "dotenv";
import { Agent } from "@cursor/sdk";
import { ensureTlsExtraCa } from "../src/agent/tlsCa.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const AGENT_ROOT = path.resolve(__dirname, "..");
const PROJECT_ROOT = path.resolve(AGENT_ROOT, "../..");

function loadEnv() {
  const home = process.env.USERPROFILE || process.env.HOME || "";
  for (const p of [
    path.join(home, ".v57", "gameforge.env"),
    path.join(PROJECT_ROOT, ".env.local"),
    path.join(AGENT_ROOT, ".env"),
  ]) {
    if (!fs.existsSync(p)) continue;
    const parsed = dotenv.parse(fs.readFileSync(p));
    for (const [k, v] of Object.entries(parsed)) {
      const next = String(v ?? "").trim();
      if (!next) continue;
      if (!process.env[k] || !String(process.env[k]).trim()) process.env[k] = next;
    }
  }
}

loadEnv();

const ca = ensureTlsExtraCa(AGENT_ROOT);
const key = process.env.CURSOR_API_KEY || "";
console.log(
  JSON.stringify({
    hasCursorKey: key.length > 0,
    keyLen: key.length,
    model: process.env.CURSOR_MODEL || "auto",
    projectRoot: PROJECT_ROOT,
    tlsCa: ca,
  }),
);

if (!key) {
  console.error("FAIL: CURSOR_API_KEY missing");
  process.exit(2);
}

const modelId = process.env.CURSOR_MODEL || "auto";

try {
  console.log("creating agent…");
  const agent = await Agent.create({
    apiKey: key,
    model: { id: modelId },
    local: { cwd: PROJECT_ROOT },
  });
  console.log("send Hi (ask)…");
  const run = await agent.send(
    "ASK MODE (read-only). Reply with exactly: hi-ok",
  );
  let text = "";
  for await (const event of run.stream()) {
    if (event.type === "assistant") {
      for (const block of event.message?.content || []) {
        if (block.type === "text" && block.text) text = String(block.text);
      }
    }
  }
  const result = await run.wait();
  console.log(
    JSON.stringify({
      ok: true,
      status: result?.status || "unknown",
      textLen: (text || result?.result || "").length,
      textPreview: String(text || result?.result || "").slice(0, 120),
    }),
  );
  try {
    await agent[Symbol.asyncDispose]?.();
  } catch {
    /* */
  }
} catch (err) {
  const cause = err?.cause;
  console.log(
    JSON.stringify({
      ok: false,
      name: err?.name,
      message: String(err?.message || err).slice(0, 400),
      code: err?.code || cause?.code || null,
      status: err?.status || err?.httpStatus || null,
      causeName: cause?.name || null,
      causeMessage: cause ? String(cause.message || cause).slice(0, 300) : null,
      stackHead: String(err?.stack || "")
        .split("\n")
        .slice(0, 8),
    }),
  );
  process.exit(1);
}
