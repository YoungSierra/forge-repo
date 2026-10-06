import fs from "node:fs/promises";
import fsSync from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const GRAPH_REL = path.join("Docs", "V57", "reports", "context-graph-viewer", "index.html");
const INDEX_REL = "project-index.json";

function resolvePowerShell() {
  if (process.platform !== "win32") return "pwsh";
  const candidates = [
    process.env.SystemRoot
      ? path.join(process.env.SystemRoot, "System32", "WindowsPowerShell", "v1.0", "powershell.exe")
      : null,
    "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
    "powershell.exe",
    "pwsh.exe",
  ].filter(Boolean);
  for (const c of candidates) {
    try {
      if (c.includes("\\") || c.includes("/")) {
        if (fsSync.existsSync(c)) return c;
      } else {
        return c; // PATH lookup via spawn
      }
    } catch {
      /* try next */
    }
  }
  return "powershell.exe";
}

function runPs(scriptRel, args, cwd) {
  return new Promise((resolve) => {
    const script = path.join(cwd, ...scriptRel.split("/"));
    if (!fsSync.existsSync(script)) {
      resolve({ ok: false, code: 127, stdout: "", stderr: `Missing script: ${scriptRel}` });
      return;
    }
    const psArgs = [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      script,
      "-ProjectRoot",
      cwd,
      ...args,
    ];
    const child = spawn(resolvePowerShell(), psArgs, {
      cwd,
      windowsHide: true,
      env: process.env,
      shell: false,
    });
    let stdout = "";
    let stderr = "";
    child.stdout?.on("data", (d) => {
      stdout += String(d);
    });
    child.stderr?.on("data", (d) => {
      stderr += String(d);
    });
    child.on("error", (err) => {
      resolve({ ok: false, code: 1, stdout, stderr: err.message });
    });
    child.on("close", (code) => {
      resolve({ ok: code === 0, code: code ?? 1, stdout, stderr });
    });
  });
}

function parseReadinessStatus(text) {
  const t = String(text || "");
  if (/\bRed\b/i.test(t) && /Phase 2 recommended|Recommended/i.test(t)) return "Red";
  if (/\bStatus\b[^\n]*Red\b/i.test(t) || /\*\*Red\*\*/i.test(t)) return "Red";
  if (/\bYellow\b/i.test(t)) return "Yellow";
  if (/\bGreen\b/i.test(t)) return "Green";
  // Exit / banner heuristics from script output
  if (/status:\s*red/i.test(t)) return "Red";
  if (/status:\s*yellow/i.test(t)) return "Yellow";
  if (/status:\s*green/i.test(t)) return "Green";
  return "Yellow";
}

async function ensureMinimalIndex(root) {
  const indexPath = path.join(root, INDEX_REL);
  try {
    const raw = await fs.readFile(indexPath, "utf8");
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed.modules) && parsed.modules.length > 0) return { created: false, path: indexPath };
  } catch {
    /* build one */
  }

  const roots = [
    ["Assets", "Prototypes"],
    ["Assets", "VerticalSlice"],
    ["Assets", "Scripts"],
  ];
  /** @type {{ id: string, scripts: string[], dependsOn: string[], classes: string[] }[]} */
  const modules = [];
  for (const segs of roots) {
    const abs = path.join(root, ...segs);
    const scripts = [];
    async function walk(dir) {
      let entries = [];
      try {
        entries = await fs.readdir(dir, { withFileTypes: true });
      } catch {
        return;
      }
      for (const ent of entries) {
        const p = path.join(dir, ent.name);
        if (ent.isDirectory()) await walk(p);
        else if (ent.isFile() && ent.name.endsWith(".cs")) {
          scripts.push(path.relative(root, p).replace(/\\/g, "/"));
        }
      }
    }
    await walk(abs);
    if (scripts.length) {
      modules.push({
        id: segs[segs.length - 1],
        scripts,
        dependsOn: [],
        classes: scripts.map((s) => path.basename(s, ".cs")),
      });
    }
  }
  if (!modules.length) {
    modules.push({
      id: "_Empty",
      scripts: [],
      dependsOn: [],
      classes: [],
      note: "No .cs under Prototypes/VerticalSlice/Scripts yet",
    });
  }
  const doc = {
    indexVersion: "1.0",
    generatedAt: new Date().toISOString(),
    generator: "gameforge-context-postmortem-minimal",
    modules,
    interfaces: [],
    events: [],
    assets: [],
  };
  await fs.writeFile(indexPath, JSON.stringify(doc, null, 2), "utf8");
  return { created: true, path: indexPath };
}

