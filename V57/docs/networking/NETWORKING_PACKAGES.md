# NGO packages reference (V57)

Unity Package Manager ids for `/network-setup` by tier. Pin versions in consumer `Packages/manifest.json` after MCP install; record resolved versions in `network-setup-report-*.md`.

**Unity baseline:** 2022.3 LTS+ (NGO 1.x) or Unity 6+ (verify NGO 2.x compatibility in project).

---

## All multiplayer tiers (base)

| Package | Purpose | Required |
|---------|---------|----------|
| `com.unity.netcode.gameobjects` | NGO core | Yes |
| `com.unity.transport` | Unity Transport (UTP) | Yes (often pulled by NGO) |
| `com.unity.multiplayer.playmode` | MPPM — Editor multi-client testing | Dev only (strongly recommended) |

---

## Tier: `lan`

Base packages only. No Unity Gaming Services required.

**Transport:** `UnityTransport` on `NetworkManager` — set address/port for clients.

---

## Tier: `relay`

| Package | Purpose |
|---------|---------|
| `com.unity.services.multiplayer` | UGS Multiplayer (Lobby + Relay integration) |
| `com.unity.services.authentication` | Anonymous or platform sign-in for UGS |

**Notes:**

- Configure UGS project id in Unity Dashboard; link project in **Edit → Project Settings → Services**.
- Relay replaces direct UDP hole-punching for internet private sessions.
- Document join flow in `NetworkingGuidelines.md` and `network_session` spec.

If `com.unity.services.multiplayer` is unavailable on your Unity version, use the split packages documented in Unity docs for that editor version and update this file in the consumer project CONTEXT Technical Notes.

---

## Tier: `dedicated`

| Package | Purpose |
|---------|---------|
| Base NGO + Transport | Same as LAN |
| Platform module (optional) | Linux headless for cloud servers |

**Build:**

- Scripting define / `Dedicated Server` build target per Unity version (`NETWORKING_NGO_DEDICATED.md`).
- No Relay required on server if clients connect to public server IP.

---

## Install via MCP

```
Unity.ManageEditor / Package Manager via Unity.RunCommand → com.unity.netcode.gameobjects
Unity.ManageEditor / Package Manager via Unity.RunCommand → com.unity.multiplayer.playmode   # dev
# tier relay:
Unity.ManageEditor / Package Manager via Unity.RunCommand → com.unity.services.multiplayer
Unity.ManageEditor / Package Manager via Unity.RunCommand → com.unity.services.authentication
```

After install: wait for compile, run `Unity.ReadConsole (Errors)`.

---

## Version policy

- V57 pack does **not** pin exact semver — consumer project locks after first successful `/network-setup`.
- On upgrade, re-run `/network-setup verify` and MPPM smoke.

---

*Last updated: 2026-07-02*
