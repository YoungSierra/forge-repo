#!/usr/bin/env node
'use strict';
// V57 delivery import: places a provider delivery that arrived as a loose folder into the repo's provider areas.
//   node delivery.js --from <folder> --repo <path> [--new-root Assets/_Game/Art/<Category>] [--apply] [--json]
// Dry run by default. Never deletes or renames repo files. Run the intake afterwards.

const { planDelivery, applyDelivery, renderPlan } = require('./lib/delivery');

const USAGE = `Usage: node V57/tools/intake/delivery.js --from <folder> --repo <path> [--new-root <dir>] [--apply] [--json]

  --from      delivery folder (as received)
  --repo      game repository root
  --new-root  repo folder for assets not in the repo yet (default Assets/_Game/Art/Delivered)
  --apply     copy the files (default: dry run, print the plan)
  --json      print the plan as JSON`;

function parseArgs(argv) {
  const a = { apply: false, json: false };
  for (let i = 0; i < argv.length; i++) {
    const k = argv[i];
    const take = () => { if (i + 1 >= argv.length) throw new Error(`${k} needs a value`); return argv[++i]; };
    if (k === '--from') a.from = take();
    else if (k === '--repo') a.repo = take();
    else if (k === '--new-root') a.newRoot = take();
    else if (k === '--apply') a.apply = true;
    else if (k === '--json') a.json = true;
    else if (k === '-h' || k === '--help') a.help = true;
    else throw new Error(`unknown argument ${k}`);
  }
  return a;
}

try {
  const args = parseArgs(process.argv.slice(2));
  if (args.help) { process.stdout.write(`${USAGE}\n`); process.exit(0); }
  if (!args.from || !args.repo) throw new Error('--from and --repo are required');
  const plan = planDelivery(args);
  if (args.apply) applyDelivery(plan, args);
  process.stdout.write(args.json ? `${JSON.stringify(plan, null, 2)}\n` : `${renderPlan(plan, args.apply)}\n`);
} catch (e) {
  process.stderr.write(`${e.message}\n${USAGE}\n`);
  process.exitCode = 1;
}