/**
 * Always: readiness → (optional phase2 work) → HTML graph.
 * @param {string} root
 * @param {{ force?: boolean }} [opts]
 */
export async function runContextPostmortem(root, opts = {}) {
  const force = !!opts.force;
  const steps = [];

  const readiness = await runPs("V57/tools/check-context-readiness.ps1", ["-WriteReport"], root);
  steps.push({
    id: "readiness",
    ok: readiness.ok || /Green|Yellow|Red/i.test(readiness.stdout + readiness.stderr),
    stdout: (readiness.stdout || "").slice(0, 4000),
    stderr: (readiness.stderr || "").slice(0, 1000),
  });
  const status = parseReadinessStatus(readiness.stdout + "\n" + readiness.stderr);

  let phase2 = "none";
  if (force || status === "Red") {
    phase2 = "full";
    const index = await runPs(
      "V57/tools/index-project.ps1",
      ["-ProposeContext", "-Drift", "-SkipUnityCheck"],
      root,
    );
    steps.push({
      id: "index",
      ok: index.ok,
      stdout: (index.stdout || "").slice(0, 2000),
      stderr: (index.stderr || "").slice(0, 1500),
    });
  } else if (status === "Yellow") {
    phase2 = "drift-only";
    const drift = await runPs("V57/tools/index-project.ps1", ["-Drift", "-SkipUnityCheck"], root);
    steps.push({
      id: "drift",
      ok: drift.ok,
      stdout: (drift.stdout || "").slice(0, 2000),
      stderr: (drift.stderr || "").slice(0, 1500),
    });
  } else {
    // Green: still try a light index so the graph has real edges when possible
    phase2 = "graph-only";
    const light = await runPs("V57/tools/index-project.ps1", ["-SkipUnityCheck"], root);
    steps.push({
      id: "index-light",
      ok: light.ok,
      stdout: (light.stdout || "").slice(0, 1500),
      stderr: (light.stderr || "").slice(0, 1000),
    });
  }

  const minimal = await ensureMinimalIndex(root);
  if (minimal.created) {
    steps.push({ id: "minimal-index", ok: true, stdout: `Wrote fallback ${INDEX_REL}`, stderr: "" });
  }

  const graph = await runPs(
    "V57/tools/export-context-graph.ps1",
    ["-SkipIndex", "-NoOpen", "-SkipUnityCheck"],
    root,
  );
  steps.push({
    id: "graph-html",
    ok: graph.ok,
    stdout: (graph.stdout || "").slice(0, 1500),
    stderr: (graph.stderr || "").slice(0, 1500),
  });

  const graphAbs = path.join(root, GRAPH_REL);
  const graphExists = fsSync.existsSync(graphAbs);

  return {
    ok: graphExists,
    readiness: status,
    phase2,
    force,
    graphPath: graphExists ? graphAbs.replace(/\\/g, "/") : null,
    graphRel: graphExists ? GRAPH_REL.replace(/\\/g, "/") : null,
    steps,
    summary:
      `Readiness=${status}; Phase2=${phase2}; Graph=${graphExists ? "ok" : "missing"}` +
      (force ? " (force)" : ""),
  };
}

export function getGraphHtmlPath(root) {
  const abs = path.join(root, GRAPH_REL);
  return fsSync.existsSync(abs) ? abs : null;
}
