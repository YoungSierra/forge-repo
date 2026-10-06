# Unity Save Meta Skill (`/save-meta`)

Persistent save slots / meta progression per TDD `save_model`. New-game, save, load round-trip for the slice. N/A when `save_model: N/A`.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **Task subagent** | When G-SAVE applies |

---

## Invocation

```bash
/save-meta
/save-meta --spec @V57/specs/<slug>/systems/save_system.yaml
/save-meta verify
```

---

## Workflow

1. Read `save_model` from CONTEXT/TDD (e.g. local JSON, PlayerPrefs, file slots)
2. Implement or wire SaveService per spec (code under `Assets/_Game/Scripts/<Area>/`, one asmdef per area)
3. Define serializable meta DTO (version field mandatory)
4. New Game → default meta; Save → Load → assert equality for slice fields
5. Corrupt/missing file → safe fallback (do not crash)
6. Verify round-trip in Play Mode (evidence for `/playability-cert` and `/qa` GQ-10 at M4 — always enabled)
7. Never store secrets in plaintext beyond what TDD allows

### Report

`Docs/V57/reports/M2-save-meta-{slug}.md`

---

## Integration

Pipeline stage **M2** (`/vertical-slice`, `/game-setup`) when save is in run scope. Coordinate with BankedCurrency / meta mechanics in specs. If not required: **silent skip** — no report, no progress line.

---

*Last updated: 2026-09-28*
