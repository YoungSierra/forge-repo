import fs from "node:fs/promises";
import path from "node:path";
import { normalizeForgeMode } from "./writePolicy.js";
import { needsIntake } from "./v57State.js";

/**
 * V57 skill files wired into Forge Generate by mode.
 * Only modes with an explicit path are injected (user-gated).
 *
 * Routing:
 *  - Prototype     → unity-prototype-skill.md (unchanged)
 *  - VerticalSlice → unity-vertical-slice-skill.md (the stage driver I0…M4), plus
 *                    unity-intake-skill.md on the first turn while Docs/Generated is missing
 *  - Production    → unity-game-setup-skill.md
 *
 * Chat-only skills (slash menu) live in CHAT_SKILL_REL — never in SKILL_REL_BY_MODE.
 */
const SKILL_REL_BY_MODE = {
  Prototype: "V57/agents/skills/commands/setup/unity-prototype-skill.md",
  VerticalSlice: "V57/agents/skills/commands/setup/unity-vertical-slice-skill.md",
  Production: "V57/agents/skills/commands/setup/unity-game-setup-skill.md",
};

/** Intake companion skill (I0) — injected for VerticalSlice until Docs/Generated exists. */
const INTAKE_SKILL_REL = "V57/agents/skills/commands/setup/unity-intake-skill.md";

const COMMAND_BY_MODE = {
  Prototype: "/prototype",
  VerticalSlice: "/vertical-slice",
  Production: "/game-setup",
};

/** Slash-menu skills forwarded to the agent (not Workbench Build). */
const CHAT_SKILL_REL = {
  "prototype-full": "V57/agents/skills/commands/setup/unity-prototype-full-skill.md",
  "game-setup": "V57/agents/skills/commands/setup/unity-game-setup-skill.md",
  "game-setup-run-all": "V57/agents/skills/commands/setup/unity-game-setup-run-all-skill.md",
  "vertical-slice": "V57/agents/skills/commands/setup/unity-vertical-slice-skill.md",
};

const CHAT_SKILL_COMMAND = {
  "prototype-full": "/prototype-full",
  "game-setup": "/game-setup",
  "game-setup-run-all": "/game-setup-run-all",
  "vertical-slice": "/vertical-slice",
};

/**
 * @param {string} message
 * @returns {string | null} skill id without leading slash
 */
export function detectChatSkillId(message) {
  const m = String(message || "").trim();
  if (!m.startsWith("/")) return null;
  const tokens = m.slice(1).split(/[\s\n]/).filter(Boolean);
  const id = (tokens[0] || "").toLowerCase();
  // `/game-setup --run-all` → run-all skill
  if (id === "game-setup" && tokens.some((t) => t.toLowerCase() === "--run-all")) {
    return CHAT_SKILL_REL["game-setup-run-all"] ? "game-setup-run-all" : null;
  }
  return CHAT_SKILL_REL[id] ? id : null;
}

/**
 * @param {string} projectRoot
 * @param {string} skillId
 */
export async function loadChatSkill(projectRoot, skillId) {
  const id = String(skillId || "").toLowerCase();
  const rel = CHAT_SKILL_REL[id] || null;
  const command = CHAT_SKILL_COMMAND[id] || (id ? `/${id}` : null);
  if (!rel) {
    return { command, path: null, text: "", id: null };
  }
  const abs = path.join(projectRoot, ...rel.split("/"));
  try {
    const text = await fs.readFile(abs, "utf8");
    return { command, path: rel, text: String(text || "").trim(), id };
  } catch {
    return {
      command,
      path: rel,
      id,
      text: `(Skill file missing at ${rel} — follow generate-final.md and V57 standards.)`,
    };
  }
}

async function readSkill(projectRoot, rel) {
  try {
    return String((await fs.readFile(path.join(projectRoot, ...rel.split("/")), "utf8")) || "").trim();
  } catch {
    return null;
  }
}

/**
 * @param {string} projectRoot
 * @param {string} forgeMode
 * @param {{ firstTurn?: boolean }} [opts] firstTurn: allow the intake companion skill (VerticalSlice)
 * @returns {Promise<{ command: string | null, path: string | null, text: string, paths?: string[] }>}
 */
export async function loadForgeModeSkill(projectRoot, forgeMode = "Prototype", opts = {}) {
  const mode = normalizeForgeMode(forgeMode);
  const rel = SKILL_REL_BY_MODE[mode] || null;
  const command = COMMAND_BY_MODE[mode] || null;
  if (!rel) {
    return { command, path: null, text: "" };
  }
  const text = await readSkill(projectRoot, rel);
  const main =
    text ?? `(Skill file missing at ${rel} — follow generate-final.md and V57 standards.)`;
  const firstTurn = opts.firstTurn !== false;
  if (mode === "VerticalSlice" && firstTurn && needsIntake(projectRoot)) {
    const intake = await readSkill(projectRoot, INTAKE_SKILL_REL);
    return {
      command,
      path: rel,
      paths: [rel, INTAKE_SKILL_REL],
      text: [
        main,
        "",
        `## Companion skill for this turn: I0 INTAKE (${INTAKE_SKILL_REL})`,
        "Docs/Generated is missing — run intake first (`node V57/tools/intake/index.js --repo .`); a blocking issue (exit code 2) is a hard stop.",
        "",
        intake ?? `(Intake skill missing at ${INTAKE_SKILL_REL} — run the intake tool as described in the vertical-slice skill.)`,
      ].join("\n"),
    };
  }
  return { command, path: rel, text: main };
}

/**
 * @param {{ command?: string | null, path?: string | null, text?: string }} skill
 */
export function formatSkillBlock(skill) {
  if (!skill?.text) return "";
  const cmd = skill.command || "V57 skill";
  const where = skill.paths?.length ? ` (${skill.paths.join(" + ")})` : skill.path ? ` (${skill.path})` : "";
  return [
    `## Active V57 skill: ${cmd}${where}`,
    "",
    "You MUST follow this skill end-to-end for this run (stages, write roots, hardening gates, MCP + CLI policy).",
    "MCP = writes; Unity CLI = verify, compile poll, unity test, play smoke — see V57/docs/mcp/UNITY_CLI_INTEGRATIONS.md.",
    "",
    skill.text,
  ].join("\n");
}

export { CHAT_SKILL_REL, CHAT_SKILL_COMMAND, SKILL_REL_BY_MODE, INTAKE_SKILL_REL };
