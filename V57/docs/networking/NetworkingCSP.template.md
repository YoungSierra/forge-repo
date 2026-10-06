# Networking Content Security Policy

<!-- Deployed from V57/docs/networking/NetworkingCSP.template.md via /network-setup -->
<!-- Required for tier relay; recommended for dedicated server with public endpoints -->

## Allowed Endpoints

| Service | Endpoint / region | Purpose |
|---------|-------------------|---------|
| Unity Authentication | `https://*.unity.com` (UGS) | Player sign-in |
| Unity Lobby | UGS Lobby API | Private session discovery |
| Unity Relay | UGS Relay API | NAT traversal |
| {{custom_endpoint}} | {{url}} | {{purpose}} |

## Authentication

- **Method:** {{auth_method}} <!-- anonymous | platform | custom -->
- **Tokens:** Stored in memory only; never log raw tokens
- **Guest policy:** {{guest_policy}}

## Data Validation

- All gameplay-changing RPCs validated on server
- Connection approval: {{connection_approval}} <!-- open | lobby_members_only | invite_list -->
- Reject connections when session full or not in lobby (relay tier)

## Anti-Cheat Considerations

- Server authoritative state for {{critical_systems}}
- Client reports treated as requests, not facts
- {{additional_notes}}

## Rate Limiting

- Join attempts per client: {{join_rate_limit}}
- RPC spam: throttle ServerRpc handlers server-side

## Privacy

- No PII in lobby metadata unless TDD requires
- Join codes rotate per session

## References

- V57/docs/networking/NETWORKING_NGO_RELAY.md
- Unity Gaming Services documentation
