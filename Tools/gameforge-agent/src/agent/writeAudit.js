/**
 * Post-turn write audit for the game repo.
 *
 * Why: the OpenAI-compatible provider (llm.js) enforces `assertAgentWriteAllowed` on its own
 * `write_file` tool and on path-bearing Unity MCP write tools, but the Cursor SDK agent edits
 * files inside its own local sandbox (`Agent.create({ local: { cwd } })`) — the SDK exposes
 * tool-call events after the fact, not a pre-write hook, and `eval`/`unity command` can write
 * anything. So every agent turn is bracketed by a snapshot:
 *
 *   begin(): git porcelain map (if the repo is git) + stat snapshot of provider-owned trees
 *            + in-memory backup of provider text docs (Docs/Design, Docs/ArtDirection, Docs/Audio).
 *   end():   paths touched during the turn → write policy. Provider-owned violations are
 *            reverted (mode "revert": agent version copied to .v57/quarantine/<UTC>/, original
 *            restored from the backup or `git checkout`) or only flagged (mode "flag").
 *            Everything else outside the roots is flagged, never reverted (Unity also writes).
 *            Any change to Docs/Design/TDD.md is restored and reported as a hard stop.
 *
 * Renames/moves of provider files are undone too (a deleted + an added provider file of the same
 * size in one turn → moved back), so provider files are never renamed by V57.
 * Unity-generated files (`*.fbm/` texture folders under Art, root URP/TMP/template assets) are
 * reported as noise, never as violations.
 *
 * Mode: GAMEFORGE_WRITE_AUDIT=revert|flag|off (default revert for VerticalSlice and Production).
 */

import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { checkAgentWrite, isProviderOwnedPath, normalizeForgeMode, PROVIDER_TDD_REL } from "./writePolicy.js";

const PROVIDER_TREES = [
  "Docs/Design",
  "Docs/ArtDirection",
  "Docs/Audio",
  "Docs/Marketing",
  "Assets/_Game/Art",
  "Assets/_Game/Audio",
];
const BACKUP_TREES = ["Docs/Design", "Docs/ArtDirection", "Docs/Audio"];
const BACKUP_EXT = /\.(md|csv|ya?ml|json|txt)$/i;
const BACKUP_MAX_FILE = 4 * 1024 * 1024;
const BACKUP_MAX_TOTAL = 40 * 1024 * 1024;

/** Written by Unity, the IDE or the sidecar itself — never attributed to the agent. */
const IGNORE_PREFIXES = [
  "library/",
  "temp/",
  "logs/",
  "obj/",
  "build/",
  "builds/",
  "usersettings/",
  "memorycaptures/",
  ".vs/",
  ".idea/",
  ".vscode/",
  ".git/",
  ".v57/",
  ".plastic/",
  "tools/gameforge-agent/",
  "v57/docs/reports/context-graph-viewer/",
];
const IGNORE_FILES = new Set([".gameforge-last-benchmark.json", ".gameforge-agent-provider", ".lab-last-benchmark.json"]);
const IGNORE_RE = /\.(csproj|sln|slnx|user|pidb|booproj|svd|tmp)$/i;

function isIgnored(rel) {
  const r = rel.toLowerCase();
  if (IGNORE_FILES.has(r)) return true;
  if (IGNORE_RE.test(r)) return true;
  return IGNORE_PREFIXES.some((p) => r.startsWith(p));
}

/** Files Unity (not the agent) creates: FBX embedded-media folders, template/URP/TMP root assets. */
const UNITY_NOISE_FILES = new Set([
  "assets/universalrenderpipelineglobalsettings.asset",
  "assets/defaultvolumeprofile.asset",
  "assets/inputsystem_actions.inputactions",
  "assets/readme.asset",
]);
const UNITY_NOISE_PREFIXES = ["assets/textmesh pro/", "assets/settings/"];

export function isUnityNoise(rel) {
  const r = rel.toLowerCase().replace(/\.meta$/, "");
  if (r.startsWith("assets/_game/art/") && /\.fbm(\/|$)/.test(r)) return true;
  if (UNITY_NOISE_FILES.has(r)) return true;
  if (r === "assets/textmesh pro" || r === "assets/settings") return true;
  return UNITY_NOISE_PREFIXES.some((p) => r.startsWith(p));
}

export function resolveAuditMode(forgeMode, env = process.env) {
  const v = String(env.GAMEFORGE_WRITE_AUDIT || "").trim().toLowerCase();
  if (["revert", "flag", "off"].includes(v)) return v;
  const mode = normalizeForgeMode(forgeMode);
  return mode === "VerticalSlice" || mode === "Production" ? "revert" : "flag";
}

function git(root, args) {
  const r = spawnSync("git", ["-C", root, ...args], { encoding: "buffer", timeout: 60_000, maxBuffer: 64 * 1024 * 1024 });
  if (r.error || r.status !== 0) return null;
  return r.stdout;
}

function isGitRepo(root) {
  const out = git(root, ["rev-parse", "--is-inside-work-tree"]);
  return !!out && out.toString().trim() === "true";
}

