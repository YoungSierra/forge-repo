import fs from "node:fs/promises";
import path from "node:path";

const FILE = ".gameforge-last-benchmark.json";
const LEGACY_FILE = ".lab-last-benchmark.json";
const MAX_HISTORY = 12;

/** @type {{ last: object | null, history: object[] }} */
let cache = { last: null, history: [] };

export async function initBenchmarkStore(root) {
  for (const name of [FILE, LEGACY_FILE]) {
    try {
      const raw = await fs.readFile(path.join(root, name), "utf8");
      const parsed = JSON.parse(raw);
      cache = {
        last: parsed.last || null,
        history: Array.isArray(parsed.history) ? parsed.history.slice(0, MAX_HISTORY) : [],
      };
      return cache;
    } catch {
      /* try next */
    }
  }
  cache = { last: null, history: [] };
  return cache;
}

export function getBenchmarkState() {
  return cache;
}

export async function recordBenchmark(root, entry) {
  if (!entry || entry.type !== "benchmark") return entry;
  const row = { ...entry };
  delete row.type;
  cache.last = row;
  cache.history = [row, ...cache.history.filter((h) => h.at !== row.at)].slice(0, MAX_HISTORY);
  try {
    await fs.writeFile(path.join(root, FILE), JSON.stringify(cache, null, 2), "utf8");
  } catch {
    /* non-fatal */
  }
  return { type: "benchmark", ...row };
}

export async function clearBenchmark(root) {
  cache = { last: null, history: [] };
  for (const name of [FILE, LEGACY_FILE]) {
    try {
      await fs.unlink(path.join(root, name));
    } catch {
      /* */
    }
  }
  try {
    await fs.writeFile(path.join(root, FILE), JSON.stringify(cache, null, 2), "utf8");
  } catch {
    /* non-fatal */
  }
  return cache;
}
