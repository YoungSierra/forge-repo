# TDD mechanic template (spec-ready)

Reference for the `## Mechanic:` blocks inside a provider TDD (TDD Standard 2.x, §B — full template `TDD_Template.md`). V57 only reads these blocks — the TDD is provider-owned and never edited by V57. Run `/tdd-to-spec` per `V57/agents/skills/commands/tdd/unity-tdd-to-spec-skill.md`: one mechanic by name, or `--all` for every mechanic in the TDD.

Aligned with:

- Extraction targets in that skill (player-facing behavior, rules, I/O, dependencies, edge cases).
- `V57/specs/template/feature_spec_template.yaml` field mapping.

---

## Mechanic: <Human-readable name>

### Spec metadata (for YAML header)

- **spec `name` (PascalCase):** <FeatureOrSystemName>
- **spec `type`:** one of `feature` | `system` | `mechanic` (per template: small rule set → `mechanic`; cross-cutting → `system`; player module → `feature`)
- **status:** `active` | `deprecated` (deprecation rules: provider TDD §0.3)
- **One-line `description`:** <what it does and why>
- **Suggested version:** <semver, e.g. 0.1.0>
- **Author / area:** <name or team>
- **Last updated:** <YYYY-MM-DD>

### Player-facing behavior

- **Goal / fantasy:** <what the player is trying to achieve>
- **Loop:** <trigger → actions → feedback → outcome>
- **Feedback by outcome:** <one channel per player-visible outcome — UI id from the TDD §9.1 screen registry (`UI_<Screen>`), diegetic/world visual, SFX, camera/transform. Every outcome the player must read names its channel>
- **Progression / tuning levers:** <what designers balance>

### Rules and constraints

- **Core rules:** <numbered, unambiguous>
- **Limits:** <cooldowns, caps, rates, stacking, exclusivity>
- **Authority:** <who is source of truth: client, server, single system>
- **Multiplayer / determinism:** <if relevant: host-authoritative, server-authoritative, owner-only input, `NetworkVariable`, `ServerRpc`, prediction, sync — see `V57/docs/networking/NETWORKING_VOCABULARY.md`; use `N/A` for single-player>

### Inputs and outputs

- **Player inputs:** <action names from the TDD §11.3 input map, with context (e.g. only in Playing state) — align with project input strategy / `input_system` in CONTEXT.md>
- **System inputs:** <events, data read from other modules>
- **Outputs:** <state changes, events raised (name + payload shape), resources granted/consumed. Each raised event declares its consumers — `consumed by: <§B/§B-S ids | none (reason)>` (TDD gate G-18: no orphan events)>

### Persistence

- <`none` | what survives a session (fields/snapshot), format, save trigger — must match a TDD §11.4 row. Runtime state persists via snapshots of plain C# models, never by mutating ScriptableObject assets>

### Dependencies and integration (`dependencies[]` in spec)

For each integration point, list objects the spec will use. Each line should be convertible to:

`{ kind: feature|system|mechanic, id: "<CONTEXT.md id>", minVersion: "<semver or omit>" }`

Do **not** use legacy forms like `- system: Foo` or bare keys without `kind` + `id`.

| kind     | id (string) | minVersion (optional) | Why needed |
| -------- | ----------- | --------------------- | ---------- |
| system   | ...         | ...                   | ...        |
| feature  | ...         | ...                   | ...        |

- **EventBus / messaging:** <which events published/subscribed; payload shape if known>
- **Reused interfaces:** <existing APIs from CONTEXT.md; no duplicates>

### Preconditions

- Bullet list of **world/game state** required before this mechanic is valid (maps to `preconditions` in YAML).

### State machine (if applicable)

If behavior is stateful, define:

- **States:** <list>
- **Initial state:**
- **Transitions:** from → to, **condition** (testable), **guards** (boolean rules)
- **On enter / on exit** hooks (names only, for `stateMachine` section)

If not stateful, write **N/A** and describe pure request/response flow instead.

### Components (sketch for `components[]`)

For each logical piece, enough detail to fill `name`, `type` (`MonoBehaviour` | `ScriptableObject` | `enum` | …), `responsibilities`, `files[].path` (`.cs` only), and optional `designerAssetSuggestedPath` for SO assets.

