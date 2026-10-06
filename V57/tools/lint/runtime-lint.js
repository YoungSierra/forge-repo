#!/usr/bin/env node
'use strict';
// V57 runtime lint (M2 gate + F acceptance-test honesty).
//   node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts [--root Assets/_Game/Tests] [--max-lines 200] [--pretty]
// Output: JSON {root, files_scanned, summary, findings:[{file,line,rule,severity,message}]} on stdout.
// Exit: 0 no errors, 2 errors found, 1 crash/usage.
const fs = require('fs');
const path = require('path');
const { LINE_RULES, FILE_RULES, reflectionSetValue } = require('./rules');

/** Remove // and /* *\/ comments, keep strings and line count. */
function stripComments(src) {
  let out = '';
  let i = 0;
  let mode = 'code';
  while (i < src.length) {
    const c = src[i];
    const n = src[i + 1];
    if (mode === 'code') {
      if (c === '/' && n === '/') { mode = 'line'; i += 2; continue; }
      if (c === '/' && n === '*') { mode = 'block'; i += 2; continue; }
      if (c === '@' && n === '"') { mode = 'verbatim'; out += '@"'; i += 2; continue; }
      if (c === '"') mode = 'string';
      else if (c === "'") mode = 'char';
      out += c; i++; continue;
    }
    if (mode === 'line') { if (c === '\n') { mode = 'code'; out += c; } i++; continue; }
    if (mode === 'block') { if (c === '*' && n === '/') { mode = 'code'; i += 2; continue; } if (c === '\n') out += c; i++; continue; }
    if (mode === 'string' || mode === 'char') {
      out += c;
      if (c === '\\') { out += n || ''; i += 2; continue; }
      if ((mode === 'string' && c === '"') || (mode === 'char' && c === "'") || c === '\n') mode = 'code';
      i++; continue;
    }
    if (mode === 'verbatim') {
      out += c;
      if (c === '"' && n === '"') { out += n; i += 2; continue; }
      if (c === '"') mode = 'code';
      i++;
    }
  }
  return out;
}

function nearestAsmdef(dir, stop) {
  let d = dir;
  for (;;) {
    let hit = null;
    try { hit = fs.readdirSync(d).find((f) => f.endsWith('.asmdef')); } catch (_) { /* unreadable */ }
    if (hit) { try { return JSON.parse(fs.readFileSync(path.join(d, hit), 'utf8')); } catch (_) { return {}; } }
    if (path.resolve(d) === path.resolve(stop) || path.dirname(d) === d) return null;
    d = path.dirname(d);
  }
}

/** runtime | editor | test-edit | test | acceptance | verify */
function contextOf(file, root) {
  const full = file.replace(/\\/g, '/');
  const segs = full.split('/');
  const base = path.basename(file, '.cs');
  const asm = nearestAsmdef(path.dirname(file), root) || {};
  const editorOnly = Array.isArray(asm.includePlatforms) && asm.includePlatforms.length === 1 && asm.includePlatforms[0] === 'Editor';
  const testAsm = (asm.defineConstraints || []).includes('UNITY_INCLUDE_TESTS') || (asm.optionalUnityReferences || []).includes('TestAssemblies')
    || (asm.references || []).some((r) => /TestRunner/.test(r));
  const isTest = testAsm || segs.includes('Tests') || segs.includes('Test') || /Tests?$/.test(base);
  if (isTest) {
    if (editorOnly || segs.includes('EditMode') || segs.includes('Editor')) return 'test-edit';
    const playIdx = segs.indexOf('PlayMode');
    if (playIdx >= 0 && (segs.slice(playIdx + 1).some((s) => /^Acceptance/i.test(s)))) return 'acceptance';
    return 'test';
  }
  if (editorOnly || segs.includes('Editor')) return 'editor';
  if (/(Smoke|Verify|Verification|Probe|Acceptance)|Gate\d+$/.test(base)) return 'verify';
  return 'runtime';
}

