# Unity Editor Search Skill (`/editor-search`)

Translate natural-language **find asset / scene object** requests into Unity Search queries and optionally open the Search window via CLI `eval` or MCP. Adapted from [unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) `generate-editor-search-query`.

| Item | Detail |
|------|--------|
| **requires_unity_mcp** | Optional — open window via MCP/`eval` |
| **read_only** | `true` — never modify results |
| **on_unavailable** | Return query text for manual paste |

---

## Invocation

```bash
/editor-search materials named Almond
/editor-search "what references this prefab" --asset Assets/_Game/Prefabs/Player.prefab
/editor-search t:AudioClip --query-only
/editor-search Rigidbody in scene
```

---

## Workflow

1. Classify: project assets vs scene objects vs both.
2. Build one concise query (`t:`, `dir:`, `l:`, `ref=`, expressions when useful).
3. Show query to the user.
4. Unless `--query-only` / “don’t open”: open Search via CLI:

```csharp
// eval — fully qualified, no using
var providers = new System.Collections.Generic.List<string> { "asset", "scene" };
UnityEditor.Search.SearchService.ShowWindow(
    UnityEditor.Search.SearchService.CreateContext(providers.ToArray(), "QUERY_HERE"));
```

5. If open fails → paste query into Unity Search manually.

## Query tips

| Intent | Example |
|--------|---------|
| Type | `t:material`, `t:prefab`, `t:AudioClip`, `t:Rigidbody` |
| Folder | `dir:Assets/Prototypes` |
| References | `t:prefab ref={t:material}` |
| Label | `l:Hero` |

Do **not** use for repo `rg`/git search, package installs, or “how do I code X”.

---

## Integration

On-demand helper for agents during OVR / asset audits. Not a `/game-setup` craft stage.

---

*Last updated: 2026-09-10*
