# Game TDD — Template (Standard)

> **What this is.** The complete, **self-contained** Technical Design Document (TDD) standard for a single game: strategic vision (§1–14) plus the production-ready definition — **§A** Project Identity, **§B** per-mechanic engineering blocks, **§B-S** support systems registry, **§C** companion spec YAML, **§D** dependency graph.
>
> **Tool-agnostic.** Producible and consumable by any person, team, model, or tool. Any producer that fills every slot and passes the **§0.2 Completeness Gate** emits a valid TDD.
>
> **Systems, not layouts.** The TDD defines **systems, rules and contracts** — what every level must satisfy, never what a particular level contains. Level layouts, placements and routes are **level deliveries** (e.g. `Docs/Design/LevelMaps/<LevelId>/`) validated against the TDD's rules by the consuming pipeline. A new or redesigned map never requires a TDD amendment unless it needs a **new rule** (§0.0 rule 5, gate G-19).
>
> **One external reference.** Narrative or extra content lives in the **GDD**, referenced via `narrativeRef:` — the TDD holds the systems that consume it.
>
> **Living.** After development starts, changes enter through this document first (**§0.3**) and every amended version re-passes the gate.

---

## 0.0 · Fill-policy legend

Every incomplete slot carries one marker so producers and reviewers know who fills it and whether it blocks completeness.

| Marker | Meaning | Resolved by |
|---|---|---|
| `[REQUIRED]` | Must hold a concrete value for the gate to pass; **blocker** while placeholder | Document author / producer |
| `[RECOMMENDED]` | Strongly advised; the gate reports a warning, never a failure | Document author / producer |
| `[TO-FILL:eng]` | Late-bound engineering detail (exact file paths, signatures) resolved during implementation; never allowed on a `[REQUIRED]` field | Implementation team |
| `[PENDING owner=<who> resolve-by=<milestone>]` | Slot awaiting content while the document is being assembled or amended. **Owner and resolve-by are mandatory.** A final TDD contains **zero** `[PENDING]` markers (gate G-07) | The named owner |

**Rules**

1. A `[REQUIRED]` field left as a placeholder marks the TDD **incomplete for production**.
2. Conditional concerns (save, multiplayer, …) are declared with an **explicit `N/A`** — absence is never a valid "not applicable".
3. Every `[PENDING]` marker must also appear as a row in the **§14.2 Pending registry**.
4. Any other bracketed marker from older documents or producer tools is treated as `[PENDING]` without owner — i.e. a gate failure until converted.
5. **Scope boundary** *(gate G-19)*. The TDD never contains:
   - **Level content** — coordinates, per-level placement counts ("Zone 1 has 6 flies"), specific routes, measured distances or gaps of a particular map, object lists of a layout. Express level-dependent quantities as **rules** (`zone total = collectibles placed in the zone`) or **bounds** (`authored gap ≤ 3.2 m`, `1 ≤ enemies per zone ≤ 4`).
   - **Execution scope** — slice/milestone flags, "implement first" lists, roadmaps, staffing or schedules (decided by the consuming tool or team).
   - **Storefront data** — regional price matrices, live comparables, store copy.
   - **Asset production detail** — per-asset briefs, texture sizes per asset, file lists (the art delivery and the ADD own them). Content **categories** with first-pass counts stay in §13.1.

## 0.1 · Document Control

| Field | Value |
|---|---|
| **Game title** | `{{project.name}}` |
| **Studio** | `{{project.studio_name}}` |
| **Document** | Game Technical Design Document (TDD) |
| **Template standard** | `TDD Standard 2.1.0` |
| **Document version** | `{{semver}}` — versioning rules in §0.3 |
| **Date** | `{{date}}` |
| **Phase reached** | `<Ideation | Concept | Production>` |
| **Intended use** | Production source of truth (design + engineering) |
| **Owner** | `{{project.owner}}` |

### Changelog  `[REQUIRED]`

One row per document version from `1.0.0` onward (see §0.3).

| Version | Date | Change summary | Sections touched | Author |
|---|---|---|---|---|
| `1.0.0` | `<date>` | Initial gate-passing version | all | `<who>` |

## 0.2 · Completeness Gate  `[REQUIRED]`

The TDD is **complete** when every item below passes. Each item is objective and machine-checkable; a reviewer or tool fills Status = `PASS` / `FAIL` / `WARN`. **A production-ready TDD has all items PASS**, and every mechanic is held to the same production bar.

