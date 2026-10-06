import fs from "node:fs";
import path from "node:path";
import tls from "node:tls";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const AGENT_ROOT = path.resolve(__dirname, "../..");

let injectedPath = "";

/**
 * Trust AV/SSL-inspection roots for Cursor API (e.g. Kaspersky MITM).
 * Call before Agent.create. Uses Node setDefaultCACertificates (Node 22+).
 * @param {string} [agentRoot]
 * @returns {{ ok: boolean, path?: string, reason?: string }}
 */
export function ensureTlsExtraCa(agentRoot = AGENT_ROOT) {
  try {
    const fromEnv = String(process.env.NODE_EXTRA_CA_CERTS || "").trim();
    const pemPath = fromEnv || path.join(agentRoot, ".certs", "extra-ca.pem");
    if (!pemPath || !fs.existsSync(pemPath)) {
      return { ok: false, reason: "no-pem" };
    }
    if (injectedPath === pemPath) return { ok: true, path: pemPath };

    const pem = fs.readFileSync(pemPath, "utf8");
    if (!pem.includes("BEGIN CERTIFICATE")) {
      return { ok: false, path: pemPath, reason: "invalid-pem" };
    }

    if (!fromEnv) process.env.NODE_EXTRA_CA_CERTS = pemPath;

    if (typeof tls.setDefaultCACertificates !== "function") {
      return { ok: false, path: pemPath, reason: "no-setDefaultCACertificates" };
    }

    const base =
      typeof tls.getCACertificates === "function"
        ? tls.getCACertificates("default")
        : [...(tls.rootCertificates || [])];
    tls.setDefaultCACertificates([...base, pem]);
    injectedPath = pemPath;
    return { ok: true, path: pemPath };
  } catch (err) {
    return { ok: false, reason: String(err?.message || err).slice(0, 160) };
  }
}
