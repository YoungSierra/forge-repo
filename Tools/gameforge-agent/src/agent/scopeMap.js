import fs from "node:fs/promises";
import path from "node:path";
import { detectRepoLayout, normalizeForgeMode } from "./writePolicy.js";

/**
 * Short under-the-hood path index for Agent chat — not a project dump.
 * @param {string} root
 * @param {string} slug
 * @param {string} forgeMode
 */
export async function buildScopeMap({ root, slug, forgeMode = "Prototype" }) {
  const mode = normalizeForgeMode(forgeMode);
  const safeSlug = String(slug || "Project").replace(/[^A-Za-z0-9_-]/g, "") || "Project";
  /** @type {string[]} */
  const lines = ["## Scope map (start here — use tools for detail)", `Forge mode: ${mode}`, `TDD slug: ${safeSlug}`];

  if (mode === "Prototype") {
    const proto = `Assets/Prototypes/${safeSlug}`;
    lines.push(`Write root: ${proto}/`);
    lines.push(`Entry hint: ${proto}/Scenes/ (or first .unity under ${proto}/)`);
    await appendExisting(root, lines, [
      `${proto}/Scenes`,
      `${proto}/Scripts`,
      `Docs/tdds/${safeSlug}`,
    ]);
  } else if (detectRepoLayout(root) === "provider") {
    // Provider repo (DESIGN_BRIEF §1/§2) — same layout for VerticalSlice and Production.
    lines.push("Layout: provider repo (V57 works in place on branch v57/setup)");
    lines.push(
      "Write roots: Assets/_Game/{Scripts,Scenes,Prefabs,Data,Settings,Tests}/, Assets/_Game/Art/**/Materials/, Docs/V57/, V57/specs/, Packages/manifest.json, ProjectSettings/",
    );
    lines.push("Read-only (provider): Docs/Design/, Docs/ArtDirection/, Docs/Audio/, Assets/_Game/Art/ (except Materials), Assets/_Game/Audio/");
    lines.push("State: Docs/V57/STATUS.json (stage/status/next/next_spec/gates) · PLAN.md · TODO.md · DEVLOG.md · DECISIONS.md");
    lines.push("Entry hint: Assets/_Game/Scenes/ (SCN_*.unity from Docs/Generated/scenes.yaml)");
    await appendExisting(root, lines, [
      "Docs/V57/STATUS.json",
      "Docs/Design/TDD.md",
      "Docs/Generated",
      "Assets/_Game/Scenes",
      "Assets/_Game/Scripts",
      "V57/knowledge",
    ]);
  } else if (mode === "VerticalSlice") {
    lines.push("Write root: Assets/VerticalSlice/");
    lines.push("Entry hint: Assets/VerticalSlice/Scenes/");
    await appendExisting(root, lines, [
      "Assets/VerticalSlice/Scenes",
      "Assets/VerticalSlice/Scripts",
      `Docs/tdds/${safeSlug}`,
    ]);
  } else {
    lines.push("Write roots: Assets/Scripts/, Assets/Scenes/, Assets/Prefabs/, Assets/UI/");
    lines.push("Entry hint: Assets/Scenes/Main.unity (or active build scene)");
    await appendExisting(root, lines, [
      "Assets/Scenes",
      "Assets/Scripts",
      `Docs/tdds/${safeSlug}`,
      "CONTEXT.md",
    ]);
  }

  lines.push("Do not dump entire folders into chat — read_file / list_dir only what you need.");
  return lines.join("\n");
}

async function appendExisting(root, lines, rels) {
  for (const rel of rels) {
    const abs = path.join(root, ...rel.split("/"));
    try {
      const st = await fs.stat(abs);
      if (st.isDirectory()) {
        const ents = await fs.readdir(abs);
        const sample = ents
          .filter((n) => !n.startsWith("."))
          .slice(0, 8)
          .join(", ");
        lines.push(`- ${rel}/ ${sample ? `(${sample}${ents.length > 8 ? ", …" : ""})` : "(empty)"}`);
      } else {
        lines.push(`- ${rel} (file)`);
      }
    } catch {
      lines.push(`- ${rel}/ (missing — create if needed)`);
    }
  }
}