/** @returns {Map<string, string>} rel → porcelain XY code */
function gitPorcelain(root) {
  const out = git(root, ["status", "--porcelain=v1", "-z", "--untracked-files=all"]);
  const map = new Map();
  if (!out) return map;
  const parts = out.toString("utf8").split("\0");
  for (let i = 0; i < parts.length; i += 1) {
    const entry = parts[i];
    if (!entry || entry.length < 4) continue;
    const code = entry.slice(0, 2);
    const rel = entry.slice(3);
    map.set(rel, code);
    if (code[0] === "R" || code[0] === "C") i += 1; // skip rename source
  }
  return map;
}

function gitTracked(root, rel) {
  return git(root, ["ls-files", "--error-unmatch", "--", rel]) !== null;
}

function statSig(abs) {
  try {
    const st = fsSync.statSync(abs);
    return `${st.size}:${Math.round(st.mtimeMs)}`;
  } catch {
    return "missing";
  }
}

async function walkFiles(root, relDir, out) {
  let ents = [];
  try {
    ents = await fs.readdir(path.join(root, ...relDir.split("/")), { withFileTypes: true });
  } catch {
    return;
  }
  for (const e of ents) {
    const rel = `${relDir}/${e.name}`;
    if (e.isDirectory()) await walkFiles(root, rel, out);
    else if (e.isFile()) out.push(rel);
  }
}

async function snapshotTrees(root, trees) {
  const map = new Map();
  for (const t of trees) {
    const files = [];
    await walkFiles(root, t, files);
    for (const rel of files) {
      if (rel.toLowerCase().endsWith(".meta") || isUnityNoise(rel)) continue;
      map.set(rel, statSig(path.join(root, ...rel.split("/"))));
    }
  }
  return map;
}

async function backupDocs(root) {
  const backup = new Map();
  let total = 0;
  for (const t of BACKUP_TREES) {
    const files = [];
    await walkFiles(root, t, files);
    for (const rel of files) {
      if (!BACKUP_EXT.test(rel)) continue;
      try {
        const buf = await fs.readFile(path.join(root, ...rel.split("/")));
        if (buf.length > BACKUP_MAX_FILE || total + buf.length > BACKUP_MAX_TOTAL) continue;
        total += buf.length;
        backup.set(rel, buf);
      } catch {
        /* ignore */
      }
    }
  }
  return backup;
}

function utcStamp(d = new Date()) {
  return d.toISOString().replace(/[-:]/g, "").replace(/\.\d+Z$/, "Z");
}

/**
 * @param {{ root: string, forgeMode?: string, slug?: string, writeMode?: string, mode?: string }} opts
 */
