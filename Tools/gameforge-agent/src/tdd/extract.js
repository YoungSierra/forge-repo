/**
 * TDD context builder — section index.
 *
 * A provider TDD ("TDD Standard 2.0.0", e.g. Docs/tdds/Pinball) is ~170k chars; only part of it
 * drives the current stage. Instead of a flat char budget over "core-looking" sections, the
 * extract always inlines the sections every stage needs and the mechanic the run is working on:
 *
 *   always : §0.2 completeness gate · §A project identity · §3 core gameplay · §9.1 screen registry
 *            · §11.x technical design (engine, input, persistence, movement/camera, perf)
 *            · §13.2 scene manifest
 *   focus  : `## Mechanic: <Name>` (§B) + its `# ── C-NN <Name>` YAML block (§C) for every
 *            mechanic referenced by STATUS.json `next_spec`
 *
 * Every other heading is listed in a section index with its line range, so the agent can
 * `read_file` exactly what it needs. Headings inside fenced code blocks are ignored (the §C YAML
 * comments `# ── C-01 …` are not markdown headings). A total budget still applies
 * (GAMEFORGE_TDD_BUDGET_CHARS, default 60000): lower-priority pieces are truncated with a
 * read_file pointer instead of silently dropped.
 */

const HEADING_RE = /^(#{1,6})\s+(.*)$/;
const FENCE_RE = /^\s*(```|~~~)/;
const C_BLOCK_RE = /^#\s*[─━—–-]{1,}\s*(C-\d{1,3})\s+([A-Za-z0-9_]+)/;

/** Strip markdown, section glyphs and `[REQUIRED]`-style tags from a heading. */
function normalizeTitle(raw) {
  return String(raw || "")
    .replace(/`[^`]*`/g, " ")
    .replace(/\*/g, "")
    .replace(/[§✦]/g, " ")
    .replace(/[─━—–]{2,}/g, " ")
    .replace(/�/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

const keyOf = (s) => String(s || "").toLowerCase().replace(/[^a-z0-9]/g, "");

/**
 * Fence-aware heading + §C block scan.
 * @param {string[]} lines
 */
export function parseTddStructure(lines) {
  /** @type {{ level: number, title: string, raw: string, line: number, endLine: number }[]} */
  const nodes = [];
  /** @type {{ id: string, name: string, line: number, endLine: number }[]} */
  const cBlocks = [];
  let inFence = false;
  let fenceStart = 0;
  /** @type {{ id: string, name: string, line: number } | null} */
  let openC = null;
  lines.forEach((line, i) => {
    if (FENCE_RE.test(line)) {
      if (!inFence) {
        inFence = true;
        fenceStart = i + 1;
      } else {
        inFence = false;
        if (openC) {
          cBlocks.push({ ...openC, endLine: i + 1 });
          openC = null;
        }
      }
      return;
    }
    if (inFence) {
      const c = C_BLOCK_RE.exec(line);
      if (c) {
        if (openC) cBlocks.push({ ...openC, endLine: i });
        openC = { id: c[1].toUpperCase(), name: c[2], line: openC ? i + 1 : fenceStart };
      }
      return;
    }
    const m = HEADING_RE.exec(line);
    if (!m) return;
    nodes.push({ level: m[1].length, title: normalizeTitle(m[2]), raw: line, line: i + 1, endLine: lines.length });
  });
  if (openC) cBlocks.push({ ...openC, endLine: lines.length });
  for (let i = 0; i < nodes.length; i += 1) {
    const next = nodes.findIndex((n, j) => j > i && n.level <= nodes[i].level);
    nodes[i].endLine = next === -1 ? lines.length : nodes[next].line - 1;
  }
  return { nodes, cBlocks };
}

/** Sections inlined on every turn, in priority order. */
const ALWAYS = [
  { id: "§0.2", re: /^0\.2(\s|$|·)/ },
  { id: "§A", re: /^A(\s|$|·)/ },
  { id: "§3", re: /^3(\s|$|·)/ },
  { id: "§9.1", re: /^9\.1(\s|$|·)/ },
  { id: "§11", re: /^11(\s|$|·)/ }, // parent node covers 11.1–11.7
  { id: "§13.2", re: /^13\.2(\s|$|·)/ },
];

/**
 * Resolve STATUS `next_spec` values to mechanic names present in the TDD.
 * Accepts "C-03", "FlipperController", "V57/specs/<slug>/features/FlipperController.yaml", "Mechanic: X".
 * @param {string[]} specs
 * @param {{ name: string }[]} mechanics
 * @param {{ id: string, name: string }[]} cBlocks
 */
export function resolveMechanics(specs, mechanics, cBlocks) {
  const names = new Set();
  const unresolved = [];
  for (const raw of specs || []) {
    const s = String(raw || "").trim();
    if (!s) continue;
    let hit = false;
    const cid = /\bC-?(\d{1,3})\b/i.exec(s);
    if (cid) {
      const id = `C-${cid[1].padStart(2, "0")}`;
      const block = cBlocks.find((b) => b.id === id);
      if (block) {
        names.add(block.name);
        hit = true;
      }
    }
    const stem = s.split(/[\\/]/).pop().replace(/\.(ya?ml|md|json|cs)$/i, "").replace(/^mechanic:\s*/i, "");
    const k = keyOf(stem.replace(/^C-?\d{1,3}[\s_-]*/i, ""));
    if (k.length >= 4) {
      for (const m of mechanics) {
        const mk = keyOf(m.name);
        if (mk === k || (k.length >= 6 && (mk.includes(k) || k.includes(mk)))) {
          names.add(m.name);
          hit = true;
        }
      }
    }
    if (!hit) unresolved.push(s);
  }
  return { names: [...names], unresolved };
}

function sliceChars(lines, from, to) {
  let n = 0;
  for (let i = from - 1; i < to && i < lines.length; i += 1) n += lines[i].length + 1;
  return n;
}

/**
 * @param {string} markdown
 * @param {{ relPath?: string, slug?: string, budgetChars?: number, nextSpec?: string[]|string }} [opts]
 * @returns {{ text: string, chars: number, fullChars: number, omitted: number, included: number,
 *   mechanics: string[], unresolvedSpecs: string[] }}
 */
export function buildTddContext(markdown, opts = {}) {
  const envBudget = Number(process.env.GAMEFORGE_TDD_BUDGET_CHARS || 0);
  const { relPath = "", slug = "" } = opts;
  const budgetChars = Number(opts.budgetChars) > 0 ? Number(opts.budgetChars) : envBudget > 0 ? envBudget : 60_000;
  const nextSpec = Array.isArray(opts.nextSpec) ? opts.nextSpec : opts.nextSpec ? [opts.nextSpec] : [];
  const text = String(markdown || "");
  const lines = text.split(/\r?\n/);
  const { nodes, cBlocks } = parseTddStructure(lines);

  // No headings, or already small enough to be cheap: pass it through untouched.
  if (!nodes.length || text.length <= 12_000) {
    return {
      text: `## TDD${slug ? ` — ${slug}` : ""}${relPath ? ` (${relPath})` : ""}\n${text}`,
      chars: text.length,
      fullChars: text.length,
      omitted: 0,
      included: nodes.length,
      mechanics: [],
      unresolvedSpecs: [],
    };
  }

  const mechanics = nodes
    .filter((n) => n.level === 2 && /^Mechanic:\s*/i.test(n.title))
    .map((n) => ({ name: n.title.replace(/^Mechanic:\s*/i, "").trim(), node: n }));
  const { names: focus, unresolved } = resolveMechanics(nextSpec, mechanics, cBlocks);

  /** @type {{ label: string, from: number, to: number, priority: number }[]} */
  const pieces = [];
  ALWAYS.forEach((sel, idx) => {
    const node = nodes.find((n) => sel.re.test(n.title));
    if (node) pieces.push({ label: sel.id, from: node.line, to: node.endLine, priority: idx < 3 ? 0 : 2 });
  });
  for (const name of focus) {
    const mech = mechanics.find((m) => m.name === name);
    if (mech) pieces.push({ label: `§B Mechanic: ${name}`, from: mech.node.line, to: mech.node.endLine, priority: 1 });
    const block = cBlocks.find((b) => keyOf(b.name) === keyOf(name));
    if (block) pieces.push({ label: `§C ${block.id} ${block.name}`, from: block.line, to: block.endLine, priority: 1 });
  }
  // Drop pieces fully contained in another (e.g. a sub-node of §11).
  const uniq = pieces.filter(
    (p, i) => !pieces.some((q, j) => j !== i && q.from <= p.from && q.to >= p.to && (q.from < p.from || q.to > p.to || j < i)),
  );

  // The index and header are part of the total budget — reserve their (upper-bound) size first.
  const indexReserve =
    nodes.filter((n) => n.level <= 2).reduce((sum, n) => sum + n.title.length + 40, 0) +
    cBlocks.reduce((sum, b) => sum + b.name.length + 44, 0) +
    800;
  const bodyBudget = Math.max(4000, budgetChars - indexReserve);

  // Budget by priority (identity/core/focus first), then emit in document order.
  const ordered = [...uniq].sort((a, b) => a.priority - b.priority || a.from - b.from);
  let used = 0;
  /** @type {Map<object, { to: number, truncated: boolean }>} */
  const plan = new Map();
  for (const p of ordered) {
    const size = sliceChars(lines, p.from, p.to);
    if (used + size <= bodyBudget) {
      plan.set(p, { to: p.to, truncated: false });
      used += size;
      continue;
    }
    const room = bodyBudget - used;
    if (room < 1500) {
      plan.set(p, { to: p.from - 1, truncated: true });
      continue;
    }
    let to = p.from;
    let acc = 0;
    while (to <= p.to && acc + lines[to - 1].length + 1 <= room) {
      acc += lines[to - 1].length + 1;
      to += 1;
    }
    plan.set(p, { to: to - 1, truncated: true });
    used += acc;
  }

  const inlined = [...plan.entries()].filter(([, v]) => v.to >= 0);
  const isInlined = (line) => inlined.some(([p, v]) => line >= p.from && line <= v.to);

  /** @type {string[]} */
  const index = [];
  let omitted = 0;
  let included = 0;
  for (const n of nodes) {
    if (n.level > 2) {
      if (isInlined(n.line)) included += 1;
      else omitted += 1;
      continue;
    }
    const inc = isInlined(n.line);
    if (inc) included += 1;
    else omitted += 1;
    index.push(`- \`${n.title}\` — lines ${n.line}-${n.endLine}${inc ? " · included" : ""}`);
  }
  if (cBlocks.length) {
    index.push("- §C companion YAML blocks:");
    for (const b of cBlocks) {
      index.push(`  - \`${b.id} ${b.name}\` — lines ${b.line}-${b.endLine}${isInlined(b.line) ? " · included" : ""}`);
    }
  }

  /** @type {string[]} */
  const body = [];
  for (const [p, v] of [...plan.entries()].sort((a, b) => a[0].from - b[0].from)) {
    if (v.to < p.from) {
      body.push(`<!-- ${p.label} omitted (budget) — read_file offset ${p.from} limit ${p.to - p.from + 1} -->`, "");
      continue;
    }
    body.push(`<!-- ${p.label} · lines ${p.from}-${v.to} -->`);
    for (let i = p.from - 1; i < v.to; i += 1) body.push(lines[i]);
    if (v.truncated) {
      body.push(`<!-- ${p.label} truncated (budget) — read_file offset ${v.to + 1} limit ${p.to - v.to} -->`);
    }
    body.push("");
  }

  const focusLine = focus.length
    ? `Focus mechanic(s) from STATUS next_spec: ${focus.join(", ")} (§B + §C inlined).`
    : nextSpec.length
      ? "STATUS next_spec did not match a TDD mechanic — open the right `## Mechanic:` block from the index."
      : "No STATUS next_spec — mechanic blocks (§B/§C) are indexed only; open the one you work on.";
  const out = [
    `## TDD extract${slug ? ` — ${slug}` : ""}`,
    `Source: ${relPath || "Docs/Design/TDD.md"} · ${lines.length} lines · inlined ${used} of ${text.length} chars (total budget ${budgetChars}).`,
    "Always inlined: §0.2, §A, §3, §9.1, §11.x, §13.2. " + focusLine,
    unresolved.length ? `Unresolved next_spec: ${unresolved.join(", ")}` : "",
    "Everything else: `read_file` the TDD with the line range from this index — do not re-read inlined sections. The TDD is read-only.",
    "",
    "### Section index",
    ...index,
    "",
    ...body,
  ]
    .filter((l, i, a) => !(l === "" && a[i - 1] === ""))
    .join("\n");

  return {
    text: out,
    chars: out.length,
    fullChars: text.length,
    omitted,
    included,
    mechanics: focus,
    unresolvedSpecs: unresolved,
  };
}