The TDD carries **no execution-scope metadata**: it describes the complete game. What to prototype or implement first is a decision of the consuming tool or team — never of this document.

| # | Item | Check | Status |
|---|---|---|---|
| **G-01** | §A required fields | Every `[REQUIRED]` key in §A holds a concrete value; conditional keys (`save_model`, `multiplayer_model`) are explicit (`N/A` allowed, absence not) | ☐ |
| **G-02** | Engine pin | §A `engine` is an **exact, reproducible version** (e.g. `Unity 6000.0.32f1`), and `render_pipeline` + `dimension` are declared | ☐ |
| **G-03** | Mechanic bar | Every §B mechanic has: quantified rules (concrete numbers), inputs/outputs, `dependencies[]`, and a state machine or explicit `N/A` | ☐ |
| **G-04** | Acceptance criteria | Every §B mechanic has ≥ 1 testable criterion tagged `EditMode` or `PlayMode` | ☐ |
| **G-05** | §C parity | Exactly one §C spec YAML per §B mechanic; `name` fields match 1:1 | ☐ |
| **G-06** | No orphans | Every id referenced in §D or in any `dependencies[]` resolves to a §B mechanic or a §B-S entry | ☐ |
| **G-07** | Zero pending | No `[PENDING]` (or legacy/producer-specific) markers remain anywhere in the document; §14.2 registry is empty | ☐ |
| **G-08** | Consistency ledger | Every invariant in §14.3 has Status = `PASS` (no `FLAG` remains) | ☐ |
| **G-09** | Persistence coverage | Every §B mechanic whose **Persistence** declaration ≠ `none` has a row in §11.4, and §11.4 is consistent with §A `save_model` | ☐ |
| **G-10** | Input coverage | Every player input named in a §B block maps to an action in the §11.3 input table | ☐ |
| **G-11** | UI coverage | Every `UI_*` id referenced in §B feedback exists in the §9.1 screen registry, and every §9.1 screen is consumed by ≥ 1 mechanic or marked `standalone` | ☐ |
| **G-12** | Scene coverage | Every `SCN_*` id referenced anywhere, and every **PlayMode** acceptance criterion, maps to a scene in the §13.2 manifest | ☐ |
| **G-13** | Performance budgets | §11.6 sets fps / frame-time targets for every platform listed in §A `target_platform` | ☐ |
| **G-14** | Player agency & locomotion | §11.5 declares **Control mode** (`player-driven | click-to-move | auto | none` — `auto`/`none` justified) and **In-play camera** (owner + who controls it); when the mode is player-driven or click-to-move, a Move (or equivalent) action exists in §11.3 with a §B consumer, and a Look action exists **only if** the player controls the camera (an authored camera declares `Look: none (authored camera)`) | ☐ |
| **G-15** | Core loop traceability | Every §3 core-loop step maps to ≥ 1 §B mechanic, its triggering input (§11.3) or system event, and a feedback channel declared in that mechanic's block | ☐ |
| **G-16** | Play space & bootstrap | Every gameplay scene in §13.2 names a **world owner** (§B/§B-S id that owns the play space at runtime: binds the player spawn, initializes systems in order, owns transitions); the entry scene declares session bootstrap (player spawn + session/run start) | ☐ |
| **G-17** | Content inventory | Every externally referenced data asset (`narrativeRef:` targets, tuning tables, string files) has a §13.1 row with a count and an owner | ☐ |
| **G-18** | Event graph closure | Every event published in a §B block declares ≥ 1 resolvable consumer (or explicit `none (reason)`), every subscribed event has a publisher, and §D reflects the closed graph | ☐ |
| **G-19** | Scope boundary | No section contains level content, execution scope, storefront data or asset production detail (§0.0 rule 5): no §B rule, acceptance criterion or §14.3 invariant depends on a specific map's placements; level-dependent quantities are rules or bounds | ☐ |

> Failing the gate does not mean discarding the document — each failed item names the **section** to complete.

## 0.3 · Living TDD — Change Management  `[REQUIRED]`

The TDD is a **versioned living contract**, not a one-shot output. After development starts, design changes enter **through this document first** — never through code.

### Amendment workflow (add / change / remove a mechanic)

