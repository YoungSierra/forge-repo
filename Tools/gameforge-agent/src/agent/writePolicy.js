import { canonicalizeRel } from "../security/paths.js";
import fs from "node:fs";
import fsp from "node:fs/promises";
import path from "node:path";

/**
 * Unity GameForge write policy (mode roots under Assets/).
 * Modes: generate | chat | ask | sync
 * forgeMode: Prototype | VerticalSlice | Production
 *
 * Two repository layouts are supported:
 *  - legacy   : V57 template project (Assets/Prototypes, Assets/VerticalSlice, Assets/Scripts …)
 *  - provider : provider-delivered game repo (Docs/Design, Docs/ArtDirection, Assets/_Game/…) — see
 *               DESIGN_BRIEF §1/§2. V57 writes Assets/_Game/{Scripts,Scenes,Prefabs,Data,Settings,Tests},
 *               Art/**\/Materials, Docs/V57, Docs/Generated, V57/specs, Packages/manifest.json, ProjectSettings.
 *
 * Provider-owned paths are denied in every forge mode; the error tells the agent to log a D-### decision.
 * Errors carry `code`: PROVIDER_OWNED | KERNEL | OUTSIDE_ROOTS | READ_ONLY | SYNC_SCOPE.
 */

const BLOCKED_PREFIXES = [
  "packages/com.v57.unity-game-forge/",
  "tools/gameforge-agent/",
  "projectsettings/",
  "library/",
  ".env",
  "source/",
];

/** Kernel prefixes that VerticalSlice/Production may write (provider layout needs ProjectSettings). */
const KERNEL_EXEMPT_FOR_SLICE = ["projectsettings/"];

/** Sandbox roots that Production / VerticalSlice must never write. */
const SANDBOX_PREFIXES = ["assets/prototypes/", "assets/verticalslice/"];

const PRODUCTION_ASSET_PREFIXES = [
  "assets/scripts/",
  "assets/scenes/",
  "assets/prefabs/",
  "assets/ui/",
  "assets/materials/",
  "assets/audio/",
  "assets/settings/",
  "assets/input/",
  "assets/art/",
  "assets/animations/",
  "assets/shaders/",
];

/** Provider-repo layout: V57-owned write roots (VerticalSlice + Production). */
const PROVIDER_LAYOUT_PREFIXES = [
  "assets/_game/scripts/",
  "assets/_game/scenes/",
  "assets/_game/prefabs/",
  "assets/_game/data/",
  "assets/_game/settings/",
  "assets/_game/tests/",
  "docs/v57/",
  "docs/generated/", // intake tool output — allowed so the intake step (run by the agent) is not refused
  "v57/specs/",
  "projectsettings/",
];

const PROVIDER_LAYOUT_FILES = ["packages/manifest.json", "packages/packages-lock.json"];

/** Legacy report folder (GF-PROGRESS file, implement reports, NUnit XML). */
const LEGACY_REPORT_PREFIX = "v57/docs/reports/";

/**
 * Provider-owned trees. V57 never edits these (DESIGN_BRIEF §3 autonomy rules) except `.meta`
 * importer settings and generated `Materials/` folders under Art.
 */
const PROVIDER_OWNED_PREFIXES = [
  "docs/design/",
  "docs/artdirection/",
  "docs/audio/",
  "docs/marketing/",
  "assets/_game/art/",
  "assets/_game/audio/",
];
const PROVIDER_OWNED_FILES = ["docs/licenses.md"];

/** The provider TDD — editing it is a hard stop for autonomous runs. */
export const PROVIDER_TDD_REL = "Docs/Design/TDD.md";

