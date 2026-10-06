import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describeWriteRoots, normalizeForgeMode } from "../writePolicy.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PROMPTS_DIR = __dirname;

async function readPrompt(name) {
  return fs.readFile(path.join(PROMPTS_DIR, name), "utf8");
}

export async function loadPromptPack() {
  const [quality, generateFinal, chat, chatAsk, syncTdd, syncPreview, tddPlayableLoop, autonomousContinue] =
    await Promise.all([
      readPrompt("playable-quality.md"),
      readPrompt("generate-final.md"),
      readPrompt("chat.md"),
      readPrompt("chat-ask.md"),
      readPrompt("sync-tdd.md"),
      readPrompt("sync-preview.md"),
      readPrompt("tdd-playable-loop.md"),
      readPrompt("autonomous-continue.md"),
    ]);
  return {
    quality,
    generateFinal,
    chat,
    chatAsk,
    syncTdd,
    syncPreview,
    tddPlayableLoop,
    autonomousContinue,
  };
}

function fillTemplate(tpl, vars) {
  return String(tpl || "").replace(/\{\{(\w+)\}\}/g, (_, k) => (vars[k] == null ? "" : String(vars[k])));
}

function formatElapsed(ms) {
  const min = Math.max(0, Math.round((Number(ms) || 0) / 60_000));
  return min >= 60 ? `${Math.floor(min / 60)}h${String(min % 60).padStart(2, "0")}` : `${min} min`;
}

/**
 * Re-anchor block prepended to every autonomous turn and every resume (WoO after_compact analogue).
 * @param {{ pack: object, state?: { json?: object|null, source?: string, stage?: string, status?: string, next?: string, nextSpec?: string[] },
 *   turn?: number, maxTurns?: number, elapsedMs?: number, maxHours?: number,
 *   knowledge?: { rel: string, relevant: boolean }[], warnings?: string[] }} opts
 */
export function buildReanchorBlock({
  pack,
  state = null,
  turn = 1,
  maxTurns = 0,
  elapsedMs = 0,
  maxHours = 0,
  knowledge = [],
  warnings = [],
}) {
  let status = "(Docs/V57/STATUS.json not found — create it at the end of this turn.)";
  if (state?.json) {
    const raw = JSON.stringify(state.json, null, 2);
    status = "```json\n" + (raw.length > 4000 ? `${raw.slice(0, 4000)}\n… (truncated — read the file)` : raw) + "\n```";
  } else if (state?.source === "legacy") {
    status = `(legacy GF-PROGRESS) stage=${state.stage || "?"} status=${state.status || "?"} next=${state.next || "?"}` +
      (state.nextSpec?.length ? ` spec=${state.nextSpec.join(",")}` : "") +
      " — migrate to Docs/V57/STATUS.json this turn.";
  }
  const relevant = knowledge.filter((k) => k.relevant);
  const others = knowledge.filter((k) => !k.relevant);
  const knowledgeText = knowledge.length
    ? [
        ...relevant.map((k) => `- ${k.rel} ← re-read (relevant to current stage)`),
        ...(others.length ? [`- also available: ${others.map((k) => k.rel).join(", ")}`] : []),
      ].join("\n")
    : "- (no V57/knowledge/*.md files found)";
  const warningText = warnings.filter(Boolean).length
    ? `\n## Runner findings from the previous turn (fix first)\n${warnings.filter(Boolean).join("\n\n")}\n`
    : "";
  return fillTemplate(pack?.autonomousContinue || "", {
    turn,
    maxTurns: maxTurns || "∞",
    elapsed: formatElapsed(elapsedMs),
    maxHours: maxHours || "∞",
    status,
    knowledge: knowledgeText,
    warnings: warningText,
  }).trim();
}

/**
 * Rules that only apply to the active forge mode. Stated once here — the
 * quality bar, write roots and skill blocks must not restate them.
 */