1. **Edit the TDD**: add or edit the §B block (and §B-S entries), its §C spec, and §D edges. Bump versions (rules below). Register in-flight work as `[PENDING owner resolve-by]` while drafting.
2. **Re-run the §0.2 gate** on the whole document. The delta-sensitive items catch integration regressions: G-06 (new orphan references), G-08 (broken shared invariants), G-09..G-13 (coverage), G-14..G-19 (playability, scope).
3. **Sync downstream documents** derived from the TDD (architecture/context docs, specs).
4. **Regenerate only the affected spec(s)** — update in place, diffing against the previous version.
5. **Implement** against the updated spec, tests included.
6. **Monitor drift** (docs ↔ code) periodically; any divergence returns to step 1.

A **new level delivery** (map, layout, placement change) is **not** an amendment: the consumer validates it against §6 level rules. Only a new rule or a changed bound amends the TDD.

### Versioning rules (chained)

| Change | Mechanic `version` | §C spec | Document version |
|---|---|---|---|
| New mechanic | starts `0.1.0` | new spec file | **minor** bump |
| Rule / number change | patch or minor | update in place | patch or minor |
| Breaking behavior or API change | **major** | **major** | minor or major |
| Mechanic removal | → `status: deprecated` first | keep until deleted | minor |

Every amendment adds a **§0.1 changelog row** and re-certifies the gate. Gate certification is **per document version** — never one-shot.

### Deprecation policy

- A mechanic being removed is first marked **`status: deprecated`** in its §B metadata; the block stays in place.
- While any §D edge or `dependencies[]` entry still references it, it **cannot be deleted** (G-06 enforces this).
- Delete the block and its §C spec only when nothing references it; record the deletion in the changelog.

---

# 1 · High Concept  `[REQUIRED]`

- **One-liner.** `<single-sentence hook>`
- **Elevator pitch.** `<2–4 sentences: genre, core verb, what the player does>`
- **Core fantasy.** `<the single fantasy the game delivers>`
- **Pillars.** `<3–4 design pillars that every decision defends>`

---

# 2 · Game Overview  `[REQUIRED]`

| Attribute | Value |
|---|---|
| **Genre / sub-genre** | `<genre>` |
| **Setting** | `<setting>` |
| **Primary platform** | `<PC | Console | Mobile | …>` |
| **Target audience** | `<who, age band, taste>` |
| **Price / model** | `<premium / F2P / …>` |

- **USP.** `<what makes this defensibly different>`
- **Positioning.** `<"For <audience> who <need>, <game> is a <category> that <differentiator>.">`

---

# 3 · Core Gameplay  `[REQUIRED]`  *(gate G-15)*

- **Core verbs.** `<verb · verb · verb>`
- **Core loop.** `<OBSERVE → ACT → FEEDBACK → OUTCOME>`
- **Win / lose conditions.** `<win = …; lose = …>` — stated as rules that hold on any level (e.g. "all collectibles placed in the zone"), never as one map's numbers.

Every core-loop step must be traceable to a §B mechanic, the input or event that triggers it, and a declared feedback channel (gate G-15).

---

# 4 · Mechanics & Systems (strategic summary)  `[REQUIRED]`

> The **engineering-ready** definition of every mechanic lives in **§B** (and its spec YAML in **§C**). This section is the human-readable overview only — one bullet per mechanic.

- **`<MechanicName>`** *(core | system | feature)* — `<one-line purpose>`
- `<… repeat per mechanic …>`

---

# 5 · Game Modes  `[RECOMMENDED]`

- **`<ModeName>`** — `<fantasy / framing>`
- `<… repeat per mode …>`

---

# 6 · World Structure & Level Rules  `[RECOMMENDED]`

> **Rules, not layouts** (§0.0 rule 5, G-19). This section says how the world is organized and which constraints **every** level must satisfy. Concrete maps are level deliveries validated against these rules by the consumer.

- **Structure.** `<how the world is split (levels → zones → …), how play spaces connect, and their order — no geometry, no placements>`
- **Level rules.** `<constraints any delivered level must satisfy, each derived from a §B value — e.g. "collectible spacing ≥ 2 × trigger radius", "authored gap ≤ 50 % of max flat jump distance", "one key anchor and one exit door per zone">`
- **Set-piece types.** `<reusable gameplay/presentation patterns a level may contain and their rules — e.g. "background creature transit: non-interactive, behind glass, no collision">`
- **Progression.** `<how content unlocks>`

