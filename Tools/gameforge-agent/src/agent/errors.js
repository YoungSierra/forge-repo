/**
 * Classify operator-facing failures for GameForge Chat UI.
 * @param {unknown} err
 * @returns {{ kind: "attachment"|"provider"|"network"|"cancel"|"other", message: string, retryable: boolean, action: string }}
 */
export function classifyAgentError(err) {
  const code = err?.code || err?.cause?.code || "";
  const status = Number(err?.status || err?.httpStatus || 0);
  const msg = String(err?.message || err || "Unknown error");
  const causeBlob = collectErrorBlob(err);

  if (/abort|stopp?ed|cancel/i.test(msg)) {
    return {
      kind: "cancel",
      message: "Stopped.",
      retryable: false,
      action: "",
    };
  }

  if (
    code === "ATTACHMENT_TOO_LARGE" ||
    code === "ATTACHMENT_TYPE" ||
    code === "ATTACHMENT_COUNT" ||
    code === "ATTACHMENT_EMPTY" ||
    code === "ATTACHMENT_MISSING" ||
    /attachment/i.test(msg)
  ) {
    return {
      kind: "attachment",
      message: msg.slice(0, 240),
      retryable: false,
      action: "Attach a smaller JPEG/PNG (under 2 MB) or fewer images.",
    };
  }

  // AV / corporate SSL inspection (e.g. Kaspersky) MITMs Cursor API — not a flaky network blip.
  if (
    code === "TLS_MITM" ||
    /SELF_SIGNED_CERT|UNABLE_TO_VERIFY_LEAF|CERT_HAS_EXPIRED|certificate|SSL.?inspection|Kaspersky|Zscaler|Fortinet/i.test(
      causeBlob,
    )
  ) {
    return {
      kind: "provider",
      message: msg.slice(0, 240),
      retryable: false,
      action:
        "Antivirus HTTPS scan is blocking Cursor API (TLS). Bootstrap Agent again (exports the AV root CA), or disable HTTPS scanning for Node / set NODE_EXTRA_CA_CERTS.",
    };
  }

  if (
    status === 401 ||
    status === 403 ||
    /api key|unauthorized|invalid.?key|quota|billing|model.?not.?found/i.test(msg)
  ) {
    return {
      kind: "provider",
      message: msg.slice(0, 240),
      retryable: false,
      action: "Check API key in Workbench / .env, or switch model.",
    };
  }

  if (
    status >= 500 ||
    status === 429 ||
    code === "ECONNRESET" ||
    code === "ETIMEDOUT" ||
    code === "ENOTFOUND" ||
    code === "ECONNREFUSED" ||
    /timeout|network|fetch failed|socket|ECONN|temporarily/i.test(msg)
  ) {
    return {
      kind: "network",
      message: msg.slice(0, 240),
      retryable: true,
      action: "Retry — this looks transient.",
    };
  }

  if (status >= 400 && status < 500) {
    return {
      kind: "provider",
      message: msg.slice(0, 240),
      retryable: false,
      action: "Fix the request or switch model, then retry.",
    };
  }

  return {
    kind: "other",
    message: msg.slice(0, 240),
    retryable: false,
    action: "Retry or check Agent status in Workbench.",
  };
}

function collectErrorBlob(err) {
  const parts = [];
  let cur = err;
  let depth = 0;
  while (cur && depth < 8) {
    parts.push(String(cur.code || ""), String(cur.message || cur || ""));
    cur = cur.cause;
    depth += 1;
  }
  return parts.join(" ");
}

export function formatClassifiedError(classified) {
  const c = classified || classifyAgentError("Unknown error");
  return c.action ? `${c.message}\n→ ${c.action}` : c.message;
}