function lintFile(file, root, opts) {
  const ctx = contextOf(file, root);
  const raw = fs.readFileSync(file, 'utf8');
  const lines = stripComments(raw).split(/\r?\n/);
  const rel = path.relative(process.cwd(), file).replace(/\\/g, '/');
  const out = [];
  const push = (line, rule, severity, message) => { if (severity) out.push({ file: rel, line, rule, severity, message, context: ctx }); };
  lines.forEach((l, i) => {
    for (const r of LINE_RULES) {
      const s = r.severity(ctx);
      if (s && r.re.test(l)) push(i + 1, r.id, s, r.message);
    }
  });
  const rs = FILE_RULES.reflection;
  reflectionSetValue(lines).forEach((i) => push(i + 1, rs.id, rs.severity(ctx), rs.message));
  const count = raw.split(/\r?\n/).length - (raw.endsWith('\n') ? 1 : 0);
  if (count > opts.maxLines) push(1, 'max-lines', FILE_RULES.maxLines.severity(ctx), `${count} lines > ${opts.maxLines}: split by responsibility`);
  if (opts.varRule) {
    const hits = [];
    lines.forEach((l, i) => { if (FILE_RULES.noVar.re.test(l)) hits.push(i + 1); });
    if (hits.length) push(hits[0], 'no-var', FILE_RULES.noVar.severity(ctx), `\`var\` used ${hits.length}× (V57 standard: explicit types); lines ${hits.slice(0, 12).join(', ')}${hits.length > 12 ? ', …' : ''}`);
  }
  return out;
}

function listCs(dir, acc = []) {
  let ents;
  try { ents = fs.readdirSync(dir, { withFileTypes: true }); } catch (_) { return acc; }
  for (const e of ents) {
    if (['Library', 'Temp', 'obj', 'node_modules', '.git'].includes(e.name)) continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) listCs(p, acc);
    else if (e.name.endsWith('.cs')) acc.push(p);
  }
  return acc.sort();
}

/** @returns {{roots, files_scanned, summary, findings}} */
function lint(roots, opts = {}) {
  const o = { maxLines: 200, varRule: true, ...opts };
  const findings = [];
  let files = 0;
  for (const r of roots) {
    const root = path.resolve(r);
    const list = fs.existsSync(root) && fs.statSync(root).isFile() ? [root] : listCs(root);
    const base = fs.existsSync(root) && fs.statSync(root).isFile() ? path.dirname(root) : root;
    list.forEach((f) => { files++; findings.push(...lintFile(f, base, o)); });
  }
  const errors = findings.filter((f) => f.severity === 'error').length;
  return {
    roots, files_scanned: files,
    summary: { errors, warnings: findings.length - errors, files_with_findings: new Set(findings.map((f) => f.file)).size },
    findings,
  };
}

function main(argv) {
  const roots = [];
  const opts = {};
  let pretty = false;
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--root') roots.push(argv[++i]);
    else if (a === '--max-lines') opts.maxLines = Number(argv[++i]);
    else if (a === '--no-var-rule') opts.varRule = false;
    else if (a === '--pretty') pretty = true;
    else if (a === '-h' || a === '--help') { process.stdout.write('Usage: runtime-lint.js --root <dir> [--root <dir>] [--max-lines 200] [--no-var-rule] [--pretty]\n'); return 0; }
    else { process.stderr.write(`unknown argument ${a}\n`); return 1; }
  }
  if (!roots.length) {
    ['Assets/_Game/Scripts', 'Assets/_Game/Tests'].filter((d) => fs.existsSync(d)).forEach((d) => roots.push(d));
    if (!roots.length) { process.stderr.write('no --root given and Assets/_Game/{Scripts,Tests} not found\n'); return 1; }
  }
  const res = lint(roots, opts);
  process.stdout.write(`${JSON.stringify(res, null, pretty ? 2 : 0)}\n`);
  return res.summary.errors ? 2 : 0;
}

if (require.main === module) {
  try { process.exitCode = main(process.argv.slice(2)); } catch (e) {
    process.stderr.write(`runtime-lint crashed: ${e && e.stack ? e.stack : e}\n`);
    process.exitCode = 1;
  }
}

module.exports = { lint, lintFile, contextOf, stripComments };