| Rule id | Rule (bound or derivation) | Derived from (§B id + value) | Checked by |
|---|---|---|---|
| `LR-01` | `<e.g. min spacing between collectibles ≥ 1.2 m>` | `<CollectibleMechanic triggerRadius 0.6 m × 2>` | `<consumer level validation>` |

- **Level contract.** What a level delivery must contain for the systems to bind it — **types and naming only, never positions or counts of a specific level**. Spatial games list the markers and placed actor types; non-spatial games (match-3 boards, waves, puzzles) list the fields of their level data and the bounds of each field.

| Element | Delivered as | Bound by (§B / §B-S id) |
|---|---|---|
| `<e.g. player spawn>` | `<Marker_Spawn_Player — point>` | `<SceneFlow>` |
| `<e.g. zone volume>` | `<Marker_Zone_<ZoneId> — box>` | `<SceneFlow>` |
| `<e.g. board (non-spatial)>` | `<LevelData data.board: int[rows][cols], 5 ≤ rows,cols ≤ 9>` | `<BoardSystem>` |

> Delivery formats (consumer-defined): a level FBX with `Marker_*` nodes, a DCC layout export with `Marker_*` objects, or a `level_data/1.x` JSON per level. The TDD names the elements; the delivery places them.

---

# 7 · Narrative & Characters  `[RECOMMENDED]`

- **Tone.** `<tone / themes>`
- **Protagonist.** `<who the player is / controls>`
- **Key archetypes.** `<recurring character/NPC roles>`

> **Narrative or extra content** (dialogue lines, quest scripts, lore, world depth) lives in the **GDD** and its data documents — never here. §B mechanics that consume it point to it with `narrativeRef:`; every referenced asset gets a §13.1 inventory row (gate G-17).

---

# 8 · Art Direction & Visual Style  `[REQUIRED]`

- **Style.** `<rendering style, palette, references>`
- **Readability.** `<what game state must read at a glance and how>`
- **Scope coherence.** `<why this style is achievable within the team's production timeline — the style is the primary scope lever>`

> Per-asset briefs, texture sizes and file lists belong to the art delivery / ADD (§0.0 rule 5).

---

# 9 · UI / UX  `[REQUIRED]`

- **Principle.** `<the #1 usability requirement>`
- **Accessibility.** `[RECOMMENDED]` `<input remapping, subtitles, colorblind/contrast, text scaling — or justified N/A>`

## 9.1 Screen registry  *(gate G-11)*

One row per screen. Ids follow **`UI_<Screen>`** (PascalCase) and are the **only** way §B mechanics reference UI. Every screen is consumed by at least one mechanic or explicitly marked `standalone`.

| Screen id | Purpose | Key states | Consumed by (§B / §B-S ids) |
|---|---|---|---|
| `UI_<Screen>` | `<what it shows / decides>` | `<Default | Active | Hidden | …>` | `<MechanicName>` or `standalone` |

---

# 10 · Audio Direction  `[RECOMMENDED]`

- `<music direction, diegetic vs non-diegetic, key stingers/cues>`
- **Middleware.** `<engine built-in | FMOD | Wwise | …>` — justify anything beyond built-in with the concrete feature that requires it.

---

# 11 · Technical Design  `[REQUIRED]`

## 11.1 Engine & rendering

| Area | Decision |
|---|---|
| **Engine** | `<exact pinned version — feeds §A engine>` |
| **Render pipeline** | `<the lightest pipeline that meets §8's visual targets — feeds §A render_pipeline>` |
| **Dimension** | `<2D | 2.5D | 3D | isometric-2D | isometric-3D — feeds §A dimension; drives §11.5>` |
| **Architecture** | `<component-based / data-driven / ECS; messaging model — feeds §A pattern>` |
| **AI** | `<approach, or N/A>` |

> **Coherence rule:** rendering features (deferred paths, upscalers, post stacks) must be justified by §8 visual targets and §11.6 budgets — never included "because available".

## 11.2 Data ownership (engine guardrails)

