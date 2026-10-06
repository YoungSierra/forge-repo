/**
 * Offline check for the section-index TDD extract (src/tdd/extract.js).
 * Fixture: the Happy Habitat provider TDD (V57/tools/intake/test/fixtures/TDD_HappyHabitat.md),
 * copied into a temp provider repo as Docs/Design/TDD.md.
 *
 *   node scripts/smoke-tdd-extract.js [path/to/TDD.md]
 */
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const candidates = [
  process.argv[2],
  process.env.GAMEFORGE_TDD_FIXTURE,
  path.resolve(here, "../../../V57/tools/intake/test/fixtures/TDD_HappyHabitat.md"),
].filter(Boolean);
const fixture = candidates.find((p) => fs.existsSync(p));
if (!fixture) {
  console.error("Provider TDD fixture (V57/tools/intake/test/fixtures/TDD_HappyHabitat.md) not found — pass its path as the first argument.");
  process.exit(1);
}

const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "gf-tdd-"));
fs.mkdirSync(path.join(tmp, "Docs", "Design"), { recursive: true });
const tddAbs = path.join(tmp, "Docs", "Design", "TDD.md");
fs.copyFileSync(fixture, tddAbs);
const markdown = fs.readFileSync(tddAbs, "utf8");
const lines = markdown.split(/\r?\n/);

const { buildTddContext, parseTddStructure, resolveMechanics } = await import("../src/tdd/extract.js");

const errors = [];
function ok(cond, msg) {
  if (cond) console.log("OK  ", msg);
  else {
    console.error("FAIL", msg);
    errors.push(msg);
  }
}
const lineOf = (re) => lines.findIndex((l) => re.test(l)) + 1;
const included = (text, label) => text.includes(`<!-- ${label} · lines`);

// ── structure ───────────────────────────────────────────────────────────────────────────────
const { nodes, cBlocks } = parseTddStructure(lines);
ok(cBlocks.length === 10, `§C yaml blocks found: ${cBlocks.length} (expect 10)`);
ok(cBlocks[1]?.id === "C-02" && cBlocks[1]?.name === "FlipperController", "C-02 → FlipperController");
ok(!nodes.some((n) => /C-0\d/.test(n.title)), "`# ── C-NN` comments inside fences are not headings");
const mechs = nodes.filter((n) => /^Mechanic:/.test(n.title));
ok(mechs.length === 10, `§B mechanic blocks: ${mechs.length}`);

// ── resolveMechanics ────────────────────────────────────────────────────────────────────────
const mechList = mechs.map((n) => ({ name: n.title.replace(/^Mechanic:\s*/, "") }));
const r1 = resolveMechanics(["C-02", "V57/specs/<slug>/features/ScoreSystem.yaml", "wilt system", "Nope"], mechList, cBlocks);
ok(r1.names.includes("FlipperController") && r1.names.includes("ScoreSystem") && r1.names.includes("WiltSystem"), `next_spec resolved → ${r1.names.join(", ")}`);
ok(r1.unresolved.includes("Nope"), "unknown next_spec reported as unresolved");

// ── extract with next_spec ─────────────────────────────────────────────────────────────────
const ctx = buildTddContext(markdown, { relPath: "Docs/Design/TDD.md", slug: "Pinball", nextSpec: ["C-02"] });
console.log(`extract: ${ctx.chars} chars of ${ctx.fullChars} (${Math.round((100 * ctx.chars) / ctx.fullChars)}%)`);
for (const label of ["§0.2", "§A", "§3", "§9.1", "§11", "§13.2"]) {
  ok(included(ctx.text, label), `always-section ${label} inlined`);
}
ok(included(ctx.text, "§B Mechanic: FlipperController"), "§B FlipperController inlined");
ok(included(ctx.text, "§C C-02 FlipperController"), "§C C-02 yaml inlined");
ok(!ctx.text.includes("## Mechanic: PlungerLaunchSystem\n"), "other mechanic bodies not inlined");
ok(!ctx.text.includes("# ── C-01 PlungerLaunchSystem"), "other §C blocks not inlined");
ok(ctx.text.includes("| Screen id") || /Screen id/i.test(ctx.text), "§9.1 screen registry table present");
ok(/11\.5/.test(ctx.text) && /11\.3/.test(ctx.text), "§11.3 input + §11.5 movement/camera inside §11");
const plungerLine = lineOf(/^## Mechanic: PlungerLaunchSystem/);
ok(new RegExp(`Mechanic: PlungerLaunchSystem\` — lines ${plungerLine}-\\d+`).test(ctx.text), "index lists other mechanics with line ranges");
ok(/`C-05 BallPhysicsDrainSystem` — lines \d+-\d+/.test(ctx.text), "index lists §C blocks with line ranges");
ok(/`§D · Cross-mechanic dependency graph` — lines \d+-\d+|`D · Cross-mechanic dependency graph` — lines \d+-\d+/.test(ctx.text), "index lists §D with line range");
ok(ctx.chars <= 60_000, "default total budget respected (≤60k)");
ok(ctx.mechanics.join() === "FlipperController", "focus mechanics reported");

// Line numbers in markers must point at the real lines.
const m = /<!-- §B Mechanic: FlipperController · lines (\d+)-(\d+) -->/.exec(ctx.text);
ok(m && lines[Number(m[1]) - 1].startsWith("## Mechanic: FlipperController"), "marker line numbers match the file");

// ── no next_spec → mechanics indexed only ──────────────────────────────────────────────────
const bare = buildTddContext(markdown, { relPath: "Docs/Design/TDD.md" });
ok(!/<!-- §B /.test(bare.text), "no next_spec → no mechanic body inlined");
ok(bare.chars < ctx.chars, "no next_spec extract is smaller");

// ── budget: env + truncation pointers ──────────────────────────────────────────────────────
process.env.GAMEFORGE_TDD_BUDGET_CHARS = "20000";
const tight = buildTddContext(markdown, { relPath: "Docs/Design/TDD.md", nextSpec: ["C-02", "C-05"] });
delete process.env.GAMEFORGE_TDD_BUDGET_CHARS;
console.log(`tight extract: ${tight.chars} chars`);
ok(tight.chars <= 20_000, "GAMEFORGE_TDD_BUDGET_CHARS total budget respected");
ok(/(omitted|truncated) \(budget\) — read_file offset \d+ limit \d+/.test(tight.text), "over-budget pieces keep a read_file pointer");
ok(included(tight.text, "§A") && included(tight.text, "§3"), "identity + core loop survive the tight budget");

// ── small TDD passthrough unchanged ─────────────────────────────────────────────────────────
const small = buildTddContext("# T\n## Mechanic: A\nbody\n", { slug: "S" });
ok(small.text.includes("body") && small.chars === small.fullChars, "small TDD passed through untouched");

fs.rmSync(tmp, { recursive: true, force: true });
if (errors.length) {
  console.error(`\n${errors.length} TDD extract check(s) failed`);
  process.exit(1);
}
console.log("\nAll TDD extract smoke checks passed");