const MODE_RULES = {
  Prototype: `## Prototype mode rules (active)
- Obey the injected V57 **\`/prototype\`** skill; write the whole core loop under \`Assets/Prototypes/<slug>/\` (gap-fill if the folder exists).
- Atmosphere only when the TDD names it. Finish with one Play smoke that proves world + controls.
- **No automated tests:** never create \`Assets/Tests/**\`, test asmdefs, or run Test Runner (CLI or MCP); \`/spec implement\` from prototype flows always passes \`--no-tests\`.
- Validation = compile-heal + Play smoke + \`/playability-cert\`.`,
  VerticalSlice: `## Vertical Slice mode rules (active)
- Obey the injected **\`/vertical-slice\`** skill (stage driver I0 INTAKE → I1 → I2 → M1 → F → M2 → M3 → M4).
- Autonomous: the runner re-sends the next stage by itself — never end a turn waiting for \`continue\`. Keep \`Docs/V57/STATUS.json\` current (stage, status, next, next_spec, gates) at the end of every turn.
- Automated tests run through **\`unity test <project-root> --mode EditMode|PlayMode\`** or \`POST /api/unity-cli/test\` — never Test Runner over MCP.
- Record NUnit XML paths in \`Docs/V57/reports/<stage>-<slug>.md\` (legacy repos: \`Docs/V57/reports/implement-report-*.md\`).`,
  Production: `## Production mode rules (active)
- Obey the injected **\`/game-setup\`** skill and \`SCENE_PRODUCTION_STANDARDS.md\` / \`STANDARDS_CANONICAL.md\`.
- Staged pipeline: INIT confirm, one stage per run, emit the Progress block, then STOP. Never end with optional "if you want I can…" offers.
- Automated tests run through **\`unity test\`** or \`POST /api/unity-cli/test\` — never Test Runner over MCP. Record NUnit XML paths in \`implement-report-*.md\`.`,
};

export function buildGenerateFinalPrompt({
  slug,
  tddText,
  tddRelPath = "",
  agentsMd,
  pack,
  forgeMode = "Prototype",
  qualityNotes = "",
  modeSkillBlock = "",
  layout,
  reanchorBlock = "",
  userRequest = "",
}) {
  const mode = normalizeForgeMode(forgeMode);

  return [
    reanchorBlock ? String(reanchorBlock).trim() : "",
    "",
    agentsMd,
    "",
    pack.quality,
    "",
    MODE_RULES[mode] || MODE_RULES.Prototype,
    "",
    mode === "Production" ? PRODUCTION_GAME_FEEL_RULES : "",
    "",
    pack.tddPlayableLoop,
    "",
    pack.generateFinal,
    "",
    describeWriteRoots(mode, { layout }),
    "",
    `## Forge mode: ${mode}`,
    qualityNotes ? String(qualityNotes).trim() : "",
    "",
    modeSkillBlock ? String(modeSkillBlock).trim() : "",
    "",
    `## Active TDD slug: ${slug}`,
    `Path: ${tddRelPath || `Docs/tdds/${slug}/`}`,
    "",
    tddText,
    userRequest ? `\n## Runner request\n${String(userRequest).trim()}` : "",
  ]
    .filter((block) => block !== "")
    .join("\n");
}

const STAGED_GAME_SETUP_RULES = `## Staged /game-setup rules (mandatory)
- Execute **one pipeline stage per run** (after INIT confirm, only SETUP may chain in the same turn — per skill).
- Ask the user **only** for: INIT defaults confirm (\`confirm\` / \`yes\` / overrides), or \`continue\` after a stage Progress block.
- When a stage completes, emit the skill **Progress** output including \`Reply continue\` and **STOP** — do not keep writing or start the next stage in the same turn.
- **Never** end with unsolicited offers: no "If you want, I can...", "next steps", optional feature menus, or "would you like me to wire...".
- Do not ask whether to refactor HUD, add presets, or integrate extras unless the current stage STOP report lists it as a blocker.
- **Game feel:** presentation (camera, HUD, atmosphere) comes **only** from the enriched TDD — §1, §8, §9, atmosphere, §11.5. No web search, no genre catalog. Run **G-FEEL** before G-FUNC when those sections apply (\`GAME_FEEL_STANDARDS.md\`). Production write roots only.`;

const PRODUCTION_GAME_FEEL_RULES = `## Production game feel (mandatory when applicable)
- Read \`V57/docs/standards/GAME_FEEL_STANDARDS.md\` and \`/game-feel\` before signing off locomotion/HUD/atmosphere.
- **TDD-only presentation:** implement fog, grade, vignette, HUD mood, and camera literals **only** as quantified in the active TDD. Do not web-search, invent genre defaults, or STOP because references are missing — if the TDD is silent, note the gap in the report and implement what is specified.
- **Camera motion (§11.5 profile):** use \`Mathf.Lerp\` / \`SmoothDamp\` every frame on **TDD-declared** channels only — FPS lean/FOV/headbob **or** follow smooth damp/bank roll **or** other §11.5 literals; never instant snap; never require FPS lean when TDD declares follow-cam.
- **HUD:** UI Toolkit with state-driven rim/meters and overlay fades — see \`unity-ui-uitk-skill.md\`; not debug text only.
- **Atmosphere:** when TDD names fog/grain/vignette — run \`/urp-postprocessing\` or \`/lighting-setup\`; lerp volume params on state changes.`;