| Data class | Container | Runtime mutability | Persisted? |
|---|---|---|---|
| Design config (tuning values, tables) | Data asset (e.g. ScriptableObject) | **Read-only at runtime** | No — ships with the build |
| Runtime simulation state | Plain code objects (serializable models) | Yes | Via save snapshot (§11.4) |
| Save data | Snapshot (JSON / binary) of runtime models | — | Yes |

> **Rule:** runtime state is never persisted by mutating design-config assets. Config in, state out. **Exactly one owner per datum** — the §B component or §B-S id named here is the only writer.

## 11.3 Input map  *(gates G-10, G-14)*

- **Input system.** `<old | new | hybrid — feeds §A input_system>`

| Action map | Action | Suggested binding | Consumed by (§B / §B-S id) |
|---|---|---|---|
| `<Gameplay>` | `<Move>` | `<WASD / left stick>` | `<MechanicName>` |

Every player input named in a §B block appears here — this table is the source for the project's input assets. When §11.5 **Control mode** is player-driven or click-to-move, the Move (or equivalent) action must appear here with a consumer; Look appears only when the player controls the camera.

## 11.4 Persistence spec  *(gate G-09)*

- **Save model.** `<N/A | summary — feeds §A save_model>`. If `N/A`, state **"no persistence — session-only"** and this section is complete.

| System / mechanic | What persists | Format | Save trigger | Versioning / migration |
|---|---|---|---|---|
| `<MechanicName>` | `<fields / snapshot>` | `<JSON | binary>` | `<auto on day-end | manual | checkpoint>` | `<schema version note>` |

## 11.5 Movement & spatial model  *(gate G-14)*

Decisions must be coherent with `dimension` (§11.1). Locomotion is a first-class contract: a navigation technology (e.g. NavMesh) is **not** a control mode — declare **who drives the character and how**.

| Topic | Decision |
|---|---|
| **Space** | `<tilemap grid | 3D navmesh | 2D physics | 3D physics | none>` |
| **Pathfinding** | `<A* on grid | NavMesh | none>` |
| **Control mode** | `[REQUIRED]` `<player-driven (WASD/stick) | click-to-move | auto (AI/rail) | none — auto/none require justification>` |
| **In-play camera** | `[REQUIRED]` `<owner (§B/§B-S id) + behavior during play: follow / fixed / rail / orbit; who controls Look (player | none — authored)>` |
| **Depth / sorting** | `<Y-sort | sorting layers | z-buffer>` |

## 11.6 Performance budgets  *(gate G-13)*

One row per platform in §A `target_platform`.

| Platform | Resolution | FPS target | Frame budget (ms) | Memory ceiling | Notes |
|---|---|---|---|---|---|
| `<platform>` | `<1080p>` | `<60>` | `<16.6>` | `<N GB>` | `<upscaler, caps>` |

## 11.7 Multiplayer

- **Model.** `<N/A | model — feeds §A multiplayer_model>`.
- When **not** `N/A`: declare `networking_tier`, `max_players`, `session_visibility`, and the authority model — and a **`NetworkSession`** system mechanic **must** exist in §B.

---

# 12 · Business Model  `[RECOMMENDED]`

- `<premium / F2P / live-service; price band; monetization stance>` — stance and constraints on design only (e.g. "no purchase affects any §B value"). Regional price matrices and storefront data live outside the TDD (§0.0 rule 5).

---

# 13 · Content Scope & Scene Manifest  `[REQUIRED]`

## 13.1 Content scope & inventory (quantified)  *(gate G-17)*

One row per category of authored content the game consumes — **including every externally referenced data asset** (`narrativeRef:` targets, tuning tables, localized string files). Absence of a row for a referenced asset is a gate failure. Counts are **first-pass production counts per category**, never per-level placements; character and prop ids match the **art delivery asset ids**.

| Category | First-pass count | Owner | Notes |
|---|---|---|---|
| `<Levels / districts>` | `<count>` | `<who>` | |
| `<Characters / NPCs>` | `<count + ids>` | `<who>` | |
| `<UI screens>` | `<count — must equal §9.1 rows>` | `<who>` | |
| `<Audio tracks / SFX>` | `<count>` | `<who>` | |
| `<Narrative data assets>` | `<count of narrativeRef targets>` | `<who>` | `<content lives in the GDD>` |
| `<Localized strings>` | `<languages plan, e.g. EN + …>` | `<who>` | `[RECOMMENDED]` strings externalized from day one |

## 13.2 Scene manifest  *(gates G-12, G-16)*

