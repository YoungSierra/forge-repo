'use strict';
// Appends fixable/conflict intake issues to Docs/V57/DECISIONS.md as D-### entries (idempotent by key).
const fs = require('fs');
const path = require('path');

const HEAD = `# DECISIONS

Autonomous decisions taken by V57 instead of asking (brief §3). IDs are \`D-###\`, append-only.
Intake entries carry a hidden key so re-running intake never duplicates them.
`;

const KEY_RE = /<!--\s*v57-intake-key:\s*([0-9a-f]+)\s*-->/g;

function oneLine(s) {
  return String(s || '').replace(/\s+/g, ' ').trim();
}

/**
 * @returns {{file:string, added:number, skipped:number, ids:string[]}}
 */
function appendDecisions(repoRoot, issues, { stage = 'I0 INTAKE' } = {}) {
  const file = path.join(repoRoot, 'Docs/V57/DECISIONS.md');
  let text = fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : HEAD;
  const known = new Set([...text.matchAll(KEY_RE)].map((m) => m[1]));
  const nums = [...text.matchAll(/^##\s+D-(\d{3,})/gm)].map((m) => Number(m[1]));
  let next = nums.length ? Math.max(...nums) + 1 : 1;
  const ids = [];
  let skipped = 0;
  const chunks = [];
  for (const i of issues.filter((x) => x.level === 'fixable' || x.level === 'conflict')) {
    if (known.has(i.key)) { skipped++; continue; }
    const id = `D-${String(next++).padStart(3, '0')}`;
    ids.push(id);
    chunks.push([
      `## ${id} · ${stage} · ${i.level} · ${i.code}`,
      `- **Issue:** ${oneLine(i.message)}${i.count > 1 ? ` (×${i.count})` : ''}`,
      `- **Where:** \`${oneLine(i.where) || '-'}\``,
      `- **Decision:** ${oneLine(i.fix) || 'logged; no automatic change'}`,
      `- **Rule:** ${i.level === 'conflict' ? 'authority TDD > ADD > disk (brief §1); provider files are not edited' : 'logged fixable normalization (brief §3); provider files are not edited by intake'}`,
      `<!-- v57-intake-key: ${i.key} -->`,
      '',
    ].join('\n'));
  }
  if (chunks.length) {
    if (!text.endsWith('\n')) text += '\n';
    text += `\n${chunks.join('\n')}`;
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, text);
  } else if (!fs.existsSync(file)) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, text);
  }
  return { file, added: chunks.length, skipped, ids };
}

module.exports = { appendDecisions };