const RUN_ALL_GAME_SETUP_RULES = `## /game-setup-run-all rules (mandatory)
- Same Production pipeline as /game-setup, with **run_all: true**.
- INIT confirm still required (show defaults table; wait for confirm/yes/overrides).
- After INIT + SETUP: **chain all remaining stages in this run** — do **not** wait for \`continue\` between stages.
- Still **STOP** on hard failures (gate FAIL, MCP down, compile-heal exhausted, playability FAIL, unresolved vars, destructive actions).
- Progress may say \`run_all: advancing to Stage …\` instead of \`Reply continue\`.
- **Never** end with unsolicited offers. Production write roots only (never Assets/Prototypes or VerticalSlice).`;

/** Nudge the model to stay in one language when the TDD/gameplay are English. */
export function replyLanguageDirective(message = "") {
  const m = String(message || "").trim();
  if (!m) return "";
  const esScore = (
    m.match(
      /\b(de|del|el|la|los|las|un|una|qué|que|cómo|como|por|para|con|es|está|esta|están|juego|archivo|resumir|explica|arregla|velocidad|salto|timer|porqué|porque|también|más|cuál|cuando|dónde|donde|haz|sube|solo|sólo|ayuda|funciona|rompe|roto|gracias|cuéntame|cuentame|dime|hablame|háblame|trata|sobre)\b/gi,
    ) || []
  ).length;
  const enScore = (
    m.match(
      /\b(the|what|how|why|fix|speed|jump|game|file|explain|summarize|broken|works|help|thanks|please|can you|could you|should|would|is|are|was|were|don't|doesn't|it's|that's|with|for|from|this|that|when|where|which|who|tell me|describe)\b/gi,
    ) || []
  ).length;
  if (esScore >= 2 && esScore > enScore) {
    return "\n## Reply language\nRespond **entirely in Spanish** for the full answer. The TDD may be in English — translate concepts, do not paste English paragraphs. Proper nouns (game titles, mode names) may stay in English.\n";
  }
  if (enScore >= 2 && enScore > esScore) {
    return "\n## Reply language\nRespond **entirely in English**. Do not mix in Spanish unless quoting the user.\n";
  }
  return "\n## Reply language\nUse the **same language as the user's message** for the entire reply — do not mix Spanish and English in one answer.\n";
}