One row per scene. Ids follow **`SCN_<Location>_<Context>`**. Every **gameplay** scene names a **world owner** — the §B/§B-S id that owns the play space at runtime (binds the player spawn, initializes systems in order, owns transitions); its layout comes from the level delivery. The entry scene declares the session bootstrap (player spawn + session/run start).

| Scene id | Purpose | World owner (§B / §B-S id) | Systems present (§B / §B-S ids) | PlayMode ACs covered |
|---|---|---|---|---|
| `SCN_<Location>_<Context>` | `<gameplay | menu | test>` | `<id — owns the play space>` | `<ids>` | `<AC ids>` |

> **PlayMode acceptance criteria run in test scenes** with synthetic geometry built for the mechanic — never in a delivered level, so a new map cannot break them.

---

# 14 · Risks, Open Items & Consistency  `[REQUIRED]`

## 14.1 Risks

| Risk | Severity | Mitigation / guard |
|---|---|---|
| `<risk>` | `<🔴 / 🟠 / 🟡>` | `<why it matters; mitigation>` |

## 14.2 Pending registry  *(gate G-07)*

Consolidates every `[PENDING]` marker in the document. **Empty in a final TDD.**

| Location (section) | What is pending | Owner | Resolve-by | Status |
|---|---|---|---|---|
| `<§C-05 rules.max_daily>` | `<value to lock>` | `<who>` | `<milestone>` | `<open | resolved>` |

## 14.3 Consistency ledger  *(gate G-08)*

Declared cross-system invariants — numeric or structural facts that must hold **across** §B blocks (economy caps, rate sums, timing budgets, count limits). Invariants relate **rules to rules** (e.g. "stun duration = knockback duration"); checking a delivered map against §6 level rules is the consumer's level validation, never a ledger row. Any amendment touching the involved systems re-checks the affected rows.

| Id | Invariant (statement with concrete numbers) | Systems involved | Status | Owner |
|---|---|---|---|---|
| `INV-01` | `<e.g. max achievable daily income ($X) ≤ economy cap ($Y)>` | `<§B ids>` | `PASS | FLAG` | `<who>` |

---

---

# §A · Project Identity  `[REQUIRED]`

The engineering identity of the game. Production tooling reads this block to bootstrap the project. Conditional keys are **explicit** — `N/A` is a value, absence is a gate failure (G-01).

```yaml
project_name: "{{project.name}}"          # [REQUIRED]
document_version: "1.0.0"                 # [REQUIRED] semver; matches §0.1
repo_kind: unity_game
engine: "<Unity 6000.0.32f1>"             # [REQUIRED] exact reproducible pin (G-02)
render_pipeline: "<URP 17.x | HDRP | Built-in | URP 2D Renderer>"   # [REQUIRED]
dimension: "<2D | 2.5D | 3D | isometric-2D | isometric-3D>"         # [REQUIRED] drives §11.5
language: "<per engine scripting profile>"   # [RECOMMENDED] do not overstate the runtime
pattern: "<architecture summary, e.g. component-based + EventBus>"  # [REQUIRED]
target_platform: "<PC (Windows) | Console | Mobile>"                # [REQUIRED]
input_system: "<old | new | hybrid>"      # [REQUIRED]
test_assembly_prefix: "<PascalCasePrefix>" # [REQUIRED] used for test assemblies
genre: "<genre / sub-genre>"
save_model: "N/A"                          # [REQUIRED] N/A or summary — detail in §11.4
multiplayer_model: "N/A"                   # [REQUIRED] N/A | ngo_listen_host_lan | ngo_listen_host_relay | ngo_dedicated_server — see NETWORKING_VOCABULARY.md
networking_tier: null                      # lan | relay | dedicated — must agree with multiplayer_model
max_players: null                          # required when multiplayer_model != N/A
session_visibility: null                   # lan_only | private | public — required when multiplayer_model != N/A
performance_targets:                       # [REQUIRED] summary; detail in §11.6 (G-13)
  - platform: "<platform>"
    resolution: "<1080p>"
    fps_target: 60
```

---

# §B · Production Mechanics  `[REQUIRED]`

> One `## Mechanic:` block per mechanic — the heading delimits blocks; keep it. **Every mechanic meets the same production bar** (gates G-03/G-04). Each §B mechanic must have a matching §C spec YAML block (G-05). The skeleton below shows every field; **repeat it per mechanic**.

