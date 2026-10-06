# v57-intake — stage I0 INTAKE

Reads the provider delivery (brief §1) and produces the V57 generated-file contract (brief §2).
No Unity needed. Node 18+, CommonJS, one runtime dependency (`yaml`).

```bash
cd V57/tools/intake && npm install          # add --no-bin-links on filesystems without symlinks
node V57/tools/intake/index.js --repo .     # from the provider repo root
node V57/tools/intake/index.js --repo <path> [--out Docs/Generated] [--json] [--unity-pin 6000.6.2f1]
```

| Flag | Meaning |
|---|---|
| `--repo` | provider repo root (required) |
| `--out` | output dir for generated files, relative to the repo (default `Docs/Generated`) |
| `--json` | print `{exitCode, counts, outputs, decisions, issues[]}` to stdout |
| `--unity-pin` | Unity version V57 pins. Default: `$V57_UNITY_PIN`, then `V57/templates/ProjectSettings/ProjectVersion.txt`, then `6000.6.2f1` |

**Exit codes:** `0` = continue (non-blocking issues may exist), `2` = blocking issue (A0 gate: stop the run), `1` = crash or bad usage.

## Inputs

| Input | Parsing |
|---|---|
| `Docs/Design/TDD.md` (TDD Standard 2.0.0) | Keyed by section number/title: §A fenced yaml, §0.1/§0.2 tables, §3 core-loop table, `## Mechanic: X` blocks (metadata, player inputs, levers, dependencies, ACs), §B-S table, §C fenced yaml per mechanic (`# ── C-NN Name ──`; invalid yaml is auto-quoted and retried), §8 palette prose, §9.1, §11.1, §11.3, §11.5, §11.6, §13.1, §13.2, §14.2 tables. If the file has another name in `Docs/Design/`, intake uses it and logs a `fixable`. |
| `Docs/ArtDirection/ArtDirectionDocument.md` | `## key` sections as loose pseudo-YAML bullets (`- key: value␠␠`, `- **key:** value`, nested indented blocks) **or** fenced ```` ```yaml ```` blocks. Also handles standalone screens that are only named in prose. |
| `Assets/_Game/**`, `Docs/**` on disk | Structure (allowed roots), forbidden files, naming prefixes/suffixes per folder, brief-to-file cross references, orphans. |

A missing section never crashes the run: it is reported as an issue and the dependent data gets defaults.

## Outputs

- `Docs/Generated/<name>.yaml` plus `Docs/Generated/json/<name>.json` for `package, asset_manifest, entities, mechanics, ui, scenes, input_map, camera, rendering, audio, tuning, acceptance`. Top-level keys follow brief §2 exactly. The JSON Schemas are in `V57/tools/schema/<name>.schema.json`.
- `Docs/Generated/localization.csv` (`key,en`): ADD `text_keys` plus text-valued TDD levers.
- `Docs/V57/INTAKE_REPORT.md`: verdict, per-level counts, completeness per TDD section / ADD section / asset module, and issue tables by level with the suggested auto-fix or placeholder. See `examples/INTAKE_REPORT.example.md` (HH TDD + Cartón ADD fixture).
- `Docs/V57/DECISIONS.md`: every `fixable` and `conflict` issue is appended as a `D-###` entry with a hidden `v57-intake-key`. Re-runs never duplicate entries, and numbering continues after any manual D-entries.

Files are rewritten only when their content changes. Generated data has no timestamps, so re-runs give identical diffs.

## Issue levels

| Level | Examples | Effect |
|---|---|---|
| `blocking` | no TDD, TDD unrecognizable, no asset_briefs **and** no art on disk, no slice scene can be determined, repo has neither `Docs/` nor `Assets/_Game/` | exit 2 |
| `fixable` | id typos (`did you mean`), case mismatches, VG-/AC- id prefixes, naming deviations with a deterministic canonical name (file kept as-is, mapped via asset_manifest; V57 never renames provider files), `.meta`/temp/UUID files to discard, slice scene inferred, §C yaml repaired | logged to DECISIONS; provider files untouched |
| `conflict` | ADD ids unknown to the TDD, ADD for another game, palette disagreement, files outside the contract tree | TDD > ADD > disk; logged to DECISIONS |
| `missing` | brief without V57 fields or files, reference image or mockup missing, slice scene without `BLK_`, `[DRAFT]`/`[PENDING]` markers, no audio, no gold path (`acceptance.gold_path` gets a `status: draft` derived from TDD §3; M1 authors `Docs/V57/gold_path.json` from it) | placeholder generated, run continues |

## Tests

```bash
npm test          # node --test test/*.test.js
```

The fixtures are built programmatically in a temp dir from `test/fixtures/` (provider-repo-template, the Happy Habitat TDD and the Cartón ADD). Every generated JSON file is validated against `V57/tools/schema` with ajv, which is a dev dependency.

## Related: runtime lint

`node V57/tools/lint/runtime-lint.js --root Assets/_Game/Scripts --root Assets/_Game/Tests` writes JSON findings `{file,line,rule,severity,message}` and exits with 2 on errors. The rules are in `V57/tools/lint/rules.js`.
