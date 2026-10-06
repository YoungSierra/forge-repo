# Unity Networking Skill (`/network-setup`)

Configure **Netcode for GameObjects (NGO)** session bootstrap, packages, network prefabs, and MCP context via Unity MCP with OVR verification. **On-demand only** — not part of `/game-setup` stages.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | `true` |
| **on_unavailable** | `stop` |
| **OVR helper** | `unity-editor-verification-skill.md` |
| **NGO patterns** | `unity-ngo-patterns-skill.md` |
| **Primary guide** | `V57/docs/networking/NETWORKING_NGO.md` |
| **Confirm-gated** | MCP context file writes if files already exist |

---

## When to use (triggers)

Run `/network-setup` when **any** of:

- TDD §A `multiplayer_model` is not `N/A`
- `CONTEXT.md` has `networking.enabled: true`
- Active spec YAML has a `networking:` block or depends on `NetworkSession` / `NetworkSessionManager`
- User explicitly invokes `/network-setup`

**Abort early:** if none apply and user did not force `--tier`, inform that networking is not required and stop.

---

## Invocation

```bash
/network-setup
/network-setup --tier lan
/network-setup --tier relay
/network-setup --tier dedicated
/network-setup --spec @V57/specs/<slug>/systems/network_session.yaml
/network-setup verify
/network-setup --dry-run
/network-setup show
```

| Flag | Behavior |
|------|----------|
| `--tier lan\|relay\|dedicated` | Override inferred tier from TDD/CONTEXT |
| `--spec <path>` | Read tier + ACs from system spec |
| `--dry-run` | Propose plan only; no MCP writes |
| `verify` | Re-run OVR against existing setup |
| `show` | Print current `CONTEXT.md` networking block + installed packages |

---

## Tier reference

| Tier | Doc | Packages (minimum) |
|------|-----|-------------------|
| `lan` | `NETWORKING_NGO_LAN.md` | NGO + Transport + MPPM (dev) |
| `relay` | `NETWORKING_NGO_RELAY.md` | Above + UGS Multiplayer / Lobby + Relay |
| `dedicated` | `NETWORKING_NGO_DEDICATED.md` | NGO + Transport; server build notes |

Full package list: `V57/docs/networking/NETWORKING_PACKAGES.md`.

---

## Workflow (Operate → Verify → Report)

### 1. Pre-flight

- Unity MCP pre-flight per `V57/docs/mcp/UNITY_MCP_REQUIREMENT.md`
- Read `CONTEXT.md`, TDD §A (if path known), optional `--spec`
- Infer `tier` from `networking.tier` or `multiplayer_model` (see `NETWORKING_VOCABULARY.md`)

### 2. Packages (MCP)

Use `Unity.ManageEditor / Package Manager via Unity.RunCommand` per tier (`NETWORKING_PACKAGES.md`). After add:

- `Unity.ReadConsole (Errors)` — STOP on blocking errors
- Record installed versions in report

### 3. Folder layout

```
Assets/
├── Prefabs/Network/
├── Scripts/Systems/Network/
├── Scenes/                    # bootstrap scene if TDD requires
└── MCP/Context/               # NetworkingGuidelines.md, NetworkingCSP.md
```

### 4. Scene bootstrap (MCP)

Per `V57/docs/standards/SCENE_PRODUCTION_STANDARDS.md`:

- Root container: `--- NETWORK ---`
- Child: `NetworkManager` with `UnityTransport` (LAN/Relay) or transport config per tier doc
- Register default player prefab in Network Prefabs List
- Wire references via `Unity.ManageGameObject` or `Unity.RunCommand`

Relay tier: session manager prefab or scene object per `NETWORKING_NGO_RELAY.md` (Lobby + Relay allocation before `StartHost` / `StartClient`).

Dedicated tier: document server scene + build target; do not block LAN/Relay if dedicated is future scope.

### 5. MCP context files

Copy from pack templates (confirm if destination exists):

- `V57/docs/networking/NetworkingGuidelines.template.md` → `Assets/MCP/Context/NetworkingGuidelines.md`
- `V57/docs/networking/NetworkingCSP.template.md` → `Assets/MCP/Context/NetworkingCSP.md`

Fill placeholders from TDD/CONTEXT tier and visibility.

### 6. CONTEXT.md update (autonomous)

Add the optional `networking:` YAML block per `V57/docs/context/CONTEXT_TEMPLATE.md` — summary shown, then written in the same turn (`agents.yaml` `writes` policy).

### 7. Verify (OVR)

| Check | Tool / method |
|-------|---------------|
| Compile clean | `Unity.ReadConsole (Errors)` |
| NetworkManager present | `Unity.ManageGameObject` / hierarchy read-back |
| Default player prefab registered | `Unity.RunCommand` inspect NetworkManager prefab list |
| Scene container | `--- NETWORK ---` under expected hierarchy parent |
| MPPM smoke (if MPPM installed) | MPPM UI or `Unity.RunCommand` (no guaranteed official `unity_mppm_*` tools) |

### 8. Report

Save `Docs/V57/reports/network-setup-report-{slug}.md`:

```markdown
## Network setup report
- **Tier:** lan | relay | dedicated
- **Overall:** PASS | FAIL
- **Packages:** ...
- **Scene:** ...
- **MCP context:** PASS | FAIL | SKIPPED
- **MPPM smoke:** PASS | FAIL | SKIPPED
- **Blockers:** ...
```

---

## Spec block (`networking:`)

When spec defines `networking:` (see `feature_spec_template.yaml` or `network_session_spec_template.yaml`):

- Honor `networkPrefabs`, `sync`, and `verification` invariants during setup
- Session system spec (`network_session.yaml`) is the source of truth for join flow ACs

---

## Related commands

| Command | When |
|---------|------|
| `/spec implementa @.../network_session.yaml` | After or in parallel with session layer bootstrap |
| `/scene-setup` | Validates `--- NETWORK ---` when `CONTEXT.networking.enabled` |
| `/build-setup` | Dedicated server build (tier `dedicated`) |

---

## Apply this skill

1. User or upstream skill triggers `/network-setup` (or conditional hook from `/spec`).
2. Read `NETWORKING_NGO.md` + tier doc + `unity-ngo-patterns-skill.md`.
3. Pre-flight MCP; abort if single-player project unless user forces tier.
4. Apply plan — CONTEXT write is autonomous (summary shown); MCP context file writes stay confirm-gated only when overwriting existing files.
5. Operate via MCP; verify; write report.
6. Suggest implementing `network_session` spec if not done.

---

*Last updated: 2026-07-02*
