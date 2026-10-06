# UPM package selection (V57)

Adapted from Unity Agent Plugin `unity-package-management/references/select-packages.md`.

**Principle:** install what the concept needs, not everything. Prefer packages the template already includes.

## Discover

```bash
# Known id metadata (no Editor)
curl -fsSL https://packages.unity.com/com.unity.ai.navigation
```

In-Editor: `Client.Search("<id>")` / `Client.SearchAll()` via MCP/`eval` (async poll).

## Foundation

| Need | Package |
|------|---------|
| Input System | `com.unity.inputsystem` |
| Cinemachine | `com.unity.cinemachine` |
| Tests | `com.unity.test-framework` |
| Addressables | `com.unity.addressables` (large/streamed content) |
| AI Navigation | `com.unity.ai.navigation` |
| Localization | `com.unity.localization` |
| Timeline | `com.unity.timeline` |
| URP | `com.unity.render-pipelines.universal` |
| NGO | `com.unity.netcode.gameobjects` |

## By dimension

| Look | Packages |
|------|----------|
| 2D | `com.unity.2d.feature` (+ pixel-perfect as needed) |
| 3D pathfinding | `com.unity.ai.navigation` |

## V57 defaults

GameForge consumer projects: URP + Input System + Test Framework + UI Toolkit (Editor-bundled). Add Nav / Localization / Cinemachine only when TDD asks.

*Upstream: Unity-Technologies/unity-agent-plugin — Companion License.*
