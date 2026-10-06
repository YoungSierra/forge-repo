# Networking Guidelines

<!-- Deployed from V57/docs/networking/NetworkingGuidelines.template.md via /network-setup -->
<!-- Replace placeholders; keep in sync with CONTEXT.md networking block -->

## Network Architecture

- **Framework:** netcode_for_gameobjects
- **Tier:** {{tier}} <!-- lan | relay | dedicated -->
- **Topology:** {{topology}} <!-- listen_host | dedicated_server -->
- **Type:** Client-Server
- **Transport:** Unity Transport (UTP)
- **Max players:** {{max_players}}
- **Session visibility:** {{session_visibility}} <!-- lan_only | private | public -->

## Connection flow

<!-- Tier-specific steps; see V57/docs/networking/NETWORKING_NGO_*.md -->

### Host

1. {{host_step_1}}
2. {{host_step_2}}
3. StartHost / StartServer per topology

### Client

1. {{client_step_1}}
2. StartClient with address or relay data

## Synchronization Strategy

| System | Mechanism | Authority |
|--------|-----------|-----------|
| Player movement | NetworkTransform | Server |
| Player health | NetworkVariable | Server |
| Session state | NetworkSessionManager events | Server |
| Local VFX | Client-only | Owner |

<!-- Add rows per mechanic spec networking.sync -->

## Authority Model

- **Default:** Server-authoritative gameplay; host is server in listen_host topology.
- **Input:** Owner client reads input; actions validated via ServerRpc on server.
- **UI:** Owner-only for local HUD; session UI may run on all clients.

## Disconnect and host leave

- **Client disconnect:** Despawn NetworkObject; notify UI.
- **Host leave (listen_host):** End session for all clients; return to main menu.
- **Late join:** {{late_join_policy}} <!-- allowed | not allowed -->
- **Reconnect:** {{reconnect_policy}} <!-- N/A | optional per TDD -->

## Bandwidth Considerations

- Target tick / sync rate: {{sync_rate_hz}} Hz (default 20–30 for casual games)
- Prefer NetworkVariable delta over per-frame RPCs
- Compress large state via infrequent sync or server-side aggregation

## Scene and prefabs

- Network bootstrap container: `--- NETWORK ---`
- Default player prefab: `Assets/_Game/Prefabs/Network/NetworkPlayer.prefab`
- Network Prefabs List: maintained on NetworkManager

## References

- V57/docs/networking/NETWORKING_NGO.md
- CONTEXT.md `networking` block
