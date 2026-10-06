/**
 * Pick one final reply from streamed assistant events.
 * Cursor often sends growing snapshots (`**Biol**` → `**Biolum**`) that are NOT
 * strict prefixes — never concatenate those or the chat turns into gibberish.
 * @param {string[]} chunks
 * @returns {string}
 */
export function mergeAssistantChunks(chunks = []) {
  const parts = chunks.map((c) => String(c || "").trim()).filter(Boolean);
  if (!parts.length) return "";
  if (parts.length === 1) return parts[0];
  const longest = parts.reduce((a, b) => (a.length >= b.length ? a : b));
  const last = parts[parts.length - 1];
  if (last.length >= Math.max(48, longest.length * 0.7)) return last;
  return longest;
}