export function buildChatPrompt({
  slug,
  message,
  agentsMd,
  pack,
  tddText = "",
  tddRelPath = "",
  adviceDigest = "",
  mode = "agent",
  chatSkillId = "",
  chatSkillBlock = "",
  forgeMode = "Prototype",
  scopeMap = "",
  layout,
  reanchorBlock = "",
}) {
  const ask = mode === "ask";
  const lang = replyLanguageDirective(message);

  if (ask) {
    return [
      pack.chatAsk,
      "",
      `## Mode: ASK (read-only)\nDo not write or modify any files. TDD slug: ${slug}.\nRead gameplay/TDD only as needed to answer. Match reply length to the question.`,
      lang,
      adviceDigest
        ? `\n## Soft playability notes (only if relevant)\n${adviceDigest}\n`
        : "",
      `User request:\n${message}`,
    ]
      .filter((s) => s !== "")
      .join("\n");
  }

  const stagedSetup = chatSkillId === "game-setup";
  const runAllSetup = chatSkillId === "game-setup-run-all";
  const productionSetup = stagedSetup || runAllSetup;
  const prototypeMode =
    !productionSetup &&
    (chatSkillId === "prototype-full" || normalizeForgeMode(forgeMode) === "Prototype");

  // Chat skills: inject skill block instead of casual chat rules.
  if (chatSkillBlock) {
    const skillMode = productionSetup
      ? "Production"
      : prototypeMode
        ? "Prototype"
        : normalizeForgeMode(forgeMode);
    return [
      reanchorBlock ? String(reanchorBlock).trim() : "",
      "",
      agentsMd,
      "",
      runAllSetup ? RUN_ALL_GAME_SETUP_RULES : stagedSetup ? STAGED_GAME_SETUP_RULES : pack.quality,
      "",
      productionSetup ? PRODUCTION_GAME_FEEL_RULES : "",
      "",
      pack.tddPlayableLoop,
      "",
      productionSetup ? "" : pack.generateFinal,
      "",
      describeWriteRoots(skillMode, { layout }),
      "",
      productionSetup ? "" : MODE_RULES[skillMode] || "",
      "",
      String(chatSkillBlock).trim(),
      "",
      `## Active TDD slug: ${slug}`,
      `Path: ${tddRelPath || `Docs/tdds/${slug}/`}`,
      !prototypeMode
        ? ""
        : `## Prototype project root: Assets/Prototypes/${slug}/\n(Host ensures Assets/Prototypes/ and Assets/Prototypes/${slug}/ exist before this turn. Write all product files under that project root.)`,
      "",
      tddText || "## TDD\n(TDD missing — stop and ask user to select a TDD in Workbench.)",
      lang,
      "",
      `## User request\n${message}`,
    ]
      .filter((block) => block !== "")
      .join("\n");
  }

  return [
    reanchorBlock ? String(reanchorBlock).trim() : "",
    pack.chat,
    "",
    `## Mode: AGENT (edit Unity gameplay under the mode write root)`,
    `TDD slug: ${slug} (TDD file is read-only this turn — read with tools if needed)`,
    describeWriteRoots(normalizeForgeMode(forgeMode), { layout }),
    "",
    scopeMap || "",
    lang,
    adviceDigest
      ? `\n## Soft playability notes from last check (fix if the user is addressing them)\n${adviceDigest}\n`
      : "",
    `User request:\n${message}`,
  ]
    .filter((s) => s !== "")
    .join("\n");
}

function evidenceList(gameplayFiles = []) {
  return gameplayFiles.length > 0
    ? gameplayFiles.map((f) => `- ${f}`).join("\n")
    : "- Assets/Prototypes/** (or VerticalSlice / Scripts for the active mode)\n- Docs/tdds/<slug>/*.md (canonical TDD file in that folder)";
}

export function buildSyncPreviewPrompt({
  slug,
  tddText,
  tddRelPath = "",
  summary,
  chatDigest,
  gameplayFiles = [],
  root,
  pack,
}) {
  return [
    pack.syncPreview,
    "",
    `Project root: ${root || "."}`,
    `TDD path: ${tddRelPath || `Docs/tdds/${slug}/`}`,
    "",
    "## Operator summary",
    summary || "List TDD updates implied by the current playable.",
    "",
    "## Chat / iteration digest",
    chatDigest || "(Infer deltas from gameplay vs the TDD.)",
    "",
    "## Gameplay evidence — read these before proposing",
    evidenceList(gameplayFiles),
    "",
    "## Current TDD",
    tddText,
    "",
    "Do not write files. End with the JSON object only.",
  ].join("\n");
}

export function buildSyncPrompt({
  slug,
  tddText,
  tddRelPath = "",
  summary,
  chatDigest,
  gameplayFiles = [],
  root,
  agentsMd,
  pack,
  selectedItems = "",
}) {
  const tddPath = tddRelPath || `Docs/tdds/${slug}/`;
  return [
    agentsMd,
    "",
    pack.syncTdd,
    "",
    `Project root: ${root || "."}`,
    `TDD path: ${tddPath}`,
    "",
    "## Operator summary",
    summary || "Promote validated prototype changes into the TDD product spec.",
    "",
    "## Operator-approved checklist — apply ONLY these items",
    selectedItems ||
      "(No checklist provided — infer from digest + gameplay, but prefer chat-validated features.)",
    "",
    "## Validated change digest (chat + iteration the operator approved)",
    chatDigest || "(Read gameplay evidence and infer deltas vs the TDD below.)",
    "",
    "## Gameplay evidence — read ALL of these before editing the TDD",
    evidenceList(gameplayFiles),
    "",
    "Do not copy file paths or web stack names into the TDD. Extract product rules only.",
    "",
    "## Current TDD",
    tddText,
    "",
    "Edit ONLY the canonical TDD markdown at " + tddPath + " now.",
  ].join("\n");
}
