/**
 * User messages when resuming from Stop checkpoint — must preserve staged pipeline context.
 */

/** @param {{ op?: string, chatSkillId?: string, forgeMode?: string }} cp */
export function buildResumeUserMessage(cp, userReply = "continue") {
  const reply = String(userReply || "continue").trim() || "continue";
  const op = cp?.op || "";
  const skill = cp?.chatSkillId || "";
  const forge = String(cp?.forgeMode || "");

  if (op === "game-setup-run-all" || skill === "game-setup-run-all") {
    return [
      `User replied: ${reply}`,
      "",
      "Resume **/game-setup-run-all** (Production, run_all). Mandatory:",
      "- Do NOT switch to prototype graybox or `/prototype-full`.",
      "- Do NOT write under `Assets/Prototypes/` or `Assets/VerticalSlice/`.",
      "- After fixing the blocker (if any), **chain remaining stages** without waiting for continue.",
      "- Still STOP on hard failures. INIT confirm if never confirmed.",
      "- Never end with optional offers ('If you want I can…').",
      "- Tests: **`unity test`** / `POST /api/unity-cli/test` only — not Test Runner MCP.",
    ].join("\n");
  }

  if (op === "game-setup" || skill === "game-setup" || forge === "Production") {
    return [
      `User replied: ${reply}`,
      "",
      "Resume the **staged /game-setup** pipeline (Production). Mandatory:",
      "- Write only under production roots: `Assets/Scripts/`, `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/UI/`, etc.",
      "- Complete ONLY the next incomplete stage, emit the skill **Progress** block with `Reply continue`, then **STOP**.",
      "- When §11.5 camera or §9 HUD is in run scope, obey `GAME_FEEL_STANDARDS.md` at **G-FEEL** (branch on §11.5 profile — do not require FPS lean on follow-cam games).",
      "- Never end with optional offers ('If you want I can…').",
      "- If INIT was never confirmed, re-emit INIT defaults table and STOP — do not run SETUP yet.",
      "- When running tests (Stage K / spec implement gates): use **`unity test`** or `POST /api/unity-cli/test` only — not Test Runner MCP.",
    ].join("\n");
  }

  if (op === "vertical-slice" || skill === "vertical-slice" || forge === "VerticalSlice") {
    return [
      `User replied: ${reply}`,
      "",
      "Resume the **/vertical-slice** stage driver (provider repo, autonomous). Mandatory:",
      "- Re-read Docs/V57/STATUS.json first and continue its `next` — do not restart finished stages.",
      "- Write only V57-owned roots (`Assets/_Game/{Scripts,Scenes,Prefabs,Data,Settings,Tests}`, Art `Materials/`, `Docs/V57/`, `V57/specs/`). Provider files are read-only — log `D-###` instead.",
      "- Do not wait for `continue` between stages; end the turn with STATUS.json updated. Stop only via `status: \"blocked\"` + `hard_stop` for a real hard stop.",
      "- Tests: **`unity test`** / `POST /api/unity-cli/test` only — not Test Runner MCP. Gates need evidence files; never self-grade.",
    ].join("\n");
  }

  if (op === "prototype-full" || skill === "prototype-full") {
    return [
      `User replied: ${reply}`,
      "",
      "Resume **/prototype-full** under `Assets/Prototypes/<slug>/` only. Do not promote to production.",
      "Never create `Assets/Tests/**` or run Test Runner — prototype uses compile-heal + Play smoke only.",
      "Finish remaining prototype work; obey write policy.",
    ].join("\n");
  }

  return "Continue from this checkpoint. Finish remaining work. Do not redo completed writes unless they are broken.";
}
