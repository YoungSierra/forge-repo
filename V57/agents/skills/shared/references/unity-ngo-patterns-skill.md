# Unity NGO Patterns Reference

Shared patterns for **Netcode for GameObjects (NGO)** in V57 consumer projects. Loaded on demand from `/network-setup`, `/spec implement` (when spec has `networking:`), or networking-related specs.

| Item | Detail |
|------|--------|
| **Primary guide** | `V57/docs/networking/NETWORKING_NGO.md` |
| **Vocabulary** | `V57/docs/networking/NETWORKING_VOCABULARY.md` |
| **Not a slash command** | Referenced by skills; do not invoke directly |

---

## Authority model

| Data / action | Default authority | NGO mechanism |
|---------------|-------------------|---------------|
| Player transform (gameplay) | Server (host counts as server) | `NetworkTransform` or server-validated position |
| Player input | Owner client only | Read input in `Update`; apply on server via `ServerRpc` or server-side sim |
| Health, score, inventory | Server | `NetworkVariable<T>` write on server only |
| Cosmetic / local FX | Owner client | Local-only components, no sync |
| UI local to player | Owner client | `IsOwner` guard |

**Rule:** Never write authoritative `NetworkVariable` values from non-server clients unless using server-validated RPCs.

---

## NetworkBehaviour conventions

- One primary `NetworkBehaviour` per networked prefab root (player, pickup, door).
- Class names: `NetworkPlayerController`, `NetworkSessionManager` — prefix `Network` when the type is NGO-specific.
- Use `[RequireComponent(typeof(NetworkObject))]` on behaviours that must be networked.
- Spawn only via `NetworkManager.Spawn` / registered prefabs — no `Instantiate` for authoritative gameplay objects.

---

## NetworkVariable

```csharp
private NetworkVariable<int> _health = new NetworkVariable<int>(
    100,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
```

- Prefer primitive or `INetworkSerializable` structs for sync.
- Subscribe to `OnValueChanged` for UI feedback on all clients.
- Do not use `NetworkVariable` for large blobs; use targeted RPCs or separate SO config (not synced).

---

## RPC naming

| Attribute | Direction | Use |
|-----------|-----------|-----|
| `[ServerRpc(RequireOwnership = true)]` | Client → server | Owner requests action (jump, fire, interact) |
| `[ServerRpc(RequireOwnership = false)]` | Client → server | Session UI (join request) — validate caller on server |
| `[ClientRpc]` | Server → all clients | Broadcast FX, state that is not a NetworkVariable |

Method names: verb + `ServerRpc` / `ClientRpc` suffix optional but be consistent project-wide.

---

## Input (owner-only)

```csharp
public override void OnNetworkSpawn()
{
    if (!IsOwner) return;
    // enable local input / camera
}
```

Non-owner clients disable input and often disable local camera on remote players.

---

## Session tiers (summary)

| Tier | Start flow | See |
|------|------------|-----|
| `lan` | Host calls `NetworkManager.StartHost()`; clients `StartClient()` with IP:port | `NETWORKING_NGO_LAN.md` |
| `relay` | Lobby create/join → Relay allocation → NGO start with relay data | `NETWORKING_NGO_RELAY.md` |
| `dedicated` | Server build `StartServer()`; clients connect to server address | `NETWORKING_NGO_DEDICATED.md` |

---

## Disconnect and host leave

| Event | LAN / Relay (listen host) | Dedicated |
|-------|---------------------------|-----------|
| Client disconnect | Server despawns player object; optional reconnect if TDD requires | Same |
| Host leaves (listen host) | Session ends unless host migration specified in TDD | N/A |
| Late join | Only if TDD + NGO scene management allow it | Same |

Document chosen behavior in `Assets/MCP/Context/NetworkingGuidelines.md`.

---

## Testing (Editor)

- **MPPM:** use Editor MPPM UI or `Unity.RunCommand` for multi-player smoke tests (legacy docs cited `unity_mppm_*`).
- **ParrelSync:** open clone Editor instances manually (no official `unity_select_instance` tool).
- PlayMode tests: mock session layer where possible; full NGO tests often need MPPM (mark `playMode: required_when_behavior_is_runtime` in network session spec).

---

## Anti-patterns

- `GameObject.Find` / `Camera.main` for networked refs (same as V57 standards).
- Syncing every frame via RPC instead of `NetworkVariable` or `NetworkTransform`.
- Public mutable fields on `NetworkBehaviour` — use `NetworkVariable` or properties.
- Client-authoritative gameplay state without explicit TDD approval.

---

*Last updated: 2026-07-02*
