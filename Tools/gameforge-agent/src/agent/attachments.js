import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";
import { randomUUID } from "node:crypto";

const MAX_BYTES = 2 * 1024 * 1024; // 2 MB after client compression
const MAX_PER_MESSAGE = 4;
const TTL_MS = 24 * 60 * 60 * 1000;
const MAX_DISK_BYTES = 80 * 1024 * 1024;

const ALLOWED_MIME = new Set([
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
  "image/gif",
]);

/**
 * @param {string} agentRoot
 */
export function attachmentsDir(agentRoot) {
  return path.join(agentRoot, ".attachments");
}

/**
 * @param {string} agentRoot
 */
export async function ensureAttachmentsDir(agentRoot) {
  const dir = attachmentsDir(agentRoot);
  await fs.mkdir(dir, { recursive: true });
  return dir;
}

function extForMime(mime) {
  const m = String(mime || "").toLowerCase();
  if (m.includes("png")) return ".png";
  if (m.includes("webp")) return ".webp";
  if (m.includes("gif")) return ".gif";
  return ".jpg";
}

/**
 * Persist an uploaded image buffer. Rejects oversize / bad mime.
 * @param {string} agentRoot
 * @param {{ buffer: Buffer, mime?: string, originalName?: string }} file
 */
export async function saveAttachment(agentRoot, file) {
  const mime = String(file.mime || "image/jpeg").toLowerCase();
  if (!ALLOWED_MIME.has(mime) && !mime.startsWith("image/")) {
    const err = new Error("Unsupported attachment type — use JPEG, PNG, or WebP");
    err.code = "ATTACHMENT_TYPE";
    throw err;
  }
  const buf = file.buffer;
  if (!buf?.length) {
    const err = new Error("Empty attachment");
    err.code = "ATTACHMENT_EMPTY";
    throw err;
  }
  if (buf.length > MAX_BYTES) {
    const err = new Error(
      `Attachment too large (${Math.round(buf.length / 1024)} KB). Compress under ${MAX_BYTES / 1024} KB.`,
    );
    err.code = "ATTACHMENT_TOO_LARGE";
    throw err;
  }

  await ensureAttachmentsDir(agentRoot);
  await pruneAttachments(agentRoot);

  const id = randomUUID();
  const ext = extForMime(mime);
  const filename = `${id}${ext}`;
  const abs = path.join(attachmentsDir(agentRoot), filename);
  await fs.writeFile(abs, buf);

  const meta = {
    id,
    filename,
    mime: mime === "image/jpg" ? "image/jpeg" : mime,
    bytes: buf.length,
    originalName: String(file.originalName || "").slice(0, 120),
    createdAt: Date.now(),
  };
  await fs.writeFile(path.join(attachmentsDir(agentRoot), `${id}.json`), JSON.stringify(meta));
  return {
    ...meta,
    absPath: abs,
    relPath: path.join("Tools/gameforge-agent/.attachments", filename).replace(/\\/g, "/"),
  };
}

/**
 * @param {string} agentRoot
 * @param {string} id
 */
export async function loadAttachmentMeta(agentRoot, id) {
  const safe = String(id || "").replace(/[^a-f0-9-]/gi, "");
  if (!safe) return null;
  const metaPath = path.join(attachmentsDir(agentRoot), `${safe}.json`);
  try {
    const raw = await fs.readFile(metaPath, "utf8");
    const meta = JSON.parse(raw);
    const abs = path.join(attachmentsDir(agentRoot), meta.filename);
    if (!fsSync.existsSync(abs)) return null;
    return {
      ...meta,
      absPath: abs,
      relPath: path.join("Tools/gameforge-agent/.attachments", meta.filename).replace(/\\/g, "/"),
    };
  } catch {
    return null;
  }
}

/**
 * @param {string} agentRoot
 * @param {string[]} ids
 */
export async function resolveAttachments(agentRoot, ids) {
  const list = Array.isArray(ids) ? ids : [];
  if (list.length > MAX_PER_MESSAGE) {
    const err = new Error(`Max ${MAX_PER_MESSAGE} attachments per message`);
    err.code = "ATTACHMENT_COUNT";
    throw err;
  }
  const out = [];
  for (const id of list) {
    const meta = await loadAttachmentMeta(agentRoot, id);
    if (!meta) {
      const err = new Error(`Attachment not found: ${id}`);
      err.code = "ATTACHMENT_MISSING";
      throw err;
    }
    out.push(meta);
  }
  return out;
}

/**
 * @param {string} agentRoot
 */
