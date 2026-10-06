# URP Render Graph validation (V57)

Review Unity 6+ **URP `ScriptableRendererFeature`** + pass code for Render Graph correctness. Adapted from [unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) `validate-urp-render-graph-renderer-feature`. Invoked from `/shader-setup` or `/render-setup` when custom features exist — not a standalone craft.

## When

- Custom `ScriptableRendererFeature` / Render Graph pass in the project
- AI-generated blit/copy/fullscreen effects before ship
- Suspected magenta/black screen after adding a feature

## Output structure

1. Validation Summary  
2. Confirmed Issues  
3. Likely Issues / Risky Assumptions  
4. Recommended Fixes  
5. Corrected Snippets (minimal)  
6. Missing Information  

## Checklist (condensed)

| Area | Fail if |
|------|---------|
| Material binding | Main input texture not bound; property names wrong; null material |
| Resource wiring | Read/write roles confused; missing `UseTexture` / attachments |
| Static render fn | Non-static render function; stale/`PassData` not fully assigned each record |
| Descriptors | Fresh `TextureDesc` instead of graph-derived descriptor when matching RT |
| Copy/blit | Custom raster pass when `AddBlitPass` / appropriate helper suffices |
| Globals | Unnecessary `SetGlobalTextureAfterPass` / cmd globals |
| `ConfigureInput` | Missing or over-declared pipeline inputs |

**Preferred descriptor pattern:** copy from `resourceData.activeColorTexture.GetDescriptor(renderGraph)`, mutate only needed fields, then `CreateTexture`.

## Non-goals

No full rewrite, no unrelated gameplay review, no guaranteed runtime PASS without Play verify.

*Upstream: Unity-Technologies/unity-agent-plugin — Companion License.*