## Mechanic: <MechanicName>

### Spec metadata
- **name (PascalCase):** `<FeatureOrSystemName>`
- **type:** `<feature | system | mechanic>`
- **status:** `<active | deprecated>` — deprecation rules in §0.3
- **version:** `<semver, e.g. 0.1.0>`
- **One-line description:** `<what it does and why>`
- **narrativeRef:** *(optional)* `<gdd-or-data-doc.md#anchor>` — narrative/extra content lives in the GDD (§7); referenced assets need a §13.1 row (G-17)

### Player-facing behavior  `[REQUIRED]`
- **Goal / fantasy:** `<what the player is trying to achieve>`
- **Loop:** `<trigger → actions → feedback → outcome>`
- **Feedback by outcome:** `<one channel per player-visible outcome — UI id (§9.1), diegetic/world, audio, camera/transform, animation state>` *(G-11, G-15)*
- **Progression / tuning levers:** `<the parameters designers balance>`

### Rules and constraints  `[REQUIRED]` (quantified)
1. `<numbered, unambiguous rule with concrete numbers>`
2. `<…>`
- **Limits:** `<cooldowns, caps, rates, stacking, exclusivity>`
- **Authority:** `<client | server | single system>`
- **Multiplayer / determinism:** `<N/A | host, prediction, sync>`

> Level-dependent quantities are rules or bounds (`total = instances placed in the zone`, `range ≤ 4.0 m one-way`), never one map's values (G-19).

### Inputs and outputs  `[REQUIRED]`
- **Player inputs:** `<action names from §11.3, with context>` *(G-10)*
- **System inputs:** `<events / data read from other modules>`
- **Outputs:** `<state changes; events raised (name + payload shape), each with declared consumers — consumed by: <§B/§B-S ids | none (reason)>>` *(G-18)*

### Persistence  `[REQUIRED]`
- `<none | what survives a session — must have a §11.4 row>` *(G-09)*

### Dependencies and integration  `[REQUIRED]`
| kind | id | minVersion | Why needed |
|---|---|---|---|
| `<feature | system | mechanic>` | `<PascalCase id — must resolve to §B or §B-S (G-06)>` | `<semver or ->` | `<why>` |

- **Messaging:** `<events published/subscribed; payload shape>`

### Preconditions
- `<world/game state required before this mechanic is valid>`

### State machine  `[REQUIRED]` *(or explicit `N/A` for stateless request/response)*
- **States:** `<list>`
- **Initial:** `<state>`
- **Transitions:** `<from → to, condition (testable), guard>`