function normalizeRel(p) {
  return String(p || "")
    .replace(/\\/g, "/")
    .replace(/^\.\//, "")
    .replace(/^\/+/, "")
    .toLowerCase();
}

/** @returns {"Prototype"|"VerticalSlice"|"Production"} */
export function normalizeForgeMode(forgeMode) {
  const m = String(forgeMode || "Prototype").trim();
  const lower = m.toLowerCase();
  if (lower.includes("vertical")) return "VerticalSlice";
  if (lower.includes("production")) return "Production";
  if (lower.includes("prototype")) return "Prototype";
  return "Prototype";
}

export function isKernelPath(rel) {
  const r = normalizeRel(rel);
  return BLOCKED_PREFIXES.some((p) => r === p || r.startsWith(p) || r.startsWith(p.replace(/\/$/, "")));
}

/**
 * `provider` when the root looks like a provider-delivered repo (Docs/Design or Assets/_Game), else `legacy`.
 * @param {string} root absolute project root
 * @returns {"provider"|"legacy"}
 */
export function detectRepoLayout(root) {
  if (!root) return "legacy";
  try {
    if (fs.existsSync(path.join(root, "Docs", "Design"))) return "provider";
    if (fs.existsSync(path.join(root, "Assets", "_Game"))) return "provider";
  } catch {
    /* ignore */
  }
  return "legacy";
}

function writeRootForForgeMode(forgeMode) {
  const mode = normalizeForgeMode(forgeMode);
  if (mode === "VerticalSlice") return "assets/verticalslice/";
  if (mode === "Production") return "assets/scripts/";
  return "assets/prototypes/";
}

function isSandboxPath(rn) {
  return SANDBOX_PREFIXES.some((p) => rn.startsWith(p));
}

function productionAssetAllowed(rn) {
  return PRODUCTION_ASSET_PREFIXES.some((p) => rn.startsWith(p));
}

/** Art `…/Materials/…` folders are V57-generated (MAT_<Asset>.mat). */
function isArtMaterialPath(rn) {
  if (rn.startsWith("assets/_game/art/ui/atlases")) return true; // SpriteAtlas assets from BuildUiAtlases()
  return rn.startsWith("assets/_game/art/") && /\/materials(\/|\.meta$)/.test(rn.slice("assets/_game/art".length));
}

export function providerLayoutAllowed(rn) {
  if (PROVIDER_LAYOUT_FILES.includes(rn)) return true;
  if (PROVIDER_LAYOUT_PREFIXES.some((p) => rn.startsWith(p))) return true;
  if (isArtMaterialPath(rn)) return true;
  return false;
}

/**
 * Provider-owned? `.meta` importer settings and Art Materials are V57's (AssetPostprocessor / BuildMaterials).
 * @param {string} rel
 */
export function isProviderOwnedPath(rel) {
  const rn = normalizeRel(rel);
  if (rn.endsWith(".meta")) return false;
  if (isArtMaterialPath(rn)) return false;
  if (PROVIDER_OWNED_FILES.includes(rn)) return true;
  return PROVIDER_OWNED_PREFIXES.some((p) => rn.startsWith(p));
}

/** Folder `.meta` for an ancestor of an allowed root (e.g. `Assets/_Game.meta`). */
function isAncestorFolderMeta(rn) {
  if (!rn.endsWith(".meta")) return false;
  const dir = rn.slice(0, -".meta".length) + "/";
  return [...PROVIDER_LAYOUT_PREFIXES, ...PRODUCTION_ASSET_PREFIXES, ...SANDBOX_PREFIXES].some((p) =>
    p.startsWith(dir),
  );
}

/** `.meta` next to provider art/audio = importer settings written by V57 import rules. */
function isProviderImporterMeta(rn) {
  return rn.endsWith(".meta") && (rn.startsWith("assets/_game/art/") || rn.startsWith("assets/_game/audio/"));
}

function specsAllowed(rn, mode) {
  if (mode === "Production") {
    return rn.startsWith("v57/specs/systems/") || rn.startsWith("v57/specs/features/");
  }
  if (mode === "VerticalSlice") {
    return rn.startsWith("v57/specs/features/") || rn.startsWith("v57/specs/systems/");
  }
  return (
    rn.startsWith("v57/specs/prototypes/") ||
    rn.startsWith("v57/specs/features/") ||
    rn.startsWith("v57/specs/systems/")
  );
}

function policyError(code, message) {
  const err = new Error(message);
  err.code = code;
  return err;
}

const PROVIDER_LAYOUT_ROOTS_TEXT = [
  "- **Game code/content (V57):** `Assets/_Game/Scripts/**` (one asmdef per area), `Assets/_Game/Scenes/**`, `Assets/_Game/Prefabs/**`, `Assets/_Game/Data/**`, `Assets/_Game/Settings/**`, `Assets/_Game/Tests/{EditMode,PlayMode}/**`",
  "- **Materials:** `Assets/_Game/Art/**/Materials/MAT_<Asset>.mat` only (the rest of Art is provider-owned)",
  "- **Run state / reports / evidence:** `Docs/V57/**` (STATUS.json, PLAN.md, TODO.md, DEVLOG.md, DECISIONS.md, reports/, evidence/)",
  "- **Generated data:** `Docs/Generated/**` — only via `node V57/tools/intake/index.js --repo .` (never hand-edit)",
  "- **Specs:** `V57/specs/**` · **Project:** `Packages/manifest.json`, `ProjectSettings/**`",
  "- **PROVIDER-OWNED (read-only):** `Docs/Design/**` (TDD — editing it is a hard stop), `Docs/ArtDirection/**`, `Docs/Audio/**`, `Assets/_Game/Art/**` (except Materials), `Assets/_Game/Audio/**`. Need a change there? Log a `D-###` entry in `Docs/V57/DECISIONS.md` instead.",
];

/**
 * Human-readable write roots for prompts (forge mode).
 * @param {string} forgeMode
 * @param {{ layout?: "provider"|"legacy" }} [opts] VerticalSlice defaults to the provider layout.
 */
export function describeWriteRoots(forgeMode, opts = {}) {
  const mode = normalizeForgeMode(forgeMode);
  const layout = opts.layout || (mode === "VerticalSlice" ? "provider" : "legacy");
  if (mode === "Production" && layout === "provider") {
    return ["## Write roots (Production · provider repo — mandatory)", ...PROVIDER_LAYOUT_ROOTS_TEXT].join("\n");
  }
  if (mode === "Production") {
    return [
      "## Write roots (Production — mandatory)",
      "- **Scripts:** `Assets/Scripts/**` (namespaces per STANDARDS_CANONICAL)",
      "- **Scenes:** `Assets/Scenes/**`",
      "- **Prefabs:** `Assets/Prefabs/**`",
      "- **UI:** `Assets/UI/**` (UI Toolkit UXML/USS/PanelSettings)",
      "- **Materials / Audio / Settings / Input:** under `Assets/Materials/`, `Assets/Audio/`, `Assets/Settings/`, `Assets/Input/` as needed",
      "- **Specs:** `V57/specs/<slug>/systems/**`, `V57/specs/<slug>/features/**`",
      "- **FORBIDDEN:** `Assets/Prototypes/**`, `Assets/VerticalSlice/**` — Build Production runs `/game-setup`, not a sandbox prototype.",
      "- **Unity MCP:** scene/asset edits must stay under production roots above — MCP does not exempt `Assets/Prototypes/`.",
    ].join("\n");
  }
  if (mode === "VerticalSlice" && layout === "provider") {
    return [
      "## Write roots (Vertical Slice · provider repo — mandatory)",
      ...PROVIDER_LAYOUT_ROOTS_TEXT,
      "- **FORBIDDEN:** `Assets/Prototypes/**`.",
    ].join("\n");
  }
  if (mode === "VerticalSlice") {
    return [
      "## Write roots (Vertical Slice — mandatory)",
      "- **Product:** `Assets/VerticalSlice/**` (Scripts, Scenes, Prefabs, UI, Materials…)",
      "- **Specs:** `V57/specs/<slug>/features/**`",
      "- **FORBIDDEN:** `Assets/Prototypes/**`, flat production promotion into `Assets/Scripts/**` unless the user explicitly asks.",
    ].join("\n");
  }
  return [
    "## Write roots (Prototype — mandatory)",
    "- **Sandbox:** `Assets/Prototypes/**` (prefer `Assets/Prototypes/<slug>/` for full graybox runs)",
    "- **Specs:** `V57/specs/prototypes/**`",
    "- **FORBIDDEN:** `Assets/Scripts/**` production tree (use Prototype mode roots only).",
  ].join("\n");
}

/**
 * @param {string} rel
 * @param {"generate"|"chat"|"ask"|"sync"} mode
 * @param {{ slug?: string, forgeMode?: string }} [opts]
 */
export function assertAgentWriteAllowed(rel, mode = "generate", opts = {}) {
  const r = canonicalizeRel(rel);
  const rn = normalizeRel(r);
  const forgeMode = normalizeForgeMode(opts.forgeMode);

  if (mode === "ask") {
    throw policyError(
      "READ_ONLY",
      "Ask mode is read-only — switch to Agent mode to edit prototype files",
    );
  }

  if (mode === "sync") {
    const slug = opts.slug || "";
    if (!slug || !/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/.test(slug)) {
      throw policyError("SYNC_SCOPE", "Sync requires a valid slug");
    }
    const esc = slug.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const ok = new RegExp(`^docs/tdds/${esc}/[^/]+\\.md$`, "i").test(rn);
    if (!ok) {
      throw policyError("SYNC_SCOPE", `Sync may only write Docs/tdds/${slug}/<file>.md (got ${r})`);
    }
    return;
  }

  if (isProviderOwnedPath(rn)) {
    const tdd = rn === PROVIDER_TDD_REL.toLowerCase();
    throw policyError(
      "PROVIDER_OWNED",
      `Write denied: ${r} is provider-owned (Docs/Design, Docs/ArtDirection, Docs/Audio, Assets/_Game/Art except Materials, Assets/_Game/Audio). ` +
        (tdd
          ? "Editing the provider TDD is a HARD STOP — never change it to make a gate pass. "
          : "") +
        "Do NOT edit, rename or move provider files. Log a decision instead: append `D-### <title> — context / options / choice / consequence` to Docs/V57/DECISIONS.md, " +
        "then continue with a V57-owned workaround (e.g. a material, prefab or data asset under Assets/_Game/).",
    );
  }

  const sliceLike = forgeMode === "VerticalSlice" || forgeMode === "Production";
  const kernelExempt = sliceLike && KERNEL_EXEMPT_FOR_SLICE.some((p) => rn.startsWith(p));
  if (!kernelExempt && (isKernelPath(r) || isKernelPath(rn))) {
    throw policyError("KERNEL", `Write not allowed (package/agent/secrets): ${r}`);
  }

  if (forgeMode === "Production" && isSandboxPath(rn)) {
    throw policyError(
      "OUTSIDE_ROOTS",
      `Production mode must not write sandbox paths (use Assets/Scripts, Assets/Scenes, Assets/Prefabs, … or Assets/_Game/** in a provider repo): ${r}`,
    );
  }

  if (forgeMode === "VerticalSlice" && rn.startsWith("assets/prototypes/")) {
    throw policyError("OUTSIDE_ROOTS", `Vertical Slice mode must not write Assets/Prototypes/: ${r}`);
  }

  if (forgeMode === "Prototype" && rn.startsWith("assets/scripts/")) {
    throw policyError("OUTSIDE_ROOTS", `Prototype mode must not write production Assets/Scripts/: ${r}`);
  }

  const root = writeRootForForgeMode(forgeMode);
  const underModeRoot =
    rn.startsWith(root) || r.replace(/\\/g, "/").toLowerCase().startsWith(root);

  if (underModeRoot) return;

  if (forgeMode === "Production" && productionAssetAllowed(rn)) return;

  if (forgeMode === "VerticalSlice" && rn.startsWith("assets/verticalslice/")) return;

  if (sliceLike) {
    if (providerLayoutAllowed(rn)) return;
    if (isProviderImporterMeta(rn) || isAncestorFolderMeta(rn)) return;
    if (rn.startsWith(LEGACY_REPORT_PREFIX)) return;
  }

  if (specsAllowed(rn, forgeMode)) return;

  const layout = opts.layout || (forgeMode === "VerticalSlice" ? "provider" : "legacy");
  const rootsLines = describeWriteRoots(forgeMode, { layout }).split("\n").slice(1, 3).join(" ");
  throw policyError(
    "OUTSIDE_ROOTS",
    `Write not allowed in ${mode} mode (forge=${forgeMode}): ${r}. Allowed roots: ${rootsLines || root}`,
  );
}

/**
 * Non-throwing variant for audits.
 * @returns {{ ok: true } | { ok: false, code: string, reason: string }}
 */
export function checkAgentWrite(rel, mode = "generate", opts = {}) {
  try {
    assertAgentWriteAllowed(rel, mode, opts);
    return { ok: true };
  } catch (err) {
    return { ok: false, code: err?.code || "OUTSIDE_ROOTS", reason: err?.message || String(err) };
  }
}

export { writeRootForForgeMode, normalizeRel };

/**
 * `/prototype-full` folder bootstrap:
 * ensures `Assets/Prototypes/` and `Assets/Prototypes/<slug>/` (+ standard subfolders).
 * @param {string} projectRoot absolute Unity project root
 * @param {string} slug TDD slug / project folder name
 * @returns {Promise<{ projectRel: string, created: string[] }>}
 */
export async function ensurePrototypeProjectFolders(projectRoot, slug) {
  const safe = String(slug || "").trim();
  if (!safe || !/^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$/.test(safe)) {
    throw new Error(`Invalid TDD slug for Prototypes project folder: ${slug}`);
  }

  const created = [];
  const rels = [
    "Assets/Prototypes",
    `Assets/Prototypes/${safe}`,
    `Assets/Prototypes/${safe}/Scripts`,
    `Assets/Prototypes/${safe}/Scenes`,
    `Assets/Prototypes/${safe}/Materials`,
    `Assets/Prototypes/${safe}/Prefabs`,
    `Assets/Prototypes/${safe}/UI`,
  ];

  for (const rel of rels) {
    const abs = path.join(projectRoot, rel);
    try {
      await fsp.access(abs);
    } catch {
      await fsp.mkdir(abs, { recursive: true });
      created.push(rel);
    }
  }

  return { projectRel: `Assets/Prototypes/${safe}`, created };
}
