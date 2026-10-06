# NGO — Dedicated server tier (extension)

**Extension** for games that run a **dedicated server** build separate from clients. Does not block LAN or Relay implementation.

**Parent:** [NETWORKING_NGO.md](NETWORKING_NGO.md)  
**Vocabulary:** `multiplayer_model: ngo_dedicated_server`, `networking.tier: dedicated`, `topology: dedicated_server`

---

## When to use

- Competitive or persistent worlds where host migration is unacceptable
- Cloud-hosted Linux servers
- Anti-cheat / authoritative simulation on dedicated hardware

For **private casual host** games, prefer **lan** or **relay** tiers first.

---

## Topology

- **Server build:** runs `NetworkManager.StartServer()` only — no local player unless TDD allows listen-server hybrid.
- **Client build:** `StartClient()` to server public IP:port or matchmaker-provided address.
- No host-as-client unless explicitly designed.

---

## Packages

Base NGO + Transport. Optional Linux server module for headless builds.

See [NETWORKING_PACKAGES.md](NETWORKING_PACKAGES.md).

---

## Build configuration

| Target | Notes |
|--------|-------|
| `Dedicated Server` (Unity 6+) or headless standalone | Enable in Player Settings / build profile |
| Scripting defines | e.g. `UNITY_SERVER` if project uses conditional compilation |
| Scenes in build | Server bootstrap scene only on server build; menu + game on client |

Use `/build-setup` for client build; document separate server build command in `network-setup-report` and PLATFORM_MATRIX footnote.

---

## Session manager (server)

| Responsibility | Owner |
|----------------|-------|
| Start listening | Server build only |
| Spawn players on connect | Server |
| Game rules / win conditions | Server |
| Disconnect cleanup | Server |

Clients must not call gameplay `ServerRpc` without ownership rules defined in TDD.

---

## Relay vs direct

- **Direct:** Clients connect to known server IP (datacenter, LAN).
- **Relay:** Optional for clients behind NAT connecting to dedicated server — same UGS Relay packages as relay tier but allocation owned by server process.

Document chosen path in NetworkingGuidelines.

---

## Limitations (V57 pack)

- `/network-setup --tier dedicated` documents layout and verifies NGO server compile — **does not** automate cloud deploy, Docker, or matchmaking.
- Dedicated server PlayMode tests often require multi-process or custom test harness — mark optional in spec validationGates.

---

## MCP verify checklist

- [ ] Server scene with NetworkManager configured for server-only
- [ ] Client project connects to server address (manual or automated test documented)
- [ ] Player spawn on server only
- [ ] Build report or notes for server target platform

---

## Migration from listen host

1. Extract authoritative logic already on host in LAN/Relay prototype.
2. Remove host UI paths from server build via `#if !UNITY_SERVER`.
3. Run `/network-setup verify --tier dedicated`.

---

*Last updated: 2026-07-02*