### Components (sketch)
Separate **config** (data asset, read-only) from **runtime state** (plain code model) per §11.2. Paths are relative to the project's script/data roots; a consuming pipeline may remap them to its layout without amending the TDD.
1. **`<ControllerName>`** (`MonoBehaviour`) — `Scripts/<area>/<Name>.cs`. Events: `<name (payload)>`. `[TO-FILL:eng]`
2. **`<ConfigName>`** (`ScriptableObject`, read-only config) — fields + suggested asset path `Data/<area>/<Name>.asset`. *(if any)*
3. **`<RuntimeModelName>`** (plain C#, serializable runtime state) — `Scripts/<area>/<Name>.cs`. *(if stateful)*

### Public API contract  `[TO-FILL:eng]`
- **Methods:** `<signature — params, return type, short description>`
- **Properties:** `<name : type (read-only?)>`
- **Events:** `<name : type signature, when fired>`

### Edge cases and fail states
- `<invalid input → behavior>`
- `<missing dependency / race → behavior>`
- `<destruction / scene unload → cleanup>`

### Implementation notes
- **Performance:** `<budgets / hot-path constraints — coherent with §11.6>`
- **Suggested tests:** EditMode `<TestName>`; PlayMode `<TestName>`

### Acceptance criteria (testable)  `[REQUIRED]` *(≥ 1, each tagged — G-04)*
- [ ] **AC1 (EditMode):** `<observable, testable outcome>`
- [ ] **AC2 (PlayMode):** `<observable, testable outcome — runs in a §13.2 test scene (G-12), never in a delivered level>`

### Open questions / assumptions
- `<anything ambiguous to resolve before implementation>` `[TO-FILL:eng]`

---

> `<… repeat one `## Mechanic:` block per mechanic …>`

---

# §B-S · Support Systems Registry  `[REQUIRED]`

> Non-mechanic infrastructure referenced by §B blocks or §D — event bus, save service, input dispatch, camera, scene flow, UI controllers, economy services. **Every dependency id that is not a §B mechanic must appear here** (gate G-06): no orphan references, ever. Engine built-ins consumed directly (e.g. an animation component) are declared here too, as `table-only` with their driver.

| Id (PascalCase) | Purpose | Public surface (summary) | Spec |
|---|---|---|---|
| `<EventBus>` | `<pub/sub messaging between modules>` | `<Publish<T>, Subscribe<T>>` | `table-only` |
| `<SaveService>` | `<snapshot persistence per §11.4>` | `<Save(), Load()>` | `<specs/systems/save_service.yaml>` |

- **Spec column:** `table-only` for trivial infrastructure; a spec path when the system deserves a full spec (recommended for anything stateful or with non-trivial rules).
- An entry promoted to a full mechanic moves to §B (with its §C spec) via the §0.3 amendment workflow.

---

# §C · Companion Specs (YAML)  `[REQUIRED]`

> **One production-spec YAML per §B mechanic** (gate G-05), emitted by whoever produces the TDD — person, team, or tool — as part of the document. A consuming pipeline may adopt them directly or regenerate from §B and use these as cross-check. Save target by `type`: `specs/features/<snake_case_name>.yaml` or `specs/systems/<snake_case_name>.yaml`.

```yaml
specVersion: "1.1"
name: <MechanicName>
type: <feature | system | mechanic>
description: <what it does and why>
version: <semver — matches §B metadata>
dependencies:
  - { kind: <feature|system|mechanic>, id: <PascalCaseId> }   # ids resolve per G-06
preconditions: []
components:
  - name: <ComponentName>
    type: <MonoBehaviour | ScriptableObject | enum>
    files: [{ path: Scripts/<area>/<ComponentName>.cs }]
publicAPI:
  methods: []
  properties: []
  events: []
acceptanceCriteria:
  - { id: AC-001, description: "<testable outcome>", verification: <EditMode | PlayMode> }
validationGates:
  specStructural: required
  compileUnity: required
  standardsValidation: required
  codeReviewer: required
  acceptanceCriteria: all_must_pass
specId: <snake_case_name>
touches: { scripts: [], prefabs: [], scriptable_objects: [], scenes: [], tests: [] }
```

> One YAML block per §B mechanic. A block not yet emitted carries `[PENDING owner resolve-by]` and a §14.2 row.

---

# §D · Cross-mechanic dependency graph  `[REQUIRED]`

```mermaid
graph TD
  <MechanicA> -->|<EventName>| <SupportSystem>
  <SupportSystem> -->|<EventName>| <MechanicB>
  <MechanicB> -->|<call/read>| <MechanicA>
```

**Rules** *(gates G-06, G-18)*

1. Node ids must match a §B mechanic `name` or a §B-S id exactly — the graph is unambiguous and orphan-free.
2. Mark the **critical path** — the minimal chain that produces observable gameplay.
3. Bidirectional relationships are drawn as two directed edges with their event/read labels.
4. The pub/sub graph is **closed**: every published event edge reaches ≥ 1 consumer and every subscription has a publisher (gate G-18).

---

## Standard changelog

| Standard | Change |
|---|---|
| `2.1.0` | Scope boundary (§0.0 rule 5, gate **G-19**): no level content, execution scope, storefront data or asset production detail. §6 → *World Structure & Level Rules* (rules/bounds table + level contract: marker/actor types or level-data fields, never positions). G-14: Look required only when the player controls the camera. G-16: world owner owns the play space at runtime (binds, initializes, transitions). PlayMode ACs run in test scenes. Component paths relative to script/data roots. One owner per datum (§11.2). |
| `2.0.0` | Gate G-01…G-18, §B/§B-S/§C/§D production definition. |

_Blank template — TDD Standard 2.1.0, tool-agnostic and self-contained. Replace every `{{placeholder}}` / `<angle-bracket>` slot, resolve every marker (§0.0), and verify the §0.2 Completeness Gate. Narrative or extra content lives in the GDD via `narrativeRef:`. The document stays authoritative through §0.3 change management._