export async function pruneAttachments(agentRoot) {
  const dir = attachmentsDir(agentRoot);
  let entries = [];
  try {
    entries = await fs.readdir(dir);
  } catch {
    return;
  }
  const now = Date.now();
  /** @type {{ file: string, mtime: number, size: number }[]} */
  const files = [];
  for (const name of entries) {
    if (!/\.(jpg|jpeg|png|webp|gif|json)$/i.test(name)) continue;
    const abs = path.join(dir, name);
    try {
      const st = await fs.stat(abs);
      if (now - st.mtimeMs > TTL_MS) {
        await fs.unlink(abs).catch(() => {});
        continue;
      }
      files.push({ file: abs, mtime: st.mtimeMs, size: st.size });
    } catch {
      /* */
    }
  }
  let total = files.reduce((s, f) => s + f.size, 0);
  if (total <= MAX_DISK_BYTES) return;
  files.sort((a, b) => a.mtime - b.mtime);
  for (const f of files) {
    if (total <= MAX_DISK_BYTES) break;
    await fs.unlink(f.file).catch(() => {});
    total -= f.size;
  }
}

/**
 * Heuristic: models that typically accept image_url / multimodal parts.
 * Covers GameForge providers: Cursor, OpenAI, Anthropic, Kimi, GLM, MiniMax, OpenRouter.
 * @param {string} model
 */
export function modelLikelySupportsVision(model) {
  const id = String(model || "")
    .toLowerCase()
    .trim()
    .replace(/\s+/g, "-");
  if (!id) return false;

  // Known text-only / non-vision (check before broad matches)
  if (
    id.includes("text-01") ||
    id.includes("embedding") ||
    id.includes("whisper") ||
    id.includes("tts") ||
    id.includes("moderation") ||
    /minimax-m2(\.|-|$)/.test(id) // M2 / M2.1 / M2.5 / M2.7 — text+tools only
  ) {
    return false;
  }

  // Cursor routing & agents (attachments via paths / vision-capable backends)
  if (id === "auto" || id.includes("composer") || id.includes("cursor")) return true;

  // OpenAI family (+ OpenRouter openai/…)
  if (
    id.includes("gpt-4o") ||
    id.includes("gpt-4.1") ||
    id.includes("gpt-4.5") ||
    id.includes("gpt-5") ||
    id.includes("gpt-6") ||
    /(?:^|[-_/])o[1-4](?:[-_/]|$)/.test(id)
  ) {
    return true;
  }

  // Anthropic (+ OpenRouter anthropic/…)
  if (id.includes("claude")) return true;

  // Google Gemini (+ OpenRouter google/…)
  if (id.includes("gemini")) return true;

  // Kimi / Moonshot multimodal (kimi-k3, kimi-k3-256, k2.6, k2.7, *-vision)
  if (
    id.includes("kimi-k3") ||
    id.includes("kimi-k2.6") ||
    id.includes("kimi-k2.7") ||
    (id.includes("kimi") && (id.includes("k3") || id.includes("k2.6") || id.includes("k2.7"))) ||
    id.includes("moonshotai/kimi") ||
    (id.includes("moonshot") && id.includes("vision"))
  ) {
    return true;
  }

  // Zhipu / GLM vision series (glm-4v, glm-4.5v, glm-4.6v, …)
  if (/glm-4(\.\d+)?v/.test(id) || id.includes("glm-4v") || id.includes("/glm-4") && id.includes("v")) {
    return true;
  }

  // MiniMax multimodal (M3 + legacy VL-01)
  if (id.includes("minimax-m3") || id.includes("minimax-vl") || id.includes("minimax/vl")) {
    return true;
  }

  // Explicit multimodal markers
  if (
    id.includes("vision") ||
    id.includes("-vl-") ||
    id.includes("-vl_") ||
    id.endsWith("-vl") ||
    id.includes("vlm")
  ) {
    return true;
  }

  return false;
}

/**
 * Build OpenAI-style multimodal user content.
 * @param {string} text
 * @param {{ absPath: string, mime: string }[]} attachments
 * @param {boolean} withImages
 */
export async function buildUserContent(text, attachments, withImages) {
  if (!withImages || !attachments?.length) return text;
  /** @type {object[]} */
  const parts = [{ type: "text", text: String(text || "") }];
  for (const a of attachments) {
    const buf = await fs.readFile(a.absPath);
    const b64 = buf.toString("base64");
    const mime = a.mime || "image/jpeg";
    parts.push({
      type: "image_url",
      image_url: { url: `data:${mime};base64,${b64}` },
    });
  }
  return parts;
}

export const ATTACHMENT_LIMITS = {
  maxBytes: MAX_BYTES,
  maxPerMessage: MAX_PER_MESSAGE,
  ttlMs: TTL_MS,
};
