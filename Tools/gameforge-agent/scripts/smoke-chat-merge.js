/** Smoke: Cursor-style cumulative snapshots + short token deltas + language directive. */
import { absorbAssistantDelta } from "../src/agent/providers/cursor.js";
import { mergeAssistantChunks } from "../src/agent/assistantMerge.js";
import { replyLanguageDirective } from "../src/agent/prompts/index.js";

const errors = [];
function ok(cond, msg) {
  if (cond) console.log("OK ", msg);
  else {
    console.error("FAIL", msg);
    errors.push(msg);
  }
}

const snaps = [
  "**Biol**",
  "**Biolum**",
  "**Biolum As**",
  "**Biolum Ascent**",
  "**Biolum Ascent** es un endless vertical tipo Doodle Jump, pero bajo el mar.",
];
const merged = mergeAssistantChunks(snaps);
ok(merged.startsWith("**Biolum Ascent**"), "last snapshot wins");
ok(!merged.includes("**Biol****Biolum**"), "no concatenated bold snapshots");
ok(!merged.includes("**Biolum****Biolum As**"), "no mid-grow concat");

let live = "";
for (const s of snaps) live = absorbAssistantDelta(live, s);
ok(live === snaps[snaps.length - 1], "provider absorb prefers growing rewrite");

const deltas = ["Hello", " ", "world"];
let d = "";
for (const t of deltas) d = absorbAssistantDelta(d, t);
ok(d === "Hello world", "short deltas still append");

const esDir = replyLanguageDirective("de que trata el juego?");
ok(/entirely in Spanish/i.test(esDir), "spanish user message → Spanish reply directive");

const enDir = replyLanguageDirective("what is this game about?");
ok(/entirely in English/i.test(enDir), "english user message → English reply directive");

if (errors.length) {
  console.error(`\n${errors.length} failure(s)`);
  process.exit(1);
}
console.log("\nAll chat merge smoke checks passed");