export function createWriteAudit({ root, forgeMode = "VerticalSlice", slug = "", writeMode = "generate", mode } = {}) {
  const auditMode = mode || resolveAuditMode(forgeMode);
  const forge = normalizeForgeMode(forgeMode);
  let before = null;

  return {
    mode: auditMode,
    async begin() {
      if (auditMode === "off") return;
      const useGit = isGitRepo(root);
      const porcelain = useGit ? gitPorcelain(root) : new Map();
      const sigs = new Map();
      for (const rel of porcelain.keys()) sigs.set(rel, statSig(path.join(root, ...rel.split("/"))));
      before = {
        at: Date.now(),
        useGit,
        porcelain,
        sigs,
        trees: await snapshotTrees(root, PROVIDER_TREES),
        backup: await backupDocs(root),
      };
    },
    /**
     * @returns {Promise<{ mode: string, touched: string[], violations: {path:string, code:string, reason:string, action:string}[],
     *   hardStop: string, warningText: string }>}
     */
    async end() {
      const empty = { mode: auditMode, touched: [], noise: [], violations: [], hardStop: "", warningText: "" };
      if (auditMode === "off" || !before) return empty;
      const touched = new Set();
      /** @type {Map<string,"added"|"modified"|"deleted">} */
      const kinds = new Map();

      if (before.useGit) {
        const after = gitPorcelain(root);
        for (const [rel, code] of after) {
          const abs = path.join(root, ...rel.split("/"));
          if (!before.porcelain.has(rel)) {
            touched.add(rel);
            kinds.set(rel, code.includes("D") ? "deleted" : code === "??" || code.includes("A") ? "added" : "modified");
          } else if (before.sigs.get(rel) !== statSig(abs)) {
            touched.add(rel);
            kinds.set(rel, code.includes("D") ? "deleted" : "modified");
          }
        }
      }
      const trees = await snapshotTrees(root, PROVIDER_TREES);
      for (const [rel, sig] of trees) {
        const prev = before.trees.get(rel);
        if (prev === undefined) {
          touched.add(rel);
          if (!kinds.has(rel)) kinds.set(rel, "added");
        } else if (prev !== sig) {
          touched.add(rel);
          if (!kinds.has(rel)) kinds.set(rel, "modified");
        }
      }
      for (const rel of before.trees.keys()) {
        if (!trees.has(rel)) {
          touched.add(rel);
          kinds.set(rel, "deleted");
        }
      }

      const violations = [];
      const noise = [];
      let hardStop = "";
      const stamp = utcStamp();

      // Undo renames/moves of provider files: deleted + added provider file of equal size.
      const renamed = new Set();
      if (auditMode === "revert") {
        const sizeOf = (sig) => String(sig || "").split(":")[0];
        const added = [...touched].filter((r) => kinds.get(r) === "added" && isProviderOwnedPath(r) && !isIgnored(r));
        for (const oldRel of [...touched].filter((r) => kinds.get(r) === "deleted" && isProviderOwnedPath(r))) {
          const size = sizeOf(before.trees.get(oldRel));
          const match = added.find((n) => !renamed.has(n) && sizeOf(trees.get(n)) === size);
          if (!match) continue;
          try {
            const from = path.join(root, ...match.split("/"));
            const to = path.join(root, ...oldRel.split("/"));
            await fs.mkdir(path.dirname(to), { recursive: true });
            await fs.rename(from, to);
            if (fsSync.existsSync(`${from}.meta`) && !fsSync.existsSync(`${to}.meta`)) await fs.rename(`${from}.meta`, `${to}.meta`);
            renamed.add(match);
            renamed.add(oldRel);
            violations.push({
              path: `${oldRel} → ${match}`,
              kind: "renamed",
              code: "PROVIDER_OWNED",
              reason: `Provider file renamed/moved: ${oldRel} → ${match}. Provider files are never renamed by V57.`,
              action: "reverted (rename undone)",
            });
          } catch (err) {
            /* fall through to per-path handling */
          }
        }
      }

      for (const rel of [...touched].sort()) {
        if (isIgnored(rel) || renamed.has(rel)) continue;
        if (isUnityNoise(rel)) {
          noise.push(rel);
          continue;
        }
        const verdict = checkAgentWrite(rel, writeMode, { forgeMode: forge, slug });
        if (verdict.ok) continue;
        const kind = kinds.get(rel) || "modified";
        const providerOwned = isProviderOwnedPath(rel);
        let action = "flagged";
        if (providerOwned && rel.toLowerCase() === PROVIDER_TDD_REL.toLowerCase()) {
          hardStop = `Provider TDD (${PROVIDER_TDD_REL}) was edited during the turn`;
        }
        if (providerOwned && auditMode === "revert") {
          action = await revertPath(root, rel, kind, before, stamp);
        } else if (providerOwned && hardStop && rel.toLowerCase() === PROVIDER_TDD_REL.toLowerCase()) {
          // TDD is restored even in flag mode — it is the product truth.
          action = await revertPath(root, rel, kind, before, stamp);
        }
        violations.push({ path: rel, kind, code: verdict.code, reason: verdict.reason, action });
      }

      before = null;
      return {
        mode: auditMode,
        touched: [...touched].sort(),
        noise,
        violations,
        hardStop,
        warningText: formatAuditWarning(violations, hardStop),
      };
    },
  };
}

async function revertPath(root, rel, kind, before, stamp) {
  const abs = path.join(root, ...rel.split("/"));
  const qAbs = path.join(root, ".v57", "quarantine", stamp, ...rel.split("/"));
  try {
    if (kind !== "deleted" && fsSync.existsSync(abs)) {
      await fs.mkdir(path.dirname(qAbs), { recursive: true });
      await fs.copyFile(abs, qAbs);
    }
    const backup = before.backup.get(rel);
    if (backup) {
      await fs.mkdir(path.dirname(abs), { recursive: true });
      await fs.writeFile(abs, backup);
      return "reverted";
    }
    if (before.useGit && gitTracked(root, rel)) {
      const r = spawnSync("git", ["-C", root, "checkout", "--", rel], { timeout: 60_000 });
      if (r.status === 0) return "reverted";
    }
    if (kind === "added") {
      await fs.rm(abs, { force: true });
      return "quarantined";
    }
    return "flagged (no clean copy to restore)";
  } catch (err) {
    return `revert failed: ${err?.message || err}`;
  }
}

export function formatAuditWarning(violations = [], hardStop = "") {
  if (!violations.length && !hardStop) return "";
  const lines = ["## Write-policy audit of your previous turn (runner)"];
  if (hardStop) lines.push(`HARD STOP: ${hardStop}. It was restored from the pre-turn copy.`);
  const provider = violations.filter((v) => v.code === "PROVIDER_OWNED");
  const other = violations.filter((v) => v.code !== "PROVIDER_OWNED");
  if (provider.length) {
    lines.push("Provider-owned files were changed — these are read-only for V57:");
    for (const v of provider.slice(0, 20)) lines.push(`- ${v.path} (${v.kind}) → ${v.action}`);
    lines.push(
      "Do not retry these edits. Log each needed change as `D-###` in Docs/V57/DECISIONS.md and use a V57-owned workaround under Assets/_Game/ (Materials, Prefabs, Data).",
    );
  }
  if (other.length) {
    lines.push("Files written outside the allowed roots (left in place, fix or move them):");
    for (const v of other.slice(0, 20)) lines.push(`- ${v.path} (${v.kind}): ${v.reason.split(". ")[0]}`);
  }
  return lines.join("\n");
}
