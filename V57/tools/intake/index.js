#!/usr/bin/env node
'use strict';
// V57 I0 INTAKE CLI.
//   node index.js --repo <path> [--out Docs/Generated] [--json] [--unity-pin 6000.6.2f1]
// Exit codes: 0 ok (issues may exist), 2 blocking issues, 1 crash / bad usage.

const USAGE = `Usage: node V57/tools/intake/index.js --repo <path> [--out <dir>] [--json] [--unity-pin <version>]

  --repo       provider repository root (required)
  --out        output dir for generated yaml/json, relative to repo (default Docs/Generated)
  --json       print a machine-readable summary to stdout
  --unity-pin  Unity version V57 pins (default: V57 template ProjectVersion or 6000.6.2f1)

Writes Docs/Generated/*.yaml, Docs/Generated/json/*.json, Docs/Generated/localization.csv,
Docs/V57/INTAKE_REPORT.md and appends fixable/conflict decisions to Docs/V57/DECISIONS.md.
Exit: 0 = continue, 2 = blocking issues (stop the run), 1 = crash.`;

function parseArgs(argv) {
  const a = { json: false };
  for (let i = 0; i < argv.length; i++) {
    const k = argv[i];
    const take = () => { if (i + 1 >= argv.length) throw new Error(`${k} needs a value`); return argv[++i]; };
    if (k === '--repo') a.repo = take();
    else if (k === '--out') a.out = take();
    else if (k === '--unity-pin') a.unityPin = take();
    else if (k === '--json') a.json = true;
    else if (k === '-h' || k === '--help') a.help = true;
    else throw new Error(`unknown argument ${k}`);
  }
  return a;
}

function main() {
  let args;
  try { args = parseArgs(process.argv.slice(2)); } catch (e) {
    process.stderr.write(`${e.message}\n${USAGE}\n`);
    return 1;
  }
  if (args.help) { process.stdout.write(`${USAGE}\n`); return 0; }
  if (!args.repo) { process.stderr.write(`--repo is required\n${USAGE}\n`); return 1; }
  const { runIntake } = require('./lib/run');
  const r = runIntake(args);
  if (args.json) {
    process.stdout.write(`${JSON.stringify({ exitCode: r.exitCode, counts: r.counts, outputs: r.outputs,
      decisions: r.decisions ? { added: r.decisions.added, skipped: r.decisions.skipped } : null,
      issues: r.issues.map(({ key, ...i }) => i) }, null, 2)}\n`);
  } else {
    const c = r.counts || {};
    process.stdout.write(`V57 intake: ${r.exitCode === 2 ? 'BLOCKED' : 'OK'} — blocking ${c.blocking || 0}, conflict ${c.conflict || 0}, fixable ${c.fixable || 0}, missing ${c.missing || 0}\n`);
    r.issues.filter((i) => i.level === 'blocking').forEach((i) => process.stdout.write(`  BLOCKING ${i.code}: ${i.message}\n`));
    process.stdout.write(`Report: ${r.outputs.find((o) => o.endsWith('INTAKE_REPORT.md')) || '(none)'}\n`);
  }
  return r.exitCode;
}

try {
  process.exitCode = main();
} catch (e) {
  process.stderr.write(`v57-intake crashed: ${e && e.stack ? e.stack : e}\n`);
  process.exitCode = 1;
}
