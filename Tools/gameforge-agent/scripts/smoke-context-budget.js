/**
 * Offline check for the context-budget behaviour of the LLM provider:
 * read_file windowing, tool-result clamping, history digesting and image
 * stubbing. Serves a stub /chat/completions so no provider is billed and the
 * Editor's Unity MCP approval is left alone (GAMEFORGE_UNITY_MCP=0).
 *
 *   node scripts/smoke-context-budget.js
 */
import fs from "node:fs";
import http from "node:http";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

process.env.GAMEFORGE_UNITY_MCP = "0";
process.env.GAMEFORGE_HISTORY_TOOL_RESULTS = "3";
process.env.GAMEFORGE_IMAGE_TURNS = "2";

const { createLlmProvider, selectUnityTools } = await import("../src/agent/providers/llm.js");

/** Tool names exposed by `unity mcp` (Unity CLI pipeline, Unity 6). */
const UNITY_MCP_NAMES =
  "add_animator_layer add_animator_parameter add_animator_state add_animator_transition add_component add_scene_to_build add_timeline_clip add_timeline_track apply_prefab_overrides attach_script audit audit_status bake_lighting bake_navmesh bake_navmesh_surfaces bake_occlusion_culling build build_status cancel_lighting_bake cancel_navmesh_bake cancel_occlusion_bake cancel_tests capture_game_view capture_scene_view clear_baked_lighting clear_console clear_navmesh clear_occlusion_culling console copy_asset create_animation_clip create_animator_controller create_asset create_folder create_gameobject create_gameobjects create_prefab create_prefab_variant create_scene create_script create_timeline delete_asset delete_gameobject editor_focus editor_pause editor_play editor_status editor_stop eval eval_file find_assets find_gameobjects get_animation_clip get_animator_controller get_audio_settings get_authoring_root get_build_settings get_component_properties get_console_logs get_graphics_settings get_import_settings get_input_settings get_lighting_settings get_material_properties get_navmesh_settings get_performance_stats get_physics_settings get_player_settings get_quality_settings get_scene_hierarchy get_selection get_serialized_fields get_shader_properties get_tags_layers get_time_settings get_timeline import_asset instantiate_prefab lighting_bake_status list_build_profiles list_build_targets list_open_scenes list_shaders list_tests menu move_asset navmesh_bake_status occlusion_bake_status open_scene package_add package_list package_remove package_resolve package_search package_status read_text_file recompile recompile_status reload_file reload_file_override remove_animation_curve remove_component remove_scene_from_build rename_asset rename_gameobject revert_prefab_overrides run_tests save_all save_prefab_contents save_scene screenshot search set_active set_active_scene set_animation_curve set_audio_settings set_authoring_root set_autotick set_build_settings set_component_properties set_graphics_settings set_import_settings set_input_settings set_layer set_lighting_settings set_material_properties set_navmesh_settings set_parent set_physics_settings set_player_settings set_quality_settings set_selection set_serialized_field set_tag set_tags_layers set_time_settings set_transform switch_build_target switch_build_target_status test_status unpack_prefab write_text_file".split(
    " ",
  );

