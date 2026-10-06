# NGO — LAN tier (listen host)

Direct **Listen Host** multiplayer on LAN or same network. No Unity Gaming Services.

**Parent:** [NETWORKING_NGO.md](NETWORKING_NGO.md)  
**Vocabulary:** `multiplayer_model: ngo_listen_host_lan`, `networking.tier: lan`

---

## Topology

- One player runs **host** (`NetworkManager.StartHost()`) — server + local client.
- Other players run **client** (`NetworkManager.StartClient()`) with host **IP + port**.
- Max players: from TDD `max_players` / CONTEXT `networking.maxPlayers`.

---

## Packages

See [NETWORKING_PACKAGES.md](NETWORKING_PACKAGES.md) — base NGO + Transport + MPPM (dev).

---

## NetworkManager setup

| Component | Setting |
|-----------|---------|
| `NetworkManager` | Scene singleton under `--- NETWORK ---` |
| `UnityTransport` | Connection address `0.0.0.0` on host; clients set `ConnectionData.Address` to host LAN IP |
| Default Network Prefabs | Register `NetworkPlayer.prefab` |
| Player prefab | Assigned on NetworkManager or via connection approval callback |

**Port:** Use a fixed port (e.g. `7777`) documented in NetworkingGuidelines.

---

## Session manager responsibilities

`NetworkSessionManager` (system spec):

| State | Description |
|-------|-------------|
| Offline | No network started |
| Hosting | Host running, accepting clients |
| Client | Connected to remote host |
| ShuttingDown | Clean disconnect |

**API sketch:**

- `StartHostAsync()` / `StartClientAsync(string address, ushort port)`
- `Shutdown()`
- Events: `OnClientConnected`, `OnClientDisconnected`, `OnSessionEnded`

---

## Player spawn

- On server: `NetworkManager.OnClientConnectedCallback` → spawn player prefab at spawn point.
- Use `NetworkObject.SpawnAsPlayerObject(clientId)`.
- Despawn on disconnect.

---

## UI flow (minimal)

1. **Host game** → start host → show LAN IP for friends.
2. **Join game** → IP field + port → start client.
3. **Leave** → shutdown network → return to menu.

---

## Limitations

- No NAT traversal — internet play without Relay usually fails.
- Host leave ends session for all clients (unless TDD specifies migration — not default).
- Firewalls on host must allow UDP on chosen port.

---

## MCP verify checklist

- [ ] `UnityTransport` on NetworkManager
- [ ] Default player prefab in Network Prefabs list
- [ ] MPPM: 1 host + 1 virtual client spawn (smoke)
- [ ] Compile clean

---

## Promotion to relay tier

When TDD requires **private internet** sessions, run `/network-setup --tier relay` and implement [NETWORKING_NGO_RELAY.md](NETWORKING_NGO_RELAY.md).

---

*Last updated: 2026-07-02*
