import fs from "node:fs/promises";
import path from "node:path";
import { assertSafeSlug, SAFE_SLUG_RE } from "../security/paths.js";

const SKIP_MD = new Set(["readme.md", "changelog.md"]);

/**
 * Resolve the canonical TDD markdown file inside Docs/tdds/<slug>/.
 * Not required to be named TDD.md — see priority order in implementation.
 * @returns {Promise<string>} absolute path
 */
export async function resolveTddFilePath(tddsRoot, slug) {
  const safe = assertSafeSlug(slug);
  const dir = path.join(tddsRoot, safe);
  let entries = [];
  try {
    entries = await fs.readdir(dir, { withFileTypes: true });
  } catch {
    throw new Error(`TDD folder not found: Docs/tdds/${safe}/`);
  }

  const mdFiles = entries
    .filter((e) => e.isFile() && e.name.toLowerCase().endsWith(".md"))
    .map((e) => e.name)
    .filter((name) => !SKIP_MD.has(name.toLowerCase()));

  if (mdFiles.length === 0) {
    throw new Error(`No TDD markdown in Docs/tdds/${safe}/ (add a .md file)`);
  }

  if (mdFiles.includes("TDD.md")) return path.join(dir, "TDD.md");

  const slugMd = `${safe}.md`;
  if (mdFiles.includes(slugMd)) return path.join(dir, slugMd);

  if (mdFiles.length === 1) return path.join(dir, mdFiles[0]);

  const tddPref = mdFiles.filter((f) => /^tdd/i.test(f)).sort((a, b) => a.localeCompare(b));
  if (tddPref.length >= 1) return path.join(dir, tddPref[0]);

  mdFiles.sort((a, b) => a.localeCompare(b));
  return path.join(dir, mdFiles[0]);
}

/** @param {string} tddPath absolute or project-relative */
export function formatTddRelPath(tddPath, projectRoot = "") {
  if (!tddPath) return "Docs/tdds/<slug>/*.md";
  const rel = projectRoot
    ? path.relative(projectRoot, tddPath)
    : tddPath.replace(/^.*Docs[\\/]tdds[\\/]/i, `Docs${path.sep}tdds${path.sep}`);
  return rel.replace(/\\/g, "/");
}

export function parseMechanics(markdown) {
  const lines = String(markdown || "").split(/\r?\n/);
  const mechanics = [];
  let current = null;

  for (const line of lines) {
    const m =
      line.match(/^##\s+Mechanic:\s*(.+?)\s*$/i) ||
      line.match(/^##\s+\*\*Mechanic:\s*(.+?)\*\*\s*$/i);
    if (m) {
      if (current) mechanics.push(current);
      const title = m[1].replace(/\*+/g, "").trim();
      current = {
        id: toMechanicId(title),
        title,
        body: "",
      };
      continue;
    }
    if (current) current.body += (current.body ? "\n" : "") + line;
  }
  if (current) mechanics.push(current);

  for (const mech of mechanics) {
    const type = mech.body.match(/^\s*[-*]?\s*\*?\*?type\*?\*?\s*[:|]\s*`?(\w+)`?/im);
    if (type) mech.type = type[1];
    const desc = mech.body.match(/Player-facing behavior[\s\S]*?\n([\s\S]{0,280})/i);
    if (desc) mech.description = desc[1].trim().split("\n")[0];
  }
  return mechanics;
}

export function toMechanicId(title) {
  return String(title)
    .replace(/[^a-zA-Z0-9]+/g, "")
    .replace(/^\d+/, "") || "Mechanic";
}

export function parseProjectName(markdown) {
  const text = String(markdown || "");
  const m =
    text.match(/project_name:\s*["']?([^"'\n]+)/i) ||
    text.match(/\|\s*\*\*Game title\*\*\s*\|\s*([^|]+)\|/i) ||
    text.match(/^#\s*TDD\s*[—–-]\s*(.+?)(?:\s*\(|$)/im);
  return (m?.[1] || "Untitled").trim();
}

export async function listTdds(tddsRoot) {
  let entries = [];
  try {
    entries = await fs.readdir(tddsRoot, { withFileTypes: true });
  } catch {
    return [];
  }
  const out = [];
  for (const ent of entries) {
    if (!ent.isDirectory()) continue;
    if (!SAFE_SLUG_RE.test(ent.name)) continue;
    try {
      const tddPath = await resolveTddFilePath(tddsRoot, ent.name);
      const text = await fs.readFile(tddPath, "utf8");
      const projectRoot = path.dirname(path.dirname(tddsRoot));
      out.push({
        slug: ent.name,
        projectName: parseProjectName(text),
        path: formatTddRelPath(tddPath, projectRoot),
        mechanics: parseMechanics(text).map((m) => ({ id: m.id, title: m.title, type: m.type })),
      });
    } catch {
      /* skip folders without a resolvable TDD */
    }
  }
  return out.sort((a, b) => a.slug.localeCompare(b.slug));
}

export async function readTdd(tddsRoot, slug) {
  const safe = assertSafeSlug(slug);
  const tddPath = await resolveTddFilePath(tddsRoot, safe);
  const text = await fs.readFile(tddPath, "utf8");
  const projectRoot = path.dirname(path.dirname(tddsRoot));
  return {
    slug: safe,
    path: tddPath,
    relPath: formatTddRelPath(tddPath, projectRoot),
    text,
    projectName: parseProjectName(text),
    mechanics: parseMechanics(text),
  };
}

export async function importTddUpload(tddsRoot, filename, buffer) {
  const base =
    path
      .basename(filename, path.extname(filename))
      .replace(/[^A-Za-z0-9_-]+/g, "")
      .slice(0, 80) || "Imported";
  let slug = assertSafeSlug(base);
  let i = 1;
  while (true) {
    try {
      await fs.access(path.join(tddsRoot, slug));
      slug = assertSafeSlug(`${base}${i++}`.slice(0, 80));
    } catch {
      break;
    }
  }
  const dir = path.join(tddsRoot, slug);
  await fs.mkdir(dir, { recursive: true });
  const ext = path.extname(filename).toLowerCase();
  const outName =
    ext === ".md" && path.basename(filename).length > 0
      ? path.basename(filename)
      : "TDD.md";
  await fs.writeFile(path.join(dir, outName), buffer, "utf8");
  return readTdd(tddsRoot, slug);
}
