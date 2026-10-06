# Networking vocabulary (V57)

Canonical values for TDD §A, `CONTEXT.md`, and spec YAML. Agents must use these enums — no ad-hoc strings.

---

## `multiplayer_model` (TDD §A)

| Value | Meaning | Default tier |
|-------|---------|--------------|
| `N/A` | Single-player only | — |
| `ngo_listen_host_lan` | Host + clients on LAN / direct IP | `lan` |
| `ngo_listen_host_relay` | Private host over internet (UGS Lobby + Relay) | `relay` |
| `ngo_dedicated_server` | Headless or server build; clients only | `dedicated` |

Legacy free-text in §11 **Multiplayer** row should normalize to one of the above during `/tdd-to-context`.

---

## `networking_tier` (TDD §A optional override)

| Value | Maps to |
|-------|---------|
| `lan` | `ngo_listen_host_lan` |
| `relay` | `ngo_listen_host_relay` |
| `dedicated` | `ngo_dedicated_server` |

If both `multiplayer_model` and `networking_tier` are set, they must agree; otherwise prefer `multiplayer_model`.

---

## `session_visibility`

| Value | Use |
|-------|-----|
| `lan_only` | No internet traversal; IP:port on local network |
| `private` | Invite-only (join code / lobby id); tier `relay` or LAN with hidden IP |
| `public` | Open matchmaking (out of scope for default V57 templates; document in TDD if needed) |

---

## `topology` (CONTEXT / NetworkingGuidelines)

| Value | NGO start mode |
|-------|----------------|
| `listen_host` | `StartHost()` / `StartClient()` |
| `dedicated_server` | Server build `StartServer()`; clients `StartClient()` |

---

## `framework`

| Value | Package |
|-------|---------|
| `netcode_for_gameobjects` | `com.unity.netcode.gameobjects` |

Other frameworks (Mirror, Photon) are out of V57 NGO scope unless TDD explicitly overrides and updates NetworkingGuidelines.

---

## Mapping: TDD §A → CONTEXT.md

| TDD §A | CONTEXT `networking` |
|--------|---------------------|
| `multiplayer_model: N/A` | omit block or `enabled: false` |
| `multiplayer_model: ngo_listen_host_lan` | `enabled: true`, `tier: lan`, `topology: listen_host` |
| `multiplayer_model: ngo_listen_host_relay` | `enabled: true`, `tier: relay`, `topology: listen_host` |
| `multiplayer_model: ngo_dedicated_server` | `enabled: true`, `tier: dedicated`, `topology: dedicated_server` |
| `max_players` | `maxPlayers` |
| `session_visibility` | `sessionVisibility` |

---

## Mapping: §B mechanic → spec `networking.sync`

| §B Authority | §B Multiplayer / determinism | spec sync authority |
|--------------|------------------------------|---------------------|
| `server` | `host-authoritative` | `server` |
| `server` | `NetworkVariable` | `server` |
| `client` (owner) | `owner-only input` | `owner` (input only; state still server unless TDD says otherwise) |
| `N/A` | omit `networking:` block on feature spec |

---

## System module ids

| Module | Type | When |
|--------|------|------|
| `NetworkSession` | System | Any `multiplayer_model != N/A` |
| `NetworkPlayerSpawner` | System | Optional; split from session if TDD separates spawn rules |

Spec template: `V57/specs/template/network_session_spec_template.yaml`.

---

*Last updated: 2026-07-02*