> **Config vs runtime state:** `ScriptableObject` components are **read-only design config**; stateful mechanics also declare a **plain C# runtime model** (serializable) that owns mutable state and feeds save snapshots (TDD §11.2 guardrails).

1. **<ComponentName>** (`MonoBehaviour` | other)
   - Responsibilities:
   - Suggested path: `Assets/_Game/Scripts/.../<Name>.cs`
   - **Events** (name, payload type or `null`):

2. **<DataName>** (`ScriptableObject`) — if any
   - **Fields:** name, type, default, range or allowed values
   - **CreateAssetMenu:** menuName, fileName
   - **Suggested `.asset` path:** `Assets/.../*.asset`

3. **<EnumName>** — if any
   - Values:

### Public API contract (`publicAPI`)

- **Methods:** name, parameters, return type, short description (testable).
- **Properties:** name, type, read-only?, description.
- **Events:** name, type signature, when fired.

### Edge cases and fail states

- **Invalid input:** <behavior>
- **Missing dependency / race:** <behavior>
- **Cooldown / resource denial:** <player-visible result>
- **Destruction / scene unload:** <cleanup>

Each bullet should be concrete enough to become a test or guard in the spec.

### Implementation notes (`implementation` + tests + milestones)

- **Performance / budgets:** <if any>
- **Suggested tests (EditMode / PlayMode):** paths optional; **coverage targets** required (method names or behaviors).
- **Milestones:** *(optional — not part of TDD Standard 2.1 §B; execution order is the consumer's decision)* ordered phases with **tasks** (maps to `implementation.milestones`); when absent, the consumer derives them.

### Acceptance criteria (testable)

Each criterion is **tagged** with its verification mode (maps to spec `acceptanceCriteria[].verification`):

- [ ] **AC1 (EditMode):** Criterion 1 (observable)
- [ ] **AC2 (PlayMode):** Criterion 2 — PlayMode criteria must map to a scene in the TDD §13.2 manifest
- [ ] ...

### Open questions / assumptions

- List anything still ambiguous so `/tdd-to-spec` (single or `--all`) can ask before writing YAML.

---

## Field mapping (quick reference)

| Spec / skill need | TDD section |
| ----------------- | ----------- |
| Player-facing behavior, rules | Player-facing behavior, Rules |
| Inputs/outputs | Inputs and outputs |
| Persistence (save coverage) | Persistence + TDD §11.4 |
| `status` | Spec metadata |
| `dependencies` (`kind` + `id`) | Dependencies table (ids resolve to §B or §B-S — no orphans) |
| Edge cases / fail states | Edge cases |
| `preconditions` | Preconditions |
| `stateMachine` | State machine |
| `components`, `.cs` vs `.asset` | Components sketch (config SO vs runtime model) |
| `publicAPI` | Public API contract |
| `implementation` / tests / milestones | Implementation notes + Acceptance |
| `acceptanceCriteria[].verification` | Tagged ACs (EditMode / PlayMode) |
| `networking:` block (optional) | Multiplayer / determinism + NGO patterns doc |
| Session system spec | Use appendix **NetworkSession** when §A multiplayer enabled |

---

## Appendix: NetworkSession system mechanic (multiplayer TDDs)

When TDD §A `multiplayer_model != N/A`, add this **system** mechanic (in addition to gameplay mechanics):

### Spec metadata

- **spec `name`:** NetworkSession
- **spec `type`:** system
- **Template:** `V57/specs/template/network_session_spec_template.yaml`

### Player-facing behavior

- **Goal:** Host or join a private session; see connected players; leave cleanly.
- **Loop:** Menu → host/join → in-session gameplay → leave/disconnect.

### Rules and constraints

- **Core rules:** Max players from §A; host authoritative session state; tier-specific join (LAN IP vs lobby code).
- **Authority:** Server (host counts as server in listen_host topology).
- **Multiplayer / determinism:** Session FSM server-driven; UI reflects replicated session events.

### Acceptance (minimum)

- Host starts session (tier-appropriate).
- Client joins and player spawns.
- Shutdown despawns players and returns to Offline.

After `/tdd-to-spec --all`, run `/network-setup` before implementing this spec.