function checkToolSelection() {
  const all = UNITY_MCP_NAMES.map((name) => ({
    type: "function",
    function: { name, description: `Unity MCP tool ${name}`, parameters: { type: "object", properties: {} } },
  }));
  const generate = selectUnityTools(all, { writeMode: "generate", slots: 0 }).map((t) => t.function.name);
  const ask = selectUnityTools(all, { writeMode: "ask", slots: 0 }).map((t) => t.function.name);
  const must = ["open_scene", "save_scene", "eval", "set_transform", "set_parent", "write_text_file", "create_gameobject", "add_component", "get_console_logs", "recompile", "editor_play", "capture_game_view"];
  const missing = must.filter((n) => !generate.includes(n));
  const askWrites = ask.filter((n) => /^(create|set|delete|write|save|attach|instantiate|import|menu|editor_play|editor_stop|eval)/.test(n));
  console.log(`tool allowlist: ${all.length} available → ${generate.length} for generate / ${ask.length} for ask`);
  return [
    ...(missing.length ? [`allowlist drops required tools: ${missing.join(", ")}`] : []),
    ...(askWrites.length ? [`ask mode exposes write tools: ${askWrites.join(", ")}`] : []),
  ];
}

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../..");
const TDD_REL = "V57/tools/intake/test/fixtures/TDD_HappyHabitat.md";
const TURNS = 6;
// 1x1 PNG attachment written to a temp dir (image stubbing check only needs a real image file).
const tinyPng = path.join(fs.mkdtempSync(path.join(os.tmpdir(), "gf-ctx-")), "smoke-attachment.png");
fs.writeFileSync(tinyPng, Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==", "base64"));

/** @type {object[][]} */
const seenBodies = [];

function toolCall(id, name, args) {
  return {
    id,
    type: "function",
    function: { name, arguments: JSON.stringify(args) },
  };
}

const server = http.createServer((req, res) => {
  let raw = "";
  req.on("data", (c) => (raw += c));
  req.on("end", () => {
    const body = JSON.parse(raw || "{}");
    seenBodies.push(body);
    const turn = seenBodies.length;
    const message =
      turn === 1
        ? { role: "assistant", content: "Reading the spec.", tool_calls: [toolCall("c1", "read_file", { path: TDD_REL })] }
        : turn < TURNS
          ? {
              role: "assistant",
              content: `Listing ${turn}.`,
              tool_calls: [toolCall(`c${turn}`, "list_dir", { path: "V57/tools/intake/lib" })],
            }
          : { role: "assistant", content: "Done." };
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ choices: [{ message }], usage: { prompt_tokens: 10, completion_tokens: 2 } }));
  });
});

await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const { port } = server.address();

const provider = createLlmProvider({
  root,
  apiKey: "stub",
  baseUrl: `http://127.0.0.1:${port}/v1`,
  model: "gpt-4o",
  writeMode: "generate",
  slug: "Pinball",
  forgeMode: "Prototype",
});

const statuses = [];
await provider.run("Build the table.", {
  onEvent: (ev) => {
    if (ev?.type === "status") statuses.push(ev.message);
  },
  attachments: [
    { absPath: tinyPng, relPath: "Temp/smoke-attachment.png", mime: "image/png" },
  ],
});
server.close();

const fail = checkToolSelection();
const last = seenBodies[seenBodies.length - 1];
const toolMsgs = last.messages.filter((m) => m.role === "tool");
const readResult = toolMsgs[0];
const digested = toolMsgs.filter((m) => /trimmed after later turns/.test(String(m.content)));
const userMsg = last.messages.find((m) => m.role === "user");
const imageParts = Array.isArray(userMsg?.content)
  ? userMsg.content.filter((p) => p?.type === "image_url").length
  : 0;
const internalKeys = last.messages.flatMap((m) => Object.keys(m).filter((k) => k.startsWith("_")));

const sizeOf = (b) => JSON.stringify(b.messages).length;

console.log(`turns served: ${seenBodies.length}`);
console.log(`tools sent:   ${last.tools.length} (${last.tools.map((t) => t.function.name).join(", ")})`);
console.log(`read_file result: ${String(readResult?.content || "").length} chars`);
console.log(`  header: ${String(readResult?.content || "").split("\n")[0].slice(0, 110)}`);
console.log(`digested tool results in final body: ${digested.length} of ${toolMsgs.length}`);
console.log(`image parts still in final body: ${imageParts}`);
console.log(`message payload: turn1 ${sizeOf(seenBodies[0])} chars → final ${sizeOf(last)} chars`);
console.log("status lines about trimming:");
statuses.filter((s) => /trimmed|extract/i.test(s)).forEach((s) => console.log(`  · ${s}`));

if (seenBodies.length !== TURNS) fail.push(`expected ${TURNS} turns, got ${seenBodies.length}`);
if (!/^\[V57\/tools\/intake\/test\/fixtures\/.*lines 1-1200 of \d+/.test(String(readResult?.content || ""))) {
  fail.push("read_file result is not a labelled window");
}
if (!digested.length) fail.push("no older tool result was digested");
if (imageParts !== 0) fail.push(`image parts should be stubbed after turn 2, found ${imageParts}`);
if (internalKeys.length) fail.push(`internal fields leaked to the wire: ${internalKeys.join(", ")}`);

if (fail.length) {
  console.error("\nFAIL");
  fail.forEach((f) => console.error(` - ${f}`));
  process.exit(1);
}
console.log("\nOK — windowing, clamping, digesting and image stubbing all active.");
